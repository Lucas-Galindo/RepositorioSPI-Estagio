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
    // Programada ja chegou e dispara o envio. WhatsApp e SMS foram
    // descontinuados como canal (ver LembreteRequestValidator); somente
    // "Email" e um canal valido.
    public class LembreteDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LembreteDispatcherService> _logger;
        private readonly TimeSpan _intervalo;
        private readonly int _maxEmailsPorCiclo;

        public LembreteDispatcherService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<LembreteDispatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var segundos = configuration.GetValue<int?>("Lembretes:IntervaloVerificacaoSegundos") ?? 60;
            _intervalo = TimeSpan.FromSeconds(segundos);

            // specs/049: orcamento de envios por ciclo, para espacar lembretes com
            // muitos destinatarios (ex.: turma inteira) em vez de mandar todos de
            // uma vez -- reduz o risco de o provedor SMTP marcar como rajada.
            _maxEmailsPorCiclo = configuration.GetValue<int?>("Lembretes:MaxEmailsPorCiclo") ?? 10;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_intervalo);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessarCicloAsync(stoppingToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.LogError(e, "Falha ao processar lembretes pendentes.");
                }
            }
        }

        private async Task ProcessarCicloAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var lembreteRepository = scope.ServiceProvider.GetRequiredService<ILembreteRepository>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            await ProcessarLembretesVencidosAsync(lembreteRepository, emailSender, _maxEmailsPorCiclo, _logger, DateTime.Now, cancellationToken);
        }

        // Logica testavel sem IServiceScopeFactory/BackgroundService (mesmo padrao
        // de MensalidadeDispatcherService.GerarCobrancasDoMesAsync, specs/039).
        // Processa os lembretes vencidos (ja ordenados por HoraProgramada, o mais
        // antigo primeiro - FR-004) ate o orcamento de e-mails deste ciclo
        // (maxEmailsPorCiclo) se esgotar; os destinatarios ja tentados em ciclos
        // anteriores (DestinatariosTentados) nunca sao reenviados, e o Status so
        // vira "Enviado"/"Falha" quando 100% dos destinatarios originais do
        // lembrete ja tiverem sido tentados (FR-003/FR-004; R2 de research.md).
        public static async Task ProcessarLembretesVencidosAsync(
            ILembreteRepository lembreteRepository,
            IEmailSender emailSender,
            int maxEmailsPorCiclo,
            ILogger? logger,
            DateTime agora,
            CancellationToken cancellationToken)
        {
            var vencidos = await lembreteRepository.ListarPendentesVencidosAsync(agora, cancellationToken);
            if (vencidos.Count == 0)
            {
                return;
            }

            var orcamentoRestante = maxEmailsPorCiclo;

            foreach (var lembrete in vencidos)
            {
                if (orcamentoRestante <= 0)
                {
                    // Orcamento do ciclo esgotado: os demais lembretes (e seus
                    // destinatarios) continuam "Pendente" para o proximo ciclo.
                    break;
                }

                if (lembrete.Canal != "Email")
                {
                    // Defesa contra dados legados: WhatsApp/SMS nao sao mais um canal valido.
                    continue;
                }

                var destinatariosOriginais = ObterEmailsDestinatarios(lembrete);
                if (destinatariosOriginais.Count == 0)
                {
                    lembrete.Status = "Falha";
                    logger?.LogWarning("Lembrete {Id} sem destinatarios com e-mail cadastrado.", lembrete.Id);
                    continue;
                }

                var jaTentados = (lembrete.DestinatariosTentados ?? string.Empty)
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
                var pendentes = destinatariosOriginais.Except(jaTentados).ToList();
                var destinatariosDesteCiclo = pendentes.Take(orcamentoRestante).ToList();

                var sucessosDesteCiclo = 0;
                foreach (var email in destinatariosDesteCiclo)
                {
                    try
                    {
                        await emailSender.EnviarAsync(
                            email,
                            $"SPI - Lembrete: aula da turma {lembrete.Turma.Nome}",
                            MontarCorpoEmail(lembrete),
                            cancellationToken);
                        sucessosDesteCiclo++;
                    }
                    catch (Exception e)
                    {
                        logger?.LogWarning(e, "Falha ao enviar lembrete {Id} para {Email}.", lembrete.Id, email);
                    }
                }

                jaTentados.AddRange(destinatariosDesteCiclo);
                lembrete.DestinatariosTentados = string.Join(";", jaTentados);
                lembrete.EnviosComSucesso += sucessosDesteCiclo;
                orcamentoRestante -= destinatariosDesteCiclo.Count;

                if (jaTentados.Count >= destinatariosOriginais.Count)
                {
                    lembrete.Status = lembrete.EnviosComSucesso > 0 ? "Enviado" : "Falha";
                }
            }

            await lembreteRepository.SalvarAlteracoesAsync(cancellationToken);
            logger?.LogInformation("Processados {Quantidade} lembretes vencidos neste ciclo.", vencidos.Count);
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
