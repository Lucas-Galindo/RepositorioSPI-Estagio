using SPI.Application.Notificacoes.Dtos;

namespace SPI.Application.Notificacoes.Services
{
    public interface INotificacaoService
    {
        // Mantem as notificacoes de todos os tipos (aula pendente, aula futura,
        // pagamento pendente); professorId nulo = todas (rotina).
        Task SincronizarAsync(DateTime agora, int? professorId, CancellationToken cancellationToken = default);

        // Sincroniza a professora e devolve as notificacoes ativas dela.
        Task<List<NotificacaoResponse>> ListarAtivasAsync(int professorId, DateTime agora, CancellationToken cancellationToken = default);

        Task MarcarComoLidaAsync(int professorId, int id, DateTime agora, CancellationToken cancellationToken = default);
    }
}
