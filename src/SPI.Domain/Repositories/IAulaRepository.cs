using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface IAulaRepository
    {
        Task<Aula?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Aula>> ListarAsync(
            string? status,
            int? turmaId,
            int? alunoId,
            DateOnly? dataInicio,
            DateOnly? dataFim,
            CancellationToken cancellationToken = default);

        // Estorias 4 e 10: duas aulas ativas (nao canceladas) da mesma
        // professora nao podem se sobrepor no mesmo dia.
        Task<bool> ExisteConflitoAsync(
            int professorId,
            DateOnly data,
            TimeOnly horaInicio,
            TimeOnly horaFim,
            int? ignorarAulaId,
            CancellationToken cancellationToken = default);

        Task AdicionarAsync(Aula aula, CancellationToken cancellationToken = default);

        Task DefinirAlunosAsync(int aulaId, IEnumerable<int> alunoIds, CancellationToken cancellationToken = default);

        Task<List<int>> ObterAlunosAtivosDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default);

        // specs/052: contagem de presencas confirmadas associadas a um
        // Vinculo de Cobranca (AulaAluno.VinculoCobrancaId), filtrada pelo
        // mes/ano da aula -- usado para "aulas dadas no mes" da Mensalidade.
        Task<int> ContarPresencasPorVinculoECompetenciaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default);

        // specs/059: aulas ativas, ainda "Agendadas", cujo termino (data + hora
        // fim) esta entre `desde` e `agora`; professorId nulo = de todas.
        // Inclui Materia, Turma e AulaAlunos.Aluno.
        Task<List<Aula>> ListarPendentesDeStatusAsync(DateTime agora, DateTime desde, int? professorId, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
