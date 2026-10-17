using SPI.Application.Lembretes.Services;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.Lembretes
{
    // Diagnostico do modulo de lembretes de aula: "Cancelado" ja era um status
    // documentado no contrato da API (LembretesController, comentario do filtro
    // Status) e ja tinha suporte visual completo no frontend (StatusBadge,
    // filtro "Status — Cancelado", KPI "Cancelados") -- mas nenhum caminho do
    // backend jamais escrevia esse valor. ExcluirAsync era o unico outro ponto
    // do ciclo de vida de um Lembrete (alem do disparo, que so produz
    // Pendente/Enviado/Falha) que poderia corresponder a ele.
    public class LembreteServiceExcluirTests
    {
        private class FakeLembreteRepositoryParaExcluir : ILembreteRepository
        {
            public Lembrete? LembreteSemeado { get; set; }
            public int Salvamentos { get; private set; }

            public Task<Lembrete?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult(LembreteSemeado?.Id == id ? LembreteSemeado : null);

            public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
            {
                Salvamentos++;
                return Task.CompletedTask;
            }

            public Task<List<Lembrete>> ListarAsync(int? turmaId, string? status, bool? ativo, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
            public Task<List<Lembrete>> ListarAtivosPorTurmaAsync(int turmaId, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
            public Task<List<Lembrete>> ListarPendentesVencidosAsync(DateTime agora, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
            public Task<Aula?> ObterProximaAulaDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
            public Task AdicionarAsync(Lembrete lembrete, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private class FakeTurmaRepositoryVazia : ITurmaRepository
        {
            public Task<Turma?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Turma>> ListarAsync(string? nome, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task AdicionarAsync(Turma turma, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task VincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task DesvincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        [Fact]
        public async Task Excluir_marca_inativo_e_status_cancelado()
        {
            var lembrete = LembreteTestHelper.CriarLembretePendente(1, DateTime.Now.AddHours(2), quantidadeAlunos: 1);
            var repositorio = new FakeLembreteRepositoryParaExcluir { LembreteSemeado = lembrete };
            var servico = new LembreteService(repositorio, new FakeTurmaRepositoryVazia());

            await servico.ExcluirAsync(lembrete.Id);

            Assert.False(lembrete.Ativo);
            Assert.Equal("Cancelado", lembrete.Status);
            Assert.Equal(1, repositorio.Salvamentos);
        }

        [Fact]
        public async Task Excluir_um_lembrete_ja_enviado_tambem_vira_cancelado()
        {
            var lembrete = LembreteTestHelper.CriarLembretePendente(1, DateTime.Now.AddHours(-2), quantidadeAlunos: 1);
            lembrete.Status = "Enviado";
            var repositorio = new FakeLembreteRepositoryParaExcluir { LembreteSemeado = lembrete };
            var servico = new LembreteService(repositorio, new FakeTurmaRepositoryVazia());

            await servico.ExcluirAsync(lembrete.Id);

            Assert.Equal("Cancelado", lembrete.Status);
        }
    }
}
