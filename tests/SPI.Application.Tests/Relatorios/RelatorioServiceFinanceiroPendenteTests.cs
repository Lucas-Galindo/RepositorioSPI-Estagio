using SPI.Application.Relatorios.Services;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.Relatorios
{
    // specs/035: testa a correcao do filtro de Receita Pendente/Saldo Previsto em
    // RelatorioService.ObterFinanceiroAsync -- ReceitaPendente passa a respeitar os
    // filtros alunoId/turmaId/materiaId ja recebidos pelo metodo, do mesmo jeito que
    // TotalRecebido/SaldoRealizado ja respeitam; DespesaPendente permanece sempre
    // global. Usa um fake manual de IRelatorioRepository, no mesmo padrao de
    // RelatorioServiceFinanceiroPorTurmaTests.cs (biblioteca de mock nao instalada
    // neste projeto de testes).
    public class RelatorioServiceFinanceiroPendenteTests
    {
        private static RelatorioService CriarServico(FakeRelatorioRepository fake) =>
            new(null!, null!, null!, fake, null!);

        private static Aluno NovoAluno(int id, string nome) => new() { Id = id, Nome = nome, Ra = id.ToString(), Ativo = true };

        private static Turma NovaTurma(int id, string nome) => new() { Id = id, Nome = nome, ProfessorId = 1, Ativo = true };

        private static Materia NovaMateria(int id, string nome) => new() { Id = id, Nome = nome, Ativo = true };

        private static Aula NovaAula(int id, Turma? turma, Materia materia) => new()
        {
            Id = id,
            TurmaId = turma?.Id,
            Turma = turma,
            MateriaId = materia.Id,
            Materia = materia,
            ProfessorId = 1,
            Status = "Realizada",
            Ativo = true
        };

        private static Pagamento NovoPagamentoPago(int id, Aluno aluno, decimal valor, Aula? aula)
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
            if (aula is not null)
            {
                pagamento.PagamentosAula.Add(new PagamentoAula { PagamentoId = id, AulaId = aula.Id, Pagamento = pagamento, Aula = aula });
            }
            return pagamento;
        }

        // Cenario base: Ana (turma A, materia Matematica) tem 100 de receita pendente
        // (80 a vencer + 20 atrasada); Beto (sem turma) tem 30 a vencer. Despesa
        // pendente e sempre global: 200 a vencer + 50 atrasada = 250.
        private static FakeRelatorioRepository CriarFakeComPendenciasDeDoisAlunos(Aluno ana, Aluno beto, Turma turmaA, Materia materia)
        {
            var fake = new FakeRelatorioRepository();
            fake.Pendencias.Add(new PendenciaSimulada(ana.Id, turmaA.Id, materia.Id, AVencer: 80m, Atrasado: 20m));
            fake.Pendencias.Add(new PendenciaSimulada(beto.Id, null, null, AVencer: 30m, Atrasado: 0m));
            fake.DespesaAVencer = 200m;
            fake.DespesaAtrasada = 50m;
            return fake;
        }

        [Fact]
        public async Task Com_Filtro_De_AlunoId_ReceitaPendente_Reflete_Apenas_Esse_Aluno()
        {
            var ana = NovoAluno(1, "Ana");
            var beto = NovoAluno(2, "Beto");
            var turmaA = NovaTurma(10, "Turma A");
            var materia = NovaMateria(100, "Matematica");
            var aulaA = NovaAula(1, turmaA, materia);

            var fake = CriarFakeComPendenciasDeDoisAlunos(ana, beto, turmaA, materia);
            fake.Pagos.Add(NovoPagamentoPago(1, ana, 150m, aulaA));
            fake.FormaPagamento = new FormaPagamento { Id = 1, Forma = "Pix", Ativo = true };
            fake.Pagos[0].FormaPagamento = fake.FormaPagamento;
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, alunoId: ana.Id, null, null);

            // FR-001: Receita Pendente reflete apenas o aluno filtrado (80+20=100), nao o total (100+30=130).
            Assert.Equal(100m, resposta.ReceitaPendente);
            Assert.NotEqual(130m, resposta.ReceitaPendente);

            // FR-005: os demais campos da resposta continuam corretos e inalterados pela correcao.
            Assert.Equal(150m, resposta.TotalRecebido);
            Assert.Equal(0m, resposta.TotalPago);
            Assert.Equal(150m, resposta.SaldoRealizado);
            Assert.Equal("Pix", Assert.Single(resposta.PorFormaPagamento).Chave);
            Assert.Equal("Ana", Assert.Single(resposta.PorAluno).Chave);
            Assert.Equal("Turma A", Assert.Single(resposta.PorTurma).Chave);
        }

        [Fact]
        public async Task Com_Filtro_De_TurmaId_Ou_MateriaId_ReceitaPendente_Reflete_Apenas_Essa_Turma_Materia()
        {
            var ana = NovoAluno(1, "Ana");
            var beto = NovoAluno(2, "Beto");
            var turmaA = NovaTurma(10, "Turma A");
            var materia = NovaMateria(100, "Matematica");

            var fakePorTurma = CriarFakeComPendenciasDeDoisAlunos(ana, beto, turmaA, materia);
            var servicoPorTurma = CriarServico(fakePorTurma);
            var respostaPorTurma = await servicoPorTurma.ObterFinanceiroAsync(null, null, null, null, turmaId: turmaA.Id, null);
            Assert.Equal(100m, respostaPorTurma.ReceitaPendente);

            var fakePorMateria = CriarFakeComPendenciasDeDoisAlunos(ana, beto, turmaA, materia);
            var servicoPorMateria = CriarServico(fakePorMateria);
            var respostaPorMateria = await servicoPorMateria.ObterFinanceiroAsync(null, null, null, null, null, materiaId: materia.Id);
            Assert.Equal(100m, respostaPorMateria.ReceitaPendente);
        }

        [Fact]
        public async Task DespesaPendente_Permanece_Global_Mesmo_Com_Filtro_Aplicado()
        {
            var ana = NovoAluno(1, "Ana");
            var beto = NovoAluno(2, "Beto");
            var turmaA = NovaTurma(10, "Turma A");
            var materia = NovaMateria(100, "Matematica");

            var fakeSemFiltro = CriarFakeComPendenciasDeDoisAlunos(ana, beto, turmaA, materia);
            var respostaSemFiltro = await CriarServico(fakeSemFiltro).ObterFinanceiroAsync(null, null, null, null, null, null);

            var fakeComFiltro = CriarFakeComPendenciasDeDoisAlunos(ana, beto, turmaA, materia);
            var respostaComFiltro = await CriarServico(fakeComFiltro).ObterFinanceiroAsync(null, null, null, alunoId: ana.Id, null, null);

            Assert.Equal(250m, respostaSemFiltro.DespesaPendente);
            Assert.Equal(250m, respostaComFiltro.DespesaPendente);
        }

        [Fact]
        public async Task SaldoPrevisto_Usa_ReceitaPendente_Filtrada_E_DespesaPendente_Global()
        {
            var ana = NovoAluno(1, "Ana");
            var beto = NovoAluno(2, "Beto");
            var turmaA = NovaTurma(10, "Turma A");
            var materia = NovaMateria(100, "Matematica");
            var aulaA = NovaAula(1, turmaA, materia);

            var fake = CriarFakeComPendenciasDeDoisAlunos(ana, beto, turmaA, materia);
            fake.Pagos.Add(NovoPagamentoPago(1, ana, 150m, aulaA));
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, alunoId: ana.Id, null, null);

            // SaldoRealizado = 150 (recebido) - 0 (pago) = 150.
            // SaldoPrevisto = SaldoRealizado + (ReceitaPendente filtrada - DespesaPendente global)
            //               = 150 + (100 - 250) = 0.
            Assert.Equal(150m, resposta.SaldoRealizado);
            Assert.Equal(100m, resposta.ReceitaPendente);
            Assert.Equal(250m, resposta.DespesaPendente);
            Assert.Equal(0m, resposta.SaldoPrevisto);
        }

        [Fact]
        public async Task Sem_Nenhum_Filtro_Comportamento_Permanece_Igual_Ao_Anterior_A_Correcao()
        {
            var ana = NovoAluno(1, "Ana");
            var beto = NovoAluno(2, "Beto");
            var turmaA = NovaTurma(10, "Turma A");
            var materia = NovaMateria(100, "Matematica");

            var fake = CriarFakeComPendenciasDeDoisAlunos(ana, beto, turmaA, materia);
            var servico = CriarServico(fake);

            var resposta = await servico.ObterFinanceiroAsync(null, null, null, null, null, null);

            // Sem filtro, ReceitaPendente continua sendo o total do negocio (100 + 30 = 130).
            Assert.Equal(130m, resposta.ReceitaPendente);
            Assert.Equal(250m, resposta.DespesaPendente);
        }

        private record PendenciaSimulada(int? AlunoId, int? TurmaId, int? MateriaId, decimal AVencer, decimal Atrasado);

        private class FakeRelatorioRepository : IRelatorioRepository
        {
            public List<Pagamento> Pagos { get; } = new();
            public List<PendenciaSimulada> Pendencias { get; } = new();
            public FormaPagamento? FormaPagamento { get; set; }
            public decimal DespesaAVencer { get; set; }
            public decimal DespesaAtrasada { get; set; }

            public Task<List<Pagamento>> ListarPagosNoPeriodoAsync(
                DateOnly? inicio, DateOnly? fim, int? formaPagamentoId, int? alunoId, int? turmaId, int? materiaId, CancellationToken cancellationToken = default) =>
                Task.FromResult(Pagos.ToList());

            // specs/035: simula, em memoria, a mesma semantica de filtro por id que
            // AplicarFiltroReceita implementa no repositorio real (RelatorioRepository.cs) --
            // correspondencia exata por AlunoId/TurmaId/MateriaId.
            public Task<(decimal AVencer, decimal Atrasado)> ObterReceitasPendentesSegregadasAsync(
                CancellationToken cancellationToken = default,
                int? turmaId = null, int? materiaId = null, int? alunoId = null,
                string? turmaNome = null, string? materiaNome = null, string? alunoBusca = null)
            {
                var filtradas = Pendencias.Where(p =>
                    (!alunoId.HasValue || p.AlunoId == alunoId) &&
                    (!turmaId.HasValue || p.TurmaId == turmaId) &&
                    (!materiaId.HasValue || p.MateriaId == materiaId));
                return Task.FromResult((filtradas.Sum(p => p.AVencer), filtradas.Sum(p => p.Atrasado)));
            }

            public Task<(decimal AVencer, decimal Atrasado)> ObterDespesasPendentesSegregadasAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult((DespesaAVencer, DespesaAtrasada));

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
