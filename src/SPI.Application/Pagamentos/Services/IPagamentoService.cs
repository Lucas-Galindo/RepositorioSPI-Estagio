using SPI.Application.Pagamentos.Dtos;

namespace SPI.Application.Pagamentos.Services
{
    //Interface
    public interface IPagamentoService
    {
        Task<List<PagamentoResponse>> ListarAsync(
            int? alunoId,
            string? status,
            DateOnly? vencimentoInicio,
            DateOnly? vencimentoFim,
            CancellationToken cancellationToken = default);

        Task<PagamentoResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<PagamentoResponse> RegistrarAsync(RegistrarPagamentoRequest request, CancellationToken cancellationToken = default);

        Task<PagamentoResponse> AtualizarAsync(int id, AtualizarPagamentoRequest request, CancellationToken cancellationToken = default);

        Task<PagamentoResponse> AtualizarStatusAsync(int id, AtualizarStatusPagamentoRequest request, CancellationToken cancellationToken = default);
    }
}
