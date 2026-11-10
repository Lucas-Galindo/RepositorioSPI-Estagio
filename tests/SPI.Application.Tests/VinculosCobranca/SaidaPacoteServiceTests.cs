using SPI.Application.Tests.Aulas;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/078: saida do aluno de turma de pacote com acerto proporcional ao consumo (previa e gravacao).
    public class SaidaPacoteServiceTests
    {
        private const int TurmaId = 10;
        private const int AlunoId = 1;
        private const int OutroAlunoId = 2;

        // "Agora" dos testes: 10/11/2026 as 10h (o relogio do falso e local, como o do sistema).
        private static readonly DateTime Agora = new(2026, 11, 10, 10, 0, 0);
        private static readonly DateOnly Hoje = new(2026, 11, 10);

        // Pagamentos em memoria: o servico so adiciona. Quem grava e o repositorio de vinculos (um unico
        // SalvarAlteracoesAsync), como o contexto compartilhado do banco real.
        private sealed class PagamentosFake : IPagamentoRepository
        {
            private int _proximoId = 1;
            public List<Pagamento> Pagamentos { get; } = new();

            public Task AdicionarAsync(Pagamento pagamento, CancellationToken cancellationToken = default)
            {
                pagamento.Id = _proximoId++;
                pagamento.VinculoCobrancaId = pagamento.VinculoCobranca?.Id;
                Pagamentos.Add(pagamento);
                return Task.CompletedTask;
            }

            public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<Pagamento?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Pagamento>> ListarAsync(int? alunoId, string? status, DateOnly? vencimentoInicio, DateOnly? vencimentoFim, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<bool> ExisteMensalidadeGeradaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Pagamento>> ListarMensalidadesParaLembreteVencimentoAsync(DateOnly dataVencimentoAlvo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Pagamento>> ListarPendentesVencidosAsync(DateOnly hoje, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Pagamento>> ListarPorVinculosAsync(IReadOnlyCollection<int> vinculoIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Pagamento>> ListarPendentesFuturosDoVinculoAsync(int vinculoId, DateOnly hoje, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task VincularAulaAsync(int pagamentoId, int aulaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task AtualizarStatusViaProcedureAsync(int pagamentoId, string novoStatus, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private sealed class CategoriasFake : ICategoriaReceitaRepository
        {
            public List<CategoriaReceita> Categorias { get; } = new() { new CategoriaReceita { Id = 8, Nome = "Acerto de pacote", Ativo = true } };

            public Task<CategoriaReceita?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Categorias.FirstOrDefault(c => c.Id == id));
            public Task<CategoriaReceita?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(Categorias.FirstOrDefault(c => c.Nome == nome));
            public Task<List<CategoriaReceita>> ListarAtivasAsync(CancellationToken cancellationToken = default) => Task.FromResult(Categorias.ToList());
        }

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly FakeAlunoRepositoryParaVinculo _alunos = new();
        private readonly FakeAulaRepositoryParaVinculo _aulas = new();
        private readonly PagamentosFake _pagamentos = new();
        private readonly CategoriasFake _categorias = new();
        private readonly RelogioFalso _relogio = new(Agora);
        private readonly SaidaPacoteService _servico;
        private readonly Turma _turma;
        private readonly VinculoCobranca _pacote;
        private readonly Aluno _ana = new() { Id = AlunoId, Nome = "Ana", Ra = "RA1", Ativo = true };

        public SaidaPacoteServiceTests()
        {
            var bia = new Aluno { Id = OutroAlunoId, Nome = "Bia", Ra = "RA2", Ativo = true };
            _alunos.Alunos.AddRange(new[] { _ana, bia });
            _turmas.AlunosConhecidos[AlunoId] = _ana;
            _turmas.AlunosConhecidos[OutroAlunoId] = bia;

            _turma = new Turma { Id = TurmaId, Nome = "Pacote Novembro", Ativo = true, Tipo = TipoTurma.Pacote, DataInicio = Hoje.AddDays(-5), DataFim = Hoje.AddDays(30) };
            _turmas.Turmas.Add(_turma);
            _vinculos.TurmasConhecidas.Add(_turma);
            _turmas.VincularAlunoAsync(TurmaId, AlunoId).Wait();
            _turmas.VincularAlunoAsync(TurmaId, OutroAlunoId).Wait();

            // Pacote de R$ 100 para 10 aulas, 7 presencas registradas (o exemplo da regra).
            _pacote = SemearPacote(AlunoId, _ana, total: 10, consumidas: 7);

            _servico = new SaidaPacoteService(_turmas, _alunos, _vinculos, _aulas, _pagamentos, _categorias, _relogio);
        }

        private VinculoCobranca SemearPacote(int alunoId, Aluno aluno, int? total, int consumidas, decimal valor = 100m, int? saldo = 3)
        {
            var pacote = _vinculos.Semear(new VinculoCobranca
            {
                AlunoId = alunoId, Aluno = aluno, TurmaId = TurmaId, Turma = _turma, Modalidade = ModalidadeCobranca.Pacote,
                Valor = valor, TotalAulasPacote = total, SaldoAulas = saldo, Ativo = true,
            });
            for (var i = 0; i < consumidas; i++)
            {
                _aulas.Presencas.Add((pacote.Id, 2026, 11));
            }

            return pacote;
        }

        private AulaAluno SemearParticipacaoFutura(int alunoId = AlunoId, int aulaId = 500)
        {
            var participacao = new AulaAluno
            {
                AulaId = aulaId, AlunoId = alunoId,
                Aula = new Aula { Id = aulaId, TurmaId = TurmaId, Status = "Agendada", Ativo = true, DataInicio = Hoje.AddDays(3) },
            };
            _aulas.ParticipacoesFuturas.Add(participacao);
            return participacao;
        }

        private static SaidaPacoteRequest Pedido(decimal pago, DateOnly? vencimento = null) => new() { ValorPago = pago, Vencimento = vencimento };

        private static ConfirmarSaidaPacoteRequest Confirmacao(decimal pago, DateOnly? vencimento, int consumidas = 7, decimal devido = 70m) =>
            new() { ValorPago = pago, Vencimento = vencimento, ConsumidasEsperadas = consumidas, ValorDevidoEsperado = devido };

        // ---------- US1: a previa ----------

        [Fact]
        public async Task Previa_do_exemplo_pago_30_gera_conta_de_40()
        {
            var r = await _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(30m));

            Assert.Equal(7, r.AulasConsumidas);
            Assert.Equal(10, r.TotalAulas);
            Assert.Equal(10.00m, r.ValorPorAula);
            Assert.Equal(70.00m, r.ValorDevido);
            Assert.Equal(30m, r.ValorPago);
            Assert.Equal(40.00m, r.Diferenca);
            Assert.Equal(ConsequenciaAcerto.GerarConta, r.Consequencia);
            Assert.True(r.PrecisaVencimento);
            Assert.Equal("Ana", r.AlunoNome);
            Assert.Equal("Pacote Novembro", r.TurmaNome);
        }

        [Fact]
        public async Task Previa_pago_100_avisa_que_o_excedente_nao_e_devolvido_e_nao_pede_vencimento()
        {
            var r = await _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(100m));

            Assert.Equal(ConsequenciaAcerto.ExcedenteNaoDevolvido, r.Consequencia);
            Assert.Equal(30.00m, r.Excedente);
            Assert.False(r.PrecisaVencimento);
        }

        [Fact]
        public async Task Previa_pago_70_fecha_o_acerto()
        {
            var r = await _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(70m));

            Assert.Equal(ConsequenciaAcerto.Fechado, r.Consequencia);
            Assert.False(r.PrecisaVencimento);
        }

        [Fact]
        public async Task Previa_de_R100_em_3_aulas_com_2_consumidas_devido_66_67()
        {
            _vinculos.Vinculos.Remove(_pacote);
            SemearPacote(AlunoId, _ana, total: 3, consumidas: 2);

            var r = await _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(0m));

            Assert.Equal(66.67m, r.ValorDevido);
        }

        [Fact]
        public async Task Previa_sem_nenhuma_presenca_deve_zero_e_pacote_esgotado_deve_o_valor_inteiro()
        {
            _aulas.Presencas.Clear();
            Assert.Equal(0m, (await _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(0m))).ValorDevido);

            for (var i = 0; i < 10; i++)
            {
                _aulas.Presencas.Add((_pacote.Id, 2026, 11));
            }

            Assert.Equal(100m, (await _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(0m))).ValorDevido);
        }

        [Fact]
        public async Task Previa_de_pacote_antigo_sem_total_usa_o_total_estimado_da_ficha()
        {
            _vinculos.Vinculos.Remove(_pacote);
            _aulas.Presencas.Clear();
            SemearPacote(AlunoId, _ana, total: null, consumidas: 7, saldo: 3); // total estimado = dadas + saldo = 10

            var r = await _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(0m));

            Assert.Equal(10, r.TotalAulas);
            Assert.True(r.TotalEstimado);
            Assert.Equal(70m, r.ValorDevido);
        }

        [Fact]
        public async Task Previa_de_pacote_sem_total_e_sem_saldo_e_recusada()
        {
            _vinculos.Vinculos.Remove(_pacote);
            _aulas.Presencas.Clear();
            SemearPacote(AlunoId, _ana, total: null, consumidas: 0, saldo: null);

            var erro = await Assert.ThrowsAsync<ConflitoException>(() => _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(0m)));

            Assert.Contains("total de aulas", erro.Message);
        }

        [Fact]
        public async Task Previa_conta_as_aulas_agendadas_de_que_o_aluno_sairia_e_nao_grava_nada()
        {
            SemearParticipacaoFutura(aulaId: 500);
            SemearParticipacaoFutura(aulaId: 501);

            var r = await _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(30m));

            Assert.Equal(2, r.AulasAgendadasQueSairao);
            Assert.Equal(Agora, _aulas.UltimoAgoraLocalConsultado);
            Assert.Equal(0, _vinculos.SalvamentosRealizados);
            Assert.Empty(_pagamentos.Pagamentos);
            Assert.Empty(_aulas.ParticipacoesRemovidas);
            Assert.True(_pacote.Ativo);
            Assert.Null(_pacote.SaidaRegistradaEm);
            Assert.Equal(0, _turmas.ChamadasDesvincularAluno);
            Assert.Contains(_turma.AlunosTurma, at => at.AlunoId == AlunoId);
        }

        [Fact]
        public async Task Previa_so_olha_as_aulas_do_aluno_da_saida()
        {
            SemearParticipacaoFutura(OutroAlunoId, aulaId: 600);

            var r = await _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(0m));

            Assert.Equal(0, r.AulasAgendadasQueSairao);
        }

        // ---------- recusas (previa e gravacao) ----------

        [Fact]
        public async Task Turma_ou_aluno_inexistente_e_404()
        {
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.PreVisualizarAsync(999, AlunoId, Pedido(0m)));
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.PreVisualizarAsync(TurmaId, 999, Pedido(0m)));
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.RegistrarAsync(999, AlunoId, Confirmacao(0m, null)));
        }

        [Fact]
        public async Task Aluno_nao_matriculado_e_recusado()
        {
            await _turmas.DesvincularAlunoAsync(TurmaId, AlunoId);

            var erro = await Assert.ThrowsAsync<ConflitoException>(() => _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(0m)));

            Assert.Contains("matriculado", erro.Message);
        }

        [Fact]
        public async Task Turma_de_mensalidade_e_recusada()
        {
            _turma.Tipo = TipoTurma.Mensalidade;

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(0m)));
        }

        [Theory]
        [InlineData(-1)]   // terminou ontem
        [InlineData(-30)]
        public async Task Turma_que_ja_terminou_e_recusada(int diasDoFimAteHoje)
        {
            _turma.DataFim = Hoje.AddDays(diasDoFimAteHoje);

            var erro = await Assert.ThrowsAsync<ConflitoException>(() => _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(0m)));

            Assert.Contains("terminou", erro.Message);
        }

        [Fact]
        public async Task Turma_inativa_e_recusada()
        {
            _turma.Ativo = false;

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(0m)));
        }

        [Fact]
        public async Task Pacote_inativo_e_recusado_na_previa_e_na_gravacao()
        {
            _pacote.Ativo = false;

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.PreVisualizarAsync(TurmaId, AlunoId, Pedido(0m)));
            await Assert.ThrowsAsync<ConflitoException>(() => _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(0m, null)));
        }

        // ---------- US2: a gravacao ----------

        [Fact]
        public async Task Pagou_menos_gera_uma_conta_pendente_da_diferenca_e_faz_a_saida_completa()
        {
            SemearParticipacaoFutura(aulaId: 500);
            SemearParticipacaoFutura(aulaId: 501);
            var vencimento = new DateOnly(2026, 11, 20);

            var r = await _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, vencimento));

            var conta = Assert.Single(_pagamentos.Pagamentos);
            Assert.Equal(conta.Id, r.PagamentoId);
            Assert.Equal(40.00m, conta.ValorFinal);
            Assert.Equal("Pendente", conta.Status);
            Assert.Equal(vencimento, conta.DataVencimento);
            Assert.Equal(new DateOnly(2026, 11, 1), conta.Competencia);
            Assert.Equal(AlunoId, conta.AlunoId);
            Assert.Equal(8, conta.CategoriaReceitaId);
            Assert.Equal(_pacote.Id, conta.VinculoCobrancaId);
            Assert.Contains("Ana", conta.Descricao);
            Assert.Contains("Pacote Novembro", conta.Descricao);
            Assert.Contains("7 de 10 aulas", conta.Descricao);
            Assert.Contains("R$ 70,00", conta.Descricao);
            Assert.Contains("R$ 30,00", conta.Descricao);

            Assert.False(_pacote.Ativo);
            Assert.Equal(Agora, _pacote.SaidaRegistradaEm);
            Assert.DoesNotContain(_turma.AlunosTurma, at => at.AlunoId == AlunoId);
            Assert.Equal(2, _aulas.ParticipacoesRemovidas.Count);
            Assert.Equal(70.00m, r.Acerto.ValorDevido);
            Assert.Equal(2, r.Acerto.AulasAgendadasQueSairao);
        }

        [Fact]
        public async Task A_saida_e_um_unico_salvamento()
        {
            await _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, Hoje));

            Assert.Equal(1, _vinculos.SalvamentosRealizados);
        }

        [Theory]
        [InlineData(70)]
        [InlineData(100)]
        [InlineData(500)]
        public async Task Pagou_o_devido_ou_mais_nao_gera_nenhuma_conta_nem_devolucao(int pago)
        {
            var r = await _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(pago, null));

            Assert.Empty(_pagamentos.Pagamentos);
            Assert.Null(r.PagamentoId);
            Assert.False(_pacote.Ativo);
            Assert.NotNull(_pacote.SaidaRegistradaEm);
            Assert.DoesNotContain(_turma.AlunosTurma, at => at.AlunoId == AlunoId);
        }

        [Fact]
        public async Task Nenhuma_presenca_a_saida_e_aceita_sem_conta_e_o_pago_nao_e_devolvido()
        {
            _aulas.Presencas.Clear();

            var r = await _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(40m, null, consumidas: 0, devido: 0m));

            Assert.Empty(_pagamentos.Pagamentos);
            Assert.Equal(ConsequenciaAcerto.ExcedenteNaoDevolvido, r.Acerto.Consequencia);
            Assert.False(_pacote.Ativo);
        }

        [Fact]
        public async Task A_gravacao_so_aceita_o_pedido_uma_vez_e_nao_cria_segunda_conta()
        {
            await _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, Hoje));

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, Hoje)));
            Assert.Single(_pagamentos.Pagamentos);
        }

        [Fact]
        public async Task Vencimento_ausente_ou_de_ontem_com_diferenca_e_recusado_e_nada_muda()
        {
            await Assert.ThrowsAsync<ConflitoException>(() => _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, null)));
            await Assert.ThrowsAsync<ConflitoException>(() => _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, Hoje.AddDays(-1))));

            NadaMudou();
        }

        [Fact]
        public async Task Vencimento_de_hoje_e_aceito()
        {
            await _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, Hoje));

            Assert.Equal(Hoje, _pagamentos.Pagamentos.Single().DataVencimento);
        }

        [Fact]
        public async Task Consumo_que_mudou_desde_a_previa_e_recusado_e_nada_muda()
        {
            _aulas.Presencas.Add((_pacote.Id, 2026, 11)); // 8a presenca depois de abrir o resumo (esperado: 7 e R$ 70,00)

            var erro = await Assert.ThrowsAsync<ConflitoException>(() => _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, Hoje)));

            Assert.Contains("mudou", erro.Message);
            NadaMudou();
        }

        [Fact]
        public async Task Valor_devido_diferente_do_esperado_e_recusado()
        {
            await Assert.ThrowsAsync<ConflitoException>(() => _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, Hoje, consumidas: 7, devido: 60m)));

            NadaMudou();
        }

        [Fact]
        public async Task Categoria_ausente_e_recusada_pedindo_a_migracao_e_nada_muda()
        {
            _categorias.Categorias.Clear();

            var erro = await Assert.ThrowsAsync<ConflitoException>(() => _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, Hoje)));

            Assert.Contains("migracao 28", erro.Message);
            NadaMudou();
        }

        [Fact]
        public async Task Falha_ao_salvar_propaga_o_erro_sem_registrar_salvamento()
        {
            _vinculos.FalharAoSalvar = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, Hoje)));

            Assert.Equal(0, _vinculos.SalvamentosRealizados);
        }

        [Fact]
        public async Task Os_outros_vinculos_e_os_outros_alunos_ficam_intactos()
        {
            var mensalidade = _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, Aluno = _ana, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 300m, Ativo = true });
            var outraTurma = _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, Aluno = _ana, TurmaId = 77, Modalidade = ModalidadeCobranca.Pacote, Valor = 90m, TotalAulasPacote = 9, SaldoAulas = 9, Ativo = true });
            var pacoteDaBia = SemearPacote(OutroAlunoId, _turmas.AlunosConhecidos[OutroAlunoId], total: 10, consumidas: 2);
            var participacaoDaBia = SemearParticipacaoFutura(OutroAlunoId, aulaId: 700);

            await _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(30m, Hoje));

            Assert.True(mensalidade.Ativo);
            Assert.True(outraTurma.Ativo);
            Assert.Null(outraTurma.SaidaRegistradaEm);
            Assert.True(pacoteDaBia.Ativo);
            Assert.Contains(_turma.AlunosTurma, at => at.AlunoId == OutroAlunoId);
            Assert.Contains(participacaoDaBia, _aulas.ParticipacoesFuturas);
            Assert.Empty(_aulas.ParticipacoesRemovidas);
        }

        // ---------- depois da saida: o aluno nao volta pelo pacote ----------

        [Fact]
        public async Task Pacote_encerrado_pela_saida_nao_pode_ser_reativado()
        {
            await _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(70m, null));
            var vinculos = new VinculoCobrancaService(_vinculos, _alunos, _turmas, _aulas);

            var erro = await Assert.ThrowsAsync<ConflitoException>(() => vinculos.ReativarAsync(AlunoId, _pacote.Id));

            Assert.Contains("saida", erro.Message);
            Assert.False(_pacote.Ativo);
        }

        [Fact]
        public async Task Pacote_inativo_so_porque_a_turma_acabou_continua_reativavel_pela_regra_antiga()
        {
            // Sem SaidaRegistradaEm (encerramento automatico da spec 063): a regra de reativacao nao muda.
            _pacote.Ativo = false;
            var vinculos = new VinculoCobrancaService(_vinculos, _alunos, _turmas, _aulas);

            var resposta = await vinculos.ReativarAsync(AlunoId, _pacote.Id);

            Assert.True(resposta.Ativo);
            Assert.Null(resposta.SaidaRegistradaEm);
        }

        [Fact]
        public async Task A_resposta_do_vinculo_mostra_quando_a_saida_foi_registrada()
        {
            await _servico.RegistrarAsync(TurmaId, AlunoId, Confirmacao(70m, null));
            var vinculos = new VinculoCobrancaService(_vinculos, _alunos, _turmas, _aulas);

            var resposta = await vinculos.ObterPorIdAsync(AlunoId, _pacote.Id);

            Assert.False(resposta.Ativo);
            Assert.Equal(Agora, resposta.SaidaRegistradaEm);
        }

        // Nada gravado nem retirado: matricula, pacote, participacoes e contas como antes.
        private void NadaMudou()
        {
            Assert.Equal(0, _vinculos.SalvamentosRealizados);
            Assert.Empty(_pagamentos.Pagamentos);
            Assert.True(_pacote.Ativo);
            Assert.Null(_pacote.SaidaRegistradaEm);
            Assert.Equal(0, _turmas.ChamadasDesvincularAluno);
            Assert.Empty(_aulas.ParticipacoesRemovidas);
            Assert.Contains(_turma.AlunosTurma, at => at.AlunoId == AlunoId);
        }
    }
}
