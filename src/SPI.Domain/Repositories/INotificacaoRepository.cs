using SPI.Domain.Entities;
using SPI.Domain.Enums;

namespace SPI.Domain.Repositories
{
    public interface INotificacaoRepository
    {
        Task<Notificacao?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        // Ativas (nao resolvidas) da professora, mais recentes primeiro, com a aula
        // (materia, turma e alunos) para montar a mensagem.
        Task<List<Notificacao>> ListarAtivasPorProfessorAsync(int professorId, CancellationToken cancellationToken = default);

        // Notificacoes do tipo para as aulas informadas, em qualquer estado.
        Task<List<Notificacao>> ListarPorAulasAsync(TipoNotificacao tipo, IReadOnlyCollection<int> aulaIds, CancellationToken cancellationToken = default);

        // specs/060: notificacoes do tipo da professora para os alunos informados, em qualquer estado.
        Task<List<Notificacao>> ListarPorAlunosAsync(TipoNotificacao tipo, int professorId, IReadOnlyCollection<int> alunoIds, CancellationToken cancellationToken = default);

        // Ativas do tipo; professorId nulo = de todas as professoras (rotina).
        Task<List<Notificacao>> ListarAtivasPorTipoAsync(TipoNotificacao tipo, int? professorId, CancellationToken cancellationToken = default);

        Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken = default);

        // Lanca ConflitoException se o indice unico (tipo, aula) barrar a gravacao.
        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
