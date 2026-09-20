using SPI.Application.Relatorios.Services;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.Relatorios
{
    // specs/029: testa a correcao do agrupamento "Por Turma" em
    // RelatorioService.ObterFinanceiroAsync -- a turma de um Pagamento passa a
    // vir das Aulas efetivamente cobertas por ele (via PagamentoAula/Aula.TurmaId),
    // nao mais da lista geral de turmas do Aluno. Usa um fake manual de
    // IRelatorioRepository, no mesmo padrao de RelatorioServiceLancamentosTests.cs
    // (biblioteca de mock nao instalada neste projeto de testes).
    public class RelatorioServiceFinanceiroPorTurmaTests
    {
        private static RelatorioService CriarServico(FakeRelatorioRepository fake) =>
            new(null!, null!, null!, fake, null!);

        private static Turma NovaTurma(int id, string nome) => new() { Id = id, Nome = nome, ProfessorId = 1, Ativo = true };

        private static Aluno NovoAluno(int id, string nome, params Turma[] turmas)
        {
            var aluno = new Aluno { Id = id, Nome = nome, Ra = id.ToString(), Ativo = true };
            foreach (var turma in turmas)
            {
                aluno.AlunosTurma.Add(new AlunoTurma { AlunoId = id, TurmaId = turma.Id, Aluno = aluno, Turma = turma });
            }
            return aluno;
        }

        private static Aula NovaAula(int id, Turma? turma) => new()
        {
            Id = id,
            TurmaId = turma?.Id,
            Turma = turma,
            MateriaId = 1,
            ProfessorId = 1,
            Status = "Realizada",
            Ativo = true
        };

        private static Pagamento NovoPagamento(int id, Aluno aluno, decimal valor, params Aula[] aulas)
        {
            var pagamento = new Pagamento
            {
                Id = id,
                AlunoId = aluno.Id,
                Aluno = aluno,
                ValorFinal = valor,
                Status = "Pago",
                DataVencimento = new DateOnly(2026, 9, 5),
                DataPagamento = new DateOnly(2026, 9, 5)
            };
            foreach (var aula in aulas)
            {
                pagamento.PagamentosAula.Add(new PagamentoAula { PagamentoId = id, AulaId = aula.Id, Pagamento = pagamento, Aula = aula });
            }
            return pagamento;
        }

        [Fact]
        public async Task Pagamento_De_Turma_Unica_E_Agrupado_Na_Turma_Correta_Mesmo_Com_Aluno_Em_Outra_Turma()
        {
            var turmaA = NovaTurma(1, "Turma A");
            var turmaB = NovaTurma(2, "Turma B");
            var aluno = NovoAluno(1, "Ana", turmaA, turmaB);
            var aula = NovaAula(1, turmaA);
            var pagamento = NovoPagamento(1, aluno, 100m, aula);

            var fake = new FakeRelatorioRepository();
            fake.Pagos.Add(pagamento);
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, null, null, null);

            Assert.Contains(resposta.PorTurma, i => i.Chave == "Turma A" && i.Total == 100m);
            Assert.DoesNotContain(resposta.PorTurma, i => i.Chave == "Turma B");
        }

        [Fact]
        public async Task Pagamento_De_Aluno_Em_Turma_Unica_Continua_Correto()
        {
            var turmaA = NovaTurma(1, "Turma A");
            var aluno = NovoAluno(1, "Ana", turmaA);
            var aula = NovaAula(1, turmaA);
            var pagamento = NovoPagamento(1, aluno, 150m, aula);

            var fake = new FakeRelatorioRepository();
            fake.Pagos.Add(pagamento);
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, null, null, null);

            var item = Assert.Single(resposta.PorTurma);
            Assert.Equal("Turma A", item.Chave);
            Assert.Equal(150m, item.Total);
        }

        [Fact]
        public async Task Pagamento_De_Aula_Individual_Sem_Turma_Vai_Para_Atendimento_Particular()
        {
            var aluno = NovoAluno(1, "Ana");
            var aula = NovaAula(1, turma: null);
            var pagamento = NovoPagamento(1, aluno, 80m, aula);

            var fake = new FakeRelatorioRepository();
            fake.Pagos.Add(pagamento);
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, null, null, null);

            var item = Assert.Single(resposta.PorTurma);
            Assert.Equal("Atendimento particular", item.Chave);
        }

        [Fact]
        public async Task Pagamento_Sem_Nenhuma_Aula_Vinculada_Vai_Para_Atendimento_Particular()
        {
            var aluno = NovoAluno(1, "Ana");
            var pagamento = NovoPagamento(1, aluno, 50m);

            var fake = new FakeRelatorioRepository();
            fake.Pagos.Add(pagamento);
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, null, null, null);

            var item = Assert.Single(resposta.PorTurma);
            Assert.Equal("Atendimento particular", item.Chave);
        }

        [Fact]
        public async Task Pagamento_Com_Aulas_De_Turmas_Diferentes_Vai_Para_Multiplas_Turmas()
        {
            var turmaA = NovaTurma(1, "Turma A");
            var turmaB = NovaTurma(2, "Turma B");
            var aluno = NovoAluno(1, "Ana", turmaA, turmaB);
            var aulaA = NovaAula(1, turmaA);
            var aulaB = NovaAula(2, turmaB);
            var pagamento = NovoPagamento(1, aluno, 200m, aulaA, aulaB);

            var fake = new FakeRelatorioRepository();
            fake.Pagos.Add(pagamento);
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, null, null, null);

            var item = Assert.Single(resposta.PorTurma);
            Assert.Equal("Múltiplas turmas", item.Chave);
            Assert.Equal(200m, item.Total);
        }

        [Fact]
        public async Task Soma_Dos_Grupos_Por_Turma_Bate_Com_Total_Recebido()
        {
            var turmaA = NovaTurma(1, "Turma A");
            var turmaB = NovaTurma(2, "Turma B");
            var aluno1 = NovoAluno(1, "Ana", turmaA, turmaB);
            var aluno2 = NovoAluno(2, "Beto");

            var aulaTurmaUnica = NovaAula(1, turmaA);
            var aulaSemTurma = NovaAula(2, turma: null);
            var aulaMultiplaA = NovaAula(3, turmaA);
            var aulaMultiplaB = NovaAula(4, turmaB);

            var pagTurmaUnica = NovoPagamento(1, aluno1, 100m, aulaTurmaUnica);
            var pagSemTurma = NovoPagamento(2, aluno2, 50m, aulaSemTurma);
            var pagMultiplasTurmas = NovoPagamento(3, aluno1, 200m, aulaMultiplaA, aulaMultiplaB);

            var fake = new FakeRelatorioRepository();
            fake.Pagos.AddRange(new[] { pagTurmaUnica, pagSemTurma, pagMultiplasTurmas });
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, null, null, null);

            Assert.Equal(resposta.TotalRecebido, resposta.PorTurma.Sum(i => i.Total));
        }

        [Fact]
        public async Task Pagamento_De_Duas_Aulas_Da_Mesma_Turma_Nao_Duplica()
        {
            var turmaA = NovaTurma(1, "Turma A");
            var aluno = NovoAluno(1, "Ana", turmaA);
            var aula1 = NovaAula(1, turmaA);
            var aula2 = NovaAula(2, turmaA);
            var pagamento = NovoPagamento(1, aluno, 300m, aula1, aula2);

            var fake = new FakeRelatorioRepository();
            fake.Pagos.Add(pagamento);
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, null, null, null);

            var item = Assert.Single(resposta.PorTurma);
            Assert.Equal("Turma A", item.Chave);
            Assert.Equal(300m, item.Total);
        }

        [Fact]
        public async Task Pagamento_Com_Turma_E_Aula_Sem_Turma_Vai_Para_Multiplas_Turmas()
        {
            var turmaA = NovaTurma(1, "Turma A");
            var aluno = NovoAluno(1, "Ana", turmaA);
            var aulaComTurma = NovaAula(1, turmaA);
            var aulaSemTurma = NovaAula(2, turma: null);
            var pagamento = NovoPagamento(1, aluno, 120m, aulaComTurma, aulaSemTurma);

            var fake = new FakeRelatorioRepository();
            fake.Pagos.Add(pagamento);
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, null, null, null);

            var item = Assert.Single(resposta.PorTurma);
            Assert.Equal("Múltiplas turmas", item.Chave);
        }

        [Fact]
        public async Task PorFormaPagamento_E_PorAluno_Nao_Sao_Afetados()
        {
            var formaPix = new FormaPagamento { Id = 1, Forma = "Pix", Ativo = true };
            var turmaA = NovaTurma(1, "Turma A");
            var turmaB = NovaTurma(2, "Turma B");
            var aluno = NovoAluno(1, "Ana", turmaA, turmaB);
            var aula1 = NovaAula(1, turmaA);
            var aula2 = NovaAula(2, turmaB);

            var pagamento1 = NovoPagamento(1, aluno, 100m, aula1);
            pagamento1.FormaPagamento = formaPix;
            var pagamento2 = NovoPagamento(2, aluno, 150m, aula2);
            pagamento2.FormaPagamento = formaPix;

            var fake = new FakeRelatorioRepository();
            fake.Pagos.AddRange(new[] { pagamento1, pagamento2 });
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, null, null, null);

            var formaItem = Assert.Single(resposta.PorFormaPagamento);
            Assert.Equal("Pix", formaItem.Chave);
            Assert.Equal(250m, formaItem.Total);

            var alunoItem = Assert.Single(resposta.PorAluno);
            Assert.Equal("Ana", alunoItem.Chave);
            Assert.Equal(250m, alunoItem.Total);
        }

        private class FakeRelatorioRepository : IRelatorioRepository
        {
            public List<Pagamento> Pagos { get; } = new();

            public Task<List<Pagamento>> ListarPagosNoPeriodoAsync(
                DateOnly? inicio, DateOnly? fim, int? formaPagamentoId, int? alunoId, int? turmaId, int? materiaId, CancellationToken cancellationToken = default) =>
                Task.FromResult(Pagos.ToList());

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
            public Task<List<(int Id, string? Descricao, string AlunoNome, DateOnly DataVencimento, decimal Valor, string Status)>> ListarEntradasNoPeriodoAsync(
                DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default,
                int? turmaId = null, int? materiaId = null, int? alunoId = null) =>
                Task.FromResult(new List<(int Id, string? Descricao, string AlunoNome, DateOnly DataVencimento, decimal Valor, string Status)>());
            public Task<List<(int Id, string? Descricao, string? Favorecido, string CategoriaNome, DateOnly DataVencimento, decimal Valor, string Status)>> ListarSaidasNoPeriodoAsync(
                DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default) =>
                Task.FromResult(new List<(int Id, string? Descricao, string? Favorecido, string CategoriaNome, DateOnly DataVencimento, decimal Valor, string Status)>());
        }
    }
}
