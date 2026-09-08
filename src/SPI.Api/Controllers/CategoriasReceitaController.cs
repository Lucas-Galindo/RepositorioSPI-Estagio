using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Domain.Enums;
using SPI.Domain.Repositories;

namespace SPI.Api.Controllers
{
    [Route("api/categorias-receita")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class CategoriasReceitaController : ControllerBase
    {
        private readonly ICategoriaReceitaRepository _categoriaReceitaRepository;

        public CategoriasReceitaController(ICategoriaReceitaRepository categoriaReceitaRepository)
        {
            _categoriaReceitaRepository = categoriaReceitaRepository;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar as categorias de receita ativas (Contas a Receber)
        /// </summary>
        /// <returns>Retorna a lista de categorias de receita ativas</returns>
        public async Task<IActionResult> Listar()
        {
            try
            {
                var categorias = await _categoriaReceitaRepository.ListarAtivasAsync();
                return Ok(categorias.Select(c => new { c.Id, c.Nome }));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
