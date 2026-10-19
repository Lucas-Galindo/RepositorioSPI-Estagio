using SPI.Application.Lembretes.Dtos;

namespace SPI.Application.Lembretes.Services
{
    public interface ILembretePagamentoService
    {
        Task<RegraLembretePagamentoDto> ObterRegraAsync(CancellationToken cancellationToken = default);

        Task<RegraLembretePagamentoDto> AtualizarRegraAsync(RegraLembretePagamentoDto regra, CancellationToken cancellationToken = default);

        Task<List<EnvioLembretePagamentoResponse>> ListarEnviosAsync(int limite, CancellationToken cancellationToken = default);

        // Executa o envio do dia, se a regra mandar (chamado pela rotina).
        Task ProcessarAsync(DateTime agora, CancellationToken cancellationToken = default);
    }
}
