using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface IPagamentoRepository
    {
        Task<Pagamento?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Pagamento>> ListarAsync(
            int? alunoId,
            string? status,
            DateOnly? vencimentoInicio,
            DateOnly? vencimentoFim,
            CancellationToken cancellationToken = default);

        Task AdicionarAsync(Pagamento pagamento, CancellationToken cancellationToken = default);

        // specs/039: ja existe uma cobranca de mensalidade gerada para este vinculo
        // nesta competencia? Sustenta a idempotencia de FR-004 (junto do indice
        // unico uq_pagamento_vinculo_competencia, database/15_job_cobranca_mensalidade.sql).
        Task<bool> ExisteMensalidadeGeradaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default);

        Task VincularAulaAsync(int pagamentoId, int aulaId, CancellationToken cancellationToken = default);

        // Chama sp_atualizar_status_pagamento (04_procedures.sql): preenche
        // data_pagamento automaticamente quando o novo status e 'Pago'.
        Task AtualizarStatusViaProcedureAsync(int pagamentoId, string novoStatus, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
