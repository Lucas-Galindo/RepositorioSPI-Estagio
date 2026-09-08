using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Domain.Enums;
using SPI.Domain.Repositories;

namespace SPI.Api.Controllers
{
    [Route("api/categorias-despesa")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class CategoriasDespesaController : ControllerBase
    {
        private readonly ICategoriaDespesaRepository _categoriaDespesaRepository;

        public CategoriasDespesaController(ICategoriaDespesaRepository categoriaDespesaRepository)
        {
            _categoriaDespesaRepository = categoriaDespesaRepository;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar as categorias de despesa ativas (Contas a Pagar)
        /// </summary>
        /// <returns>Retorna a lista de categorias de despesa ativas</returns>
        public async Task<IActionResult> Listar()
        {
            try
            {
                var categorias = await _categoriaDespesaRepository.ListarAtivasAsync();
                return Ok(categorias.Select(c => new { c.Id, c.Nome }));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
