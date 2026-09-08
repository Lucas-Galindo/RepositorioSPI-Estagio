using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface IContaPagarRepository
    {
        Task<ContaPagar?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<ContaPagar>> ListarAsync(
            int? categoriaDespesaId,
            string? status,
            string? favorecido,
            DateOnly? vencimentoInicio,
            DateOnly? vencimentoFim,
            CancellationToken cancellationToken = default);

        Task AdicionarAsync(ContaPagar contaPagar, CancellationToken cancellationToken = default);

        // Chama sp_atualizar_status_conta_pagar (10_financeiro_contas.sql): preenche
        // data_pagamento automaticamente quando o novo status e 'Pago'.
        Task AtualizarStatusViaProcedureAsync(int contaPagarId, string novoStatus, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
