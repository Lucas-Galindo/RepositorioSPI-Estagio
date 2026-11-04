using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/062: planos Semestral e Anual. O cadastro cria o vinculo e TODAS as parcelas (contas a receber) de uma
    // vez; o plano nao e editado nem reativado; a exclusao cancela so as parcelas pendentes futuras.
    public class VinculoCobrancaServicePlanosTests
    {
        private const int AlunoId = 1;

        private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Now);

        // Pagamentos em memoria: so o que o servico de vinculo usa. Nao tem Salvar proprio: quem grava e o repositorio
        // de vinculos (um unico SalvarAlteracoesAsync), como o contexto compartilhado do banco real.
        private sealed class PagamentosFake : IPagamentoRepository
        {
            private int _proximoId = 1;
            public List<Pagamento> Pagamentos { get; } = new();
            public int ChamadasListarPorVinculos { get; private set; }
            public int ChamadasSalvar { get; private set; }

            public Task AdicionarAsync(Pagamento pagamento, CancellationToken cancellationToken = default)
            {
                pagamento.Id = _proximoId++;
                pagamento.VinculoCobrancaId = pagamento.VinculoCobranca?.Id;
                Pagamentos.Add(pagamento);
                return Task.CompletedTask;
            }

            public Task<List<Pagamento>> ListarPorVinculosAsync(IReadOnlyCollection<int> vinculoIds, CancellationToken cancellationToken = default)
            {
                ChamadasListarPorVinculos++;
                return Task.FromResult(Pagamentos.Where(p => p.VinculoCobrancaId.HasValue && vinculoIds.Contains(p.VinculoCobrancaId.Value)).OrderBy(p => p.DataVencimento).ToList());
            }

            public Task<List<Pagamento>> ListarPendentesFuturosDoVinculoAsync(int vinculoId, DateOnly hoje, CancellationToken cancellationToken = default) =>
                Task.FromResult(Pagamentos.Where(p => p.VinculoCobrancaId == vinculoId && p.Status == "Pendente" && p.DataVencimento > hoje).ToList());

            public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
            {
                ChamadasSalvar++;
                return Task.CompletedTask;
            }

            public Task<Pagamento?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Pagamento>> ListarAsync(int? alunoId, string? status, DateOnly? vencimentoInicio, DateOnly? vencimentoFim, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<bool> ExisteMensalidadeGeradaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Pagamento>> ListarMensalidadesParaLembreteVencimentoAsync(DateOnly dataVencimentoAlvo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Pagamento>> ListarPendentesVencidosAsync(DateOnly hoje, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task VincularAulaAsync(int pagamentoId, int aulaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task AtualizarStatusViaProcedureAsync(int pagamentoId, string novoStatus, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private sealed class CategoriasFake : ICategoriaReceitaRepository
        {
            public List<CategoriaReceita> Categorias { get; } = new()
            {
                new CategoriaReceita { Id = 6, Nome = "Semestralidade", Ativo = true },
                new CategoriaReceita { Id = 7, Nome = "Anuidade", Ativo = true },
            };

            public Task<CategoriaReceita?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Categorias.FirstOrDefault(c => c.Id == id));
            public Task<CategoriaReceita?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(Categorias.FirstOrDefault(c => c.Nome == nome));
            public Task<List<CategoriaReceita>> ListarAtivasAsync(CancellationToken cancellationToken = default) => Task.FromResult(Categorias.ToList());
        }

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly PagamentosFake _pagamentos = new();
        private readonly CategoriasFake _categorias = new();
        private readonly VinculoCobrancaService _servico;

        public VinculoCobrancaServicePlanosTests()
        {
            var alunos = new FakeAlunoRepositoryParaVinculo();
            alunos.Alunos.Add(new Aluno { Id = AlunoId, Nome = "Ana", Ra = "RA1", Ativo = true, ValorAula = 80m });
            _servico = new VinculoCobrancaService(_vinculos, alunos, new FakeTurmaRepositoryParaVinculo(), new FakeAulaRepositoryParaVinculo(), _pagamentos, _categorias);
        }

        private static VinculoCobrancaRequest Plano(ModalidadeCobranca modalidade, DateOnly inicio, decimal total, params (DateOnly Vencimento, decimal Valor)[] parcelas) => new()
        {
            Modalidade = modalidade,
            Valor = total,
            DataInicio = inicio,
            Parcelas = parcelas.Select(p => new ParcelaRequest { Vencimento = p.Vencimento, Valor = p.Valor }).ToList(),
        };

        // N parcelas mensais iguais, a primeira vencendo no dia do inicio.
        private static VinculoCobrancaRequest PlanoMensal(ModalidadeCobranca modalidade, DateOnly inicio, int quantidade, decimal valorParcela = 300m) =>
            Plano(modalidade, inicio, valorParcela * quantidade, Enumerable.Range(0, quantidade).Select(i => (inicio.AddMonths(i), valorParcela)).ToArray());

        private VinculoCobranca SemearAtivo(ModalidadeCobranca modalidade, int? turmaId = null) =>
            _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, Modalidade = modalidade, TurmaId = turmaId, Valor = 100m, Ativo = true });

        // ---------- cadastro: parcelas, categoria, descricao, competencia ----------

        [Fact]
        public async Task Cadastro_a_vista_gera_uma_parcela()
        {
            var inicio = new DateOnly(2026, 11, 1);

            var resposta = await _servico.CadastrarAsync(AlunoId, Plano(ModalidadeCobranca.Semestral, inicio, 1800m, (new DateOnly(2026, 11, 5), 1800m)));

            var parcela = Assert.Single(resposta.Parcelas!);
            Assert.Equal(1800m, parcela.Valor);
            Assert.Equal("Semestralidade - parcela 1/1", parcela.Descricao);
            Assert.Single(_pagamentos.Pagamentos);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Semestral, 6, "Semestralidade", 6)]
        [InlineData(ModalidadeCobranca.Anual, 12, "Anuidade", 7)]
        public async Task Plano_cria_uma_conta_a_receber_por_parcela_com_categoria_descricao_e_competencia(ModalidadeCobranca modalidade, int quantidade, string nome, int categoriaId)
        {
            var inicio = new DateOnly(2026, 11, 1);

            var resposta = await _servico.CadastrarAsync(AlunoId, PlanoMensal(modalidade, inicio, quantidade));

            Assert.Equal(quantidade, _pagamentos.Pagamentos.Count);
            Assert.Equal(quantidade, resposta.Parcelas!.Count);
            for (var i = 0; i < quantidade; i++)
            {
                var pagamento = _pagamentos.Pagamentos[i];
                Assert.Equal("Pendente", pagamento.Status);
                Assert.Equal(AlunoId, pagamento.AlunoId);
                Assert.Equal(categoriaId, pagamento.CategoriaReceitaId);
                Assert.Equal($"{nome} - parcela {i + 1}/{quantidade}", pagamento.Descricao);
                Assert.Equal(inicio.AddMonths(i), pagamento.DataVencimento);
                Assert.Equal(new DateOnly(pagamento.DataVencimento.Year, pagamento.DataVencimento.Month, 1), pagamento.Competencia);
                Assert.Equal(resposta.Id, pagamento.VinculoCobrancaId);
            }
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Semestral, 2026, 11, 1, 2027, 4, 30)]
        [InlineData(ModalidadeCobranca.Semestral, 2026, 8, 31, 2027, 2, 27)] // 31/08 + 6 meses cai em 28/02 (mes mais curto)
        [InlineData(ModalidadeCobranca.Anual, 2026, 11, 1, 2027, 10, 31)]
        [InlineData(ModalidadeCobranca.Anual, 2023, 3, 1, 2024, 2, 29)]      // ano bissexto
        public async Task Data_de_fim_e_o_inicio_mais_o_periodo_menos_um_dia(ModalidadeCobranca modalidade, int ai, int mi, int di, int af, int mf, int df)
        {
            var resposta = await _servico.CadastrarAsync(AlunoId, PlanoMensal(modalidade, new DateOnly(ai, mi, di), 1));

            Assert.Equal(new DateOnly(ai, mi, di), resposta.DataInicio);
            Assert.Equal(new DateOnly(af, mf, df), resposta.DataFim);
            Assert.Null(resposta.TurmaId);
        }

        [Fact]
        public async Task A_soma_das_parcelas_pode_diferir_do_valor_total()
        {
            var resposta = await _servico.CadastrarAsync(AlunoId, Plano(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 1800m,
                (new DateOnly(2026, 11, 5), 310m), (new DateOnly(2026, 12, 5), 310m)));

            Assert.Equal(1800m, resposta.Valor);
            Assert.Equal(620m, resposta.Parcelas!.Sum(p => p.Valor));
        }

        [Fact]
        public async Task Plano_com_inicio_no_passado_gera_parcelas_pendentes_inclusive_vencidas()
        {
            var inicio = Hoje.AddMonths(-3);

            var resposta = await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, inicio, 3));

            Assert.All(_pagamentos.Pagamentos, p => Assert.Equal("Pendente", p.Status));
            Assert.Contains(resposta.Parcelas!, p => p.Status == "Atrasado");
        }

        [Fact]
        public async Task Parcela_com_vencimento_antes_do_inicio_e_aceita()
        {
            var resposta = await _servico.CadastrarAsync(AlunoId, Plano(ModalidadeCobranca.Anual, new DateOnly(2026, 11, 1), 1200m, (new DateOnly(2026, 10, 20), 600m), (new DateOnly(2026, 12, 20), 600m)));

            Assert.Equal(2, resposta.Parcelas!.Count);
        }

        [Fact]
        public async Task Sem_a_categoria_do_plano_o_cadastro_e_recusado_e_nada_e_gravado()
        {
            _categorias.Categorias.Clear();

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 2)));

            Assert.Contains("migracao 24", excecao.Message);
            Assert.Empty(_pagamentos.Pagamentos);
            Assert.Empty(_vinculos.Vinculos);
        }

        // ---------- FR-006: tudo ou nada ----------

        [Fact]
        public async Task O_vinculo_e_as_parcelas_saem_num_unico_salvamento_e_so_o_repositorio_de_vinculos_salva()
        {
            await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Anual, new DateOnly(2026, 11, 1), 12));

            Assert.Equal(1, _vinculos.SalvamentosRealizados);
            Assert.Equal(0, _pagamentos.ChamadasSalvar);
        }

        [Fact]
        public async Task Falha_ao_salvar_nao_grava_nada_o_erro_sobe_e_nenhum_salvamento_acontece()
        {
            _vinculos.FalharAoSalvar = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 6)));

            Assert.Equal(0, _vinculos.SalvamentosRealizados);
            Assert.Equal(0, _pagamentos.ChamadasSalvar);
        }

        // ---------- FR-007: nunca parcelas duplicadas ----------

        [Fact]
        public async Task Dois_cadastros_iguais_nao_geram_parcelas_duplicadas()
        {
            var pedido = PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 6);
            await _servico.CadastrarAsync(AlunoId, pedido);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.CadastrarAsync(AlunoId, pedido));

            Assert.Equal("Ja existe um plano com periodo sobreposto para este aluno.", excecao.Message);
            Assert.Equal(6, _pagamentos.Pagamentos.Count);
            Assert.Equal(6, _pagamentos.Pagamentos.Select(p => (p.VinculoCobrancaId, p.Competencia)).Distinct().Count());
        }

        // ---------- combinacoes (FR-008) ----------

        [Theory]
        [InlineData(ModalidadeCobranca.Mensalidade)]
        [InlineData(ModalidadeCobranca.Pacote)]
        [InlineData(ModalidadeCobranca.Avulsa)]
        public async Task Plano_e_recusado_se_o_aluno_tem_outro_vinculo_ativo(ModalidadeCobranca existente)
        {
            SemearAtivo(existente);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 2)));

            Assert.Contains("ja tem outros vinculos ativos", excecao.Message);
            Assert.Empty(_pagamentos.Pagamentos);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Pacote)]
        [InlineData(ModalidadeCobranca.Avulsa)]
        public async Task Pacote_e_avulsa_sao_recusados_com_plano_ativo(ModalidadeCobranca novo)
        {
            await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 2));

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.CadastrarAsync(AlunoId, new VinculoCobrancaRequest { Modalidade = novo, Valor = 100m }));

            Assert.Equal("O aluno tem um plano (mensalidade, semestralidade ou anuidade) ativo, que ja cobre todas as aulas.", excecao.Message);
        }

        [Fact]
        public async Task Mensalidade_e_recusada_com_plano_ativo()
        {
            await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Anual, new DateOnly(2026, 11, 1), 2));

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.CadastrarAsync(AlunoId, new VinculoCobrancaRequest { Modalidade = ModalidadeCobranca.Mensalidade, Valor = 250m }));
        }

        [Fact]
        public async Task Renovacao_antecipada_com_periodo_que_nao_se_sobrepoe_e_aceita()
        {
            await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 2)); // ate 30/04/2027

            var renovacao = await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2027, 5, 1), 2));

            Assert.Equal(new DateOnly(2027, 10, 31), renovacao.DataFim);
            Assert.Equal(4, _pagamentos.Pagamentos.Count);
        }

        [Fact]
        public async Task Periodo_sobreposto_e_recusado_inclusive_no_ultimo_dia_do_plano_vigente()
        {
            await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 2)); // fim 30/04/2027

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Anual, new DateOnly(2027, 4, 30), 2)));
        }

        // ---------- FR-011: o plano nao e editado nem reativado ----------

        [Fact]
        public async Task Plano_salvo_nao_pode_ser_editado()
        {
            var plano = await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 2));

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.AtualizarAsync(AlunoId, plano.Id,
                PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 2)));

            Assert.Equal("Semestralidade e anuidade nao podem ser editadas: exclua e cadastre outro vinculo.", excecao.Message);
        }

        [Fact]
        public async Task Outra_modalidade_nao_pode_virar_plano_por_edicao()
        {
            var avulsa = SemearAtivo(ModalidadeCobranca.Avulsa);

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.AtualizarAsync(AlunoId, avulsa.Id,
                PlanoMensal(ModalidadeCobranca.Anual, new DateOnly(2026, 11, 1), 2)));
        }

        [Fact]
        public async Task Plano_excluido_nao_pode_ser_reativado()
        {
            var plano = await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 2));
            await _servico.ExcluirAsync(AlunoId, plano.Id);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.ReativarAsync(AlunoId, plano.Id));

            Assert.Equal("Este plano foi encerrado e suas parcelas canceladas. Cadastre um novo vinculo.", excecao.Message);
        }

        // ---------- FR-012: exclusao cancela so as pendentes futuras ----------

        [Fact]
        public async Task Excluir_cancela_somente_as_parcelas_pendentes_com_vencimento_futuro_e_nao_apaga_nada()
        {
            var plano = await _servico.CadastrarAsync(AlunoId, Plano(ModalidadeCobranca.Anual, Hoje.AddMonths(-2), 1500m,
                (Hoje.AddMonths(-2), 300m), (Hoje.AddMonths(-1), 300m), (Hoje, 300m), (Hoje.AddMonths(1), 300m), (Hoje.AddMonths(2), 300m)));
            var pagamentos = _pagamentos.Pagamentos;
            pagamentos[0].Status = "Pago"; // paga
            // [1] pendente ja vencida; [2] vence hoje; [3] e [4] pendentes futuras
            var totalAntes = pagamentos.Count;

            await _servico.ExcluirAsync(AlunoId, plano.Id);

            Assert.False(_vinculos.Vinculos.Single().Ativo);
            Assert.Equal(new[] { "Pago", "Pendente", "Pendente", "Cancelado", "Cancelado" }, pagamentos.Select(p => p.Status));
            Assert.Equal(totalAntes, _pagamentos.Pagamentos.Count); // nenhum pagamento apagado
        }

        [Fact]
        public async Task A_parcela_que_vence_amanha_e_cancelada()
        {
            var plano = await _servico.CadastrarAsync(AlunoId, Plano(ModalidadeCobranca.Semestral, Hoje, 300m, (Hoje.AddDays(1), 300m)));

            await _servico.ExcluirAsync(AlunoId, plano.Id);

            Assert.Equal("Cancelado", _pagamentos.Pagamentos.Single().Status);
        }

        [Fact]
        public async Task Plano_com_todas_as_parcelas_pagas_apenas_e_encerrado()
        {
            var plano = await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, Hoje.AddMonths(1), 2));
            _pagamentos.Pagamentos.ForEach(p => p.Status = "Pago");

            await _servico.ExcluirAsync(AlunoId, plano.Id);

            Assert.False(_vinculos.Vinculos.Single().Ativo);
            Assert.All(_pagamentos.Pagamentos, p => Assert.Equal("Pago", p.Status));
        }

        [Fact]
        public async Task Excluir_e_idempotente()
        {
            var plano = await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, Hoje.AddMonths(1), 2));

            await _servico.ExcluirAsync(AlunoId, plano.Id);
            var salvamentos = _vinculos.SalvamentosRealizados;
            await _servico.ExcluirAsync(AlunoId, plano.Id);

            Assert.Equal(salvamentos, _vinculos.SalvamentosRealizados);
            Assert.All(_pagamentos.Pagamentos, p => Assert.Equal("Cancelado", p.Status));
        }

        // ---------- listagem ----------

        [Fact]
        public async Task Listagem_traz_periodo_e_parcelas_em_uma_unica_consulta_e_marca_atrasada()
        {
            await _servico.CadastrarAsync(AlunoId, Plano(ModalidadeCobranca.Semestral, Hoje.AddMonths(-2), 900m,
                (Hoje.AddMonths(-2), 300m), (Hoje.AddMonths(-1), 300m), (Hoje.AddMonths(1), 300m)));
            _pagamentos.Pagamentos[0].Status = "Pago";
            var consultasAntes = _pagamentos.ChamadasListarPorVinculos;

            var lista = await _servico.ListarPorAlunoAsync(AlunoId, null);

            var plano = Assert.Single(lista);
            Assert.NotNull(plano.DataInicio);
            Assert.NotNull(plano.DataFim);
            Assert.Equal(new[] { "Pago", "Atrasado", "Pendente" }, plano.Parcelas!.Select(p => p.Status));
            Assert.Equal(consultasAntes + 1, _pagamentos.ChamadasListarPorVinculos);
        }

        [Fact]
        public async Task Vinculos_que_nao_sao_plano_vem_sem_periodo_nem_parcelas()
        {
            SemearAtivo(ModalidadeCobranca.Avulsa);

            var vinculo = Assert.Single(await _servico.ListarPorAlunoAsync(AlunoId, null));

            Assert.Null(vinculo.DataInicio);
            Assert.Null(vinculo.Parcelas);
        }

        [Fact]
        public async Task Plano_com_avulsa_herdada_e_sinalizado_como_conflito_e_renovacao_nao()
        {
            await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2026, 11, 1), 2));
            await _servico.CadastrarAsync(AlunoId, PlanoMensal(ModalidadeCobranca.Semestral, new DateOnly(2027, 5, 1), 2));

            Assert.All(await _servico.ListarPorAlunoAsync(AlunoId, true), v => Assert.Null(v.Conflito));

            SemearAtivo(ModalidadeCobranca.Avulsa); // dado antigo que a regra de hoje nao deixaria entrar
            Assert.All(await _servico.ListarPorAlunoAsync(AlunoId, true), v => Assert.NotNull(v.Conflito));
        }
    }
}
