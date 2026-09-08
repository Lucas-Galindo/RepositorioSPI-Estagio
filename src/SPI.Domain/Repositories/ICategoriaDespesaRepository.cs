using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface ICategoriaDespesaRepository
    {
        Task<CategoriaDespesa?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<CategoriaDespesa>> ListarAtivasAsync(CancellationToken cancellationToken = default);
    }
}
