using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Api.Extensions;
using SPI.Application.Professores.Dtos;
using SPI.Application.Professores.Services;
using SPI.Domain.Exceptions;
using SPI.Domain.Enums;

namespace SPI.Api.Controllers
{
    [Route("api/professor")]
    [ApiController]
    public class ProfessorController : ControllerBase
    {
        private readonly IProfessorService _professorService;
        private readonly IValidator<CadastroInicialProfessorRequest> _cadastroValidator;
        private readonly IValidator<AtualizarProfessorRequest> _atualizarValidator;
        private readonly IValidator<AlterarSenhaProfessorRequest> _alterarSenhaValidator;
        private readonly ILogger<ProfessorController> _logger;

        public ProfessorController(
            IProfessorService professorService,
            IValidator<CadastroInicialProfessorRequest> cadastroValidator,
            IValidator<AtualizarProfessorRequest> atualizarValidator,
            IValidator<AlterarSenhaProfessorRequest> alterarSenhaValidator,
            ILogger<ProfessorController> logger)
        {
            _professorService = professorService;
            _cadastroValidator = cadastroValidator;
            _atualizarValidator = atualizarValidator;
            _alterarSenhaValidator = alterarSenhaValidator;
            _logger = logger;
        }

        [HttpPost("cadastro-inicial")]
        [Authorize(Roles = nameof(PerfilUsuario.Admin))]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para o cadastro unico da professora, executado pelo Admin na instalacao do sistema
        /// </summary>
        /// <param name="request">Nome, CPF, Email, Senha e Telefone (opcional) da professora</param>
        /// <returns>Retorna o cadastro criado</returns>
        public async Task<IActionResult> CadastroInicial([FromBody] CadastroInicialProfessorRequest request)
        {
            try
            {
                var validacao = await _cadastroValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _professorService.CadastroInicialAsync(request);
                _logger.LogInformation("Cadastro inicial da professora criado para {Email}", request.Email);
                return CreatedAtAction(nameof(Me), response);
            }
            catch (ConflitoException e)
            {
                return Conflict(e.Message);
            }
            catch (Exception e)
            {
                return Problem(
                    title: "Erro inesperado",
                    detail: e.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpGet("me")]
        [Authorize(Roles = nameof(PerfilUsuario.Professor))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para a professora autenticada visualizar seu proprio perfil
        /// </summary>
        /// <returns>Retorna os dados da professora autenticada</returns>
        public async Task<IActionResult> Me()
        {
            try
            {
                var response = await _professorService.ObterPerfilAsync(User.ObterUsuarioId());
                return Ok(response);
            }
            catch (NaoEncontradoException e)
            {
                return NotFound(e.Message);
            }
            catch (Exception e)
            {
                return Problem(
                    title: "Erro inesperado",
                    detail: e.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpPut("me")]
        [Authorize(Roles = nameof(PerfilUsuario.Professor))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para a professora autenticada atualizar Nome, Email e/ou Telefone
        /// </summary>
        /// <param name="request">Novos dados do perfil</param>
        /// <returns>Retorna o perfil atualizado</returns>
        public async Task<IActionResult> AtualizarMe([FromBody] AtualizarProfessorRequest request)
        {
            try
            {
                var validacao = await _atualizarValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _professorService.AtualizarPerfilAsync(User.ObterUsuarioId(), request);
                _logger.LogInformation("Perfil da professora {ProfessorId} atualizado", User.ObterUsuarioId());
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
                return Problem(
                    title: "Erro inesperado",
                    detail: e.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpPut("me/senha")]
        [Authorize(Roles = nameof(PerfilUsuario.Professor))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para a professora autenticada alterar a propria senha
        /// </summary>
        /// <param name="request">Senha atual e nova senha</param>
        /// <returns>Sem conteudo em caso de sucesso</returns>
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaProfessorRequest request)
        {
            try
            {
                var validacao = await _alterarSenhaValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                await _professorService.AlterarSenhaAsync(User.ObterUsuarioId(), request);
                _logger.LogInformation("Senha alterada pela propria professora {ProfessorId}", User.ObterUsuarioId());
                return Ok();
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
                return Problem(
                    title: "Erro inesperado",
                    detail: e.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }
    }
}
