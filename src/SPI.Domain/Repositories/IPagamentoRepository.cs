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

        // Lembrete de vencimento de mensalidade: pagamentos gerados automaticamente
        // pelo job de mensalidade (VinculoCobrancaId != null), ainda Pendentes, com
        // DataVencimento dentro da janela de antecedencia (<=) e lembrete ainda nao
        // tentado. Usa "<=", nao "==": se o servico ficar fora do ar exatamente no
        // dia em que faltam N dias pro vencimento, ele ainda recupera e envia o
        // lembrete (com um dia ou dois de atraso) no primeiro ciclo seguinte, em vez
        // de perder o aviso pra sempre -- a flag LembreteVencimentoEnviado garante
        // que nunca sera enviado duas vezes.
        Task<List<Pagamento>> ListarMensalidadesParaLembreteVencimentoAsync(DateOnly dataVencimentoAlvo, CancellationToken cancellationToken = default);

        Task VincularAulaAsync(int pagamentoId, int aulaId, CancellationToken cancellationToken = default);

        // Chama sp_atualizar_status_pagamento (04_procedures.sql): preenche
        // data_pagamento automaticamente quando o novo status e 'Pago'.
        Task AtualizarStatusViaProcedureAsync(int pagamentoId, string novoStatus, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
