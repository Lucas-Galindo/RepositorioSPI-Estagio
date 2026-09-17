using Microsoft.AspNetCore.Http;
using SPI.Application.Anexos.Dtos;
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

        /// <summary>Anexa (ou substitui) o comprovante de uma conta a pagar. Nao revalida o arquivo -- ver IAnexoValidator.</summary>
        Task<AnexoResponse> AnexarArquivoAsync(int id, IFormFile arquivo, CancellationToken cancellationToken = default);

        /// <summary>Devolve o conteudo do anexo, ou null se o registro nao existir ou nao tiver anexo.</summary>
        Task<(byte[] Conteudo, string TipoMime, string NomeOriginal)?> ObterArquivoAsync(int id, CancellationToken cancellationToken = default);
    }
}
