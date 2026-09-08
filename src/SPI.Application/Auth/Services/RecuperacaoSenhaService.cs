using Microsoft.Extensions.Logging;
using SPI.Application.Auth.Dtos;
using SPI.Application.Common;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Auth.Services
{
    public class RecuperacaoSenhaService : IRecuperacaoSenhaService
    {
        // Validade curta e fixa (Estoria 1 / prompt mestre): 15 minutos, uso unico.
        private static readonly TimeSpan ValidadeToken = TimeSpan.FromMinutes(15);

        private readonly IProfessorRepository _professorRepository;
        private readonly ISenhaResetTokenRepository _senhaResetTokenRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<RecuperacaoSenhaService> _logger;

        public RecuperacaoSenhaService(
            IProfessorRepository professorRepository,
            ISenhaResetTokenRepository senhaResetTokenRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IEmailSender emailSender,
            ILogger<RecuperacaoSenhaService> logger)
        {
            _professorRepository = professorRepository;
            _senhaResetTokenRepository = senhaResetTokenRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task EsqueciSenhaAsync(EsqueciSenhaRequest request, CancellationToken cancellationToken = default)
        {
            var professor = await _professorRepository.ObterPorEmailAsync(request.Email, cancellationToken);
            if (professor is null || !professor.Ativo)
            {
                // Silencioso de proposito: a resposta ao cliente e sempre a mesma.
                return;
            }

            var (token, tokenHash) = _tokenService.GerarTokenSeguro();

            await _senhaResetTokenRepository.AdicionarAsync(new SenhaResetToken
            {
                ProfessorId = professor.Id,
                TokenHash = tokenHash,
                CriadoEm = DateTime.UtcNow,
                ExpiraEm = DateTime.UtcNow.Add(ValidadeToken),
                Usado = false
            }, cancellationToken);
            await _senhaResetTokenRepository.SalvarAlteracoesAsync(cancellationToken);

            var corpo = $"""
                <p>Ol&aacute;, {professor.Nome}.</p>
                <p>Use o c&oacute;digo abaixo para redefinir sua senha no SPI. Ele expira em 15 minutos e s&oacute; pode ser usado uma vez.</p>
                <p style="font-size:20px;font-weight:bold;letter-spacing:1px;">{token}</p>
                <p>Se voc&ecirc; n&atilde;o solicitou essa altera&ccedil;&atilde;o, ignore este e-mail.</p>
                """;

            try
            {
                await _emailSender.EnviarAsync(professor.Email, "SPI - Recuperação de senha", corpo, cancellationToken);
            }
            catch (Exception e)
            {
                // Falha de envio nunca pode mudar a resposta observavel pelo
                // cliente (senao vira canal de enumeracao de e-mail: e-mail
                // que existe mas falha ao enviar ficaria distinguivel do
                // e-mail que nao existe). O token ja foi salvo; loga e segue.
                _logger.LogError(e, "Falha ao enviar e-mail de recuperacao de senha para o professor {ProfessorId}.", professor.Id);
            }
        }

        public async Task RedefinirSenhaAsync(RedefinirSenhaRequest request, CancellationToken cancellationToken = default)
        {
            var tokenHash = _tokenService.CalcularHash(request.Token);
            var tokenSalvo = await _senhaResetTokenRepository.ObterPorTokenHashAsync(tokenHash, cancellationToken);

            if (tokenSalvo is null || tokenSalvo.Usado || tokenSalvo.ExpiraEm <= DateTime.UtcNow)
            {
                throw new ConflitoException("Token invalido, expirado ou ja utilizado.");
            }

            var professor = await _professorRepository.ObterPorIdAtivoAsync(tokenSalvo.ProfessorId, cancellationToken)
                ?? throw new ConflitoException("Token invalido, expirado ou ja utilizado.");

            professor.Senha = _passwordHasher.Hash(request.NovaSenha);
            tokenSalvo.Usado = true;

            await _professorRepository.SalvarAlteracoesAsync(cancellationToken);

            // Forca novo login em todos os dispositivos apos a troca de senha.
            await _refreshTokenRepository.RevogarTodosDoUsuarioAsync(professor.Id, PerfilUsuario.Professor, cancellationToken);
            await _refreshTokenRepository.SalvarAlteracoesAsync(cancellationToken);
        }
    }
}
