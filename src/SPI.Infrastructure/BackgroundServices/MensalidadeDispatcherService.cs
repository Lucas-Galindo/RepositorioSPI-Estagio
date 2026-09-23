using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Repositories;

namespace SPI.Infrastructure.BackgroundServices
{
    // specs/039: fecha a exceao EX-001 (specs/037-vinculo-cobranca/plan.md) para a
    // ultima modalidade que ainda nao tinha efeito automatico real. Mesmo padrao
    // tecnico de LembreteDispatcherService: BackgroundService, PeriodicTimer,
    // IServiceScopeFactory criando escopo por execucao, try/catch por tick sem
    // derrubar o servico.
    public class MensalidadeDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MensalidadeDispatcherService> _logger;
        private readonly TimeSpan _intervalo;

        public MensalidadeDispatcherService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<MensalidadeDispatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            // 3600s (1h) garante um tick por hora-do-dia todo dia, independente do
            // instante em que o servico subiu -- alcanca a janela 23h-23h59 de
            // forma confiavel, diferente de um intervalo literal de 1 dia
            // (research.md R3).
            var segundos = configuration.GetValue<int?>("Mensalidade:IntervaloVerificacaoSegundos") ?? 3600;
            _intervalo = TimeSpan.FromSeconds(segundos);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_intervalo);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    if (DeveDispararNesteMomento(DateTime.Now))
                    {
                        await ProcessarCobrancasMensaisAsync(stoppingToken);
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.LogError(e, "Falha ao processar cobranca de mensalidade.");
                }
            }
        }

        // FR-001: so dispara no ultimo dia do mes corrente, 23h (horario local) ou
        // depois. Estatico e puro -- testavel sem mockar DateTime.Now nem
        // instanciar o BackgroundService inteiro (research.md R2/R7).
        public static bool DeveDispararNesteMomento(DateTime agora) =>
            agora.Day == DateTime.DaysInMonth(agora.Year, agora.Month) && agora.Hour >= 23;

        private async Task ProcessarCobrancasMensaisAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var vinculoCobrancaRepository = scope.ServiceProvider.GetRequiredService<IVinculoCobrancaRepository>();
            var pagamentoRepository = scope.ServiceProvider.GetRequiredService<IPagamentoRepository>();
            var categoriaReceitaRepository = scope.ServiceProvider.GetRequiredService<ICategoriaReceitaRepository>();

            var competencia = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);

            await GerarCobrancasDoMesAsync(vinculoCobrancaRepository, pagamentoRepository, categoriaReceitaRepository, competencia, _logger, cancellationToken);
        }

        // Geracao em si -- estatica e testavel com fakes, sem depender de
        // IServiceScopeFactory (mesmo espirito de DeveDispararNesteMomento,
        // research.md R7). Ja embute a checagem de idempotencia (FR-004) e o
        // filtro de vinculos ativos (FR-005), pois vem do mesmo fluxo de
        // listagem/checagem por vinculo.
        public static async Task GerarCobrancasDoMesAsync(
            IVinculoCobrancaRepository vinculoCobrancaRepository,
            IPagamentoRepository pagamentoRepository,
            ICategoriaReceitaRepository categoriaReceitaRepository,
            DateOnly competencia,
            ILogger? logger,
            CancellationToken cancellationToken)
        {
            var categoria = await categoriaReceitaRepository.ObterPorNomeAsync("Mensalidade", cancellationToken);
            var vinculos = await vinculoCobrancaRepository.ListarAtivosPorModalidadeAsync(ModalidadeCobranca.Mensalidade, cancellationToken);

            var gerados = 0;

            foreach (var vinculo in vinculos)
            {
                try
                {
                    if (await pagamentoRepository.ExisteMensalidadeGeradaAsync(vinculo.Id, competencia, cancellationToken))
                    {
                        // FR-004: ja gerada para este vinculo nesta competencia -- nunca duplicar.
                        continue;
                    }

                    var contexto = vinculo.TurmaId.HasValue ? vinculo.Turma!.Nome : "Atendimento individual";
                    var pagamento = new Pagamento
                    {
                        AlunoId = vinculo.AlunoId,
                        VinculoCobrancaId = vinculo.Id,
                        Descricao = $"Mensalidade - {contexto} - {competencia:MM/yyyy}",
                        CategoriaReceitaId = categoria?.Id,
                        DataVencimento = competencia.AddMonths(1),
                        Competencia = competencia,
                        ValorFinal = vinculo.Valor,
                        Status = "Pendente"
                    };

                    await pagamentoRepository.AdicionarAsync(pagamento, cancellationToken);
                    await pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
                    gerados++;
                }
                catch (Exception e)
                {
                    // FR-011: uma falha em um vinculo nao impede os demais.
                    logger?.LogError(e, "Falha ao gerar cobranca de mensalidade para o vinculo {VinculoId}.", vinculo.Id);
                }
            }

            logger?.LogInformation("Processadas {Quantidade} cobrancas de mensalidade para a competencia {Competencia:MM/yyyy}.", gerados, competencia);
        }
    }
}
