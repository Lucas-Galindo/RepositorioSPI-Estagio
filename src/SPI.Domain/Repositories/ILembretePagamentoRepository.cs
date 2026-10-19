using SPI.Domain.Entities;

namespace SPI.Domain.Repositories
{
    // specs/060: regra e historico do e-mail semanal de pagamento pendente.
    public interface ILembretePagamentoRepository
    {
        Task<RegraLembretePagamento?> ObterRegraAsync(CancellationToken cancellationToken = default);

        Task<List<EnvioLembretePagamento>> ListarEnviosDoDiaAsync(DateOnly dataEnvio, CancellationToken cancellationToken = default);

        // Mais recentes primeiro (TentadoEm), com o Aluno.
        Task<List<EnvioLembretePagamento>> ListarEnviosRecentesAsync(int limite, CancellationToken cancellationToken = default);

        Task AdicionarEnvioAsync(EnvioLembretePagamento envio, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
