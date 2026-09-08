using SPI.Application.Common.Dtos;
using SPI.Application.Turmas.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Turmas.Services
{
    public class TurmaService : ITurmaService
    {
        private readonly ITurmaRepository _turmaRepository;
        private readonly IAlunoRepository _alunoRepository;

        public TurmaService(ITurmaRepository turmaRepository, IAlunoRepository alunoRepository)
        {
            _turmaRepository = turmaRepository;
            _alunoRepository = alunoRepository;
        }

        public async Task<List<TurmaResponse>> ListarAsync(string? nome, bool? ativo, CancellationToken cancellationToken = default)
        {
            var turmas = await _turmaRepository.ListarAsync(nome, ativo, cancellationToken);
            return turmas.Select(Mapear).ToList();
        }

        public async Task<TurmaResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            return Mapear(turma);
        }

        public async Task<TurmaResponse> CadastrarAsync(int professorId, TurmaRequest request, CancellationToken cancellationToken = default)
        {
            var turma = new Turma
            {
                Nome = request.Nome,
                ProfessorId = professorId,
                Ativo = true
            };

            await _turmaRepository.AdicionarAsync(turma, cancellationToken);
            await _turmaRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(turma);
        }

        public async Task<TurmaResponse> AtualizarAsync(int id, TurmaRequest request, CancellationToken cancellationToken = default)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            turma.Nome = request.Nome;
            await _turmaRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(turma);
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            turma.Ativo = false;
            await _turmaRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        public async Task<TurmaResponse> VincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            if (await _alunoRepository.ObterPorIdAsync(alunoId, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Aluno nao encontrado.");
            }

            if (turma.AlunosTurma.Any(at => at.AlunoId == alunoId))
            {
                throw new ConflitoException("Aluno ja esta vinculado a esta turma.");
            }

            await _turmaRepository.VincularAlunoAsync(turmaId, alunoId, cancellationToken);
            await _turmaRepository.SalvarAlteracoesAsync(cancellationToken);

            var turmaAtualizada = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken);
            return Mapear(turmaAtualizada!);
        }

        public async Task<TurmaResponse> DesvincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            if (!turma.AlunosTurma.Any(at => at.AlunoId == alunoId))
            {
                throw new NaoEncontradoException("Aluno nao esta vinculado a esta turma.");
            }

            await _turmaRepository.DesvincularAlunoAsync(turmaId, alunoId, cancellationToken);
            await _turmaRepository.SalvarAlteracoesAsync(cancellationToken);

            var turmaAtualizada = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken);
            return Mapear(turmaAtualizada!);
        }

        private static TurmaResponse Mapear(Turma turma) => new()
        {
            Id = turma.Id,
            Nome = turma.Nome,
            ProfessorId = turma.ProfessorId,
            Ativo = turma.Ativo,
            Alunos = turma.AlunosTurma.Select(at => new AlunoResumoResponse
            {
                Id = at.Aluno.Id,
                Ra = at.Aluno.Ra,
                Nome = at.Aluno.Nome
            }).ToList()
        };
    }
}
