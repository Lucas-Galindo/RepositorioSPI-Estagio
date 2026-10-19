using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class NotificacaoRepository : INotificacaoRepository
    {
        private readonly SpiDbContext _dbContext;

        public NotificacaoRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Notificacao?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.Notificacoes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        public Task<List<Notificacao>> ListarAtivasPorProfessorAsync(int professorId, CancellationToken cancellationToken = default) =>
            _dbContext.Notificacoes
                .Include(n => n.Aula!).ThenInclude(a => a.Materia)
                .Include(n => n.Aula!).ThenInclude(a => a.Turma)
                .Include(n => n.Aula!).ThenInclude(a => a.AulaAlunos).ThenInclude(aa => aa.Aluno)
                .Include(n => n.Aluno)
                .Where(n => n.ProfessorId == professorId && n.ResolvidaEm == null)
                .OrderByDescending(n => n.CriadaEm).ThenByDescending(n => n.Id)
                .ToListAsync(cancellationToken);

        public Task<List<Notificacao>> ListarPorAulasAsync(TipoNotificacao tipo, IReadOnlyCollection<int> aulaIds, CancellationToken cancellationToken = default) =>
            _dbContext.Notificacoes
                .Where(n => n.Tipo == tipo && n.AulaId.HasValue && aulaIds.Contains(n.AulaId.Value))
                .ToListAsync(cancellationToken);

        public Task<List<Notificacao>> ListarPorAlunosAsync(TipoNotificacao tipo, int professorId, IReadOnlyCollection<int> alunoIds, CancellationToken cancellationToken = default) =>
            _dbContext.Notificacoes
                .Where(n => n.Tipo == tipo && n.ProfessorId == professorId && n.AlunoId.HasValue && alunoIds.Contains(n.AlunoId.Value))
                .ToListAsync(cancellationToken);

        public Task<List<Notificacao>> ListarAtivasPorTipoAsync(TipoNotificacao tipo, int? professorId, CancellationToken cancellationToken = default) =>
            _dbContext.Notificacoes
                .Where(n => n.Tipo == tipo && n.ResolvidaEm == null && (!professorId.HasValue || n.ProfessorId == professorId.Value))
                .ToListAsync(cancellationToken);

        public async Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken = default) =>
            await _dbContext.Notificacoes.AddAsync(notificacao, cancellationToken);

        public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException e) when (e.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
            {
                // Duas sincronizacoes ao mesmo tempo: o indice unico (tipo, aula_id)
                // (ou (tipo, professora, aluno), specs/060) barrou a segunda notificacao
                // da mesma referencia (FR-003).
                throw new ConflitoException("Ja existe uma notificacao para esta referencia.");
            }
        }
    }
}
