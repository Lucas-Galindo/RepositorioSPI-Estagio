using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Application.ContasPagar.Dtos;
using SPI.Application.ContasPagar.Services;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;

namespace SPI.Api.Controllers
{
    [Route("api/contas-pagar")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class ContasPagarController : ControllerBase
    {
        private readonly IContaPagarService _contaPagarService;
        private readonly IValidator<RegistrarContaPagarRequest> _registrarValidator;
        private readonly IValidator<AtualizarContaPagarRequest> _atualizarValidator;
        private readonly IValidator<AtualizarStatusContaPagarRequest> _statusValidator;
        private readonly ILogger<ContasPagarController> _logger;

        public ContasPagarController(
            IContaPagarService contaPagarService,
            IValidator<RegistrarContaPagarRequest> registrarValidator,
            IValidator<AtualizarContaPagarRequest> atualizarValidator,
            IValidator<AtualizarStatusContaPagarRequest> statusValidator,
            ILogger<ContasPagarController> logger)
        {
            _contaPagarService = contaPagarService;
            _registrarValidator = registrarValidator;
            _atualizarValidator = atualizarValidator;
            _statusValidator = statusValidator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar e buscar contas a pagar (despesas da professora)
        /// </summary>
        /// <param name="categoriaDespesaId">Filtro por categoria de despesa</param>
        /// <param name="status">Filtro por status (Pendente, Pago, Atrasado -- calculado, ou Cancelado)</param>
        /// <param name="favorecido">Filtro por fornecedor/favorecido (busca parcial)</param>
        /// <param name="vencimentoInicio">Filtro pela Data de Vencimento inicial do periodo</param>
        /// <param name="vencimentoFim">Filtro pela Data de Vencimento final do periodo</param>
        /// <returns>Retorna a lista de contas a pagar que atendem aos filtros</returns>
        public async Task<IActionResult> Listar(
            [FromQuery] int? categoriaDespesaId,
            [FromQuery] string? status,
            [FromQuery] string? favorecido,
            [FromQuery] DateOnly? vencimentoInicio,
            [FromQuery] DateOnly? vencimentoFim)
        {
            try
            {
                return Ok(await _contaPagarService.ListarAsync(categoriaDespesaId, status, favorecido, vencimentoInicio, vencimentoFim));
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
        /// Endpoint para obter uma conta a pagar por id
        /// </summary>
        /// <param name="id">Id da conta a pagar</param>
        /// <returns>Retorna a conta a pagar encontrada</returns>
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                return Ok(await _contaPagarService.ObterPorIdAsync(id));
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
        /// Endpoint para registrar uma conta a pagar (despesa)
        /// </summary>
        /// <param name="request">Dados da conta a pagar</param>
        /// <returns>Retorna a conta a pagar registrada</returns>
        public async Task<IActionResult> Registrar([FromBody] RegistrarContaPagarRequest request)
        {
            try
            {
                var validacao = await _registrarValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _contaPagarService.RegistrarAsync(request);
                _logger.LogInformation("Conta a pagar {Id} registrada", response.Id);
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
        /// Endpoint para editar os dados de uma conta a pagar (descricao, categoria,
        /// favorecido, forma de pagamento, vencimento, valor, competencia, observacoes).
        /// Nao altera Status.
        /// </summary>
        /// <param name="id">Id da conta a pagar</param>
        /// <param name="request">Campos a atualizar</param>
        /// <returns>Retorna a conta a pagar atualizada</returns>
        public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarContaPagarRequest request)
        {
            try
            {
                var validacao = await _atualizarValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _contaPagarService.AtualizarAsync(id, request);
                _logger.LogInformation("Conta a pagar {Id} atualizada", id);
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
        /// Endpoint para atualizar o status de uma conta a pagar (ao marcar como Pago,
        /// a Data de Pagamento e preenchida automaticamente; Cancelado encerra a conta sem exclusao fisica)
        /// </summary>
        /// <param name="id">Id da conta a pagar</param>
        /// <param name="request">Novo status</param>
        /// <returns>Retorna a conta a pagar atualizada</returns>
        public async Task<IActionResult> AtualizarStatus(int id, [FromBody] AtualizarStatusContaPagarRequest request)
        {
            try
            {
                var validacao = await _statusValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _contaPagarService.AtualizarStatusAsync(id, request);
                _logger.LogInformation("Status da conta a pagar {Id} atualizado para {Status}", id, request.Status);
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
