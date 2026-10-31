using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Enums;

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
            return Ok(await _vinculoService.ListarPorAlunoAsync(alunoId, ativo));
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
            return Ok(await _vinculoService.ObterPorIdAsync(alunoId, id));
        }

        [HttpGet("{id}/aulas")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar as aulas que contaram como dadas num pacote (specs/058)
        /// </summary>
        /// <param name="alunoId">Id do aluno</param>
        /// <param name="id">Id do vinculo (pacote)</param>
        /// <returns>Retorna as aulas, mais recentes primeiro</returns>
        public async Task<IActionResult> ListarAulasDoPacote(int alunoId, int id)
        {
            return Ok(await _vinculoService.ListarAulasDoPacoteAsync(alunoId, id));
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
            var validacao = await _validator.ValidateAsync(request);
            if (!validacao.IsValid)
            {
                return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
            }

            var response = await _vinculoService.CadastrarAsync(alunoId, request);
            _logger.LogInformation("Vinculo de cobranca {Id} cadastrado para o aluno {AlunoId}", response.Id, alunoId);
            return CreatedAtAction(nameof(ObterPorId), new { alunoId, id = response.Id }, response);
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
            var validacao = await _validator.ValidateAsync(request);
            if (!validacao.IsValid)
            {
                return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
            }

            var response = await _vinculoService.AtualizarAsync(alunoId, id, request);
            _logger.LogInformation("Vinculo de cobranca {Id} do aluno {AlunoId} atualizado", id, alunoId);
            return Ok(response);
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
            await _vinculoService.ExcluirAsync(alunoId, id);
            _logger.LogInformation("Vinculo de cobranca {Id} do aluno {AlunoId} excluido (logicamente)", id, alunoId);
            return Ok();
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
            var response = await _vinculoService.ReativarAsync(alunoId, id);
            _logger.LogInformation("Vinculo de cobranca {Id} do aluno {AlunoId} reativado", id, alunoId);
            return Ok(response);
        }
    }
}
