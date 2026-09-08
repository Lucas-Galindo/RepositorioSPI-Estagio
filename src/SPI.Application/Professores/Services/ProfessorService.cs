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
        private readonly IProfessorRepository _professorRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;

        public ProfessorService(
            IProfessorRepository professorRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher)
        {
            _professorRepository = professorRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
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
