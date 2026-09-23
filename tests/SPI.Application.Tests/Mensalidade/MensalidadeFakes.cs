using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Repositories;

namespace SPI.Application.Tests.Mensalidade
{
    // Fakes manuais (biblioteca de mock nao instalada neste projeto de testes), no
    // mesmo padrao de Aulas/AulaServiceFakes.cs e VinculosCobranca/VinculoCobrancaFakes.cs.

    public class FakeVinculoCobrancaRepositoryParaMensalidade : IVinculoCobrancaRepository
    {
        public List<VinculoCobranca> Vinculos { get; } = new();

        public Task<List<VinculoCobranca>> ListarAtivosPorModalidadeAsync(ModalidadeCobranca modalidade, CancellationToken cancellationToken = default) =>
            Task.FromResult(Vinculos.Where(v => v.Ativo && v.Modalidade == modalidade).ToList());

        // Prova estrutural: o job de mensalidade nunca pode escrever nem consultar
        // o cadastro do vinculo alem da listagem por modalidade (specs/037 continua
        // sendo o unico dono do CRUD do vinculo).
        public Task<VinculoCobranca?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<VinculoCobranca>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExisteAtivoAsync(int alunoId, int? turmaId, int? ignorarId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<VinculoCobranca?> ObterAtivoPorAlunoEContextoAsync(int alunoId, int? turmaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AdicionarAsync(VinculoCobranca vinculo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakePagamentoRepositoryParaMensalidade : IPagamentoRepository
    {
        private int _proximoId = 1;

        public List<Pagamento> Gerados { get; } = new();
        public int Salvamentos { get; private set; }

        // Seedavel pelo teste para simular idempotencia pre-existente (ex.: o
        // sistema foi reiniciado e a cobranca ja tinha sido gerada antes).
        public HashSet<(int VinculoCobrancaId, DateOnly Competencia)> JaGerados { get; } = new();

        // Hook para simular falha ao gerar a cobranca de um vinculo especifico (FR-011).
        public Func<Pagamento, bool>? FalharAoAdicionarSe { get; set; }

        public Task<bool> ExisteMensalidadeGeradaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                JaGerados.Contains((vinculoCobrancaId, competencia))
                || Gerados.Any(p => p.VinculoCobrancaId == vinculoCobrancaId && p.Competencia == competencia));

        public Task AdicionarAsync(Pagamento pagamento, CancellationToken cancellationToken = default)
        {
            if (FalharAoAdicionarSe?.Invoke(pagamento) == true)
            {
                throw new InvalidOperationException("Falha simulada ao gerar a cobranca (teste de isolamento, FR-011).");
            }

            pagamento.Id = _proximoId++;
            Gerados.Add(pagamento);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.CompletedTask;
        }

        public Task<Pagamento?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<List<Pagamento>> ListarAsync(int? alunoId, string? status, DateOnly? vencimentoInicio, DateOnly? vencimentoFim, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task VincularAulaAsync(int pagamentoId, int aulaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task AtualizarStatusViaProcedureAsync(int pagamentoId, string novoStatus, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    public class FakeCategoriaReceitaRepositoryParaMensalidade : ICategoriaReceitaRepository
    {
        public CategoriaReceita? Mensalidade { get; set; }

        public Task<CategoriaReceita?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) =>
            Task.FromResult(nome == "Mensalidade" ? Mensalidade : null);

        public Task<CategoriaReceita?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<CategoriaReceita>> ListarAtivasAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
