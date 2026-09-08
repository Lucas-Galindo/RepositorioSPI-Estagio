using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Application.Pagamentos.Dtos;
using SPI.Application.Pagamentos.Services;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;

namespace SPI.Api.Controllers
{
    [Route("api/pagamentos")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class PagamentosController : ControllerBase
    {
        private readonly IPagamentoService _pagamentoService;
        private readonly IValidator<RegistrarPagamentoRequest> _registrarValidator;
        private readonly IValidator<AtualizarPagamentoRequest> _atualizarValidator;
        private readonly IValidator<AtualizarStatusPagamentoRequest> _statusValidator;
        private readonly ILogger<PagamentosController> _logger;

        public PagamentosController(
            IPagamentoService pagamentoService,
            IValidator<RegistrarPagamentoRequest> registrarValidator,
            IValidator<AtualizarPagamentoRequest> atualizarValidator,
            IValidator<AtualizarStatusPagamentoRequest> statusValidator,
            ILogger<PagamentosController> logger)
        {
            _pagamentoService = pagamentoService;
            _registrarValidator = registrarValidator;
            _atualizarValidator = atualizarValidator;
            _statusValidator = statusValidator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar e buscar pagamentos (a base tambem do modulo Financeiro, que e uma visao dos mesmos dados)
        /// </summary>
        /// <param name="alunoId">Filtro por aluno</param>
        /// <param name="status">Filtro por status (Pendente, Pago ou Atrasado -- calculado)</param>
        /// <param name="vencimentoInicio">Filtro pela Data de Vencimento inicial do periodo</param>
        /// <param name="vencimentoFim">Filtro pela Data de Vencimento final do periodo</param>
        /// <returns>Retorna a lista de pagamentos que atendem aos filtros</returns>
        public async Task<IActionResult> Listar(
            [FromQuery] int? alunoId,
            [FromQuery] string? status,
            [FromQuery] DateOnly? vencimentoInicio,
            [FromQuery] DateOnly? vencimentoFim)
        {
            try
            {
                return Ok(await _pagamentoService.ListarAsync(alunoId, status, vencimentoInicio, vencimentoFim));
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
        /// Endpoint para obter um pagamento por id
        /// </summary>
        /// <param name="id">Id do pagamento</param>
        /// <returns>Retorna o pagamento encontrado</returns>
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                return Ok(await _pagamentoService.ObterPorIdAsync(id));
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
        /// Endpoint para registrar um pagamento, vinculando as aulas que ele cobre
        /// </summary>
        /// <param name="request">Dados do pagamento</param>
        /// <returns>Retorna o pagamento registrado</returns>
        public async Task<IActionResult> Registrar([FromBody] RegistrarPagamentoRequest request)
        {
            try
            {
                var validacao = await _registrarValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _pagamentoService.RegistrarAsync(request);
                _logger.LogInformation("Pagamento {Id} registrado para o aluno {AlunoId}", response.Id, response.AlunoId);
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
        /// Endpoint para editar os dados de uma conta a receber (descricao, categoria,
        /// forma de pagamento, vencimento, valor, competencia, observacoes).
        /// Nao altera Aluno, aulas vinculadas nem Status.
        /// </summary>
        /// <param name="id">Id do pagamento</param>
        /// <param name="request">Campos a atualizar</param>
        /// <returns>Retorna o pagamento atualizado</returns>
        public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarPagamentoRequest request)
        {
            try
            {
                var validacao = await _atualizarValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _pagamentoService.AtualizarAsync(id, request);
                _logger.LogInformation("Pagamento {Id} atualizado", id);
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

        [HttpPut("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para atualizar o status de um pagamento (ao marcar como Pago, a Data de Pagamento e preenchida automaticamente)
        /// </summary>
        /// <param name="id">Id do pagamento</param>
        /// <param name="request">Novo status</param>
        /// <returns>Retorna o pagamento atualizado</returns>
        public async Task<IActionResult> AtualizarStatus(int id, [FromBody] AtualizarStatusPagamentoRequest request)
        {
            try
            {
                var validacao = await _statusValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _pagamentoService.AtualizarStatusAsync(id, request);
                _logger.LogInformation("Status do pagamento {Id} atualizado para {Status}", id, request.Status);
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
