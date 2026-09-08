using SPI.Application.Relatorios.Dtos;

namespace SPI.Application.Relatorios.Services
{
    public interface IRelatorioService
    {
        Task<List<RelatorioAgendaItem>> ObterAgendaAsync(
            DateOnly? inicio, DateOnly? fim, string? status, int? turmaId, int? alunoId, CancellationToken cancellationToken = default);

        Task<RelatorioHistoricoAlunoResponse> ObterHistoricoAlunoAsync(
            int alunoId, DateOnly? inicio, DateOnly? fim, string? status, int? turmaId, CancellationToken cancellationToken = default);

        Task<RelatorioPagamentosResponse> ObterPagamentosAsync(
            int? alunoId, DateOnly? inicio, DateOnly? fim, string? status, CancellationToken cancellationToken = default);

        Task<RelatorioFinanceiroResponse> ObterFinanceiroAsync(
            DateOnly? inicio, DateOnly? fim, int? formaPagamentoId, int? alunoId, CancellationToken cancellationToken = default);

        Task<RelatorioPeriodoAgendaResponse> ObterPeriodoAgendaAsync(
            DateOnly inicio, DateOnly fim, int? turmaId, int? materiaId, CancellationToken cancellationToken = default);

        Task<List<RelatorioAlunoItem>> ObterAlunosAsync(
            string? nome, int? turmaId, bool? ativo, CancellationToken cancellationToken = default);

        Task<List<RelatorioMateriaItem>> ObterMateriasAsync(
            string? nivel, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default);
    }
}
