using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface IAulaRepository
    {
        Task<Aula?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Aula>> ListarAsync(
            string? status,
            int? turmaId,
            int? alunoId,
            DateOnly? dataInicio,
            DateOnly? dataFim,
            CancellationToken cancellationToken = default);

        // Estorias 4 e 10: duas aulas ativas (nao canceladas) da mesma
        // professora nao podem se sobrepor no mesmo dia.
        Task<bool> ExisteConflitoAsync(
            int professorId,
            DateOnly data,
            TimeOnly horaInicio,
            TimeOnly horaFim,
            int? ignorarAulaId,
            CancellationToken cancellationToken = default);

        Task AdicionarAsync(Aula aula, CancellationToken cancellationToken = default);

        Task DefinirAlunosAsync(int aulaId, IEnumerable<int> alunoIds, CancellationToken cancellationToken = default);

        Task<List<int>> ObterAlunosAtivosDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
