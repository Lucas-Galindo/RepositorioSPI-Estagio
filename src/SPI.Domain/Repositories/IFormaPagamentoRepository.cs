using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    public interface IFormaPagamentoRepository
    {
        Task<FormaPagamento?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<FormaPagamento>> ListarAtivasAsync(CancellationToken cancellationToken = default);
    }
}
