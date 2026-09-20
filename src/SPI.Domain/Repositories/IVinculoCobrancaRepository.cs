using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface IVinculoCobrancaRepository
    {
        Task<VinculoCobranca?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        // Ativos primeiro; depois por nome da turma, com atendimento individual (sem turma) por ultimo.
        Task<List<VinculoCobranca>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default);

        // Existe outro vinculo ATIVO para a mesma combinacao aluno+turma (turmaId nulo = atendimento individual)?
        Task<bool> ExisteAtivoAsync(int alunoId, int? turmaId, int? ignorarId, CancellationToken cancellationToken = default);

        Task AdicionarAsync(VinculoCobranca vinculo, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
