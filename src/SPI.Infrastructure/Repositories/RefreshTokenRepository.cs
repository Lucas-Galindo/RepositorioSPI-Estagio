using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly SpiDbContext _dbContext;

        public RefreshTokenRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AdicionarAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);

        public Task<RefreshToken?> ObterPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        public async Task RevogarTodosDoUsuarioAsync(int usuarioId, PerfilUsuario perfil, CancellationToken cancellationToken = default)
        {
            var agora = DateTime.UtcNow;
            await _dbContext.RefreshTokens
                .Where(r => r.UsuarioId == usuarioId && r.Perfil == perfil && r.RevogadoEm == null)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevogadoEm, agora), cancellationToken);
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
