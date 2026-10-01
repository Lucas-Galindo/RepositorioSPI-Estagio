using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface IExclusaoProfessorTokenRepository
    {
        Task AdicionarAsync(ExclusaoProfessorToken token, CancellationToken cancellationToken = default);

        Task<ExclusaoProfessorToken?> ObterPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

        Task<List<ExclusaoProfessorToken>> ObterPendentesPorAdminIdAsync(int adminId, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
