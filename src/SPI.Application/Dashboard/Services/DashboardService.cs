using SPI.Application.Common.Dtos;
using SPI.Application.Dashboard.Dtos;
using SPI.Domain.Repositories;

namespace SPI.Application.Dashboard.Services
{
    public class DashboardService : IDashboardService
    {
        private const int MesesFluxoCaixa = 6;

        private readonly IRelatorioRepository _relatorioRepository;

        public DashboardService(IRelatorioRepository relatorioRepository)
        {
            _relatorioRepository = relatorioRepository;
        }

        public async Task<DashboardResponse> ObterAsync(DateOnly? periodoInicio, DateOnly? periodoFim, CancellationToken cancellationToken = default)
        {
            // Sem periodo informado, assume o mes corrente (Estoria 11 nao
            // fixa um periodo padrao explicito).
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var inicio = periodoInicio ?? new DateOnly(hoje.Year, hoje.Month, 1);
            var fim = periodoFim ?? inicio.AddMonths(1).AddDays(-1);

            var totalAulas = await _relatorioRepository.ContarAulasAgendadasNoPeriodoAsync(inicio, fim, cancellationToken);
            var totalAlunos = await _relatorioRepository.ContarAlunosAtivosAsync(cancellationToken);
            var alunosAtendidos = await _relatorioRepository.ContarAlunosAtendidosNoPeriodoAsync(inicio, fim, cancellationToken);
            var totalTurmas = await _relatorioRepository.ContarTurmasAtivasAsync(cancellationToken);
            var valorPendente = await _relatorioRepository.ObterValorPendenteAsync(cancellationToken);
            var valorFaturado = await _relatorioRepository.ObterValorFaturadoNoPeriodoAsync(inicio, fim, cancellationToken);
            var proximasAulas = await _relatorioRepository.ObterProximasAulasHojeAsync(cancellationToken);
            var lembretesPendentes = await _relatorioRepository.ContarLembretesPendentesAsync(cancellationToken);

            var indicadores = await ObterIndicadoresFinanceirosAsync(inicio, fim, valorFaturado, cancellationToken);
            var fluxoCaixaMensal = await ObterFluxoCaixaMensalAsync(hoje, cancellationToken);

            return new DashboardResponse
            {
                TotalAulasAgendadasNoPeriodo = totalAulas,
                TotalAlunosAtivos = totalAlunos,
                AlunosAtendidosNoPeriodo = alunosAtendidos,
                TotalTurmasAtivas = totalTurmas,
                ValorPendenteRecebimento = valorPendente,
                ValorFaturadoNoPeriodo = valorFaturado,
                LembretesPendentes = lembretesPendentes,
                Indicadores = indicadores,
                FluxoCaixaMensal = fluxoCaixaMensal,
                ProximasAulasHoje = proximasAulas.Select(a => new AulaResumoResponse
                {
                    Id = a.Id,
                    MateriaNome = a.Materia.Nome,
                    TurmaNome = a.Turma?.Nome,
                    DataInicio = a.DataInicio,
                    HoraInicio = a.HoraInicio,
                    HoraFim = a.HoraFim,
                    Status = a.Status
                }).ToList()
            };
        }

        private async Task<IndicadoresFinanceirosResponse> ObterIndicadoresFinanceirosAsync(
            DateOnly inicio, DateOnly fim, decimal valorFaturado, CancellationToken cancellationToken)
        {
            var (totalVencido, valorInadimplente) = await _relatorioRepository.ObterInadimplenciaNoPeriodoAsync(inicio, fim, cancellationToken);
            var taxaInadimplencia = totalVencido == 0 ? 0m : Math.Round(valorInadimplente / totalVencido * 100, 1);

            var atrasos = await _relatorioRepository.ObterPagamentosComAtrasoNoPeriodoAsync(inicio, fim, cancellationToken);
            decimal? prazoMedioAtraso = atrasos.Count == 0
                ? null
                : Math.Round((decimal)atrasos.Average(a => a.DataPagamento.DayNumber - a.DataVencimento.DayNumber), 1);

            var valorPago = await _relatorioRepository.ObterValorPagoNoPeriodoAsync(inicio, fim, cancellationToken);
            var fluxoCaixaOperacional = valorFaturado - valorPago;
            var margemSeguranca = valorFaturado == 0 ? 0m : Math.Round(fluxoCaixaOperacional / valorFaturado * 100, 1);
            decimal? indiceCobertura = valorPago == 0 ? null : Math.Round(valorFaturado / valorPago, 2);

            var entradas = await _relatorioRepository.ObterEntradasPorDiaDoMesAsync(inicio, fim, cancellationToken);
            var saidas = await _relatorioRepository.ObterSaidasPorDiaDoMesAsync(inicio, fim, cancellationToken);
            var maiorEntrada = entradas.OrderByDescending(x => x.Valor).Cast<(int Dia, decimal Valor)?>().FirstOrDefault();
            var maiorSaida = saidas.OrderByDescending(x => x.Valor).Cast<(int Dia, decimal Valor)?>().FirstOrDefault();

            return new IndicadoresFinanceirosResponse
            {
                TaxaInadimplenciaPercentual = taxaInadimplencia,
                PrazoMedioAtrasoDias = prazoMedioAtraso,
                MargemSegurancaPercentual = margemSeguranca,
                FluxoCaixaOperacional = fluxoCaixaOperacional,
                IndiceCoberturaCustosFixos = indiceCobertura,
                GargaloCaixa = new GargaloCaixaResponse
                {
                    DiaMaiorEntrada = maiorEntrada?.Dia,
                    ValorMaiorEntrada = maiorEntrada?.Valor ?? 0,
                    DiaMaiorSaida = maiorSaida?.Dia,
                    ValorMaiorSaida = maiorSaida?.Valor ?? 0
                }
            };
        }

        private async Task<List<FluxoCaixaMensalItem>> ObterFluxoCaixaMensalAsync(DateOnly hoje, CancellationToken cancellationToken)
        {
            var itens = new List<FluxoCaixaMensalItem>();

            for (var i = MesesFluxoCaixa - 1; i >= 0; i--)
            {
                var mesInicio = new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(-i);
                var mesFim = mesInicio.AddMonths(1).AddDays(-1);

                var entradas = await _relatorioRepository.ObterValorFaturadoNoPeriodoAsync(mesInicio, mesFim, cancellationToken);
                var saidas = await _relatorioRepository.ObterValorPagoNoPeriodoAsync(mesInicio, mesFim, cancellationToken);

                itens.Add(new FluxoCaixaMensalItem
                {
                    Ano = mesInicio.Year,
                    Mes = mesInicio.Month,
                    Entradas = entradas,
                    Saidas = saidas,
                    Saldo = entradas - saidas
                });
            }

            return itens;
        }
    }
}
