using System.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class PagamentoRepository : IPagamentoRepository
    {
        private readonly SpiDbContext _dbContext;

        public PagamentoRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Pagamento?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.Pagamentos
                .Include(p => p.Aluno)
                .Include(p => p.FormaPagamento)
                .Include(p => p.CategoriaReceita)
                .Include(p => p.PagamentosAula).ThenInclude(pa => pa.Aula)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        public async Task<List<Pagamento>> ListarAsync(
            int? alunoId,
            string? status,
            DateOnly? vencimentoInicio,
            DateOnly? vencimentoFim,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Pagamentos
                .Include(p => p.Aluno)
                .Include(p => p.FormaPagamento)
                .Include(p => p.CategoriaReceita)
                .Include(p => p.PagamentosAula).ThenInclude(pa => pa.Aula)
                .AsQueryable();

            if (alunoId.HasValue)
            {
                query = query.Where(p => p.AlunoId == alunoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            if (vencimentoInicio.HasValue)
            {
                query = query.Where(p => p.DataVencimento >= vencimentoInicio.Value);
            }

            if (vencimentoFim.HasValue)
            {
                query = query.Where(p => p.DataVencimento <= vencimentoFim.Value);
            }

            return await query.OrderByDescending(p => p.DataVencimento).ToListAsync(cancellationToken);
        }

        public async Task AdicionarAsync(Pagamento pagamento, CancellationToken cancellationToken = default) =>
            await _dbContext.Pagamentos.AddAsync(pagamento, cancellationToken);

        public async Task VincularAulaAsync(int pagamentoId, int aulaId, CancellationToken cancellationToken = default) =>
            await _dbContext.PagamentosAula.AddAsync(new PagamentoAula { PagamentoId = pagamentoId, AulaId = aulaId }, cancellationToken);

        public async Task AtualizarStatusViaProcedureAsync(int pagamentoId, string novoStatus, CancellationToken cancellationToken = default)
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
                command.CommandText = "sp_atualizar_status_pagamento";
                command.Parameters.Add(new MySqlParameter("p_pagamento_id", pagamentoId));
                command.Parameters.Add(new MySqlParameter("p_novo_status", novoStatus));

                await command.ExecuteNonQueryAsync(cancellationToken);

                // A procedure altera o banco por fora do EF Core: sem isso, uma
                // leitura subsequente na mesma request devolveria a instancia ja
                // rastreada (com Status/DataPagamento antigos) em vez de reconsultar
                // o banco -- comportamento de identity map do EF Core.
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
