using SPI.Application.Auth.Dtos;
using SPI.Application.Common;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Auth.Services
{
    public class AutenticacaoService : IAutenticacaoService
    {
        private readonly IProfessorRepository _professorRepository;
        private readonly IAlunoRepository _alunoRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public AutenticacaoService(
            IProfessorRepository professorRepository,
            IAlunoRepository alunoRepository,
            IAdminRepository adminRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService)
        {
            _professorRepository = professorRepository;
            _alunoRepository = alunoRepository;
            _adminRepository = adminRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipOrigem, string? userAgent, CancellationToken cancellationToken = default)
        {
            var (perfil, usuarioId, nome, email) = await IdentificarUsuarioAsync(request.Login, cancellationToken);

            switch (perfil)
            {
                case PerfilUsuario.Professor:
                    var professor = await _professorRepository.ObterPorEmailAsync(request.Login, cancellationToken);
                    ValidarCredenciais(professor?.Ativo, professor?.Senha, request.Senha);
                    break;
                case PerfilUsuario.Aluno:
                    var aluno = request.Login.Contains('@')
                        ? await _alunoRepository.ObterPorEmailAsync(request.Login, cancellationToken)
                        : await _alunoRepository.ObterPorRaAsync(request.Login, cancellationToken);
                    ValidarCredenciais(aluno?.Ativo, aluno?.Senha, request.Senha);
                    break;
                case PerfilUsuario.Admin:
                    var admin = await _adminRepository.ObterPorEmailAsync(request.Login, cancellationToken);
                    ValidarCredenciais(admin?.Ativo, admin?.Senha, request.Senha);
                    break;
            }

            return await EmitirTokensAsync(usuarioId, perfil, nome, email, ipOrigem, userAgent, cancellationToken);
        }

        // O login nao pede o perfil explicitamente: o usuario informa e-mail
        // (Professor/Admin/Aluno) ou RA (Aluno) e o sistema descobre sozinho
        // o dono da credencial, tentando nesta ordem: Professor, Aluno, Admin
        // (por e-mail), ou so Aluno (por RA, quando o Login nao contem "@").
        private async Task<(PerfilUsuario Perfil, int UsuarioId, string Nome, string? Email)> IdentificarUsuarioAsync(string login, CancellationToken cancellationToken)
        {
            if (login.Contains('@'))
            {
                var professor = await _professorRepository.ObterPorEmailAsync(login, cancellationToken);
                if (professor is not null)
                {
                    return (PerfilUsuario.Professor, professor.Id, professor.Nome, professor.Email);
                }

                var aluno = await _alunoRepository.ObterPorEmailAsync(login, cancellationToken);
                if (aluno is not null)
                {
                    return (PerfilUsuario.Aluno, aluno.Id, aluno.Nome, aluno.Email);
                }

                var admin = await _adminRepository.ObterPorEmailAsync(login, cancellationToken);
                if (admin is not null)
                {
                    return (PerfilUsuario.Admin, admin.Id, admin.Nome, admin.Email);
                }
            }
            else
            {
                var aluno = await _alunoRepository.ObterPorRaAsync(login, cancellationToken);
                if (aluno is not null)
                {
                    return (PerfilUsuario.Aluno, aluno.Id, aluno.Nome, aluno.Email);
                }
            }

            throw new CredenciaisInvalidasException();
        }

        public async Task<LoginResponse> RefreshAsync(RefreshRequest request, string? ipOrigem, string? userAgent, CancellationToken cancellationToken = default)
        {
            var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
            var tokenAtual = await _refreshTokenRepository.ObterPorTokenHashAsync(tokenHash, cancellationToken);

            if (tokenAtual is null || !tokenAtual.EstaAtivo)
            {
                throw new RefreshTokenInvalidoException();
            }

            var (nome, email) = await ObterDadosUsuarioAsync(tokenAtual.UsuarioId, tokenAtual.Perfil, cancellationToken)
                ?? throw new RefreshTokenInvalidoException();

            var response = await EmitirTokensAsync(tokenAtual.UsuarioId, tokenAtual.Perfil, nome, email, ipOrigem, userAgent, cancellationToken);

            // Rotacao: o refresh token antigo e revogado e aponta para o novo, nunca reutilizavel.
            tokenAtual.RevogadoEm = DateTime.UtcNow;
            tokenAtual.SubstituidoPorTokenHash = _tokenService.HashRefreshToken(response.RefreshToken);
            await _refreshTokenRepository.SalvarAlteracoesAsync(cancellationToken);

            return response;
        }

        public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
        {
            var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
            var tokenAtual = await _refreshTokenRepository.ObterPorTokenHashAsync(tokenHash, cancellationToken);

            if (tokenAtual is null || !tokenAtual.EstaAtivo)
            {
                throw new RefreshTokenInvalidoException();
            }

            tokenAtual.RevogadoEm = DateTime.UtcNow;
            await _refreshTokenRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        private void ValidarCredenciais(bool? ativo, string? hashArmazenado, string senhaInformada)
        {
            if (ativo is not true || string.IsNullOrEmpty(hashArmazenado) || !_passwordHasher.Verificar(senhaInformada, hashArmazenado))
            {
                throw new CredenciaisInvalidasException();
            }
        }

        private async Task<(string Nome, string? Email)?> ObterDadosUsuarioAsync(int usuarioId, PerfilUsuario perfil, CancellationToken cancellationToken)
        {
            switch (perfil)
            {
                case PerfilUsuario.Professor:
                    var professor = await _professorRepository.ObterPorIdAtivoAsync(usuarioId, cancellationToken);
                    return professor is null ? null : (professor.Nome, professor.Email);

                case PerfilUsuario.Aluno:
                    var aluno = await _alunoRepository.ObterPorIdAtivoAsync(usuarioId, cancellationToken);
                    return aluno is null ? null : (aluno.Nome, aluno.Email);

                case PerfilUsuario.Admin:
                    var admin = await _adminRepository.ObterPorIdAtivoAsync(usuarioId, cancellationToken);
                    return admin is null ? null : (admin.Nome, admin.Email);

                default:
                    return null;
            }
        }

        private async Task<LoginResponse> EmitirTokensAsync(int usuarioId, PerfilUsuario perfil, string nome, string? email, string? ipOrigem, string? userAgent, CancellationToken cancellationToken)
        {
            var (accessToken, accessTokenExpiraEm) = _tokenService.GerarAccessToken(usuarioId, perfil, nome, email);
            var (refreshToken, refreshTokenHash, refreshTokenExpiraEm) = _tokenService.GerarRefreshToken();

            await _refreshTokenRepository.AdicionarAsync(new RefreshToken
            {
                UsuarioId = usuarioId,
                Perfil = perfil,
                TokenHash = refreshTokenHash,
                CriadoEm = DateTime.UtcNow,
                ExpiraEm = refreshTokenExpiraEm,
                IpCriacao = ipOrigem,
                UserAgent = userAgent
            }, cancellationToken);
            await _refreshTokenRepository.SalvarAlteracoesAsync(cancellationToken);

            return new LoginResponse
            {
                Perfil = perfil,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiraEm = accessTokenExpiraEm
            };
        }
    }
}
