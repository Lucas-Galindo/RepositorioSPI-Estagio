using SPI.Application.Alunos.Dtos;
using SPI.Application.Common;
using SPI.Application.Common.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Alunos.Services
{
    public class AlunoService : IAlunoService
    {
        private readonly IAlunoRepository _alunoRepository;
        private readonly ITurmaRepository _turmaRepository;
        private readonly IPasswordHasher _passwordHasher;

        public AlunoService(IAlunoRepository alunoRepository, ITurmaRepository turmaRepository, IPasswordHasher passwordHasher)
        {
            _alunoRepository = alunoRepository;
            _turmaRepository = turmaRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<List<AlunoResponse>> ListarAsync(string? nome, string? ra, int? turmaId, bool? ativo, CancellationToken cancellationToken = default)
        {
            var alunos = await _alunoRepository.ListarAsync(nome, ra, turmaId, ativo, cancellationToken);
            return alunos.Select(Mapear).ToList();
        }

        public async Task<AlunoResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var aluno = await _alunoRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aluno nao encontrado.");

            return Mapear(aluno);
        }

        public async Task<AlunoResponse> CadastrarAsync(CadastrarAlunoRequest request, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(request.Cpf) && await _alunoRepository.ExisteCpfAsync(request.Cpf, cancellationToken: cancellationToken))
            {
                throw new ConflitoException("Ja existe um aluno com este CPF.");
            }

            if (request.TurmaId.HasValue && await _turmaRepository.ObterPorIdAsync(request.TurmaId.Value, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Turma nao encontrada.");
            }

            var senhaHash = string.IsNullOrWhiteSpace(request.Senha) ? null : _passwordHasher.Hash(request.Senha);

            var (id, _) = await _alunoRepository.CadastrarViaProcedureAsync(
                request.Nome,
                request.Cpf,
                request.TelefoneAluno,
                request.TelefoneResponsavel,
                request.Email,
                senhaHash,
                request.EmailResponsavel,
                request.ValorAula,
                cancellationToken);

            if (request.TurmaId.HasValue)
            {
                await _alunoRepository.VincularTurmaAsync(id, request.TurmaId.Value, cancellationToken);
                await _alunoRepository.SalvarAlteracoesAsync(cancellationToken);
            }

            var aluno = await _alunoRepository.ObterPorIdAsync(id, cancellationToken);
            return Mapear(aluno!);
        }

        public async Task<AlunoResponse> AtualizarAsync(int id, AtualizarAlunoRequest request, CancellationToken cancellationToken = default)
        {
            var aluno = await _alunoRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aluno nao encontrado.");

            if (!string.IsNullOrWhiteSpace(request.Cpf) && await _alunoRepository.ExisteCpfAsync(request.Cpf, id, cancellationToken))
            {
                throw new ConflitoException("Ja existe um aluno com este CPF.");
            }

            // Campos opcionais: null no request significa "nao informado
            // nesta chamada", nao "apagar o valor existente" -- um cliente
            // que so manda os campos que esta editando (ex: so o Nome) nao
            // pode acabar limpando CPF/telefones/e-mails ja cadastrados.
            aluno.Nome = request.Nome;
            aluno.Cpf = request.Cpf ?? aluno.Cpf;
            aluno.TelefoneAluno = request.TelefoneAluno ?? aluno.TelefoneAluno;
            aluno.TelefoneResponsavel = request.TelefoneResponsavel ?? aluno.TelefoneResponsavel;
            aluno.Email = request.Email ?? aluno.Email;
            aluno.EmailResponsavel = request.EmailResponsavel ?? aluno.EmailResponsavel;
            aluno.ValorAula = request.ValorAula;

            await _alunoRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(aluno);
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var aluno = await _alunoRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aluno nao encontrado.");

            aluno.Ativo = false;
            await _alunoRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        private static AlunoResponse Mapear(Aluno aluno) => new()
        {
            Id = aluno.Id,
            Ra = aluno.Ra,
            Nome = aluno.Nome,
            Cpf = aluno.Cpf,
            TelefoneAluno = aluno.TelefoneAluno,
            TelefoneResponsavel = aluno.TelefoneResponsavel,
            Email = aluno.Email,
            EmailResponsavel = aluno.EmailResponsavel,
            ValorAula = aluno.ValorAula,
            Frequencia = aluno.Frequencia,
            Ativo = aluno.Ativo,
            Turmas = aluno.AlunosTurma.Select(at => new TurmaResumoResponse
            {
                Id = at.Turma.Id,
                Nome = at.Turma.Nome
            }).ToList()
        };
    }
}
