using SPI.Domain.Entities;
using SPI.Domain.Repositories;

namespace SPI.Application.Tests.VinculosCobranca
{
    // Fakes manuais (biblioteca de mock nao instalada neste projeto de testes), no
    // mesmo padrao de RelatorioServiceHistoricoAlunoTests.cs. Sufixos distintos
    // para nao colidir com os fakes de Relatorios/.

    public class FakeVinculoCobrancaRepositoryParaTeste : IVinculoCobrancaRepository
    {
        private int _proximoId = 1;

        public List<VinculoCobranca> Vinculos { get; } = new();
        public int SalvamentosRealizados { get; private set; }

        // Simula o Include(v => v.Turma) do repositorio real, que resolve a navegacao pelo TurmaId.
        public List<Turma> TurmasConhecidas { get; } = new();

        public VinculoCobranca Semear(VinculoCobranca vinculo)
        {
            if (vinculo.Id == 0)
            {
                vinculo.Id = _proximoId++;
            }
            else
            {
                _proximoId = Math.Max(_proximoId, vinculo.Id + 1);
            }

            Vinculos.Add(vinculo);
            return vinculo;
        }

        public Task<VinculoCobranca?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var vinculo = Vinculos.FirstOrDefault(v => v.Id == id);
            if (vinculo is not null)
            {
                vinculo.Turma = vinculo.TurmaId is null ? null : TurmasConhecidas.FirstOrDefault(t => t.Id == vinculo.TurmaId);
            }

            return Task.FromResult(vinculo);
        }

        public Task<List<VinculoCobranca>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default)
        {
            var lista = Vinculos
                .Where(v => v.AlunoId == alunoId && (!ativo.HasValue || v.Ativo == ativo.Value))
                .OrderByDescending(v => v.Ativo)
                .ThenBy(v => v.TurmaId is null)
                .ThenBy(v => v.Id)
                .ToList();

            foreach (var v in lista)
            {
                v.Turma = v.TurmaId is null ? null : TurmasConhecidas.FirstOrDefault(t => t.Id == v.TurmaId);
            }

            return Task.FromResult(lista);
        }

        // specs/039
        public Task<List<VinculoCobranca>> ListarPacotesAsync(int? alunoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<List<VinculoCobranca>> ListarAtivosPorModalidadeAsync(SPI.Domain.Enums.ModalidadeCobranca modalidade, CancellationToken cancellationToken = default) =>
            Task.FromResult(Vinculos.Where(v => v.Ativo && v.Modalidade == modalidade).ToList());

        // specs/063
        public Task<List<VinculoCobranca>> ListarPacotesAtivosDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Vinculos
                .Where(v => v.Ativo && v.Modalidade == SPI.Domain.Enums.ModalidadeCobranca.Pacote && v.TurmaId == turmaId)
                .ToList());

        public Task<List<VinculoCobranca>> ListarPacotesAtivosDeTurmasEncerradasAsync(DateOnly hoje, CancellationToken cancellationToken = default)
        {
            var lista = new List<VinculoCobranca>();
            foreach (var v in Vinculos.Where(v => v.Ativo && v.Modalidade == SPI.Domain.Enums.ModalidadeCobranca.Pacote && v.TurmaId != null))
            {
                var turma = TurmasConhecidas.FirstOrDefault(t => t.Id == v.TurmaId);
                if (turma is not null && (!turma.Ativo || (turma.DataFim.HasValue && turma.DataFim.Value < hoje)))
                {
                    v.Turma = turma;
                    lista.Add(v);
                }
            }

            return Task.FromResult(lista);
        }

        public Task AdicionarAsync(VinculoCobranca vinculo, CancellationToken cancellationToken = default)
        {
            vinculo.Id = _proximoId++;
            Vinculos.Add(vinculo);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            SalvamentosRealizados++;
            return Task.CompletedTask;
        }
    }

    public class FakeTurmaRepositoryParaVinculo : ITurmaRepository
    {
        public List<Turma> Turmas { get; } = new();

        // Alunos que o Include do EF preencheria na matricula (AlunoTurma.Aluno).
        public Dictionary<int, Aluno> AlunosConhecidos { get; } = new();

        // Prova do requisito negativo FR-013: o servico de vinculo de cobranca nunca
        // pode criar nem remover a participacao de um aluno em uma turma.
        public int ChamadasVincularAluno { get; private set; }
        public int ChamadasDesvincularAluno { get; private set; }

        public Task<Turma?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Turmas.FirstOrDefault(t => t.Id == id));

        public Task<List<Turma>> ListarAsync(string? nome, bool? ativo, CancellationToken cancellationToken = default) =>
            Task.FromResult(Turmas.ToList());

        public Task AdicionarAsync(Turma turma, CancellationToken cancellationToken = default)
        {
            Turmas.Add(turma);
            return Task.CompletedTask;
        }

        public Task VincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default)
        {
            ChamadasVincularAluno++;
            var turma = Turmas.FirstOrDefault(t => t.Id == turmaId);
            if (turma is not null && !turma.AlunosTurma.Any(at => at.AlunoId == alunoId))
            {
                turma.AlunosTurma.Add(new AlunoTurma
                {
                    TurmaId = turmaId,
                    AlunoId = alunoId,
                    Turma = turma,
                    Aluno = AlunosConhecidos.TryGetValue(alunoId, out var aluno) ? aluno : new Aluno { Id = alunoId, Nome = "Aluno " + alunoId, Ativo = true }
                });
            }

            return Task.CompletedTask;
        }

        public Task DesvincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default)
        {
            ChamadasDesvincularAluno++;
            var turma = Turmas.FirstOrDefault(t => t.Id == turmaId);
            turma?.AlunosTurma.Remove(turma.AlunosTurma.FirstOrDefault(at => at.AlunoId == alunoId)!);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public class FakeAlunoRepositoryParaVinculo : IAlunoRepository
    {
        public List<Aluno> Alunos { get; } = new();

        public Task<Aluno?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Alunos.FirstOrDefault(a => a.Id == id));

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

    // specs/052: so o metodo de contagem tem logica -- prova estrutural de
    // que VinculoCobrancaService nunca le/escreve Aula alem de contar presencas.
    public class FakeAulaRepositoryParaVinculo : IAulaRepository
    {
        public List<(int VinculoCobrancaId, int Ano, int Mes)> Presencas { get; } = new();

        // specs/058: presencas com aula (para a lista de aulas do pacote).
        public List<AulaAluno> PresencasComAula { get; } = new();

        public Task<Dictionary<int, int>> ContarPresencasPorVinculosAsync(IReadOnlyCollection<int> vinculoIds, DateOnly? de, DateOnly? ate, CancellationToken cancellationToken = default) =>
            Task.FromResult(Presencas
                .Where(p => vinculoIds.Contains(p.VinculoCobrancaId))
                .Where(p => !de.HasValue || new DateOnly(p.Ano, p.Mes, 1) >= new DateOnly(de.Value.Year, de.Value.Month, 1))
                .Where(p => !ate.HasValue || new DateOnly(p.Ano, p.Mes, 1) <= new DateOnly(ate.Value.Year, ate.Value.Month, 1))
                .GroupBy(p => p.VinculoCobrancaId)
                .ToDictionary(g => g.Key, g => g.Count()));

        public Task<List<AulaAluno>> ListarPresencasDoVinculoAsync(int vinculoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(PresencasComAula.Where(aa => aa.VinculoCobrancaId == vinculoId).OrderByDescending(aa => aa.Aula.DataInicio).ToList());

        public Task<List<Aula>> ListarNaJanelaDeAvisoAsync(DateTime agora, int diasAntes, TimeOnly hora, int? professorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Aula>> ListarPendentesDeStatusAsync(DateTime agora, DateTime desde, int? professorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<int> ContarPresencasPorVinculoECompetenciaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(Presencas.Count(p => p.VinculoCobrancaId == vinculoCobrancaId && p.Ano == competencia.Year && p.Mes == competencia.Month));

        public Task<Aula?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Aula>> ListarAsync(string? status, int? turmaId, int? alunoId, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExisteConflitoAsync(int professorId, DateOnly data, TimeOnly horaInicio, TimeOnly horaFim, int? ignorarAulaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AdicionarAsync(Aula aula, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DefinirAlunosAsync(int aulaId, IEnumerable<int> alunoIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<int>> ObterAlunosAtivosDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
