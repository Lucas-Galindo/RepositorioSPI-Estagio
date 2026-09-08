using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly SpiDbContext _dbContext;

        public AdminRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Admin?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            _dbContext.Admins.FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

        public Task<Admin?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id && a.Ativo, cancellationToken);
    }
}
