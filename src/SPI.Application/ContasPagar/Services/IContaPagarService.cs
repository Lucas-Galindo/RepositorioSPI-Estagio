using SPI.Application.ContasPagar.Dtos;

namespace SPI.Application.ContasPagar.Services
{
    public interface IContaPagarService
    {
        Task<List<ContaPagarResponse>> ListarAsync(
            int? categoriaDespesaId,
            string? status,
            string? favorecido,
            DateOnly? vencimentoInicio,
            DateOnly? vencimentoFim,
            CancellationToken cancellationToken = default);

        Task<ContaPagarResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<ContaPagarResponse> RegistrarAsync(RegistrarContaPagarRequest request, CancellationToken cancellationToken = default);

        Task<ContaPagarResponse> AtualizarAsync(int id, AtualizarContaPagarRequest request, CancellationToken cancellationToken = default);

        Task<ContaPagarResponse> AtualizarStatusAsync(int id, AtualizarStatusContaPagarRequest request, CancellationToken cancellationToken = default);
    }
}
