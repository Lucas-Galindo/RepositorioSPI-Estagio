using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class VinculoCobrancaRepository : IVinculoCobrancaRepository
    {
        private readonly SpiDbContext _dbContext;

        public VinculoCobrancaRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<VinculoCobranca?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.VinculosCobranca.Include(v => v.Turma).FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        public async Task<List<VinculoCobranca>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.VinculosCobranca.Include(v => v.Turma).Where(v => v.AlunoId == alunoId);

            if (ativo.HasValue)
            {
                query = query.Where(v => v.Ativo == ativo.Value);
            }

            return await query
                .OrderByDescending(v => v.Ativo)
                .ThenBy(v => v.TurmaId == null)
                .ThenBy(v => v.Turma!.Nome)
                .ThenBy(v => v.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<bool> ExisteAtivoAsync(int alunoId, int? turmaId, int? ignorarId, CancellationToken cancellationToken = default) =>
            _dbContext.VinculosCobranca.AnyAsync(
                v => v.AlunoId == alunoId
                    && v.TurmaId == turmaId
                    && v.Ativo
                    && (ignorarId == null || v.Id != ignorarId),
                cancellationToken);

        public Task<VinculoCobranca?> ObterAtivoPorAlunoEContextoAsync(int alunoId, int? turmaId, CancellationToken cancellationToken = default) =>
            _dbContext.VinculosCobranca.FirstOrDefaultAsync(
                v => v.AlunoId == alunoId && v.TurmaId == turmaId && v.Ativo,
                cancellationToken);

        public async Task AdicionarAsync(VinculoCobranca vinculo, CancellationToken cancellationToken = default) =>
            await _dbContext.VinculosCobranca.AddAsync(vinculo, cancellationToken);

        public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException e) when (e.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
            {
                // Corrida entre duas requisicoes: o indice unico de vinculos ativos
                // (uq_vinculocobranca_chave_ativa) barrou a segunda gravacao.
                throw new ConflitoException("Ja existe um vinculo de cobranca ativo para esta combinacao.");
            }
        }
    }
}
