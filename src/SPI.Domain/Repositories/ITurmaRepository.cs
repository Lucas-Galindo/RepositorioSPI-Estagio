using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface ITurmaRepository
    {
        Task<Turma?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        // specs/076: filtros opcionais por materia (materiaId) ou por turmas sem materia (semMateria = true).
        Task<List<Turma>> ListarAsync(string? nome, bool? ativo, CancellationToken cancellationToken = default, int? materiaId = null, bool? semMateria = null);

        Task AdicionarAsync(Turma turma, CancellationToken cancellationToken = default);

        Task VincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default);

        Task DesvincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
