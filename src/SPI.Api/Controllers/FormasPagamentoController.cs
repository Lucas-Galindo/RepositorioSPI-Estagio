using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Domain.Enums;
using SPI.Domain.Repositories;

namespace SPI.Api.Controllers
{
    [Route("api/formas-pagamento")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class FormasPagamentoController : ControllerBase
    {
        private readonly IFormaPagamentoRepository _formaPagamentoRepository;

        public FormasPagamentoController(IFormaPagamentoRepository formaPagamentoRepository)
        {
            _formaPagamentoRepository = formaPagamentoRepository;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar as formas de pagamento ativas (Pix, Dinheiro, Cartao, Transferencia)
        /// </summary>
        /// <returns>Retorna a lista de formas de pagamento ativas</returns>
        public async Task<IActionResult> Listar()
        {
            try
            {
                var formas = await _formaPagamentoRepository.ListarAtivasAsync();
                return Ok(formas.Select(f => new { f.Id, f.Forma, f.Descricao }));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
