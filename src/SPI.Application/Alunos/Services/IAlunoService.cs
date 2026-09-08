using SPI.Application.Alunos.Dtos;

namespace SPI.Application.Alunos.Services
{
    public interface IAlunoService
    {
        Task<List<AlunoResponse>> ListarAsync(string? nome, string? ra, int? turmaId, bool? ativo, CancellationToken cancellationToken = default);

        Task<AlunoResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<AlunoResponse> CadastrarAsync(CadastrarAlunoRequest request, CancellationToken cancellationToken = default);

        Task<AlunoResponse> AtualizarAsync(int id, AtualizarAlunoRequest request, CancellationToken cancellationToken = default);

        Task ExcluirAsync(int id, CancellationToken cancellationToken = default);
    }
}
