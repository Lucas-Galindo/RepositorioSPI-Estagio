using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Api.Extensions;
using SPI.Application.Notificacoes.Services;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;

namespace SPI.Api.Controllers
{
    // specs/059 (contracts/notificacoes-api.md): sino de notificacoes da professora.
    [Route("api/notificacoes")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class NotificacoesController : ControllerBase
    {
        private readonly INotificacaoService _notificacaoService;

        public NotificacoesController(INotificacaoService notificacaoService)
        {
            _notificacaoService = notificacaoService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar as notificacoes ativas da professora (sincroniza as de aula pendente antes)
        /// </summary>
        /// <returns>Retorna as notificacoes ativas, mais recentes primeiro</returns>
        public async Task<IActionResult> Listar()
        {
            try
            {
                return Ok(await _notificacaoService.ListarAtivasAsync(User.ObterUsuarioId(), DateTime.Now));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPatch("{id}/lida")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para marcar uma notificacao como lida (ela continua ativa ate a aula ser atualizada)
        /// </summary>
        /// <param name="id">Id da notificacao</param>
        public async Task<IActionResult> MarcarComoLida(int id)
        {
            try
            {
                await _notificacaoService.MarcarComoLidaAsync(User.ObterUsuarioId(), id, DateTime.Now);
                return NoContent();
            }
            catch (NaoEncontradoException e)
            {
                return NotFound(e.Message);
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
