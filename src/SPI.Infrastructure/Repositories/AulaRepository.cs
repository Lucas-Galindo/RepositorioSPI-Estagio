using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class AulaRepository : IAulaRepository
    {
        private readonly SpiDbContext _dbContext;

        public AulaRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Aula?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.Aulas
                .Include(a => a.Materia)
                .Include(a => a.Turma)
                .Include(a => a.Professor)
                .Include(a => a.AulaAlunos).ThenInclude(aa => aa.Aluno)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        public async Task<List<Aula>> ListarAsync(
            string? status,
            int? turmaId,
            int? alunoId,
            DateOnly? dataInicio,
            DateOnly? dataFim,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Aulas
                .Include(a => a.Materia)
                .Include(a => a.Turma)
                .Include(a => a.AulaAlunos).ThenInclude(aa => aa.Aluno)
                .Where(a => a.Ativo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            if (turmaId.HasValue)
            {
                query = query.Where(a => a.TurmaId == turmaId.Value);
            }

            if (alunoId.HasValue)
            {
                query = query.Where(a => a.AulaAlunos.Any(aa => aa.AlunoId == alunoId.Value));
            }

            if (dataInicio.HasValue)
            {
                query = query.Where(a => a.DataInicio >= dataInicio.Value);
            }

            if (dataFim.HasValue)
            {
                query = query.Where(a => a.DataInicio <= dataFim.Value);
            }

            return await query.OrderBy(a => a.DataInicio).ThenBy(a => a.HoraInicio).ToListAsync(cancellationToken);
        }

        public Task<bool> ExisteConflitoAsync(
            int professorId,
            DateOnly data,
            TimeOnly horaInicio,
            TimeOnly horaFim,
            int? ignorarAulaId,
            CancellationToken cancellationToken = default) =>
            _dbContext.Aulas.AnyAsync(a =>
                a.ProfessorId == professorId &&
                a.Ativo &&
                a.Status != "Cancelada" &&
                a.DataInicio == data &&
                a.HoraInicio < horaFim &&
                horaInicio < a.HoraFim &&
                (ignorarAulaId == null || a.Id != ignorarAulaId),
                cancellationToken);

        public async Task AdicionarAsync(Aula aula, CancellationToken cancellationToken = default) =>
            await _dbContext.Aulas.AddAsync(aula, cancellationToken);

        public async Task DefinirAlunosAsync(int aulaId, IEnumerable<int> alunoIds, CancellationToken cancellationToken = default)
        {
            foreach (var alunoId in alunoIds)
            {
                await _dbContext.AulaAlunos.AddAsync(new AulaAluno { AulaId = aulaId, AlunoId = alunoId }, cancellationToken);
            }
        }

        public async Task<List<int>> ObterAlunosAtivosDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default) =>
            await _dbContext.AlunosTurma
                .Where(at => at.TurmaId == turmaId && at.Aluno.Ativo)
                .Select(at => at.AlunoId)
                .ToListAsync(cancellationToken);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
