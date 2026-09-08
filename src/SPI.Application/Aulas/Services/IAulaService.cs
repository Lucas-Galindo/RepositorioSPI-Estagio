using SPI.Application.Aulas.Dtos;

namespace SPI.Application.Aulas.Services
{
    public interface IAulaService
    {
        Task<List<AulaResponse>> ListarAsync(
            string? status,
            int? turmaId,
            int? alunoId,
            DateOnly? dataInicio,
            DateOnly? dataFim,
            CancellationToken cancellationToken = default);

        Task<AulaResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<AulaResponse> CadastrarAsync(int professorId, AulaRequest request, CancellationToken cancellationToken = default);

        Task<AulaResponse> AtualizarAsync(int id, AulaRequest request, CancellationToken cancellationToken = default);

        Task ExcluirAsync(int id, CancellationToken cancellationToken = default);

        Task<AulaResponse> RegistrarSessaoAsync(int id, RegistrarSessaoRequest request, CancellationToken cancellationToken = default);

        Task<AulaResponse> CancelarAsync(int id, CancellationToken cancellationToken = default);
    }
}
