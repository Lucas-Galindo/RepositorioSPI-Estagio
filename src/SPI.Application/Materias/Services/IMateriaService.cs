using SPI.Application.Materias.Dtos;

namespace SPI.Application.Materias.Services
{
    public interface IMateriaService
    {
        Task<List<MateriaResponse>> ListarAsync(string? nome, string? nivel, bool? ativo, CancellationToken cancellationToken = default);

        Task<MateriaResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<MateriaResponse> CadastrarAsync(MateriaRequest request, CancellationToken cancellationToken = default);

        Task<MateriaResponse> AtualizarAsync(int id, MateriaRequest request, CancellationToken cancellationToken = default);

        Task ExcluirAsync(int id, CancellationToken cancellationToken = default);
    }
}
