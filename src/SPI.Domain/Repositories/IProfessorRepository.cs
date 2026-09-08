using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface IProfessorRepository
    {
        Task<Professor?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<Professor?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default);

        // Sistema e single-tenant nesta fase: usado para permitir o cadastro
        // inicial da professora somente enquanto nenhuma existir (Estoria 1).
        Task<bool> ExisteAlgumAsync(CancellationToken cancellationToken = default);

        Task<bool> ExisteCpfAsync(string cpf, CancellationToken cancellationToken = default);

        Task<bool> ExisteEmailAsync(string email, int? ignorarId = null, CancellationToken cancellationToken = default);

        Task AdicionarAsync(Professor professor, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
