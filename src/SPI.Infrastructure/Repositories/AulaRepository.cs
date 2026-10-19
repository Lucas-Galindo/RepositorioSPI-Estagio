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

        public async Task<List<Aula>> ListarPendentesDeStatusAsync(DateTime agora, DateTime desde, int? professorId, CancellationToken cancellationToken = default)
        {
            // Termino = DataInicio + HoraFim. Comparado em duas partes (data antes,
            // ou mesma data e hora ate o limite) porque somar DateOnly + TimeOnly
            // nao e traduzido para SQL de forma confiavel.
            var hoje = DateOnly.FromDateTime(agora);
            var horaAgora = TimeOnly.FromDateTime(agora);
            var diaDesde = DateOnly.FromDateTime(desde);
            var horaDesde = TimeOnly.FromDateTime(desde);

            var query = _dbContext.Aulas
                .Include(a => a.Materia)
                .Include(a => a.Turma)
                .Include(a => a.AulaAlunos).ThenInclude(aa => aa.Aluno)
                .Where(a => a.Ativo && a.Status == "Agendada")
                .Where(a => a.DataInicio < hoje || (a.DataInicio == hoje && a.HoraFim <= horaAgora))
                .Where(a => a.DataInicio > diaDesde || (a.DataInicio == diaDesde && a.HoraFim >= horaDesde));

            if (professorId.HasValue)
            {
                query = query.Where(a => a.ProfessorId == professorId.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }

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

        // Nao filtra "Presente == true" aqui de proposito -- nao esta faltando,
        // e redundante: AulaService.GerarContasAReceberAsync so preenche
        // AulaAluno.VinculoCobrancaId dentro do loop que ja filtrou por
        // "Presente == true" (specs/052). Ou seja, todo AulaAluno com
        // VinculoCobrancaId preenchido ja É uma presenca, por construcao --
        // contar essas linhas e o mesmo que contar presencas.
        public Task<int> ContarPresencasPorVinculoECompetenciaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default) =>
            _dbContext.AulaAlunos.CountAsync(
                aa => aa.VinculoCobrancaId == vinculoCobrancaId
                    && aa.Aula.DataInicio.Year == competencia.Year
                    && aa.Aula.DataInicio.Month == competencia.Month,
                cancellationToken);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
