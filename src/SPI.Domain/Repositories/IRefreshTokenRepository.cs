using SPI.Domain.Entities;
using SPI.Domain.Enums;

namespace SPI.Domain.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task AdicionarAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

        Task<RefreshToken?> ObterPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

        // Usado ao trocar a senha (Estoria 1 / recuperacao): forca novo login
        // em todos os dispositivos, revogando os refresh tokens ativos.
        Task RevogarTodosDoUsuarioAsync(int usuarioId, PerfilUsuario perfil, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
