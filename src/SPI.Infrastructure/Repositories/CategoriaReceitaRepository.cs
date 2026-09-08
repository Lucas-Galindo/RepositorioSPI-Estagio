using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class CategoriaReceitaRepository : ICategoriaReceitaRepository
    {
        private readonly SpiDbContext _dbContext;

        public CategoriaReceitaRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<CategoriaReceita?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.CategoriasReceita.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public Task<CategoriaReceita?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) =>
            _dbContext.CategoriasReceita.FirstOrDefaultAsync(c => c.Nome == nome, cancellationToken);

        public Task<List<CategoriaReceita>> ListarAtivasAsync(CancellationToken cancellationToken = default) =>
            _dbContext.CategoriasReceita.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(cancellationToken);
    }
}
