using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class SenhaResetTokenRepository : ISenhaResetTokenRepository
    {
        private readonly SpiDbContext _dbContext;

        public SenhaResetTokenRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AdicionarAsync(SenhaResetToken token, CancellationToken cancellationToken = default) =>
            await _dbContext.SenhaResetTokens.AddAsync(token, cancellationToken);

        public Task<SenhaResetToken?> ObterPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            _dbContext.SenhaResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
