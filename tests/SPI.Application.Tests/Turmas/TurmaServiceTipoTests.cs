using FluentValidation.TestHelper;
using SPI.Application.Turmas.Dtos;
using SPI.Application.Turmas.Services;
using SPI.Application.Turmas.Validators;
using SPI.Application.Tests.VinculosCobranca;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Turmas
{
    // specs/063 (US2, FR-001, FR-004 a FR-006): tipo da turma, datas, turma fechada e a entrada
    // na turma de pacote. Os fakes de vinculo/turma/aluno sao os da spec 037/056.
    public class TurmaServiceTipoTests
    {
        private const int ProfessorId = 1;
        private const int AlunoId = 1;
        private const int TurmaId = 10;

        private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Now);

        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeAlunoRepositoryParaVinculo _alunos = new();
        private readonly TurmaService _servico;
        private readonly TurmaRequestValidator _validator = new();

        public TurmaServiceTipoTests()
        {
            var aluno = new Aluno { Id = AlunoId, Nome = "Ana", Ra = "RA1", Ativo = true };
            _alunos.Alunos.Add(aluno);
            _turmas.AlunosConhecidos[AlunoId] = aluno;
            _servico = new TurmaService(_turmas, _alunos, _vinculos, new EncerramentoTurmaPacoteService(_vinculos));
        }

        private Turma SemearTurma(TipoTurma tipo, DateOnly? inicio = null, DateOnly? fim = null, bool ativa = true)
        {
            var turma = new Turma { Id = TurmaId, Nome = "Turma", Ativo = ativa, Tipo = tipo, DataInicio = inicio, DataFim = fim };
            _turmas.Turmas.Add(turma);
            _vinculos.TurmasConhecidas.Add(turma);
            return turma;
        }

        private VinculoCobranca SemearPacote(int? turmaId, bool ativo = true, int? saldo = 5) =>
            _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, TurmaId = turmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 100m, SaldoAulas = saldo, Ativo = ativo });

        private static TurmaRequest Pedido(TipoTurma? tipo = null, DateOnly? inicio = null, DateOnly? fim = null) =>
            new() { Nome = "Turma", Tipo = tipo, DataInicio = inicio, DataFim = fim };

        // ---------- Validador: o cadastro exige as datas da turma de pacote (FR-001) ----------

        [Fact]
        public void Cadastro_de_turma_de_pacote_sem_datas_e_recusado_mas_a_edicao_aceita()
        {
            var semDatas = Pedido(TipoTurma.Pacote);

            var cadastro = _validator.TestValidate(semDatas, o => o.IncludeRuleSets(TurmaRequestValidator.RegrasDeCadastro).IncludeRulesNotInRuleSet());
            cadastro.ShouldHaveValidationErrorFor(x => x.DataInicio).WithErrorMessage("A turma de pacote precisa de data de inicio.");
            cadastro.ShouldHaveValidationErrorFor(x => x.DataFim).WithErrorMessage("A turma de pacote precisa de data de fim.");

            // A edicao (turma migrada sem datas) nao exige as datas.
            _validator.TestValidate(semDatas).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Turma_de_mensalidade_com_datas_e_recusada()
        {
            var resultado = _validator.TestValidate(Pedido(TipoTurma.Mensalidade, Hoje, Hoje.AddDays(30)));

            resultado.ShouldHaveValidationErrorFor(x => x.DataInicio);
            resultado.ShouldHaveValidationErrorFor(x => x.DataFim);
        }

        [Fact]
        public void Data_de_fim_anterior_a_de_inicio_e_recusada()
        {
            var resultado = _validator.TestValidate(Pedido(TipoTurma.Pacote, Hoje.AddDays(10), Hoje.AddDays(5)));

            resultado.ShouldHaveValidationErrorFor(x => x.DataFim).WithErrorMessage("A data de fim nao pode ser anterior a data de inicio.");
        }

        // ---------- Cadastro ----------

        [Fact]
        public async Task Cadastra_turma_de_pacote_com_datas_e_sem_tipo_vira_mensalidade()
        {
            var pacote = await _servico.CadastrarAsync(ProfessorId, Pedido(TipoTurma.Pacote, Hoje.AddDays(1), Hoje.AddDays(60)));
            var padrao = await _servico.CadastrarAsync(ProfessorId, Pedido());

            Assert.Equal(TipoTurma.Pacote, pacote.Tipo);
            Assert.Equal(Hoje.AddDays(1), pacote.DataInicio);
            Assert.False(pacote.Fechada);
            Assert.Equal(TipoTurma.Mensalidade, padrao.Tipo);
            Assert.Null(padrao.DataInicio);
            Assert.False(padrao.Fechada);
        }

        // ---------- Edicao: o tipo e a data de inicio nao mudam (FR-005) ----------

        [Fact]
        public async Task Troca_de_tipo_e_recusada()
        {
            SemearTurma(TipoTurma.Mensalidade);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.AtualizarAsync(TurmaId, Pedido(TipoTurma.Pacote, Hoje, Hoje.AddDays(30))));

            Assert.Equal("O tipo da turma nao pode ser alterado depois de criada.", excecao.Message);
        }

        [Fact]
        public async Task Data_de_inicio_ja_preenchida_nao_muda_mas_o_fim_pode_ser_ajustado()
        {
            var turma = SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-3), Hoje.AddDays(20));

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.AtualizarAsync(TurmaId, Pedido(TipoTurma.Pacote, Hoje.AddDays(5), Hoje.AddDays(20))));
            Assert.Contains("data de inicio", excecao.Message);
            Assert.Equal(Hoje.AddDays(-3), turma.DataInicio);

            // Mesma data de inicio + novo fim (prorrogar e antecipar): aceito.
            var prorrogada = await _servico.AtualizarAsync(TurmaId, Pedido(TipoTurma.Pacote, Hoje.AddDays(-3), Hoje.AddDays(90)));
            Assert.Equal(Hoje.AddDays(90), prorrogada.DataFim);

            var antecipada = await _servico.AtualizarAsync(TurmaId, Pedido(TipoTurma.Pacote, Hoje.AddDays(-3), Hoje.AddDays(-1)));
            Assert.Equal(Hoje.AddDays(-1), antecipada.DataFim);
        }

        [Fact]
        public async Task Turma_migrada_sem_datas_aceita_preencher_e_tipo_ausente_mantem_o_atual()
        {
            var turma = SemearTurma(TipoTurma.Pacote);

            var resposta = await _servico.AtualizarAsync(TurmaId, Pedido(tipo: null, inicio: Hoje.AddDays(-2), fim: Hoje.AddDays(30)));

            Assert.Equal(TipoTurma.Pacote, resposta.Tipo);
            Assert.Equal(Hoje.AddDays(-2), turma.DataInicio);
            Assert.Equal(Hoje.AddDays(30), turma.DataFim);
        }

        [Fact]
        public async Task Fim_anterior_ao_inicio_na_edicao_e_recusado()
        {
            SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-3), Hoje.AddDays(20));

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.AtualizarAsync(TurmaId, Pedido(TipoTurma.Pacote, Hoje.AddDays(-3), Hoje.AddDays(-10))));
        }

        // ---------- Turma fechada: calculada no servidor (research.md R4) ----------

        [Theory]
        [InlineData(-1, true)]   // comecou ontem
        [InlineData(0, false)]   // comeca hoje: ainda aceita
        [InlineData(5, false)]   // comeca no futuro
        public async Task Fechada_so_depois_do_dia_de_inicio(int diasParaOInicio, bool esperado)
        {
            SemearTurma(TipoTurma.Pacote, Hoje.AddDays(diasParaOInicio), Hoje.AddDays(60));

            Assert.Equal(esperado, (await _servico.ObterPorIdAsync(TurmaId)).Fechada);
        }

        [Fact]
        public async Task Turma_de_pacote_sem_data_e_turma_de_mensalidade_nunca_estao_fechadas()
        {
            SemearTurma(TipoTurma.Pacote);
            Assert.False((await _servico.ObterPorIdAsync(TurmaId)).Fechada);

            _turmas.Turmas.Clear();
            SemearTurma(TipoTurma.Mensalidade);
            Assert.False((await _servico.ObterPorIdAsync(TurmaId)).Fechada);
        }

        // ---------- Matricula direta (FR-004) ----------

        [Fact]
        public async Task Matricula_direta_de_quem_nao_tem_pacote_na_turma_de_pacote_e_recusada()
        {
            var turma = SemearTurma(TipoTurma.Pacote, Hoje.AddDays(1), Hoje.AddDays(60));

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.VincularAlunoAsync(TurmaId, AlunoId));

            Assert.Contains("cadastro do pacote", excecao.Message);
            Assert.Empty(turma.AlunosTurma);
            Assert.Equal(0, _turmas.ChamadasVincularAluno);
        }

        [Fact]
        public async Task Pacote_de_outra_turma_nao_libera_a_matricula_direta()
        {
            SemearTurma(TipoTurma.Pacote, Hoje.AddDays(1), Hoje.AddDays(60));
            SemearPacote(turmaId: 99);

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.VincularAlunoAsync(TurmaId, AlunoId));
        }

        [Fact]
        public async Task Quem_tem_pacote_ativo_da_turma_pode_voltar_enquanto_ela_esta_em_andamento()
        {
            var turma = SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-5), Hoje.AddDays(30));
            SemearPacote(TurmaId);

            var resposta = await _servico.VincularAlunoAsync(TurmaId, AlunoId);

            Assert.Contains(resposta.Alunos, a => a.Id == AlunoId);
            Assert.Single(turma.AlunosTurma);
        }

        // A tela so oferece "voltar" para quem saiu e ainda tem o pacote dela ativo: a lista vem do servidor.
        [Fact]
        public async Task Turma_de_pacote_lista_so_quem_saiu_e_ainda_tem_o_pacote_ativo()
        {
            SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-5), Hoje.AddDays(30));
            var aluno = _alunos.Alunos[0];
            _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, Aluno = aluno, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 100m, SaldoAulas = 3, Ativo = true });

            var antes = await _servico.ObterPorIdAsync(TurmaId);
            Assert.Equal(AlunoId, Assert.Single(antes.AlunosComPacoteForaDaTurma).Id);

            var depois = await _servico.VincularAlunoAsync(TurmaId, AlunoId);
            Assert.Empty(depois.AlunosComPacoteForaDaTurma);
            Assert.Contains(depois.Alunos, a => a.Id == AlunoId);
        }

        [Fact]
        public async Task Lista_de_quem_pode_voltar_ignora_pacote_inativo_e_turma_de_mensalidade()
        {
            SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-5), Hoje.AddDays(30));
            var aluno = _alunos.Alunos[0];
            _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, Aluno = aluno, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 100m, Ativo = false });
            Assert.Empty((await _servico.ObterPorIdAsync(TurmaId)).AlunosComPacoteForaDaTurma);

            _turmas.Turmas.Clear();
            SemearTurma(TipoTurma.Mensalidade);
            Assert.Empty((await _servico.ObterPorIdAsync(TurmaId)).AlunosComPacoteForaDaTurma);
        }

        [Fact]
        public async Task Pacote_inativo_nao_libera_a_volta()
        {
            SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-5), Hoje.AddDays(30));
            SemearPacote(TurmaId, ativo: false);

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.VincularAlunoAsync(TurmaId, AlunoId));
        }

        [Fact]
        public async Task Turma_encerrada_nao_aceita_nem_a_volta()
        {
            SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-30), Hoje.AddDays(-1));
            SemearPacote(TurmaId);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.VincularAlunoAsync(TurmaId, AlunoId));

            Assert.Equal("A turma de pacote esta encerrada.", excecao.Message);
        }

        [Fact]
        public async Task Turma_de_mensalidade_matricula_como_sempre()
        {
            var turma = SemearTurma(TipoTurma.Mensalidade);

            var resposta = await _servico.VincularAlunoAsync(TurmaId, AlunoId);

            Assert.Contains(resposta.Alunos, a => a.Id == AlunoId);
            Assert.Single(turma.AlunosTurma);
        }

        // ---------- Saida (FR-006, "MUST NOT"): nao altera vinculo, saldo nem pagamento ----------

        [Fact]
        public async Task Desvincular_da_turma_de_pacote_nao_altera_o_pacote()
        {
            var turma = SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-5), Hoje.AddDays(30));
            turma.AlunosTurma.Add(new AlunoTurma { AlunoId = AlunoId, TurmaId = TurmaId, Turma = turma, Aluno = _alunos.Alunos[0] });
            var pacote = SemearPacote(TurmaId, saldo: 4);
            var salvamentosAntes = _vinculos.SalvamentosRealizados;

            await _servico.DesvincularAlunoAsync(TurmaId, AlunoId);

            Assert.Empty(turma.AlunosTurma);
            Assert.True(pacote.Ativo);
            Assert.Equal(4, pacote.SaldoAulas);
            Assert.Equal(TurmaId, pacote.TurmaId);
            Assert.Equal(salvamentosAntes, _vinculos.SalvamentosRealizados);
        }
    }
}
