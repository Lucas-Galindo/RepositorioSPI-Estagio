using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;

namespace SPI.Api.Controllers
{
    [Route("api/alunos/{alunoId}/vinculos-cobranca")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class VinculosCobrancaController : ControllerBase
    {
        private readonly IVinculoCobrancaService _vinculoService;
        private readonly IValidator<VinculoCobrancaRequest> _validator;
        private readonly ILogger<VinculosCobrancaController> _logger;

        public VinculosCobrancaController(
            IVinculoCobrancaService vinculoService,
            IValidator<VinculoCobrancaRequest> validator,
            ILogger<VinculosCobrancaController> logger)
        {
            _vinculoService = vinculoService;
            _validator = validator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar os vinculos de cobranca de um aluno
        /// </summary>
        /// <param name="alunoId">Id do aluno</param>
        /// <param name="ativo">Filtro por status (omitido = ativos e excluidos)</param>
        /// <returns>Retorna os vinculos do aluno (ativos primeiro)</returns>
        public async Task<IActionResult> Listar(int alunoId, [FromQuery] bool? ativo)
        {
            try
            {
                return Ok(await _vinculoService.ListarPorAlunoAsync(alunoId, ativo));
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

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para obter um vinculo de cobranca de um aluno por id
        /// </summary>
        /// <param name="alunoId">Id do aluno</param>
        /// <param name="id">Id do vinculo</param>
        /// <returns>Retorna o vinculo encontrado</returns>
        public async Task<IActionResult> ObterPorId(int alunoId, int id)
        {
            try
            {
                return Ok(await _vinculoService.ObterPorIdAsync(alunoId, id));
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
        /// Endpoint para cadastrar um vinculo de cobranca (turma do aluno ou atendimento individual)
        /// </summary>
        /// <param name="alunoId">Id do aluno</param>
        /// <param name="request">Turma (opcional), modalidade, valor e campo especifico da modalidade</param>
        /// <returns>Retorna o vinculo cadastrado</returns>
        public async Task<IActionResult> Cadastrar(int alunoId, [FromBody] VinculoCobrancaRequest request)
        {
            try
            {
                var validacao = await _validator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _vinculoService.CadastrarAsync(alunoId, request);
                _logger.LogInformation("Vinculo de cobranca {Id} cadastrado para o aluno {AlunoId}", response.Id, alunoId);
                return CreatedAtAction(nameof(ObterPorId), new { alunoId, id = response.Id }, response);
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
        /// Endpoint para atualizar um vinculo de cobranca ativo (todos os campos)
        /// </summary>
        /// <param name="alunoId">Id do aluno</param>
        /// <param name="id">Id do vinculo</param>
        /// <param name="request">Turma (opcional), modalidade, valor e campo especifico da modalidade</param>
        /// <returns>Retorna o vinculo atualizado</returns>
        public async Task<IActionResult> Atualizar(int alunoId, int id, [FromBody] VinculoCobrancaRequest request)
        {
            try
            {
                var validacao = await _validator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _vinculoService.AtualizarAsync(alunoId, id, request);
                _logger.LogInformation("Vinculo de cobranca {Id} do aluno {AlunoId} atualizado", id, alunoId);
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
        /// Endpoint para excluir (logicamente) um vinculo de cobranca
        /// </summary>
        /// <param name="alunoId">Id do aluno</param>
        /// <param name="id">Id do vinculo</param>
        /// <returns>Sem conteudo em caso de sucesso</returns>
        public async Task<IActionResult> Excluir(int alunoId, int id)
        {
            try
            {
                await _vinculoService.ExcluirAsync(alunoId, id);
                _logger.LogInformation("Vinculo de cobranca {Id} do aluno {AlunoId} excluido (logicamente)", id, alunoId);
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

        [HttpPatch("{id}/reativar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para reativar um vinculo de cobranca excluido
        /// </summary>
        /// <param name="alunoId">Id do aluno</param>
        /// <param name="id">Id do vinculo</param>
        /// <returns>O vinculo reativado; 409 se ja existir outro ativo para a mesma combinacao</returns>
        public async Task<IActionResult> Reativar(int alunoId, int id)
        {
            try
            {
                var response = await _vinculoService.ReativarAsync(alunoId, id);
                _logger.LogInformation("Vinculo de cobranca {Id} do aluno {AlunoId} reativado", id, alunoId);
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
