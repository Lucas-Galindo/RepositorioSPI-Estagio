using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Application.Financeiro.Services;
using SPI.Domain.Enums;

namespace SPI.Api.Controllers
{
    [Route("api/financeiro")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class FinanceiroController : ControllerBase
    {
        private readonly IFinanceiroService _financeiroService;

        public FinanceiroController(IFinanceiroService financeiroService)
        {
            _financeiroService = financeiroService;
        }

        [HttpGet("visao-geral")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para obter a Visao Geral Financeira: receitas (recebido, a
        /// receber, atrasado), despesas (pago, a pagar, atrasado), saldo
        /// realizado/previsto e proximos vencimentos (7 dias) combinando
        /// Contas a Receber e Contas a Pagar.
        /// </summary>
        /// <param name="periodoInicio">Inicio do periodo considerado para recebido/pago (padrao: inicio do mes corrente)</param>
        /// <param name="periodoFim">Fim do periodo considerado (padrao: fim do mes corrente)</param>
        /// <returns>Retorna a visao geral consolidada</returns>
        public async Task<IActionResult> ObterVisaoGeral([FromQuery] DateOnly? periodoInicio, [FromQuery] DateOnly? periodoFim)
        {
            try
            {
                return Ok(await _financeiroService.ObterVisaoGeralAsync(periodoInicio, periodoFim));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
