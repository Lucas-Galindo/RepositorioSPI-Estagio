using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPI.Application.Turmas.Services;

namespace SPI.Infrastructure.BackgroundServices
{
    // specs/063 (FR-019): rotina de fim de turma. Inativa os pacotes das turmas de pacote que
    // acabaram (data de fim anterior a hoje, ou turma inativa), preservando o saldo, mesmo sem
    // ninguem com o sistema aberto. Roda uma vez ao iniciar (recupera dias perdidos) e depois a
    // cada Turmas:IntervaloEncerramentoMinutos. Mesmo padrao das demais rotinas: PeriodicTimer,
    // escopo por execucao, falha de uma execucao registrada sem derrubar o servico. A regra fica
    // toda no EncerramentoTurmaPacoteService; esta rotina nao envia e-mail.
    public class TurmaEncerramentoDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TurmaEncerramentoDispatcherService> _logger;
        private readonly TimeSpan _intervalo;

        public TurmaEncerramentoDispatcherService(IServiceScopeFactory scopeFactory, TurmasOptions opcoes, ILogger<TurmaEncerramentoDispatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _intervalo = TimeSpan.FromMinutes(Math.Max(1, opcoes.IntervaloEncerramentoMinutos));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ExecutarUmaVezAsync(stoppingToken);

            using var timer = new PeriodicTimer(_intervalo);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExecutarUmaVezAsync(stoppingToken);
            }
        }

        private async Task ExecutarUmaVezAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var servico = scope.ServiceProvider.GetRequiredService<IEncerramentoTurmaPacoteService>();
                var inativados = await servico.EncerrarPacotesDeTurmasEncerradasAsync(DateOnly.FromDateTime(DateTime.Now), stoppingToken);

                if (inativados > 0)
                {
                    _logger.LogInformation("Fim de turma: {Quantidade} pacote(s) inativado(s), saldo preservado.", inativados);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "Falha ao encerrar os pacotes de turmas encerradas.");
            }
        }
    }
}
