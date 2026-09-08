using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    // Consultas agregadas usadas pelo Dashboard (Estoria 11) e pelos
    // Relatorios (Estorias 12-18) que nao pertencem a um unico agregado
    // (contagens, somas, agrupamentos) -- por isso ficam num repositorio
    // proprio em vez de inchar IAulaRepository/IPagamentoRepository/etc.
    public interface IRelatorioRepository
    {
        Task<int> ContarAulasAgendadasNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default);

        Task<int> ContarAlunosAtivosAsync(CancellationToken cancellationToken = default);

        Task<int> ContarTurmasAtivasAsync(CancellationToken cancellationToken = default);

        // Soma de pagamentos "em aberto": status Pendente ou Atrasado
        // (Atrasado e calculado, ver PagamentoService.Mapear).
        Task<decimal> ObterValorPendenteAsync(CancellationToken cancellationToken = default);

        // Soma de pagamentos com status Pago, pela DataPagamento no periodo.
        Task<decimal> ObterValorFaturadoNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default);

        // Contrapartida das duas consultas acima, para Contas a Pagar (Sprint 4
        // da evolucao do Financeiro). Usadas pela Visao Geral (Sprint 7) e pelo
        // Dashboard (Sprint 9) -- adicionadas aqui para nao duplicar a mesma
        // consulta agregada em mais de um lugar.
        Task<decimal> ObterValorAPagarAsync(CancellationToken cancellationToken = default);

        Task<decimal> ObterValorPagoNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default);

        // Sprint 7 (Visao Geral Financeira): separa o valor "Pendente" em duas
        // fatias -- ainda no prazo (a receber/a pagar) e ja vencida (atrasada).
        // "Atrasado" continua sendo calculado (nunca persistido), so que aqui a
        // segregacao acontece no repositorio para nao trazer todas as linhas
        // "Pendente" para a Application camada so para separar em dois grupos.
        Task<(decimal AVencer, decimal Atrasado)> ObterReceitasPendentesSegregadasAsync(CancellationToken cancellationToken = default);

        Task<(decimal AVencer, decimal Atrasado)> ObterDespesasPendentesSegregadasAsync(CancellationToken cancellationToken = default);

        // Contas a receber/pagar (status Pendente) vencendo nos proximos `dias`
        // dias, para o painel de proximos vencimentos da Visao Geral.
        Task<List<Pagamento>> ObterProximasContasAReceberAsync(int dias, CancellationToken cancellationToken = default);

        Task<List<ContaPagar>> ObterProximasContasAPagarAsync(int dias, CancellationToken cancellationToken = default);

        Task<List<Aula>> ObterProximasAulasHojeAsync(CancellationToken cancellationToken = default);

        Task<int> ContarLembretesPendentesAsync(CancellationToken cancellationToken = default);

        // Estoria 16: quantidade de aulas por dia no periodo.
        Task<List<(DateOnly Data, int Quantidade)>> ContarAulasPorDiaAsync(
            DateOnly inicio, DateOnly fim, int? turmaId, int? materiaId, CancellationToken cancellationToken = default);

        // Estoria 16: comparativo Agendada/Realizada/Cancelada no periodo.
        Task<(int Agendadas, int Realizadas, int Canceladas)> ContarAulasPorStatusAsync(
            DateOnly inicio, DateOnly fim, int? turmaId, int? materiaId, CancellationToken cancellationToken = default);

        // Estoria 18: aulas e alunos distintos atendidos por materia no periodo.
        Task<int> ContarAulasDaMateriaNoPeriodoAsync(int materiaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default);

        Task<int> ContarAlunosAtendidosDaMateriaNoPeriodoAsync(int materiaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default);

        // Estoria 15: pagamentos "Pago" filtrados pela DataPagamento (nao pela
        // DataVencimento, que e o filtro usado em IPagamentoRepository.ListarAsync).
        Task<List<Pagamento>> ListarPagosNoPeriodoAsync(
            DateOnly? inicio, DateOnly? fim, int? formaPagamentoId, int? alunoId, CancellationToken cancellationToken = default);

        // Sprint 9 (Dashboard e Fluxo de Caixa) -----------------------------

        // Alunos distintos com pelo menos uma aula Realizada no periodo
        // (distinto de ContarAlunosAtivosAsync, que conta todo aluno ativo
        // independente de ter tido aula no periodo).
        Task<int> ContarAlunosAtendidosNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default);

        // Taxa de inadimplencia: total (em valor) das contas a receber com
        // vencimento no periodo (exclui Canceladas) vs. quanto desse total
        // ainda esta em aberto e vencido (status calculado "Atrasado") hoje.
        Task<(decimal TotalVencidoNoPeriodo, decimal ValorInadimplente)> ObterInadimplenciaNoPeriodoAsync(
            DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default);

        // Pares (DataVencimento, DataPagamento) de contas a receber pagas com
        // atraso (DataPagamento > DataVencimento) no periodo, para calcular o
        // Prazo Medio de Atraso (PMA) na Application.
        Task<List<(DateOnly DataVencimento, DateOnly DataPagamento)>> ObterPagamentosComAtrasoNoPeriodoAsync(
            DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default);

        // Soma de contas a receber/pagar por dia-do-mes de vencimento, para o
        // indicador de Gargalo de Caixa (dia de maior entrada x maior saida).
        Task<List<(int Dia, decimal Valor)>> ObterEntradasPorDiaDoMesAsync(
            DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default);

        Task<List<(int Dia, decimal Valor)>> ObterSaidasPorDiaDoMesAsync(
            DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default);
    }
}
