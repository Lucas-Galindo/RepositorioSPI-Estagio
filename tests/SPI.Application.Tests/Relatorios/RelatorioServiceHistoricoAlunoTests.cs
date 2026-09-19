using SPI.Application.Relatorios.Services;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.Relatorios
{
    // specs/033: testa a correcao do calculo de "% Frequencia do Aluno" em
    // RelatorioService.ObterHistoricoAlunoAsync -- o numerador passa a ser a
    // contagem real de presencas do aluno dentro do mesmo filtro que ja compoe
    // o denominador (nao mais o contador vitalicio Aluno.Frequencia) sempre que
    // qualquer filtro restritivo (periodo, status ou turma) for aplicado. Usa
    // fakes manuais de IAulaRepository/IAlunoRepository, no mesmo padrao de
    // RelatorioServiceFinanceiroPorTurmaTests.cs (biblioteca de mock nao
    // instalada neste projeto de testes).
    public class RelatorioServiceHistoricoAlunoTests
    {
        private static RelatorioService CriarServico(FakeAulaRepository aulas, FakeAlunoRepository alunos) =>
            new(aulas, alunos, null!, null!, null!);

        private static Materia NovaMateria() => new() { Id = 1, Nome = "Matematica", Ativo = true };

        private static Aluno NovoAluno(int id, string nome, string ra, int frequencia) =>
            new() { Id = id, Nome = nome, Ra = ra, Frequencia = frequencia, Ativo = true };

        private static Aula NovaAula(int id, Aluno aluno, string status, bool? presente, Materia materia) => new()
        {
            Id = id,
            MateriaId = materia.Id,
            Materia = materia,
            ProfessorId = 1,
            Status = status,
            DataInicio = new DateOnly(2026, 9, 1).AddDays(id),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
            Ativo = true,
            AulaAlunos = new List<AulaAluno>
            {
                new() { AulaId = id, AlunoId = aluno.Id, Aluno = aluno, Presente = presente }
            }
        };

        // Cenario base dos Acceptance Scenarios 1-2 de spec.md: aluno com Frequencia
        // vitalicia de 50, mas apenas 8 de 10 aulas Realizadas no filtro com presenca real.
        private static (Aluno Aluno, List<Aula> Aulas) CriarCenarioBase()
        {
            var materia = NovaMateria();
            var aluno = NovoAluno(1, "Ana", "1", frequencia: 50);

            var aulas = new List<Aula>();
            for (var i = 1; i <= 8; i++)
            {
                aulas.Add(NovaAula(i, aluno, "Realizada", presente: true, materia));
            }
            for (var i = 9; i <= 10; i++)
            {
                aulas.Add(NovaAula(i, aluno, "Realizada", presente: false, materia));
            }

            return (aluno, aulas);
        }

        [Fact]
        public async Task Com_Filtro_De_Periodo_Percentual_Usa_Presencas_Reais_Nunca_Contador_Vitalicio()
        {
            var (aluno, aulas) = CriarCenarioBase();
            var fakeAulas = new FakeAulaRepository { Aulas = aulas };
            var fakeAlunos = new FakeAlunoRepository { Aluno = aluno };
            var servico = CriarServico(fakeAulas, fakeAlunos);

            var resposta = await servico.ObterHistoricoAlunoAsync(
                aluno.Id, new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 20), null, null);

            Assert.Equal(80.0m, resposta.PercentualFrequencia);
            Assert.NotEqual(500.0m, resposta.PercentualFrequencia);

            // FR-005: nenhum outro campo da resposta muda -- dados do aluno e lista de
            // Aulas continuam refletindo exatamente o que o repositorio retornou.
            Assert.Equal(aluno.Id, resposta.AlunoId);
            Assert.Equal(aluno.Nome, resposta.AlunoNome);
            Assert.Equal(aluno.Ra, resposta.Ra);
            Assert.Equal(aulas.Count, resposta.Aulas.Count);
            for (var i = 0; i < aulas.Count; i++)
            {
                Assert.Equal(aulas[i].Id, resposta.Aulas[i].AulaId);
                Assert.Equal(aulas[i].DataInicio, resposta.Aulas[i].Data);
                Assert.Equal(aulas[i].Status, resposta.Aulas[i].Status);
            }
        }

        [Fact]
        public async Task Com_Filtro_De_Turma_Sem_Periodo_Tambem_Usa_Presencas_Reais()
        {
            var (aluno, aulas) = CriarCenarioBase();
            var fakeAulas = new FakeAulaRepository { Aulas = aulas };
            var fakeAlunos = new FakeAlunoRepository { Aluno = aluno };
            var servico = CriarServico(fakeAulas, fakeAlunos);

            var resposta = await servico.ObterHistoricoAlunoAsync(
                aluno.Id, null, null, null, turmaId: 7);

            Assert.Equal(80.0m, resposta.PercentualFrequencia);
        }

        [Fact]
        public async Task Com_Filtro_De_Status_Sem_Periodo_Nem_Turma_Tambem_Usa_Presencas_Reais()
        {
            var (aluno, aulas) = CriarCenarioBase();
            var fakeAulas = new FakeAulaRepository { Aulas = aulas };
            var fakeAlunos = new FakeAlunoRepository { Aluno = aluno };
            var servico = CriarServico(fakeAulas, fakeAlunos);

            var resposta = await servico.ObterHistoricoAlunoAsync(
                aluno.Id, null, null, status: "Realizada", turmaId: null);

            Assert.Equal(80.0m, resposta.PercentualFrequencia);
        }

        [Fact]
        public async Task Sem_Nenhum_Filtro_Restritivo_Continua_Usando_Contador_Vitalicio()
        {
            var (aluno, aulas) = CriarCenarioBase();
            var fakeAulas = new FakeAulaRepository { Aulas = aulas };
            var fakeAlunos = new FakeAlunoRepository { Aluno = aluno };
            var servico = CriarServico(fakeAulas, fakeAlunos);

            var resposta = await servico.ObterHistoricoAlunoAsync(
                aluno.Id, null, null, null, null);

            // 50 (Frequencia vitalicia) / 10 (Realizadas no filtro) * 100 = 500%
            // comportamento inalterado quando nenhum filtro restringe a consulta.
            Assert.Equal(500.0m, resposta.PercentualFrequencia);
        }

        [Fact]
        public async Task Sem_Aulas_Realizadas_No_Filtro_Percentual_E_Zero_Sem_Divisao_Por_Zero()
        {
            var materia = NovaMateria();
            var aluno = NovoAluno(1, "Ana", "1", frequencia: 50);
            var aulaAgendada = NovaAula(1, aluno, "Agendada", presente: null, materia);

            var fakeAulas = new FakeAulaRepository { Aulas = new List<Aula> { aulaAgendada } };
            var fakeAlunos = new FakeAlunoRepository { Aluno = aluno };
            var servico = CriarServico(fakeAulas, fakeAlunos);

            var respostaComFiltro = await servico.ObterHistoricoAlunoAsync(
                aluno.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), null, null);
            var respostaSemFiltro = await servico.ObterHistoricoAlunoAsync(
                aluno.Id, null, null, null, null);

            Assert.Equal(0m, respostaComFiltro.PercentualFrequencia);
            Assert.Equal(0m, respostaSemFiltro.PercentualFrequencia);
        }

        private class FakeAulaRepository : IAulaRepository
        {
            public List<Aula> Aulas { get; set; } = new();

            public Task<List<Aula>> ListarAsync(
                string? status, int? turmaId, int? alunoId, DateOnly? dataInicio, DateOnly? dataFim,
                CancellationToken cancellationToken = default) => Task.FromResult(Aulas.ToList());

            public Task<Aula?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult<Aula?>(null);
            public Task<bool> ExisteConflitoAsync(
                int professorId, DateOnly data, TimeOnly horaInicio, TimeOnly horaFim, int? ignorarAulaId,
                CancellationToken cancellationToken = default) => Task.FromResult(false);
            public Task AdicionarAsync(Aula aula, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DefinirAlunosAsync(int aulaId, IEnumerable<int> alunoIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<List<int>> ObterAlunosAtivosDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default) =>
                Task.FromResult(new List<int>());
            public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private class FakeAlunoRepository : IAlunoRepository
        {
            public Aluno? Aluno { get; set; }

            public Task<Aluno?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult(Aluno);

            public Task<Aluno?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
                Task.FromResult<Aluno?>(null);
            public Task<Aluno?> ObterPorRaAsync(string ra, CancellationToken cancellationToken = default) =>
                Task.FromResult<Aluno?>(null);
            public Task<Aluno?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult<Aluno?>(null);
            public Task<List<Aluno>> ListarAsync(string? nome, string? ra, int? turmaId, bool? ativo, CancellationToken cancellationToken = default) =>
                Task.FromResult(new List<Aluno>());
            public Task<bool> ExisteCpfAsync(string cpf, int? ignorarId = null, CancellationToken cancellationToken = default) =>
                Task.FromResult(false);
            public Task<(int Id, string Ra)> CadastrarViaProcedureAsync(
                string nome, string? cpf, string? telefoneAluno, string? telefoneResponsavel, string? email,
                string? senhaHash, string? emailResponsavel, decimal valorAula, CancellationToken cancellationToken = default) =>
                Task.FromResult((0, string.Empty));
            public Task VincularTurmaAsync(int alunoId, int turmaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
