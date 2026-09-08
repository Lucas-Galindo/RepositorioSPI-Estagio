using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPI.Application.Common;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;

namespace SPI.Infrastructure.BackgroundServices
{
    // Estoria 19: verifica periodicamente os lembretes "Pendente" cuja Hora
    // Programada ja chegou e dispara o envio. Nesta fase, so o canal
    // "Email" e efetivamente enviado (ver prompt mestre) -- WhatsApp/SMS
    // ficam registrados como Pendente ate um provedor ser integrado.
    public class LembreteDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LembreteDispatcherService> _logger;
        private readonly TimeSpan _intervalo;

        public LembreteDispatcherService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<LembreteDispatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var segundos = configuration.GetValue<int?>("Lembretes:IntervaloVerificacaoSegundos") ?? 60;
            _intervalo = TimeSpan.FromSeconds(segundos);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_intervalo);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessarLembretesVencidosAsync(stoppingToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.LogError(e, "Falha ao processar lembretes pendentes.");
                }
            }
        }

        private async Task ProcessarLembretesVencidosAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var lembreteRepository = scope.ServiceProvider.GetRequiredService<ILembreteRepository>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var vencidos = await lembreteRepository.ListarPendentesVencidosAsync(DateTime.Now, cancellationToken);
            if (vencidos.Count == 0)
            {
                return;
            }

            foreach (var lembrete in vencidos)
            {
                if (lembrete.Canal != "Email")
                {
                    // Canal ainda nao suportado para envio real: permanece Pendente.
                    continue;
                }

                var destinatarios = ObterEmailsDestinatarios(lembrete);
                if (destinatarios.Count == 0)
                {
                    lembrete.Status = "Falha";
                    _logger.LogWarning("Lembrete {Id} sem destinatarios com e-mail cadastrado.", lembrete.Id);
                    continue;
                }

                var enviosComSucesso = 0;
                foreach (var email in destinatarios)
                {
                    try
                    {
                        await emailSender.EnviarAsync(
                            email,
                            $"SPI - Lembrete: aula da turma {lembrete.Turma.Nome}",
                            MontarCorpoEmail(lembrete),
                            cancellationToken);
                        enviosComSucesso++;
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Falha ao enviar lembrete {Id} para {Email}.", lembrete.Id, email);
                    }
                }

                lembrete.Status = enviosComSucesso > 0 ? "Enviado" : "Falha";
            }

            await lembreteRepository.SalvarAlteracoesAsync(cancellationToken);
            _logger.LogInformation("Processados {Quantidade} lembretes vencidos.", vencidos.Count);
        }

        private static List<string> ObterEmailsDestinatarios(Lembrete lembrete)
        {
            var emails = new List<string>();
            var incluiAlunos = lembrete.Destinatarios is "Alunos" or "AlunosEResponsaveis";
            var incluiResponsaveis = lembrete.Destinatarios is "Responsaveis" or "AlunosEResponsaveis";

            foreach (var vinculo in lembrete.Turma.AlunosTurma)
            {
                if (!vinculo.Aluno.Ativo)
                {
                    continue;
                }

                if (incluiAlunos && !string.IsNullOrWhiteSpace(vinculo.Aluno.Email))
                {
                    emails.Add(vinculo.Aluno.Email);
                }

                if (incluiResponsaveis && !string.IsNullOrWhiteSpace(vinculo.Aluno.EmailResponsavel))
                {
                    emails.Add(vinculo.Aluno.EmailResponsavel);
                }
            }

            return emails.Distinct().ToList();
        }

        private static string MontarCorpoEmail(Lembrete lembrete) => $"""
            <p>Ol&aacute;!</p>
            <p>Este &eacute; um lembrete da turma <b>{lembrete.Turma.Nome}</b>: sua pr&oacute;xima aula est&aacute; se aproximando.</p>
            <p>Data/hora do lembrete: {lembrete.HoraProgramada:dd/MM/yyyy HH:mm}</p>
            """;
    }
}
