using SPI.Application.Turmas.Dtos;

namespace SPI.Application.Turmas.Services
{
    public interface ITurmaService
    {
        Task<List<TurmaResponse>> ListarAsync(string? nome, bool? ativo, CancellationToken cancellationToken = default);

        Task<TurmaResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<TurmaResponse> CadastrarAsync(int professorId, TurmaRequest request, CancellationToken cancellationToken = default);

        Task<TurmaResponse> AtualizarAsync(int id, TurmaRequest request, CancellationToken cancellationToken = default);

        Task ExcluirAsync(int id, CancellationToken cancellationToken = default);

        Task<TurmaResponse> VincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default);

        Task<TurmaResponse> DesvincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default);
    }
}
