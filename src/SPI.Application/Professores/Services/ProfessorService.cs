using SPI.Application.Common;
using SPI.Application.Professores.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Professores.Services
{
    public class ProfessorService : IProfessorService
    {
        // Validade curta e fixa (mesmo valor de SenhaResetToken): 15 minutos, uso unico.
        private static readonly TimeSpan ValidadeTokenExclusao = TimeSpan.FromMinutes(15);

        private readonly IProfessorRepository _professorRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IExclusaoProfessorTokenRepository _exclusaoProfessorTokenRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly ITokenService _tokenService;
        private readonly IEmailSender _emailSender;

        public ProfessorService(
            IProfessorRepository professorRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            IExclusaoProfessorTokenRepository exclusaoProfessorTokenRepository,
            IAdminRepository adminRepository,
            ITokenService tokenService,
            IEmailSender emailSender)
        {
            _professorRepository = professorRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _exclusaoProfessorTokenRepository = exclusaoProfessorTokenRepository;
            _adminRepository = adminRepository;
            _tokenService = tokenService;
            _emailSender = emailSender;
        }

        public async Task<ProfessorResponse> CadastroInicialAsync(CadastroInicialProfessorRequest request, CancellationToken cancellationToken = default)
        {
            // Sistema single-tenant nesta fase: so permite o cadastro
            // enquanto nenhuma professora existir ainda (ver prompt mestre).
            if (await _professorRepository.ExisteAlgumAsync(cancellationToken))
            {
                throw new ConflitoException("Ja existe uma professora cadastrada neste sistema.");
            }

            if (await _professorRepository.ExisteCpfAsync(request.Cpf, cancellationToken))
            {
                throw new ConflitoException("Ja existe um cadastro com este CPF.");
            }

            if (await _professorRepository.ExisteEmailAsync(request.Email, cancellationToken: cancellationToken))
            {
                throw new ConflitoException("Ja existe um cadastro com este e-mail.");
            }

            var professor = new Professor
            {
                Nome = request.Nome,
                Cpf = request.Cpf,
                Email = request.Email,
                Senha = _passwordHasher.Hash(request.Senha),
                Telefone = request.Telefone,
                Ativo = true
            };

            await _professorRepository.AdicionarAsync(professor, cancellationToken);
            await _professorRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(professor);
        }

        public async Task<ProfessorResponse> ObterPerfilAsync(int professorId, CancellationToken cancellationToken = default)
        {
            var professor = await _professorRepository.ObterPorIdAtivoAsync(professorId, cancellationToken)
                ?? throw new NaoEncontradoException("Professor nao encontrado.");

            return Mapear(professor);
        }

        public async Task<ProfessorResponse> AtualizarPerfilAsync(int professorId, AtualizarProfessorRequest request, CancellationToken cancellationToken = default)
        {
            var professor = await _professorRepository.ObterPorIdAtivoAsync(professorId, cancellationToken)
                ?? throw new NaoEncontradoException("Professor nao encontrado.");

            if (await _professorRepository.ExisteEmailAsync(request.Email, professorId, cancellationToken))
            {
                throw new ConflitoException("Ja existe um cadastro com este e-mail.");
            }

            professor.Nome = request.Nome;
            professor.Email = request.Email;
            professor.Telefone = request.Telefone ?? professor.Telefone;

            await _professorRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(professor);
        }

        public async Task AlterarSenhaAsync(int professorId, AlterarSenhaProfessorRequest request, CancellationToken cancellationToken = default)
        {
            var professor = await _professorRepository.ObterPorIdAtivoAsync(professorId, cancellationToken)
                ?? throw new NaoEncontradoException("Professor nao encontrado.");

            if (!_passwordHasher.Verificar(request.SenhaAtual, professor.Senha))
            {
                throw new ConflitoException("Senha atual incorreta.");
            }

            professor.Senha = _passwordHasher.Hash(request.NovaSenha);
            await _professorRepository.SalvarAlteracoesAsync(cancellationToken);

            // Forca novo login em todos os dispositivos apos a troca de senha.
            await _refreshTokenRepository.RevogarTodosDoUsuarioAsync(professor.Id, PerfilUsuario.Professor, cancellationToken);
            await _refreshTokenRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        public async Task<ProfessorResponse?> ObterUnicaAsync(CancellationToken cancellationToken = default)
        {
            var professor = await _professorRepository.ObterUnicaAsync(cancellationToken);
            return professor is null ? null : Mapear(professor);
        }

        public async Task<ProfessorResponse> AtualizarComoAdminAsync(AtualizarProfessorRequest request, CancellationToken cancellationToken = default)
        {
            var professor = await _professorRepository.ObterUnicaAsync(cancellationToken)
                ?? throw new NaoEncontradoException("Nenhuma professora cadastrada ainda.");

            if (await _professorRepository.ExisteEmailAsync(request.Email, professor.Id, cancellationToken))
            {
                throw new ConflitoException("Ja existe um cadastro com este e-mail.");
            }

            professor.Nome = request.Nome;
            professor.Email = request.Email;
            professor.Telefone = request.Telefone ?? professor.Telefone;

            await _professorRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(professor);
        }

        public async Task SolicitarExclusaoAsync(int adminId, CancellationToken cancellationToken = default)
        {
            var professor = await _professorRepository.ObterUnicaAsync(cancellationToken)
                ?? throw new NaoEncontradoException("Nenhuma professora cadastrada ainda.");

            // R4: pedir um novo codigo invalida qualquer codigo pendente do
            // mesmo Admin (nunca dois codigos validos ao mesmo tempo para o
            // mesmo solicitante) - um codigo pendente de outro Admin nao e afetado.
            var pendentes = await _exclusaoProfessorTokenRepository.ObterPendentesPorAdminIdAsync(adminId, cancellationToken);
            foreach (var pendente in pendentes)
            {
                pendente.Usado = true;
            }

            var admin = await _adminRepository.ObterPorIdAtivoAsync(adminId, cancellationToken)
                ?? throw new NaoEncontradoException("Admin nao encontrado.");

            var (token, tokenHash) = _tokenService.GerarTokenSeguro();

            await _exclusaoProfessorTokenRepository.AdicionarAsync(new ExclusaoProfessorToken
            {
                AdminId = adminId,
                ProfessorId = professor.Id,
                TokenHash = tokenHash,
                CriadoEm = DateTime.UtcNow,
                ExpiraEm = DateTime.UtcNow.Add(ValidadeTokenExclusao),
                Usado = false
            }, cancellationToken);
            // Salva antes de tentar enviar: mesmo padrao de RecuperacaoSenhaService.
            // Diferente dela, aqui a falha de envio e reportada ao Admin (nao ha
            // preocupacao de nao-enumeracao de e-mail - o Admin ja esta autenticado).
            await _exclusaoProfessorTokenRepository.SalvarAlteracoesAsync(cancellationToken);

            var corpo = $"""
                <p>Ol&aacute;, {admin.Nome}.</p>
                <p>Voc&ecirc; solicitou a exclus&atilde;o do cadastro da professora {professor.Nome}. Use o c&oacute;digo abaixo para confirmar. Ele expira em 15 minutos e s&oacute; pode ser usado uma vez.</p>
                <p style="font-size:20px;font-weight:bold;letter-spacing:1px;">{token}</p>
                <p>Se voc&ecirc; n&atilde;o solicitou essa a&ccedil;&atilde;o, ignore este e-mail e considere revisar o acesso &agrave; sua conta de Admin.</p>
                """;

            await _emailSender.EnviarAsync(admin.Email, "SPI - Código de confirmação para excluir professora", corpo, cancellationToken);
        }

        public async Task ConfirmarExclusaoAsync(int adminId, string codigo, CancellationToken cancellationToken = default)
        {
            var tokenHash = _tokenService.CalcularHash(codigo);
            var tokenSalvo = await _exclusaoProfessorTokenRepository.ObterPorTokenHashAsync(tokenHash, cancellationToken);

            if (tokenSalvo is null || tokenSalvo.AdminId != adminId || tokenSalvo.Usado || tokenSalvo.ExpiraEm <= DateTime.UtcNow)
            {
                throw new ConflitoException("Codigo invalido, expirado ou ja utilizado.");
            }

            var professor = await _professorRepository.ObterUnicaAsync(cancellationToken)
                ?? throw new NaoEncontradoException("Nenhuma professora cadastrada ainda.");

            tokenSalvo.Usado = true;
            professor.Ativo = false;

            await _exclusaoProfessorTokenRepository.SalvarAlteracoesAsync(cancellationToken);
            await _professorRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        public async Task<ProfessorResponse> ReativarAsync(CancellationToken cancellationToken = default)
        {
            var professor = await _professorRepository.ObterUnicaAsync(cancellationToken)
                ?? throw new NaoEncontradoException("Nenhuma professora cadastrada ainda.");

            if (professor.Ativo)
            {
                throw new ConflitoException("A professora ja esta ativa.");
            }

            professor.Ativo = true;
            await _professorRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(professor);
        }

        private static ProfessorResponse Mapear(Domain.Entities.Professor professor) => new()
        {
            Id = professor.Id,
            Nome = professor.Nome,
            Cpf = professor.Cpf,
            Email = professor.Email,
            Telefone = professor.Telefone,
            Ativo = professor.Ativo
        };
    }
}
