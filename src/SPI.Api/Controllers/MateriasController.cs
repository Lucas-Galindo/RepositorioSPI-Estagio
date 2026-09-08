using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Application.Materias.Dtos;
using SPI.Application.Materias.Services;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;

namespace SPI.Api.Controllers
{
    [Route("api/materias")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class MateriasController : ControllerBase
    {
        private readonly IMateriaService _materiaService;
        private readonly IValidator<MateriaRequest> _validator;
        private readonly ILogger<MateriasController> _logger;

        public MateriasController(IMateriaService materiaService, IValidator<MateriaRequest> validator, ILogger<MateriasController> logger)
        {
            _materiaService = materiaService;
            _validator = validator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar e buscar materias
        /// </summary>
        /// <param name="nome">Filtro por nome (busca parcial)</param>
        /// <param name="nivel">Filtro por nivel (busca parcial)</param>
        /// <param name="ativo">Filtro por status</param>
        /// <returns>Retorna a lista de materias que atendem aos filtros</returns>
        public async Task<IActionResult> Listar([FromQuery] string? nome, [FromQuery] string? nivel, [FromQuery] bool? ativo)
        {
            try
            {
                return Ok(await _materiaService.ListarAsync(nome, nivel, ativo));
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
        /// Endpoint para obter uma materia por id
        /// </summary>
        /// <param name="id">Id da materia</param>
        /// <returns>Retorna a materia encontrada</returns>
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                return Ok(await _materiaService.ObterPorIdAsync(id));
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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para cadastrar uma nova materia
        /// </summary>
        /// <param name="request">Nome, Nivel e Descricao da materia</param>
        /// <returns>Retorna a materia cadastrada</returns>
        public async Task<IActionResult> Cadastrar([FromBody] MateriaRequest request)
        {
            try
            {
                var validacao = await _validator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _materiaService.CadastrarAsync(request);
                _logger.LogInformation("Materia {Nome} cadastrada", request.Nome);
                return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
            }
            catch (ConflitoException e)
            {
                return Conflict(e.Message);
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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para atualizar uma materia
        /// </summary>
        /// <param name="id">Id da materia</param>
        /// <param name="request">Novos dados da materia</param>
        /// <returns>Retorna a materia atualizada</returns>
        public async Task<IActionResult> Atualizar(int id, [FromBody] MateriaRequest request)
        {
            try
            {
                var validacao = await _validator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _materiaService.AtualizarAsync(id, request);
                _logger.LogInformation("Materia {Id} atualizada", id);
                return Ok(response);
            }
            catch (NaoEncontradoException e)
            {
                return NotFound(e.Message);
            }
            catch (ConflitoException e)
            {
                return Conflict(e.Message);
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
        /// Endpoint para excluir (logicamente) uma materia
        /// </summary>
        /// <param name="id">Id da materia</param>
        /// <returns>Sem conteudo em caso de sucesso</returns>
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                await _materiaService.ExcluirAsync(id);
                _logger.LogInformation("Materia {Id} excluida (logicamente)", id);
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
