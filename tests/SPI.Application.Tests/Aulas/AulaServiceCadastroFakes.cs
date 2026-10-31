using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;

namespace SPI.Application.Tests.Aulas
{
    // Fakes para os fluxos de criar/editar aula (specs/055). Ficam separados de
    // AulaServiceFakes.cs, cujos fakes lancam NotImplementedException de proposito
    // fora do escopo de RegistrarSessaoAsync.

    // Guarda as aulas em memoria e simula o que o repositorio real faz ao criar a
    // aula e vincular os alunos (inclusive as navegacoes Materia/Turma/Aluno que o
    // Include do EF Core preencheria).
    public class FakeAulaRepositoryParaCadastro : IAulaRepository
    {
        private int _proximoId = 1;

        public List<Aula> Aulas { get; } = new();
        public Dictionary<int, Aluno> AlunosConhecidos { get; } = new();
        public Dictionary<int, List<int>> AlunosAtivosPorTurma { get; } = new();
        public Materia Materia { get; set; } = new() { Id = 1, Nome = "Matematica", Ativo = true };
        public Turma? Turma { get; set; }
        public int Salvamentos { get; private set; }

        public Task AdicionarAsync(Aula aula, CancellationToken cancellationToken = default)
        {
            aula.Id = _proximoId++;
            aula.Materia = Materia;
            aula.Turma = aula.TurmaId.HasValue ? Turma : null;
            Aulas.Add(aula);
            return Task.CompletedTask;
        }

        public Task DefinirAlunosAsync(int aulaId, IEnumerable<int> alunoIds, CancellationToken cancellationToken = default)
        {
            var aula = Aulas.Single(a => a.Id == aulaId);
            foreach (var alunoId in alunoIds)
            {
                aula.AulaAlunos.Add(new AulaAluno { AulaId = aulaId, AlunoId = alunoId, Aluno = AlunosConhecidos[alunoId], Aula = aula });
            }

            return Task.CompletedTask;
        }

        public Task<Aula?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Aulas.FirstOrDefault(a => a.Id == id));

        public Task<List<int>> ObterAlunosAtivosDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(AlunosAtivosPorTurma.TryGetValue(turmaId, out var ids) ? ids.ToList() : new List<int>());

        // specs/063 (FR-016): a regra de conflito de horario e da PROFESSORA (qualquer aula dela), nunca do
        // aluno. O fake so registra as consultas e devolve o resultado configurado.
        public bool ConflitoSimulado { get; set; }
        public List<(int ProfessorId, DateOnly Data, TimeOnly Inicio, TimeOnly Fim)> ConsultasDeConflito { get; } = new();

        public Task<bool> ExisteConflitoAsync(int professorId, DateOnly data, TimeOnly horaInicio, TimeOnly horaFim, int? ignorarAulaId, CancellationToken cancellationToken = default)
        {
            ConsultasDeConflito.Add((professorId, data, horaInicio, horaFim));
            return Task.FromResult(ConflitoSimulado);
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.CompletedTask;
        }

        public Task<List<Aula>> ListarAsync(string? status, int? turmaId, int? alunoId, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Dictionary<int, int>> ContarPresencasPorVinculosAsync(IReadOnlyCollection<int> vinculoIds, DateOnly? de, DateOnly? ate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<AulaAluno>> ListarPresencasDoVinculoAsync(int vinculoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Aula>> ListarNaJanelaDeAvisoAsync(DateTime agora, int diasAntes, TimeOnly hora, int? professorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Aula>> ListarPendentesDeStatusAsync(DateTime agora, DateTime desde, int? professorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<int> ContarPresencasPorVinculoECompetenciaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    public class FakeMateriaRepositoryParaCadastro : IMateriaRepository
    {
        public Materia? Materia { get; set; }

        public Task<Materia?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Materia?.Id == id ? Materia : null);

        public Task<List<Materia>> ListarAsync(string? nome, string? nivel, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExisteNomeAsync(string nome, int? ignorarId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AdicionarAsync(Materia materia, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakeTurmaRepositoryParaCadastro : ITurmaRepository
    {
        public List<Turma> Turmas { get; } = new();

        public Task<Turma?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Turmas.FirstOrDefault(t => t.Id == id));

        public Task<List<Turma>> ListarAsync(string? nome, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AdicionarAsync(Turma turma, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task VincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DesvincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakeAlunoRepositoryParaCadastro : IAlunoRepository
    {
        public Dictionary<int, Aluno> Alunos { get; } = new();

        public Task<Aluno?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Alunos.TryGetValue(id, out var aluno) ? aluno : null);

        public Task<Aluno?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Aluno?> ObterPorRaAsync(string ra, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Aluno?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Aluno>> ListarAsync(string? nome, string? ra, int? turmaId, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExisteCpfAsync(string cpf, int? ignorarId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<(int Id, string Ra)> CadastrarViaProcedureAsync(
            string nome, string? cpf, string? telefoneAluno, string? telefoneResponsavel, string? email,
            string? senhaHash, string? emailResponsavel, decimal valorAula, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task VincularTurmaAsync(int alunoId, int turmaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    // Monta um AulaService pronto para criar aulas: 1 materia, 1 turma (id 10) e
    // os alunos informados (todos ativos e matriculados nessa turma).
    public class CenarioCadastroAula
    {
        public const int TurmaId = 10;

        public FakeAulaRepositoryParaCadastro Aulas { get; } = new();
        public FakePagamentoRepositoryParaAula Pagamentos { get; } = new();
        public FakeVinculoCobrancaRepositoryParaAula Vinculos { get; } = new();
        public FakeLembreteServiceVazio Lembretes { get; } = new();
        public FakeAlunoRepositoryParaCadastro AlunosRepo { get; } = new();
        public FakeTurmaRepositoryParaCadastro TurmasRepo { get; } = new();
        public AulaService Servico { get; }

        public CenarioCadastroAula(params Aluno[] alunos)
        {
            var turma = new Turma { Id = TurmaId, Nome = "Turma A", Ativo = true };
            Aulas.Turma = turma;
            TurmasRepo.Turmas.Add(turma);
            Aulas.AlunosAtivosPorTurma[TurmaId] = alunos.Select(a => a.Id).ToList();
            foreach (var aluno in alunos)
            {
                Aulas.AlunosConhecidos[aluno.Id] = aluno;
                AlunosRepo.Alunos[aluno.Id] = aluno;
            }

            Servico = new AulaService(
                Aulas,
                AlunosRepo,
                TurmasRepo,
                new FakeMateriaRepositoryParaCadastro { Materia = Aulas.Materia },
                Lembretes,
                Pagamentos,
                new FakeCategoriaReceitaRepositoryParaAula
                {
                    AulaEmTurma = new CategoriaReceita { Id = 1, Nome = "Aula em turma", Ativo = true },
                    AulaParticular = new CategoriaReceita { Id = 2, Nome = "Aula particular", Ativo = true },
                },
                Vinculos);
        }

        public static Aluno NovoAluno(int id, string nome) => new() { Id = id, Nome = nome, Ra = $"RA{id}", Ativo = true, ValorAula = 80m };
    }
}
