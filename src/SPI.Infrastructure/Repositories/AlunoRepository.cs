using System.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class AlunoRepository : IAlunoRepository
    {
        private readonly SpiDbContext _dbContext;

        public AlunoRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Aluno?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            _dbContext.Alunos.FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

        public Task<Aluno?> ObterPorRaAsync(string ra, CancellationToken cancellationToken = default) =>
            _dbContext.Alunos.FirstOrDefaultAsync(a => a.Ra == ra, cancellationToken);

        public Task<Aluno?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.Alunos.FirstOrDefaultAsync(a => a.Id == id && a.Ativo, cancellationToken);

        public Task<Aluno?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.Alunos.Include(a => a.AlunosTurma).ThenInclude(at => at.Turma).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        public async Task<List<Aluno>> ListarAsync(string? nome, string? ra, int? turmaId, bool? ativo, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Alunos.Include(a => a.AlunosTurma).ThenInclude(at => at.Turma).AsQueryable();

            if (!string.IsNullOrWhiteSpace(nome))
            {
                query = query.Where(a => a.Nome.Contains(nome));
            }

            if (!string.IsNullOrWhiteSpace(ra))
            {
                query = query.Where(a => a.Ra.Contains(ra));
            }

            if (turmaId.HasValue)
            {
                query = query.Where(a => a.AlunosTurma.Any(at => at.TurmaId == turmaId.Value));
            }

            if (ativo.HasValue)
            {
                query = query.Where(a => a.Ativo == ativo.Value);
            }

            return await query.OrderBy(a => a.Nome).ToListAsync(cancellationToken);
        }

        public Task<bool> ExisteCpfAsync(string cpf, int? ignorarId = null, CancellationToken cancellationToken = default) =>
            _dbContext.Alunos.AnyAsync(a => a.Cpf == cpf && (ignorarId == null || a.Id != ignorarId), cancellationToken);

        public async Task<(int Id, string Ra)> CadastrarViaProcedureAsync(
            string nome,
            string? cpf,
            string? telefoneAluno,
            string? telefoneResponsavel,
            string? email,
            string? senhaHash,
            string? emailResponsavel,
            decimal valorAula,
            CancellationToken cancellationToken = default)
        {
            var connection = (MySqlConnection)_dbContext.Database.GetDbConnection();
            var precisaAbrir = connection.State != ConnectionState.Open;
            if (precisaAbrir)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "sp_cadastrar_aluno";

                command.Parameters.Add(new MySqlParameter("p_nome", nome));
                command.Parameters.Add(new MySqlParameter("p_cpf", (object?)cpf ?? DBNull.Value));
                command.Parameters.Add(new MySqlParameter("p_telefone_aluno", (object?)telefoneAluno ?? DBNull.Value));
                command.Parameters.Add(new MySqlParameter("p_telefone_responsavel", (object?)telefoneResponsavel ?? DBNull.Value));
                command.Parameters.Add(new MySqlParameter("p_email", (object?)email ?? DBNull.Value));
                command.Parameters.Add(new MySqlParameter("p_senha", (object?)senhaHash ?? DBNull.Value));
                command.Parameters.Add(new MySqlParameter("p_email_responsavel", (object?)emailResponsavel ?? DBNull.Value));
                command.Parameters.Add(new MySqlParameter("p_valor_aula", valorAula));

                var idParametro = new MySqlParameter("p_novo_id", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                var raParametro = new MySqlParameter("p_novo_ra", MySqlDbType.VarChar, 20) { Direction = ParameterDirection.Output };
                command.Parameters.Add(idParametro);
                command.Parameters.Add(raParametro);

                await command.ExecuteNonQueryAsync(cancellationToken);

                return ((int)idParametro.Value!, (string)raParametro.Value!);
            }
            finally
            {
                if (precisaAbrir)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task VincularTurmaAsync(int alunoId, int turmaId, CancellationToken cancellationToken = default) =>
            await _dbContext.AlunosTurma.AddAsync(new AlunoTurma { AlunoId = alunoId, TurmaId = turmaId }, cancellationToken);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
