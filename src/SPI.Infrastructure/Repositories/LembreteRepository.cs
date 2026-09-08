using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class LembreteRepository : ILembreteRepository
    {
        private readonly SpiDbContext _dbContext;

        public LembreteRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Lembrete?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.Lembretes.Include(l => l.Turma).FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        public async Task<List<Lembrete>> ListarAsync(int? turmaId, string? status, bool? ativo, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Lembretes.Include(l => l.Turma).AsQueryable();

            if (turmaId.HasValue)
            {
                query = query.Where(l => l.TurmaId == turmaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            if (ativo.HasValue)
            {
                query = query.Where(l => l.Ativo == ativo.Value);
            }

            return await query.OrderBy(l => l.HoraProgramada).ToListAsync(cancellationToken);
        }

        public Task<List<Lembrete>> ListarAtivosPorTurmaAsync(int turmaId, CancellationToken cancellationToken = default) =>
            _dbContext.Lembretes.Where(l => l.TurmaId == turmaId && l.Ativo).ToListAsync(cancellationToken);

        public Task<List<Lembrete>> ListarPendentesVencidosAsync(DateTime agora, CancellationToken cancellationToken = default) =>
            _dbContext.Lembretes
                .Include(l => l.Turma).ThenInclude(t => t.AlunosTurma).ThenInclude(at => at.Aluno)
                .Where(l => l.Ativo && l.Status == "Pendente" && l.HoraProgramada <= agora)
                .ToListAsync(cancellationToken);

        public Task<Aula?> ObterProximaAulaDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default)
        {
            // DataInicio/HoraInicio da Aula sao horario local (sem fuso
            // horario armazenado -- sistema single-tenant, mesmo fuso da
            // professora), entao a comparacao usa hora local, nao UTC.
            var agora = DateTime.Now;
            var hoje = DateOnly.FromDateTime(agora);
            var horaAtual = TimeOnly.FromDateTime(agora);

            return _dbContext.Aulas
                .Where(a => a.TurmaId == turmaId && a.Ativo && a.Status == "Agendada")
                .Where(a => a.DataInicio > hoje || (a.DataInicio == hoje && a.HoraInicio >= horaAtual))
                .OrderBy(a => a.DataInicio).ThenBy(a => a.HoraInicio)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AdicionarAsync(Lembrete lembrete, CancellationToken cancellationToken = default) =>
            await _dbContext.Lembretes.AddAsync(lembrete, cancellationToken);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
