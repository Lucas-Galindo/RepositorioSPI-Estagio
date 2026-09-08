using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class TurmaRepository : ITurmaRepository
    {
        private readonly SpiDbContext _dbContext;

        public TurmaRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Turma?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.Turmas.Include(t => t.AlunosTurma).ThenInclude(at => at.Aluno).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        public async Task<List<Turma>> ListarAsync(string? nome, bool? ativo, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Turmas.Include(t => t.AlunosTurma).ThenInclude(at => at.Aluno).AsQueryable();

            if (!string.IsNullOrWhiteSpace(nome))
            {
                query = query.Where(t => t.Nome.Contains(nome));
            }

            if (ativo.HasValue)
            {
                query = query.Where(t => t.Ativo == ativo.Value);
            }

            return await query.OrderBy(t => t.Nome).ToListAsync(cancellationToken);
        }

        public async Task AdicionarAsync(Turma turma, CancellationToken cancellationToken = default) =>
            await _dbContext.Turmas.AddAsync(turma, cancellationToken);

        public async Task VincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default) =>
            await _dbContext.AlunosTurma.AddAsync(new AlunoTurma { TurmaId = turmaId, AlunoId = alunoId }, cancellationToken);

        public async Task DesvincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default)
        {
            var vinculo = await _dbContext.AlunosTurma.FirstOrDefaultAsync(at => at.TurmaId == turmaId && at.AlunoId == alunoId, cancellationToken);
            if (vinculo is not null)
            {
                _dbContext.AlunosTurma.Remove(vinculo);
            }
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
