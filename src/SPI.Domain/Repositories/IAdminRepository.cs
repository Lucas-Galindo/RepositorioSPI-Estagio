using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface IAdminRepository
    {
        Task<Admin?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<Admin?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default);
    }
}
