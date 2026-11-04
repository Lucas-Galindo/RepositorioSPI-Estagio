using SPI.Application.VinculosCobranca.Dtos;
using SPI.Domain.Entities;

namespace SPI.Application.VinculosCobranca.Services
{
    // specs/068: resumo de cobranca dos alunos, SOMENTE LEITURA e em lote (no maximo duas consultas por
    // chamada, qualquer que seja o numero de alunos). A chave do resultado e o Id do aluno.
    public interface IResumoCobrancaService
    {
        // Lista de alunos: o vinculo principal (Mensalidade, Pacote, Avulsa) + quantos outros + todos
        // (para a dica).
        Task<Dictionary<int, SeloCobrancaResponse>> ResumirParaListaAsync(IReadOnlyCollection<Aluno> alunos, CancellationToken cancellationToken = default);

        // Turma e chamada: o vinculo que VALE naquele grupo (turmaId nulo = aula individual), decidido pela
        // mesma regra que cobra a presenca (RegraCobrancaPresenca.Decidir).
        Task<Dictionary<int, SeloCobrancaResponse>> ResumirParaGrupoAsync(IReadOnlyCollection<Aluno> alunos, int? turmaId, CancellationToken cancellationToken = default);
    }
}
