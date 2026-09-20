using SPI.Application.VinculosCobranca.Dtos;

namespace SPI.Application.VinculosCobranca.Services
{
    public interface IVinculoCobrancaService
    {
        Task<List<VinculoCobrancaResponse>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default);

        Task<VinculoCobrancaResponse> ObterPorIdAsync(int alunoId, int id, CancellationToken cancellationToken = default);

        Task<VinculoCobrancaResponse> CadastrarAsync(int alunoId, VinculoCobrancaRequest request, CancellationToken cancellationToken = default);

        Task<VinculoCobrancaResponse> AtualizarAsync(int alunoId, int id, VinculoCobrancaRequest request, CancellationToken cancellationToken = default);

        Task ExcluirAsync(int alunoId, int id, CancellationToken cancellationToken = default);

        Task<VinculoCobrancaResponse> ReativarAsync(int alunoId, int id, CancellationToken cancellationToken = default);
    }
}
