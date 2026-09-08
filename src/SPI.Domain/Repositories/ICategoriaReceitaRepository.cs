using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface ICategoriaReceitaRepository
    {
        Task<CategoriaReceita?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<CategoriaReceita?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);

        Task<List<CategoriaReceita>> ListarAtivasAsync(CancellationToken cancellationToken = default);
    }
}
