using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SPI.Application.Auth.Dtos;
using SPI.Application.Auth.Services;
using SPI.Domain.Exceptions;

namespace SPI.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        //significa que um campo só pode ter seu valor atribuído na declaração ou dentro de um construtor da mesma classe
        //o construtor termina de rodar, o valor não pode mais ser alterado
        private readonly IAutenticacaoService _autenticacaoService;
        private readonly IRecuperacaoSenhaService _recuperacaoSenhaService;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly IValidator<RefreshRequest> _refreshValidator;
        private readonly IValidator<LogoutRequest> _logoutValidator;
        private readonly IValidator<EsqueciSenhaRequest> _esqueciSenhaValidator;
        private readonly IValidator<RedefinirSenhaRequest> _redefinirSenhaValidator;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAutenticacaoService autenticacaoService,
            IRecuperacaoSenhaService recuperacaoSenhaService,
            IValidator<LoginRequest> loginValidator,
            IValidator<RefreshRequest> refreshValidator,
            IValidator<LogoutRequest> logoutValidator,
            IValidator<EsqueciSenhaRequest> esqueciSenhaValidator,
            IValidator<RedefinirSenhaRequest> redefinirSenhaValidator,
            ILogger<AuthController> logger)
        {
            _autenticacaoService = autenticacaoService;
            _recuperacaoSenhaService = recuperacaoSenhaService;
            _loginValidator = loginValidator;
            _refreshValidator = refreshValidator;
            _logoutValidator = logoutValidator;
            _esqueciSenhaValidator = esqueciSenhaValidator;
            _redefinirSenhaValidator = redefinirSenhaValidator;
            _logger = logger;
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para autenticar um Professor, Aluno ou Admin
        /// </summary>
        /// <param name="request">E-mail (ou RA, para aluno) e senha; o perfil e identificado automaticamente</param>
        /// <returns>Retorna o perfil identificado, o access token e o refresh token emitidos</returns>
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var validacao = await _loginValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _autenticacaoService.LoginAsync(request, ObterIpOrigem(), ObterUserAgent());
                _logger.LogInformation("Login bem-sucedido para {Login}, identificado como {Perfil}", request.Login, response.Perfil);
                return Ok(response);
            }
            catch (CredenciaisInvalidasException e)
            {
                _logger.LogWarning("Tentativa de login falhou para {Login}", request.Login);
                return Unauthorized(e.Message);
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

        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para trocar um refresh token valido por um novo par de tokens
        /// </summary>
        /// <param name="request">Refresh token emitido em um login ou refresh anterior</param>
        /// <returns>Retorna o novo access token e o novo refresh token</returns>
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            try
            {
                var validacao = await _refreshValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                var response = await _autenticacaoService.RefreshAsync(request, ObterIpOrigem(), ObterUserAgent());
                _logger.LogInformation("Refresh token trocado com sucesso (fragmento {Fragmento})", FragmentoToken(request.RefreshToken));
                return Ok(response);
            }
            catch (RefreshTokenInvalidoException e)
            {
                _logger.LogWarning("Tentativa de refresh com token invalido (fragmento {Fragmento})", FragmentoToken(request.RefreshToken));
                return Unauthorized(e.Message);
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

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para revogar o refresh token da sessao/dispositivo atual
        /// </summary>
        /// <param name="request">Refresh token a ser revogado</param>
        /// <returns>Sem conteudo em caso de sucesso</returns>
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                var validacao = await _logoutValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                await _autenticacaoService.LogoutAsync(request);
                _logger.LogInformation("Logout realizado com sucesso (fragmento {Fragmento})", FragmentoToken(request.RefreshToken));
                return Ok();
            }
            catch (RefreshTokenInvalidoException e)
            {
                _logger.LogWarning("Tentativa de logout com token invalido (fragmento {Fragmento})", FragmentoToken(request.RefreshToken));
                return Unauthorized(e.Message);
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

        [HttpPost("esqueci-senha")]
        [EnableRateLimiting("esqueci-senha")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para solicitar a recuperacao de senha da Professora (unico perfil com esse fluxo)
        /// </summary>
        /// <param name="request">E-mail da professora</param>
        /// <returns>Sempre uma mensagem generica, para nao revelar se o e-mail existe</returns>
        public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest request)
        {
            try
            {
                var validacao = await _esqueciSenhaValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                await _recuperacaoSenhaService.EsqueciSenhaAsync(request);
                _logger.LogInformation("Solicitacao de recuperacao de senha processada para {Email}", request.Email);
                return Ok(new { mensagem = "Se o e-mail informado estiver cadastrado, enviaremos instrucoes de recuperacao." });
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

        [HttpPost("redefinir-senha")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para redefinir a senha da Professora usando o token recebido por e-mail
        /// </summary>
        /// <param name="request">Token de recuperacao e nova senha</param>
        /// <returns>Sem conteudo em caso de sucesso</returns>
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequest request)
        {
            try
            {
                var validacao = await _redefinirSenhaValidator.ValidateAsync(request);
                if (!validacao.IsValid)
                {
                    return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
                }

                await _recuperacaoSenhaService.RedefinirSenhaAsync(request);
                _logger.LogInformation("Senha redefinida com sucesso via token (fragmento {Fragmento})", FragmentoToken(request.Token));
                return Ok();
            }
            catch (ConflitoException e)
            {
                _logger.LogWarning("Tentativa de redefinicao de senha com token invalido (fragmento {Fragmento})", FragmentoToken(request.Token));
                return BadRequest(e.Message);
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

        private string? ObterIpOrigem() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private string? ObterUserAgent() => Request.Headers.UserAgent.ToString();

        // Nunca logar o token completo (ver prompt mestre); so os ultimos
        // caracteres, suficiente para correlacionar logs sem expor o segredo.
        private static string FragmentoToken(string token) =>
            token.Length <= 6 ? "***" : $"...{token[^6..]}";
    }
}
