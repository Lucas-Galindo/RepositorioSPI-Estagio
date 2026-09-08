using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface ILembreteRepository
    {
        Task<Lembrete?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Lembrete>> ListarAsync(int? turmaId, string? status, bool? ativo, CancellationToken cancellationToken = default);

        Task<List<Lembrete>> ListarAtivosPorTurmaAsync(int turmaId, CancellationToken cancellationToken = default);

        // Usado pelo disparo automatico (Estoria 19): lembretes pendentes cuja
        // hora programada ja chegou.
        Task<List<Lembrete>> ListarPendentesVencidosAsync(DateTime agora, CancellationToken cancellationToken = default);

        // Proxima aula agendada (futura, Status = Agendada, Ativo) da turma,
        // usada para calcular/recalcular a hora programada (Estoria 5).
        Task<Aula?> ObterProximaAulaDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default);

        Task AdicionarAsync(Lembrete lembrete, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
