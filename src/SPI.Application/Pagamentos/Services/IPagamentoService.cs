using Microsoft.AspNetCore.Http;
using SPI.Application.Anexos.Dtos;
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

        /// <summary>Anexa (ou substitui) o comprovante de uma conta a receber. Nao revalida o arquivo -- ver IAnexoValidator.</summary>
        Task<AnexoResponse> AnexarArquivoAsync(int id, IFormFile arquivo, CancellationToken cancellationToken = default);

        /// <summary>Devolve o conteudo do anexo, ou null se o registro nao existir ou nao tiver anexo.</summary>
        Task<(byte[] Conteudo, string TipoMime, string NomeOriginal)?> ObterArquivoAsync(int id, CancellationToken cancellationToken = default);
    }
}
