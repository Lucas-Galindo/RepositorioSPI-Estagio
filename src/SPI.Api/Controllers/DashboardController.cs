using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Application.Dashboard.Services;
using SPI.Domain.Enums;

namespace SPI.Api.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para obter os indicadores resumidos do dashboard (Estoria 11)
        /// </summary>
        /// <param name="periodoInicio">Inicio do periodo considerado para aulas/faturamento (padrao: inicio do mes corrente)</param>
        /// <param name="periodoFim">Fim do periodo considerado (padrao: fim do mes corrente)</param>
        /// <returns>Retorna os indicadores consolidados em tempo real</returns>
        public async Task<IActionResult> Obter([FromQuery] DateOnly? periodoInicio, [FromQuery] DateOnly? periodoFim)
        {
            try
            {
                return Ok(await _dashboardService.ObterAsync(periodoInicio, periodoFim));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
