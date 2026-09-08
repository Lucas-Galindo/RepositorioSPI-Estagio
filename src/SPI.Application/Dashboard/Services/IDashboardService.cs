using SPI.Application.Dashboard.Dtos;

namespace SPI.Application.Dashboard.Services
{
    public interface IDashboardService
    {
        Task<DashboardResponse> ObterAsync(DateOnly? periodoInicio, DateOnly? periodoFim, CancellationToken cancellationToken = default);
    }
}
