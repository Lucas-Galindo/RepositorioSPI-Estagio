using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Api.Extensions;
using SPI.Application.Turmas.Dtos;
using SPI.Application.Turmas.Services;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;

namespace SPI.Api.Controllers
{
    [Route("api/turmas")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class TurmasController : ControllerBase
    {
        private readonly ITurmaService _turmaService;
        private readonly IValidator<TurmaRequest> _validator;
        private readonly ILogger<TurmasController> _logger;

        public TurmasController(ITurmaService turmaService, IValidator<TurmaRequest> validator, ILogger<TurmasController> logger)
        {
            _turmaService = turmaService;
            _validator = validator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar e buscar turmas
        /// </summary>
        /// <param name="nome">Filtro por nome (busca parcial)</param>
        /// <param name="ativo">Filtro por status</param>
        /// <returns>Retorna a lista de turmas que atendem aos filtros</returns>
        public async Task<IActionResult> Listar([FromQuery] string? nome, [FromQuery] bool? ativo)
        {
            try
            {
                return Ok(await _turmaService.ListarAsync(nome, ativo));
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
        /// Endpoint para obter uma turma por id, com os alunos vinculados
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <returns>Retorna a turma encontrada</returns>
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                return Ok(await _turmaService.ObterPorIdAsync(id));
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
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para cadastrar uma nova turma (professora associada automaticamente)
        /// </summary>
        /// <param name="request">Nome da turma</param>
        /// <returns>Retorna a turma cadastrada</returns>
        public async Task<IActionResult> Cadastrar([FromBody] TurmaRequest request)
        {
            try
            {
                var validacao = await _validator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _turmaService.CadastrarAsync(User.ObterUsuarioId(), request);
                _logger.LogInformation("Turma {Nome} cadastrada", request.Nome);
                return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
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
        /// Endpoint para atualizar o nome de uma turma
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <param name="request">Novo nome da turma</param>
        /// <returns>Retorna a turma atualizada</returns>
        public async Task<IActionResult> Atualizar(int id, [FromBody] TurmaRequest request)
        {
            try
            {
                var validacao = await _validator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _turmaService.AtualizarAsync(id, request);
                _logger.LogInformation("Turma {Id} atualizada", id);
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
        /// Endpoint para excluir (logicamente) uma turma
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <returns>Sem conteudo em caso de sucesso</returns>
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                await _turmaService.ExcluirAsync(id);
                _logger.LogInformation("Turma {Id} excluida (logicamente)", id);
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

        [HttpPost("{id}/alunos/{alunoId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para vincular um aluno a uma turma
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <param name="alunoId">Id do aluno</param>
        /// <returns>Retorna a turma atualizada com o novo aluno</returns>
        public async Task<IActionResult> VincularAluno(int id, int alunoId)
        {
            try
            {
                var response = await _turmaService.VincularAlunoAsync(id, alunoId);
                _logger.LogInformation("Aluno {AlunoId} vinculado a turma {TurmaId}", alunoId, id);
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

        [HttpDelete("{id}/alunos/{alunoId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para desvincular um aluno de uma turma
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <param name="alunoId">Id do aluno</param>
        /// <returns>Retorna a turma atualizada</returns>
        public async Task<IActionResult> DesvincularAluno(int id, int alunoId)
        {
            try
            {
                var response = await _turmaService.DesvincularAlunoAsync(id, alunoId);
                _logger.LogInformation("Aluno {AlunoId} desvinculado da turma {TurmaId}", alunoId, id);
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
    }
}
