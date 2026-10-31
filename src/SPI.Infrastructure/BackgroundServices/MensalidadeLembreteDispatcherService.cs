using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPI.Application.Common;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;

namespace SPI.Infrastructure.BackgroundServices
{
    // Card Trello "Lembrete por e-mail de vencimento da Mensalidade (5 dias
    // antes)": quando uma cobranca de mensalidade (gerada por
    // MensalidadeDispatcherService, specs/039) esta Pendente e sua
    // DataVencimento cai dentro de AntecedenciaDias, avisa aluno/responsavel
    // por e-mail. Mesmo padrao tecnico de LembreteDispatcherService/
    // MensalidadeDispatcherService: BackgroundService, PeriodicTimer,
    // IServiceScopeFactory por execucao, try/catch por tick.
    //
    // Idempotencia: fire-once via Pagamento.LembreteVencimentoEnviado -- uma
    // vez tentado (sucesso ou falha de envio), nunca mais reenviado para o
    // mesmo pagamento, mesmo que o dispatcher rode varias vezes no mesmo dia.
    public class MensalidadeLembreteDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MensalidadeLembreteDispatcherService> _logger;
        private readonly TimeSpan _intervalo;
        private readonly int _antecedenciaDias;

        public MensalidadeLembreteDispatcherService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<MensalidadeLembreteDispatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var segundos = configuration.GetValue<int?>("MensalidadeLembrete:IntervaloVerificacaoSegundos") ?? 3600;
            _intervalo = TimeSpan.FromSeconds(segundos);

            _antecedenciaDias = configuration.GetValue<int?>("MensalidadeLembrete:AntecedenciaDias") ?? 5;
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
                    _logger.LogError(e, "Falha ao processar lembretes de vencimento de mensalidade.");
                }
            }
        }

        private async Task ProcessarCicloAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var pagamentoRepository = scope.ServiceProvider.GetRequiredService<IPagamentoRepository>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            await ProcessarLembretesVencimentoAsync(
                pagamentoRepository, emailSender, _antecedenciaDias, _logger, DateOnly.FromDateTime(DateTime.Now), cancellationToken);
        }

        // Logica testavel sem IServiceScopeFactory/BackgroundService (mesmo padrao
        // de MensalidadeDispatcherService.GerarCobrancasDoMesAsync).
        // Avisa uma unica vez cada mensalidade pendente (a que nasce de um vinculo de cobranca) cujo
        // vencimento chega ate hoje + antecedencia -- "ate", nao "igual": uma que ja venceu sem nunca
        // ter recebido o aviso tambem entra. O aviso vai para o aluno e o responsavel.
        public static async Task ProcessarLembretesVencimentoAsync(
            IPagamentoRepository pagamentoRepository,
            IEmailSender emailSender,
            int antecedenciaDias,
            ILogger? logger,
            DateOnly hoje,
            CancellationToken cancellationToken)
        {
            var dataVencimentoAlvo = hoje.AddDays(antecedenciaDias);
            var pagamentos = await pagamentoRepository.ListarMensalidadesParaLembreteVencimentoAsync(dataVencimentoAlvo, cancellationToken);

            foreach (var pagamento in pagamentos)
            {
                try
                {
                    var emails = ObterEmailsDestinatarios(pagamento.Aluno);
                    if (emails.Count == 0)
                    {
                        logger?.LogWarning("Pagamento {Id} sem e-mail de aluno/responsavel cadastrado para lembrete de vencimento.", pagamento.Id);
                    }

                    foreach (var email in emails)
                    {
                        try
                        {
                            await emailSender.EnviarAsync(
                                email,
                                "SPI - Lembrete: mensalidade vence em breve",
                                MontarCorpoEmail(pagamento),
                                cancellationToken);
                        }
                        catch (Exception e)
                        {
                            logger?.LogWarning(e, "Falha ao enviar lembrete de vencimento do pagamento {Id} para {Email}.", pagamento.Id, email);
                        }
                    }
                }
                finally
                {
                    // Fire-once: marca como tentado mesmo sem e-mail cadastrado ou com
                    // falha de envio, para nao varrer e tentar reenviar indefinidamente.
                    pagamento.LembreteVencimentoEnviado = true;
                }
            }

            await pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
            logger?.LogInformation("Processados {Quantidade} lembretes de vencimento de mensalidade para {DataVencimento:dd/MM/yyyy}.", pagamentos.Count, dataVencimentoAlvo);
        }

        private static List<string> ObterEmailsDestinatarios(Aluno aluno)
        {
            var emails = new List<string>();

            if (!string.IsNullOrWhiteSpace(aluno.Email))
            {
                emails.Add(aluno.Email);
            }

            if (!string.IsNullOrWhiteSpace(aluno.EmailResponsavel))
            {
                emails.Add(aluno.EmailResponsavel);
            }

            return emails.Distinct().ToList();
        }

        private static string MontarCorpoEmail(Pagamento pagamento) => $"""
            <p>Ol&aacute;!</p>
            <p>Este &eacute; um lembrete: a mensalidade de <b>{pagamento.Aluno.Nome}</b>, no valor de R$ {pagamento.ValorFinal:F2}, vence em {pagamento.DataVencimento:dd/MM/yyyy}.</p>
            <p>Se o pagamento ja foi realizado, desconsidere este aviso.</p>
            """;
    }
}
