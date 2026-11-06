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
            CancellationToken cancellationToken = default,
            int? materiaId = null, TimeOnly? horaInicioDe = null, TimeOnly? horaInicioAte = null);

        Task<AulaResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<AulaResponse> CadastrarAsync(int professorId, AulaRequest request, CancellationToken cancellationToken = default);

        Task<AulaResponse> AtualizarAsync(int id, AulaRequest request, CancellationToken cancellationToken = default);

        Task ExcluirAsync(int id, CancellationToken cancellationToken = default);

        Task<AulaResponse> RegistrarSessaoAsync(int id, RegistrarSessaoRequest request, CancellationToken cancellationToken = default);

        Task<AulaResponse> CancelarAsync(int id, CancellationToken cancellationToken = default);

        // specs/067: link da reuniao, so em memoria (ver ILinkAulaTemporario).
        Task<AulaResponse> DefinirLinkAsync(int id, string link, CancellationToken cancellationToken = default);

        Task<AulaResponse> RemoverLinkAsync(int id, CancellationToken cancellationToken = default);

        Task<AulaResponse> AdicionarParticipanteAsync(int aulaId, int alunoId, CancellationToken cancellationToken = default);

        Task<AulaResponse> RemoverParticipanteAsync(int aulaId, int alunoId, CancellationToken cancellationToken = default);
    }
}
