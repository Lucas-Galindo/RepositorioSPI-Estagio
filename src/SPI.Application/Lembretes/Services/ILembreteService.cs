using SPI.Application.Lembretes.Dtos;

namespace SPI.Application.Lembretes.Services
{
    public interface ILembreteService
    {
        Task<List<LembreteResponse>> ListarAsync(int? turmaId, string? status, bool? ativo, CancellationToken cancellationToken = default);

        Task<LembreteResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        Task<LembreteResponse> CadastrarAsync(LembreteRequest request, CancellationToken cancellationToken = default);

        Task<LembreteResponse> AtualizarAsync(int id, LembreteRequest request, CancellationToken cancellationToken = default);

        Task ExcluirAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recalcula a Hora Programada de todos os lembretes ativos da turma
        /// a partir da proxima aula agendada (Estoria 5, fluxo de recorrencia).
        /// Chamado pelo AulaService sempre que uma aula de turma e criada,
        /// alterada ou cancelada.
        /// </summary>
        Task RecalcularAsync(int turmaId, CancellationToken cancellationToken = default);
    }
}
