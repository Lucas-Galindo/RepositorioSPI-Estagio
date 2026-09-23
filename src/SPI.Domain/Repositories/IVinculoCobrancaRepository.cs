using SPI.Domain.Entities;
using SPI.Domain.Enums;

namespace SPI.Domain.Repositories
{
    public interface IVinculoCobrancaRepository
    {
        Task<VinculoCobranca?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        // Ativos primeiro; depois por nome da turma, com atendimento individual (sem turma) por ultimo.
        Task<List<VinculoCobranca>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default);

        // Existe outro vinculo ATIVO para a mesma combinacao aluno+turma (turmaId nulo = atendimento individual)?
        Task<bool> ExisteAtivoAsync(int alunoId, int? turmaId, int? ignorarId, CancellationToken cancellationToken = default);

        // specs/038: o unico vinculo ATIVO do aluno para o contexto exato (mesma
        // turma da aula, ou nenhuma turma para atendimento individual). Nunca e
        // ambiguo -- a unicidade "no maximo um vinculo ativo por combinacao" ja e
        // garantida em specs/037 (validacao + indice unico no banco).
        Task<VinculoCobranca?> ObterAtivoPorAlunoEContextoAsync(int alunoId, int? turmaId, CancellationToken cancellationToken = default);

        // specs/039: todo vinculo Ativo com a modalidade informada (usado pelo job
        // de cobranca de mensalidade, para listar os candidatos a gerar cobranca).
        Task<List<VinculoCobranca>> ListarAtivosPorModalidadeAsync(ModalidadeCobranca modalidade, CancellationToken cancellationToken = default);

        Task AdicionarAsync(VinculoCobranca vinculo, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
