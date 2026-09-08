using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class MateriaRepository : IMateriaRepository
    {
        private readonly SpiDbContext _dbContext;

        public MateriaRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Materia?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.Materias.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        public async Task<List<Materia>> ListarAsync(string? nome, string? nivel, bool? ativo, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Materias.AsQueryable();

            if (!string.IsNullOrWhiteSpace(nome))
            {
                query = query.Where(m => m.Nome.Contains(nome));
            }

            if (!string.IsNullOrWhiteSpace(nivel))
            {
                query = query.Where(m => m.Nivel != null && m.Nivel.Contains(nivel));
            }

            if (ativo.HasValue)
            {
                query = query.Where(m => m.Ativo == ativo.Value);
            }

            return await query.OrderBy(m => m.Nome).ToListAsync(cancellationToken);
        }

        public Task<bool> ExisteNomeAsync(string nome, int? ignorarId = null, CancellationToken cancellationToken = default) =>
            _dbContext.Materias.AnyAsync(m => m.Nome == nome && (ignorarId == null || m.Id != ignorarId), cancellationToken);

        public async Task AdicionarAsync(Materia materia, CancellationToken cancellationToken = default) =>
            await _dbContext.Materias.AddAsync(materia, cancellationToken);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
