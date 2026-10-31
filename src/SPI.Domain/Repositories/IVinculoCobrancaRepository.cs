using SPI.Domain.Entities;
using SPI.Domain.Enums;

namespace SPI.Domain.Repositories
{
    public interface IVinculoCobrancaRepository
    {
        Task<VinculoCobranca?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        // Ativos primeiro; depois por nome da turma, com os vinculos sem turma por ultimo.
        // Com ativo = true e a base das regras de combinacao (VinculoCobrancaService)
        // e da decisao de cobranca de cada presenca (RegraCobrancaPresenca, specs/056).
        Task<List<VinculoCobranca>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default);

        // specs/039: todo vinculo Ativo com a modalidade informada (usado pelo job
        // de cobranca de mensalidade, para listar os candidatos a gerar cobranca).
        Task<List<VinculoCobranca>> ListarAtivosPorModalidadeAsync(ModalidadeCobranca modalidade, CancellationToken cancellationToken = default);

        // specs/058: pacotes ativos e inativos, com o Aluno (visao de negocio da
        // Home); alunoId nulo = de todos os alunos.
        Task<List<VinculoCobranca>> ListarPacotesAsync(int? alunoId, CancellationToken cancellationToken = default);

        // specs/063: pacotes ATIVOS apontando para a turma (com o Aluno). Base da regra
        // "quem pode voltar a uma turma de pacote" e do encerramento da turma.
        Task<List<VinculoCobranca>> ListarPacotesAtivosDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default);

        // specs/063: pacotes ATIVOS de turmas ENCERRADAS -- turma inativa ou com data de
        // fim anterior a `hoje` (com a Turma). A rotina de fim de turma os inativa.
        Task<List<VinculoCobranca>> ListarPacotesAtivosDeTurmasEncerradasAsync(DateOnly hoje, CancellationToken cancellationToken = default);

        Task AdicionarAsync(VinculoCobranca vinculo, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
