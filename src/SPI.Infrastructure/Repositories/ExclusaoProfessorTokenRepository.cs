using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class ExclusaoProfessorTokenRepository : IExclusaoProfessorTokenRepository
    {
        private readonly SpiDbContext _dbContext;

        public ExclusaoProfessorTokenRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AdicionarAsync(ExclusaoProfessorToken token, CancellationToken cancellationToken = default) =>
            await _dbContext.ExclusaoProfessorTokens.AddAsync(token, cancellationToken);

        public Task<ExclusaoProfessorToken?> ObterPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            _dbContext.ExclusaoProfessorTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        public Task<List<ExclusaoProfessorToken>> ObterPendentesPorAdminIdAsync(int adminId, CancellationToken cancellationToken = default) =>
            _dbContext.ExclusaoProfessorTokens
                .Where(t => t.AdminId == adminId && !t.Usado)
                .ToListAsync(cancellationToken);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
