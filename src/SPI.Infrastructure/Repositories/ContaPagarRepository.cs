using System.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class ContaPagarRepository : IContaPagarRepository
    {
        private readonly SpiDbContext _dbContext;

        public ContaPagarRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<ContaPagar?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.ContasPagar
                .Include(c => c.CategoriaDespesa)
                .Include(c => c.FormaPagamento)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public async Task<List<ContaPagar>> ListarAsync(
            int? categoriaDespesaId,
            string? status,
            string? favorecido,
            DateOnly? vencimentoInicio,
            DateOnly? vencimentoFim,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.ContasPagar
                .Include(c => c.CategoriaDespesa)
                .Include(c => c.FormaPagamento)
                .AsQueryable();

            if (categoriaDespesaId.HasValue)
            {
                query = query.Where(c => c.CategoriaDespesaId == categoriaDespesaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(favorecido))
            {
                query = query.Where(c => c.Favorecido != null && c.Favorecido.Contains(favorecido));
            }

            if (vencimentoInicio.HasValue)
            {
                query = query.Where(c => c.DataVencimento >= vencimentoInicio.Value);
            }

            if (vencimentoFim.HasValue)
            {
                query = query.Where(c => c.DataVencimento <= vencimentoFim.Value);
            }

            return await query.OrderByDescending(c => c.DataVencimento).ToListAsync(cancellationToken);
        }

        public async Task AdicionarAsync(ContaPagar contaPagar, CancellationToken cancellationToken = default) =>
            await _dbContext.ContasPagar.AddAsync(contaPagar, cancellationToken);

        public async Task AtualizarStatusViaProcedureAsync(int contaPagarId, string novoStatus, CancellationToken cancellationToken = default)
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
                command.CommandText = "sp_atualizar_status_conta_pagar";
                command.Parameters.Add(new MySqlParameter("p_conta_pagar_id", contaPagarId));
                command.Parameters.Add(new MySqlParameter("p_novo_status", novoStatus));

                await command.ExecuteNonQueryAsync(cancellationToken);

                // Mesma ressalva do PagamentoRepository: a procedure altera o banco
                // por fora do EF Core, entao limpamos o change tracker para que a
                // proxima leitura reconsulte o banco em vez de devolver a instancia
                // ja rastreada (ver PagamentoRepository.AtualizarStatusViaProcedureAsync).
                _dbContext.ChangeTracker.Clear();
            }
            finally
            {
                if (precisaAbrir)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
