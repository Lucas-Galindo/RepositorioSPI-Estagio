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

        public Task<bool> ExisteAtivoAsync(int alunoId, int? turmaId, int? ignorarId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Vinculos.Any(v =>
                v.AlunoId == alunoId && v.TurmaId == turmaId && v.Ativo && (ignorarId is null || v.Id != ignorarId)));

        // specs/038
        public Task<VinculoCobranca?> ObterAtivoPorAlunoEContextoAsync(int alunoId, int? turmaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Vinculos.FirstOrDefault(v => v.AlunoId == alunoId && v.TurmaId == turmaId && v.Ativo));

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
            return Task.CompletedTask;
        }

        public Task DesvincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default)
        {
            ChamadasDesvincularAluno++;
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
}
