using SPI.Application.Auth.Dtos;

namespace SPI.Application.Auth.Services
{
    public interface IAutenticacaoService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request, string? ipOrigem, string? userAgent, CancellationToken cancellationToken = default);

        Task<LoginResponse> RefreshAsync(RefreshRequest request, string? ipOrigem, string? userAgent, CancellationToken cancellationToken = default);

        Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
    }
}
