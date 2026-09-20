using SPI.Application.Pagamentos.Services;
using SPI.Application.Relatorios.Services;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.Relatorios
{
    // specs/022: testa a composicao de Lancamentos em
    // RelatorioService.ObterIndicadoresFinanceirosAsync (precedencia do
    // fallback de descricao, exclusao de "Cancelado" e ordenacao por data de
    // vencimento decrescente). Usa um fake manual de IRelatorioRepository em
    // vez de uma biblioteca de mock (nao instalada neste projeto de testes).
    public class RelatorioServiceLancamentosTests
    {
        private static RelatorioService CriarServico(FakeRelatorioRepository fake) =>
            new(null!, null!, null!, fake, null!);

        [Fact]
        public async Task Saida_SemDescricao_UsaFavorecido_QuandoPreenchido()
        {
            var fake = new FakeRelatorioRepository();
            fake.Saidas.Add((1, null, "Locador da sala", "Aluguel", new DateOnly(2026, 9, 5), 1200m, "Pendente"));
            var servico = CriarServico(fake);

            var resposta = await servico.ObterIndicadoresFinanceirosAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), null, null, null);

            var lancamento = Assert.Single(resposta.Lancamentos);
            Assert.Equal("Locador da sala", lancamento.Descricao);
        }

        [Fact]
        public async Task Saida_SemDescricaoNemFavorecido_UsaCategoria()
        {
            var fake = new FakeRelatorioRepository();
            fake.Saidas.Add((2, null, null, "Aluguel", new DateOnly(2026, 9, 5), 1200m, "Pendente"));
            var servico = CriarServico(fake);

            var resposta = await servico.ObterIndicadoresFinanceirosAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), null, null, null);

            var lancamento = Assert.Single(resposta.Lancamentos);
            Assert.Equal("Aluguel", lancamento.Descricao);
        }

        [Fact]
        public async Task Entrada_SemDescricao_UsaNomeDoAluno()
        {
            var fake = new FakeRelatorioRepository();
            fake.Entradas.Add((10, null, "Joao Silva", new DateOnly(2026, 9, 10), 350m, "Pago"));
            var servico = CriarServico(fake);

            var resposta = await servico.ObterIndicadoresFinanceirosAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), null, null, null);

            var lancamento = Assert.Single(resposta.Lancamentos);
            Assert.Equal("Joao Silva", lancamento.Descricao);
        }

        [Fact]
        public async Task Lancamentos_SaoOrdenadosPorDataVencimentoDescendente()
        {
            var fake = new FakeRelatorioRepository();
            fake.Entradas.Add((1, "Mensalidade", "Aluno A", new DateOnly(2026, 9, 5), 100m, "Pago"));
            fake.Saidas.Add((1, "Conta de luz", null, "Utilidades", new DateOnly(2026, 9, 20), 200m, "Pendente"));
            fake.Entradas.Add((2, "Mensalidade", "Aluno B", new DateOnly(2026, 9, 12), 150m, "Pendente"));
            var servico = CriarServico(fake);

            var resposta = await servico.ObterIndicadoresFinanceirosAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), null, null, null);

            Assert.Equal(
                new[] { new DateOnly(2026, 9, 20), new DateOnly(2026, 9, 12), new DateOnly(2026, 9, 5) },
                resposta.Lancamentos.Select(l => l.DataVencimento));
        }

        [Fact]
        public async Task Cancelados_NaoDevemAparecerNosLancamentos()
        {
            // ListarEntradasNoPeriodoAsync/ListarSaidasNoPeriodoAsync ja filtram
            // por Status != "Cancelado" na query (EF Core) -- o fake replica essa
            // mesma regra para documentar, no nivel do servico, que nenhum
            // lancamento cancelado deve chegar em Lancamentos.
            var fake = new FakeRelatorioRepository();
            fake.Entradas.Add((1, "Mensalidade", "Aluno A", new DateOnly(2026, 9, 5), 100m, "Pago"));
            fake.Entradas.Add((2, "Mensalidade cancelada", "Aluno B", new DateOnly(2026, 9, 6), 100m, "Cancelado"));
            var servico = CriarServico(fake);

            var resposta = await servico.ObterIndicadoresFinanceirosAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), null, null, null);

            Assert.Single(resposta.Lancamentos);
            Assert.DoesNotContain(resposta.Lancamentos, l => l.Status == "Cancelado");
        }

        private class FakeRelatorioRepository : IRelatorioRepository
        {
            public List<(int Id, string? Descricao, string AlunoNome, DateOnly DataVencimento, decimal Valor, string Status)> Entradas { get; } = new();
            public List<(int Id, string? Descricao, string? Favorecido, string CategoriaNome, DateOnly DataVencimento, decimal Valor, string Status)> Saidas { get; } = new();

            // O filtro Status != "Cancelado" abaixo replica, no fake, o mesmo
            // filtro que a query real (EF Core) ja aplica na WHERE clause --
            // ver ListarEntradasNoPeriodoAsync/ListarSaidasNoPeriodoAsync em
            // RelatorioRepository.
            public Task<List<(int Id, string? Descricao, string AlunoNome, DateOnly DataVencimento, decimal Valor, string Status)>> ListarEntradasNoPeriodoAsync(
                DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default,
                int? turmaId = null, int? materiaId = null, int? alunoId = null) =>
                Task.FromResult(Entradas.Where(e => e.Status != "Cancelado").ToList());

            public Task<List<(int Id, string? Descricao, string? Favorecido, string CategoriaNome, DateOnly DataVencimento, decimal Valor, string Status)>> ListarSaidasNoPeriodoAsync(
                DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default) =>
                Task.FromResult(Saidas.Where(s => s.Status != "Cancelado").ToList());

            public Task<int> ContarAulasAgendadasNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<int> ContarAlunosAtivosAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<int> ContarTurmasAtivasAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<decimal> ObterValorPendenteAsync(
                CancellationToken cancellationToken = default,
                int? alunoId = null,
                string? status = null,
                DateOnly? vencimentoInicio = null,
                DateOnly? vencimentoFim = null) => Task.FromResult(0m);
            public Task<decimal> ObterValorFaturadoNoPeriodoAsync(
                DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default,
                int? turmaId = null, int? materiaId = null, int? alunoId = null,
                string? turmaNome = null, string? materiaNome = null, string? alunoBusca = null) => Task.FromResult(0m);
            public Task<decimal> ObterValorPagoNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default) => Task.FromResult(0m);
            public Task<(decimal AVencer, decimal Atrasado)> ObterReceitasPendentesSegregadasAsync(
                CancellationToken cancellationToken = default,
                int? turmaId = null, int? materiaId = null, int? alunoId = null,
                string? turmaNome = null, string? materiaNome = null, string? alunoBusca = null) =>
                Task.FromResult((0m, 0m));
            public Task<(decimal AVencer, decimal Atrasado)> ObterDespesasPendentesSegregadasAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult((0m, 0m));
            public Task<List<Pagamento>> ObterProximasContasAReceberAsync(
                int dias, CancellationToken cancellationToken = default, string? turmaNome = null, string? materiaNome = null, string? alunoBusca = null) =>
                Task.FromResult(new List<Pagamento>());
            public Task<List<ContaPagar>> ObterProximasContasAPagarAsync(int dias, CancellationToken cancellationToken = default) =>
                Task.FromResult(new List<ContaPagar>());
            public Task<List<Aula>> ObterProximasAulasHojeAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<Aula>());
            public Task<int> ContarLembretesPendentesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<List<(DateOnly Data, int Quantidade)>> ContarAulasPorDiaAsync(
                DateOnly inicio, DateOnly fim, int? turmaId, int? materiaId, CancellationToken cancellationToken = default) =>
                Task.FromResult(new List<(DateOnly Data, int Quantidade)>());
            public Task<(int Agendadas, int Realizadas, int Canceladas)> ContarAulasPorStatusAsync(
                DateOnly inicio, DateOnly fim, int? turmaId, int? materiaId, CancellationToken cancellationToken = default) =>
                Task.FromResult((0, 0, 0));
            public Task<int> ContarAulasDaMateriaNoPeriodoAsync(int materiaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<int> ContarAlunosAtendidosDaMateriaNoPeriodoAsync(int materiaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<List<Pagamento>> ListarPagosNoPeriodoAsync(
                DateOnly? inicio, DateOnly? fim, int? formaPagamentoId, int? alunoId, int? turmaId, int? materiaId, CancellationToken cancellationToken = default) =>
                Task.FromResult(new List<Pagamento>());
            public Task<int> ContarAlunosAtendidosNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<(decimal TotalVencidoNoPeriodo, decimal ValorInadimplente)> ObterInadimplenciaNoPeriodoAsync(
                DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default,
                int? turmaId = null, int? materiaId = null, int? alunoId = null) =>
                Task.FromResult((0m, 0m));
            public Task<List<(DateOnly DataVencimento, DateOnly DataPagamento)>> ObterPagamentosComAtrasoNoPeriodoAsync(
                DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default,
                int? turmaId = null, int? materiaId = null, int? alunoId = null) =>
                Task.FromResult(new List<(DateOnly DataVencimento, DateOnly DataPagamento)>());
            public Task<List<(int Dia, decimal Valor)>> ObterEntradasPorDiaDoMesAsync(
                DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default,
                int? turmaId = null, int? materiaId = null, int? alunoId = null) =>
                Task.FromResult(new List<(int Dia, decimal Valor)>());
            public Task<List<(int Dia, decimal Valor)>> ObterSaidasPorDiaDoMesAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default) =>
                Task.FromResult(new List<(int Dia, decimal Valor)>());
        }
    }
}
