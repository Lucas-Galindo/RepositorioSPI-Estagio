using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface ISenhaResetTokenRepository
    {
        Task AdicionarAsync(SenhaResetToken token, CancellationToken cancellationToken = default);

        Task<SenhaResetToken?> ObterPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
