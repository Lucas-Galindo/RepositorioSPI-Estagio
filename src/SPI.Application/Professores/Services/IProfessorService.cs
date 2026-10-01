using SPI.Application.Professores.Dtos;

namespace SPI.Application.Professores.Services
{
    public interface IProfessorService
    {
        Task<ProfessorResponse> CadastroInicialAsync(CadastroInicialProfessorRequest request, CancellationToken cancellationToken = default);

        Task<ProfessorResponse> ObterPerfilAsync(int professorId, CancellationToken cancellationToken = default);

        Task<ProfessorResponse> AtualizarPerfilAsync(int professorId, AtualizarProfessorRequest request, CancellationToken cancellationToken = default);

        Task AlterarSenhaAsync(int professorId, AlterarSenhaProfessorRequest request, CancellationToken cancellationToken = default);

        // Sistema e single-tenant: usados pelo Admin para ver/editar a
        // professora ja cadastrada (Sprint 5 da evolucao de Admin).
        Task<ProfessorResponse?> ObterUnicaAsync(CancellationToken cancellationToken = default);

        Task<ProfessorResponse> AtualizarComoAdminAsync(AtualizarProfessorRequest request, CancellationToken cancellationToken = default);

        // Exclusao logica com 2FA por e-mail (spec 044): o codigo de
        // verificacao e sempre enviado ao Admin autenticado que solicita,
        // nunca a professora-alvo.
        Task SolicitarExclusaoAsync(int adminId, CancellationToken cancellationToken = default);

        Task ConfirmarExclusaoAsync(int adminId, string codigo, CancellationToken cancellationToken = default);

        Task<ProfessorResponse> ReativarAsync(CancellationToken cancellationToken = default);
    }
}
