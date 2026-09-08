using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface IAlunoRepository
    {
        Task<Aluno?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<Aluno?> ObterPorRaAsync(string ra, CancellationToken cancellationToken = default);

        Task<Aluno?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default);

        Task<Aluno?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Aluno>> ListarAsync(string? nome, string? ra, int? turmaId, bool? ativo, CancellationToken cancellationToken = default);

        Task<bool> ExisteCpfAsync(string cpf, int? ignorarId = null, CancellationToken cancellationToken = default);

        // Chama sp_cadastrar_aluno (04_procedures.sql): gera o RA de forma
        // atomica (lock em ra_controle) e insere o aluno em uma unica
        // transacao no banco. Nunca reimplementar essa geracao em C#.
        Task<(int Id, string Ra)> CadastrarViaProcedureAsync(
            string nome,
            string? cpf,
            string? telefoneAluno,
            string? telefoneResponsavel,
            string? email,
            string? senhaHash,
            string? emailResponsavel,
            decimal valorAula,
            CancellationToken cancellationToken = default);

        Task VincularTurmaAsync(int alunoId, int turmaId, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
