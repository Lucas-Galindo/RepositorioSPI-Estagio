using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPI.Application.Lembretes.Services;

namespace SPI.Infrastructure.BackgroundServices
{
    // specs/060: rotina do e-mail semanal de pagamento pendente. Roda a cada
    // poucos minutos; quem decide se e dia/hora de enviar (e para quem) e
    // LembretePagamentoService, lendo a regra editavel da aba Lembretes.
    public class LembretePagamentoDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LembretePagamentoDispatcherService> _logger;
        private readonly TimeSpan _intervalo;

        public LembretePagamentoDispatcherService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<LembretePagamentoDispatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            var segundos = configuration.GetValue<int?>("LembretePagamento:IntervaloVerificacaoSegundos") ?? 300;
            _intervalo = TimeSpan.FromSeconds(segundos);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_intervalo);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var servico = scope.ServiceProvider.GetRequiredService<ILembretePagamentoService>();
                    await servico.ProcessarAsync(DateTime.Now, stoppingToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.LogError(e, "Falha ao processar o lembrete semanal de pagamento pendente.");
                }
            }
        }
    }
}
