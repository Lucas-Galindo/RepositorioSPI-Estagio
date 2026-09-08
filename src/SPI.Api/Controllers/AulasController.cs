using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Api.Extensions;
using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;

namespace SPI.Api.Controllers
{
    [Route("api/aulas")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class AulasController : ControllerBase
    {
        private readonly IAulaService _aulaService;
        private readonly IValidator<AulaRequest> _aulaValidator;
        private readonly IValidator<RegistrarSessaoRequest> _sessaoValidator;
        private readonly ILogger<AulasController> _logger;

        public AulasController(
            IAulaService aulaService,
            IValidator<AulaRequest> aulaValidator,
            IValidator<RegistrarSessaoRequest> sessaoValidator,
            ILogger<AulasController> logger)
        {
            _aulaService = aulaService;
            _aulaValidator = aulaValidator;
            _sessaoValidator = sessaoValidator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar a agenda de aulas, com filtros
        /// </summary>
        /// <param name="status">Filtro por status (Agendada, Realizada, Cancelada)</param>
        /// <param name="turmaId">Filtro por turma</param>
        /// <param name="alunoId">Filtro por aluno</param>
        /// <param name="dataInicio">Filtro por data inicial do periodo</param>
        /// <param name="dataFim">Filtro por data final do periodo</param>
        /// <returns>Retorna a lista de aulas que atendem aos filtros</returns>
        public async Task<IActionResult> Listar(
            [FromQuery] string? status,
            [FromQuery] int? turmaId,
            [FromQuery] int? alunoId,
            [FromQuery] DateOnly? dataInicio,
            [FromQuery] DateOnly? dataFim)
        {
            try
            {
                return Ok(await _aulaService.ListarAsync(status, turmaId, alunoId, dataInicio, dataFim));
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
        /// Endpoint para obter uma aula por id, com os alunos vinculados e presenca
        /// </summary>
        /// <param name="id">Id da aula</param>
        /// <returns>Retorna a aula encontrada</returns>
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                return Ok(await _aulaService.ObterPorIdAsync(id));
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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para cadastrar uma nova aula (individual ou de turma), validando conflito de horario
        /// </summary>
        /// <param name="request">Dados da aula</param>
        /// <returns>Retorna a aula cadastrada com Status = Agendada</returns>
        public async Task<IActionResult> Cadastrar([FromBody] AulaRequest request)
        {
            try
            {
                var validacao = await _aulaValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _aulaService.CadastrarAsync(User.ObterUsuarioId(), request);
                _logger.LogInformation("Aula {Id} cadastrada para {Data} {HoraInicio}-{HoraFim}", response.Id, response.DataInicio, response.HoraInicio, response.HoraFim);
                return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
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

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para atualizar uma aula, revalidando conflito de horario
        /// </summary>
        /// <param name="id">Id da aula</param>
        /// <param name="request">Novos dados da aula</param>
        /// <returns>Retorna a aula atualizada</returns>
        public async Task<IActionResult> Atualizar(int id, [FromBody] AulaRequest request)
        {
            try
            {
                var validacao = await _aulaValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _aulaService.AtualizarAsync(id, request);
                _logger.LogInformation("Aula {Id} atualizada", id);
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
        /// Endpoint para excluir (logicamente) uma aula
        /// </summary>
        /// <param name="id">Id da aula</param>
        /// <returns>Sem conteudo em caso de sucesso</returns>
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                await _aulaService.ExcluirAsync(id);
                _logger.LogInformation("Aula {Id} excluida (logicamente)", id);
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

        [HttpPost("{id}/registrar-sessao")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para registrar a realizacao de uma aula, marcando presenca/falta de cada aluno
        /// </summary>
        /// <param name="id">Id da aula</param>
        /// <param name="request">Presenca de cada aluno vinculado</param>
        /// <returns>Retorna a aula com Status = Realizada e a frequencia atualizada</returns>
        public async Task<IActionResult> RegistrarSessao(int id, [FromBody] RegistrarSessaoRequest request)
        {
            try
            {
                var validacao = await _sessaoValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _aulaService.RegistrarSessaoAsync(id, request);
                _logger.LogInformation("Sessao da aula {Id} registrada", id);
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

        [HttpPost("{id}/cancelar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para cancelar uma aula (a frequencia dos alunos nao e alterada)
        /// </summary>
        /// <param name="id">Id da aula</param>
        /// <returns>Retorna a aula com Status = Cancelada</returns>
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                var response = await _aulaService.CancelarAsync(id);
                _logger.LogInformation("Aula {Id} cancelada", id);
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
    }
}
