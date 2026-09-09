using SPI.Application.Financeiro.Dtos;
using SPI.Domain.Repositories;

namespace SPI.Application.Financeiro.Services
{
    // Visao Geral Financeira (evolucao do Financeiro, Sprint 7): consolida
    // Contas a Receber e Contas a Pagar numa unica leitura. Nao duplica os
    // indicadores do Dashboard (Estoria 11) -- reaproveita as mesmas consultas
    // agregadas do IRelatorioRepository, so que combinando receita e despesa.
    public class FinanceiroService : IFinanceiroService
    {
        private const int DiasProximosVencimentos = 7;

        private readonly IRelatorioRepository _relatorioRepository;

        public FinanceiroService(IRelatorioRepository relatorioRepository)
        {
            _relatorioRepository = relatorioRepository;
        }

        public async Task<VisaoGeralFinanceiraResponse> ObterVisaoGeralAsync(
            DateOnly? periodoInicio,
            DateOnly? periodoFim,
            CancellationToken cancellationToken = default,
            string? turmaNome = null,
            string? materiaNome = null,
            string? alunoBusca = null)
        {
            // Mesma convencao de periodo padrao do Dashboard (Estoria 11):
            // sem periodo informado, assume o mes corrente.
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var inicio = periodoInicio ?? new DateOnly(hoje.Year, hoje.Month, 1);
            var fim = periodoFim ?? inicio.AddMonths(1).AddDays(-1);

            var recebidoNoPeriodo = await _relatorioRepository.ObterValorFaturadoNoPeriodoAsync(
                inicio, fim, cancellationToken, turmaNome: turmaNome, materiaNome: materiaNome, alunoBusca: alunoBusca);
            var pagoNoPeriodo = await _relatorioRepository.ObterValorPagoNoPeriodoAsync(inicio, fim, cancellationToken);
            var (aReceber, receitaAtrasada) = await _relatorioRepository.ObterReceitasPendentesSegregadasAsync(
                cancellationToken, turmaNome, materiaNome, alunoBusca);
            var (aPagar, despesaAtrasada) = await _relatorioRepository.ObterDespesasPendentesSegregadasAsync(cancellationToken);

            var proximasReceber = await _relatorioRepository.ObterProximasContasAReceberAsync(
                DiasProximosVencimentos, cancellationToken, turmaNome, materiaNome, alunoBusca);
            var proximasPagar = await _relatorioRepository.ObterProximasContasAPagarAsync(DiasProximosVencimentos, cancellationToken);

            var proximosVencimentos = proximasReceber
                .Select(p => new ProximoVencimentoItem
                {
                    Tipo = "Receber",
                    Descricao = p.Descricao ?? p.Aluno.Nome,
                    DataVencimento = p.DataVencimento,
                    Valor = p.ValorFinal
                })
                .Concat(proximasPagar.Select(c => new ProximoVencimentoItem
                {
                    Tipo = "Pagar",
                    Descricao = c.Descricao,
                    DataVencimento = c.DataVencimento,
                    Valor = c.Valor
                }))
                .OrderBy(i => i.DataVencimento)
                .ToList();

            return new VisaoGeralFinanceiraResponse
            {
                Receitas = new ReceitasResumo
                {
                    Recebido = recebidoNoPeriodo,
                    AReceber = aReceber,
                    Atrasado = receitaAtrasada
                },
                Despesas = new DespesasResumo
                {
                    Pago = pagoNoPeriodo,
                    APagar = aPagar,
                    Atrasado = despesaAtrasada
                },
                Resultado = new ResultadoResumo
                {
                    SaldoRealizado = recebidoNoPeriodo - pagoNoPeriodo,
                    SaldoPrevisto = (recebidoNoPeriodo + aReceber + receitaAtrasada) - (pagoNoPeriodo + aPagar + despesaAtrasada)
                },
                ProximosVencimentos = proximosVencimentos
            };
        }
    }
}
