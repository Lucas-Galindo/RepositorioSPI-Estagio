using SPI.Application.Dashboard.Dtos;

namespace SPI.Application.Dashboard.Services
{
    public interface IDashboardService
    {
        Task<DashboardResponse> ObterAsync(DateOnly? periodoInicio, DateOnly? periodoFim, int? alunoId, CancellationToken cancellationToken = default);
    }
}
