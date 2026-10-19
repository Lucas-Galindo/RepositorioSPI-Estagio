using SPI.Application.Notificacoes.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Tests.Notificacoes
{
    // specs/059: aulas em memoria. ListarPendentesDeStatusAsync aplica a mesma
    // regra da query real (research.md R2); a query SQL em si e conferida no
    // quickstart contra o banco local.
    public class FakeAulaRepositoryParaNotificacao : IAulaRepository
    {
        public List<Aula> Aulas { get; } = new();

        public Task<List<Aula>> ListarPendentesDeStatusAsync(DateTime agora, DateTime desde, int? professorId, CancellationToken cancellationToken = default)
        {
            DateTime Termino(Aula a) => a.DataInicio.ToDateTime(a.HoraFim);
            return Task.FromResult(Aulas
                .Where(a => a.Ativo && a.Status == "Agendada" && Termino(a) <= agora && Termino(a) >= desde)
                .Where(a => !professorId.HasValue || a.ProfessorId == professorId.Value)
                .ToList());
        }

        public Task<Aula?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Aula>> ListarAsync(string? status, int? turmaId, int? alunoId, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExisteConflitoAsync(int professorId, DateOnly data, TimeOnly horaInicio, TimeOnly horaFim, int? ignorarAulaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AdicionarAsync(Aula aula, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DefinirAlunosAsync(int aulaId, IEnumerable<int> alunoIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<int>> ObterAlunosAtivosDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> ContarPresencasPorVinculoECompetenciaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    // Guarda as notificacoes em memoria; as adicionadas so "existem" depois de
    // salvar, como no EF. LancarConflitoNoProximoSalvar simula o indice unico
    // barrando uma insercao concorrente.
    public class FakeNotificacaoRepository : INotificacaoRepository
    {
        private int _proximoId = 1;
        private readonly List<Notificacao> _pendentesDeSalvar = new();

        public List<Notificacao> Salvas { get; } = new();
        public Dictionary<int, Aula> AulasConhecidas { get; } = new();
        public bool LancarConflitoNoProximoSalvar { get; set; }
        public int Salvamentos { get; private set; }

        public Task<Notificacao?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Salvas.FirstOrDefault(n => n.Id == id));

        public Task<List<Notificacao>> ListarAtivasPorProfessorAsync(int professorId, CancellationToken cancellationToken = default)
        {
            var ativas = Salvas.Where(n => n.ProfessorId == professorId && n.ResolvidaEm == null)
                .OrderByDescending(n => n.CriadaEm).ThenByDescending(n => n.Id).ToList();
            foreach (var n in ativas)
            {
                n.Aula = n.AulaId.HasValue && AulasConhecidas.TryGetValue(n.AulaId.Value, out var aula) ? aula : null;
            }
            return Task.FromResult(ativas);
        }

        public Task<List<Notificacao>> ListarPorAulasAsync(TipoNotificacao tipo, IReadOnlyCollection<int> aulaIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(Salvas.Where(n => n.Tipo == tipo && n.AulaId.HasValue && aulaIds.Contains(n.AulaId.Value)).ToList());

        public Task<List<Notificacao>> ListarAtivasPorTipoAsync(TipoNotificacao tipo, int? professorId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Salvas.Where(n => n.Tipo == tipo && n.ResolvidaEm == null && (!professorId.HasValue || n.ProfessorId == professorId)).ToList());

        public Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken = default)
        {
            _pendentesDeSalvar.Add(notificacao);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            if (LancarConflitoNoProximoSalvar)
            {
                LancarConflitoNoProximoSalvar = false;
                _pendentesDeSalvar.Clear();
                throw new ConflitoException("Ja existe uma notificacao para esta aula.");
            }

            foreach (var n in _pendentesDeSalvar)
            {
                n.Id = _proximoId++;
                Salvas.Add(n);
            }
            _pendentesDeSalvar.Clear();
            Salvamentos++;
            return Task.CompletedTask;
        }
    }

    public class CenarioNotificacao
    {
        public static readonly DateTime Desde = new(2026, 10, 19, 0, 0, 0);
        public const int ProfessoraId = 6;

        public FakeAulaRepositoryParaNotificacao Aulas { get; } = new();
        public FakeNotificacaoRepository Notificacoes { get; } = new();
        public NotificacaoService Servico { get; }

        public CenarioNotificacao()
        {
            Servico = new NotificacaoService(Notificacoes, Aulas, new NotificacoesOptions { AulasPendentesDesde = Desde });
        }

        // Aula de turma (ou individual, com turmaNome nulo) das 10h as 11h na data informada.
        public Aula NovaAula(int id, DateOnly data, string? turmaNome = "Turma A", int professorId = ProfessoraId)
        {
            var aula = new Aula
            {
                Id = id,
                ProfessorId = professorId,
                Materia = new Materia { Id = 1, Nome = "Matematica", Ativo = true },
                TurmaId = turmaNome is null ? null : 10,
                Turma = turmaNome is null ? null : new Turma { Id = 10, Nome = turmaNome, Ativo = true },
                DataInicio = data,
                HoraInicio = new TimeOnly(10, 0),
                HoraFim = new TimeOnly(11, 0),
                Status = "Agendada",
                Ativo = true,
            };
            aula.AulaAlunos.Add(new AulaAluno { AulaId = id, AlunoId = 1, Aluno = new Aluno { Id = 1, Nome = "Bruno", Ativo = true } });
            Aulas.Aulas.Add(aula);
            Notificacoes.AulasConhecidas[id] = aula;
            return aula;
        }
    }
}
