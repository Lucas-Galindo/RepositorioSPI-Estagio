using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Application.Lembretes.Dtos;
using SPI.Application.Lembretes.Services;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;

namespace SPI.Api.Controllers
{
    [Route("api/lembretes")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class LembretesController : ControllerBase
    {
        private readonly ILembreteService _lembreteService;
        private readonly IValidator<LembreteRequest> _validator;
        private readonly ILogger<LembretesController> _logger;

        public LembretesController(ILembreteService lembreteService, IValidator<LembreteRequest> validator, ILogger<LembretesController> logger)
        {
            _lembreteService = lembreteService;
            _validator = validator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar e buscar lembretes
        /// </summary>
        /// <param name="turmaId">Filtro por turma</param>
        /// <param name="status">Filtro por status (Pendente, Enviado, Falha, Cancelado)</param>
        /// <param name="ativo">Filtro por status de atividade</param>
        /// <returns>Retorna a lista de lembretes que atendem aos filtros</returns>
        public async Task<IActionResult> Listar([FromQuery] int? turmaId, [FromQuery] string? status, [FromQuery] bool? ativo)
        {
            try
            {
                return Ok(await _lembreteService.ListarAsync(turmaId, status, ativo));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para obter um lembrete por id
        /// </summary>
        /// <param name="id">Id do lembrete</param>
        /// <returns>Retorna o lembrete encontrado</returns>
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                return Ok(await _lembreteService.ObterPorIdAsync(id));
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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para cadastrar um novo lembrete para uma turma
        /// </summary>
        /// <param name="request">Turma, Canal, Antecedencia e Destinatarios do lembrete</param>
        /// <returns>Retorna o lembrete cadastrado, com a Hora Programada calculada</returns>
        public async Task<IActionResult> Cadastrar([FromBody] LembreteRequest request)
        {
            try
            {
                var validacao = await _validator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _lembreteService.CadastrarAsync(request);
                _logger.LogInformation("Lembrete {Id} cadastrado para a turma {TurmaId}", response.Id, response.TurmaId);
                return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
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

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para atualizar Canal, Antecedencia e/ou Destinatarios de um lembrete
        /// </summary>
        /// <param name="id">Id do lembrete</param>
        /// <param name="request">Novos dados do lembrete</param>
        /// <returns>Retorna o lembrete atualizado</returns>
        public async Task<IActionResult> Atualizar(int id, [FromBody] LembreteRequest request)
        {
            try
            {
                var validacao = await _validator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _lembreteService.AtualizarAsync(id, request);
                _logger.LogInformation("Lembrete {Id} atualizado", id);
                return Ok(response);
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

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para excluir (logicamente) um lembrete
        /// </summary>
        /// <param name="id">Id do lembrete</param>
        /// <returns>Sem conteudo em caso de sucesso</returns>
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                await _lembreteService.ExcluirAsync(id);
                _logger.LogInformation("Lembrete {Id} excluido (logicamente)", id);
                return Ok();
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
