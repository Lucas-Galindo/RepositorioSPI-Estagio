using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPI.Application.Notificacoes.Services;

namespace SPI.Infrastructure.BackgroundServices
{
    // specs/059: rotina que mantem os avisos de aula pendente de todas as
    // professoras, mesmo sem ninguem com o sistema aberto. Mesmo padrao das
    // demais rotinas: PeriodicTimer, escopo por execucao, falha de uma execucao
    // registrada sem derrubar o servico. A regra fica toda no NotificacaoService.
    public class NotificacaoDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificacaoDispatcherService> _logger;
        private readonly TimeSpan _intervalo;

        public NotificacaoDispatcherService(IServiceScopeFactory scopeFactory, NotificacoesOptions opcoes, ILogger<NotificacaoDispatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _intervalo = TimeSpan.FromSeconds(opcoes.IntervaloVerificacaoSegundos);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_intervalo);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var servico = scope.ServiceProvider.GetRequiredService<INotificacaoService>();
                    await servico.SincronizarAulasPendentesAsync(DateTime.Now, professorId: null, stoppingToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.LogError(e, "Falha ao sincronizar notificacoes de aula pendente.");
                }
            }
        }
    }
}
