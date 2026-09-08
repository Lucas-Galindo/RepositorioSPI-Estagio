using SPI.Application.Professores.Dtos;

namespace SPI.Application.Professores.Services
{
    public interface IProfessorService
    {
        Task<ProfessorResponse> CadastroInicialAsync(CadastroInicialProfessorRequest request, CancellationToken cancellationToken = default);

        Task<ProfessorResponse> ObterPerfilAsync(int professorId, CancellationToken cancellationToken = default);

        Task<ProfessorResponse> AtualizarPerfilAsync(int professorId, AtualizarProfessorRequest request, CancellationToken cancellationToken = default);

        Task AlterarSenhaAsync(int professorId, AlterarSenhaProfessorRequest request, CancellationToken cancellationToken = default);
    }
}
