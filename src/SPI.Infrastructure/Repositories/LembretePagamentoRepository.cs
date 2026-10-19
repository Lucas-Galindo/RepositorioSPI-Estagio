using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class LembretePagamentoRepository : ILembretePagamentoRepository
    {
        private readonly SpiDbContext _dbContext;

        public LembretePagamentoRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<RegraLembretePagamento?> ObterRegraAsync(CancellationToken cancellationToken = default) =>
            _dbContext.RegrasLembretePagamento.FirstOrDefaultAsync(r => r.Id == RegraLembretePagamento.IdUnico, cancellationToken);

        public Task<List<EnvioLembretePagamento>> ListarEnviosDoDiaAsync(DateOnly dataEnvio, CancellationToken cancellationToken = default) =>
            _dbContext.EnviosLembretePagamento.Where(e => e.DataEnvio == dataEnvio).ToListAsync(cancellationToken);

        public Task<List<EnvioLembretePagamento>> ListarEnviosRecentesAsync(int limite, CancellationToken cancellationToken = default) =>
            _dbContext.EnviosLembretePagamento
                .Include(e => e.Aluno)
                .OrderByDescending(e => e.TentadoEm).ThenByDescending(e => e.Id)
                .Take(limite)
                .ToListAsync(cancellationToken);

        public async Task AdicionarEnvioAsync(EnvioLembretePagamento envio, CancellationToken cancellationToken = default) =>
            await _dbContext.EnviosLembretePagamento.AddAsync(envio, cancellationToken);

        public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException e) when (e.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
            {
                // Duas execucoes da rotina ao mesmo tempo: o indice unico (aluno, dia)
                // barrou o segundo registro do mesmo envio (specs/060 FR-007).
                throw new ConflitoException("Ja existe um envio deste lembrete para o aluno neste dia.");
            }
        }
    }
}
