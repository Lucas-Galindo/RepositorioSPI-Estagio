using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class CategoriaDespesaRepository : ICategoriaDespesaRepository
    {
        private readonly SpiDbContext _dbContext;

        public CategoriaDespesaRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<CategoriaDespesa?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.CategoriasDespesa.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public Task<List<CategoriaDespesa>> ListarAtivasAsync(CancellationToken cancellationToken = default) =>
            _dbContext.CategoriasDespesa.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(cancellationToken);
    }
}
