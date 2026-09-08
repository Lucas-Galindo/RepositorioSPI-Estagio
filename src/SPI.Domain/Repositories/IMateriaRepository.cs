using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface IMateriaRepository
    {
        Task<Materia?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Materia>> ListarAsync(string? nome, string? nivel, bool? ativo, CancellationToken cancellationToken = default);

        Task<bool> ExisteNomeAsync(string nome, int? ignorarId = null, CancellationToken cancellationToken = default);

        Task AdicionarAsync(Materia materia, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
