using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Application.Alunos.Dtos;
using SPI.Application.Alunos.Services;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;

namespace SPI.Api.Controllers
{
    [Route("api/alunos")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class AlunosController : ControllerBase
    {
        private readonly IAlunoService _alunoService;
        private readonly IValidator<CadastrarAlunoRequest> _cadastrarValidator;
        private readonly IValidator<AtualizarAlunoRequest> _atualizarValidator;
        private readonly ILogger<AlunosController> _logger;

        public AlunosController(
            IAlunoService alunoService,
            IValidator<CadastrarAlunoRequest> cadastrarValidator,
            IValidator<AtualizarAlunoRequest> atualizarValidator,
            ILogger<AlunosController> logger)
        {
            _alunoService = alunoService;
            _cadastrarValidator = cadastrarValidator;
            _atualizarValidator = atualizarValidator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar e buscar alunos
        /// </summary>
        /// <param name="nome">Filtro por nome (busca parcial)</param>
        /// <param name="ra">Filtro por RA (busca parcial)</param>
        /// <param name="turmaId">Filtro por turma</param>
        /// <param name="ativo">Filtro por status</param>
        /// <returns>Retorna a lista de alunos que atendem aos filtros</returns>
        public async Task<IActionResult> Listar([FromQuery] string? nome, [FromQuery] string? ra, [FromQuery] int? turmaId, [FromQuery] bool? ativo)
        {
            try
            {
                return Ok(await _alunoService.ListarAsync(nome, ra, turmaId, ativo));
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
        /// Endpoint para obter um aluno por id
        /// </summary>
        /// <param name="id">Id do aluno</param>
        /// <returns>Retorna o aluno encontrado</returns>
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                return Ok(await _alunoService.ObterPorIdAsync(id));
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
        /// Endpoint para cadastrar um novo aluno (RA gerado automaticamente)
        /// </summary>
        /// <param name="request">Dados do aluno</param>
        /// <returns>Retorna o aluno cadastrado, com o RA gerado</returns>
        public async Task<IActionResult> Cadastrar([FromBody] CadastrarAlunoRequest request)
        {
            try
            {
                var validacao = await _cadastrarValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _alunoService.CadastrarAsync(request);
                _logger.LogInformation("Aluno {Nome} cadastrado com RA {Ra}", request.Nome, response.Ra);
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
        /// Endpoint para atualizar os dados de um aluno (RA nao pode ser alterado)
        /// </summary>
        /// <param name="id">Id do aluno</param>
        /// <param name="request">Novos dados do aluno</param>
        /// <returns>Retorna o aluno atualizado</returns>
        public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarAlunoRequest request)
        {
            try
            {
                var validacao = await _atualizarValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _alunoService.AtualizarAsync(id, request);
                _logger.LogInformation("Aluno {Id} atualizado", id);
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
        /// Endpoint para excluir (logicamente) um aluno, preservando historico de aulas e pagamentos
        /// </summary>
        /// <param name="id">Id do aluno</param>
        /// <returns>Sem conteudo em caso de sucesso</returns>
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                await _alunoService.ExcluirAsync(id);
                _logger.LogInformation("Aluno {Id} excluido (logicamente)", id);
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
