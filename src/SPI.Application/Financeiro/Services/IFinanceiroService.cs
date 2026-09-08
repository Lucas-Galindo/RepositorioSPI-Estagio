using SPI.Application.Financeiro.Dtos;

namespace SPI.Application.Financeiro.Services
{
    public interface IFinanceiroService
    {
        Task<VisaoGeralFinanceiraResponse> ObterVisaoGeralAsync(
            DateOnly? periodoInicio,
            DateOnly? periodoFim,
            CancellationToken cancellationToken = default);
    }
}
