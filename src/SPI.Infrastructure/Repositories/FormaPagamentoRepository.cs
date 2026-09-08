using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class FormaPagamentoRepository : IFormaPagamentoRepository
    {
        private readonly SpiDbContext _dbContext;

        public FormaPagamentoRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<FormaPagamento?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.FormasPagamento.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        public Task<List<FormaPagamento>> ListarAtivasAsync(CancellationToken cancellationToken = default) =>
            _dbContext.FormasPagamento.Where(f => f.Ativo).OrderBy(f => f.Forma).ToListAsync(cancellationToken);
    }
}
