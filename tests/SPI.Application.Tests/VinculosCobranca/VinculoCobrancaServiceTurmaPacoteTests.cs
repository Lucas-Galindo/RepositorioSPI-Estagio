using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/063 (US2 e US5): o pacote pertence a um grupo -- uma turma de pacote ou as aulas
    // individuais --, o cadastro do pacote de turma matricula o aluno, a turma e fechada depois
    // do inicio, o aluno pode ter 0 ou N pacotes de turma (um por turma) e um unico de aulas
    // individuais, e a reativacao so vale com a turma em andamento.
    public class VinculoCobrancaServiceTurmaPacoteTests
    {
        private const int AlunoId = 1;
        private const int TurmaBasica = 10;
        private const int TurmaMedio = 20;

        private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Now);

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly VinculoCobrancaService _servico;

        public VinculoCobrancaServiceTurmaPacoteTests()
        {
            var aluno = new Aluno { Id = AlunoId, Nome = "Ana", Ativo = true };
            var alunos = new FakeAlunoRepositoryParaVinculo();
            alunos.Alunos.Add(aluno);
            _turmas.AlunosConhecidos[AlunoId] = aluno;
            _servico = new VinculoCobrancaService(_vinculos, alunos, _turmas, new FakeAulaRepositoryParaVinculo());
        }

        private Turma SemearTurma(int id, TipoTurma tipo = TipoTurma.Pacote, DateOnly? inicio = null, DateOnly? fim = null, bool ativa = true, bool aluno = false)
        {
            var turma = new Turma { Id = id, Nome = $"Turma {id}", Ativo = ativa, Tipo = tipo, DataInicio = inicio, DataFim = fim };
            if (aluno)
            {
                turma.AlunosTurma.Add(new AlunoTurma { AlunoId = AlunoId, TurmaId = id, Turma = turma });
            }

            _turmas.Turmas.Add(turma);
            _vinculos.TurmasConhecidas.Add(turma);
            return turma;
        }

        private VinculoCobranca Semear(ModalidadeCobranca modalidade, int? turmaId = null, bool ativo = true, int? saldo = null) =>
            _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, TurmaId = turmaId, Modalidade = modalidade, Valor = 100m, SaldoAulas = saldo, Ativo = ativo });

        private static VinculoCobrancaRequest Pedido(ModalidadeCobranca modalidade, int? turmaId = null) =>
            new() { Modalidade = modalidade, TurmaId = turmaId, Valor = 100m, TotalAulasPacote = modalidade == ModalidadeCobranca.Pacote ? 10 : null };

        private async Task<string> RecusaAsync(Func<Task> acao)
        {
            var antes = _vinculos.Vinculos.Select(v => (v.Id, v.Modalidade, v.TurmaId, v.Ativo)).ToList();
            var excecao = await Assert.ThrowsAsync<ConflitoException>(acao);
            Assert.Equal(antes, _vinculos.Vinculos.Select(v => (v.Id, v.Modalidade, v.TurmaId, v.Ativo)).ToList());
            return excecao.Message;
        }

        // ---------- Cadastro do pacote de turma (FR-003) ----------

        [Fact]
        public async Task Cadastra_pacote_de_turma_e_matricula_o_aluno_na_mesma_gravacao()
        {
            var turma = SemearTurma(TurmaBasica, inicio: Hoje.AddDays(1), fim: Hoje.AddDays(60));

            var resposta = await _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote, TurmaBasica));

            Assert.Equal(TurmaBasica, resposta.TurmaId);
            Assert.True(resposta.Ativo);
            Assert.Single(turma.AlunosTurma);
            Assert.Equal(AlunoId, turma.AlunosTurma.Single().AlunoId);
            Assert.Equal(1, _turmas.ChamadasVincularAluno);
            Assert.Equal(1, _vinculos.SalvamentosRealizados); // vinculo e matricula num salvamento so
        }

        [Fact]
        public async Task Cadastro_nao_duplica_a_matricula_que_ja_existe()
        {
            var turma = SemearTurma(TurmaBasica, inicio: Hoje, fim: Hoje.AddDays(60), aluno: true);

            await _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote, TurmaBasica));

            Assert.Single(turma.AlunosTurma);
            Assert.Equal(0, _turmas.ChamadasVincularAluno);
        }

        [Fact]
        public async Task Turma_migrada_sem_datas_aceita_pacote_novo()
        {
            SemearTurma(TurmaBasica);

            var resposta = await _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote, TurmaBasica));

            Assert.True(resposta.Ativo);
        }

        [Fact]
        public async Task Turma_de_pacote_ja_iniciada_e_fechada_para_pacote_novo()
        {
            SemearTurma(TurmaBasica, inicio: Hoje.AddDays(-1), fim: Hoje.AddDays(30));

            var mensagem = await RecusaAsync(() => _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote, TurmaBasica)));

            Assert.Contains("ja comecou", mensagem);
            Assert.Equal(0, _turmas.ChamadasVincularAluno);
        }

        [Fact]
        public async Task Recusa_turma_de_mensalidade_inativa_e_inexistente()
        {
            SemearTurma(TurmaBasica, tipo: TipoTurma.Mensalidade);
            SemearTurma(TurmaMedio, inicio: Hoje.AddDays(1), fim: Hoje.AddDays(30), ativa: false);

            Assert.Equal("Somente turma do tipo Pacote recebe pacote.",
                await RecusaAsync(() => _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote, TurmaBasica))));
            Assert.Equal("A turma informada esta inativa.",
                await RecusaAsync(() => _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote, TurmaMedio))));
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote, 999)));
        }

        // ---------- 0 ou N pacotes de turma, um individual (FR-022) ----------

        [Fact]
        public async Task Aluno_tem_pacote_de_duas_turmas_e_o_de_aulas_individuais_ao_mesmo_tempo()
        {
            SemearTurma(TurmaBasica, inicio: Hoje.AddDays(1), fim: Hoje.AddDays(60));
            SemearTurma(TurmaMedio, inicio: Hoje.AddDays(1), fim: Hoje.AddDays(60));

            await _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote, TurmaBasica));
            await _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote, TurmaMedio));
            await _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote));

            Assert.Equal(3, _vinculos.Vinculos.Count(v => v.Ativo && v.Modalidade == ModalidadeCobranca.Pacote));
        }

        [Fact]
        public async Task Segundo_pacote_da_mesma_turma_e_recusado()
        {
            SemearTurma(TurmaBasica, inicio: Hoje.AddDays(1), fim: Hoje.AddDays(60));
            Semear(ModalidadeCobranca.Pacote, TurmaBasica);

            var mensagem = await RecusaAsync(() => _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote, TurmaBasica)));

            Assert.Equal("O aluno ja tem um pacote ativo desta turma.", mensagem);
        }

        [Fact]
        public async Task Segundo_pacote_de_aulas_individuais_e_recusado()
        {
            Semear(ModalidadeCobranca.Pacote);

            var mensagem = await RecusaAsync(() => _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Pacote)));

            Assert.Equal("O aluno ja tem um pacote de aulas individuais ativo.", mensagem);
        }

        [Fact]
        public async Task Mensalidade_continua_exclusiva_mesmo_com_varios_pacotes()
        {
            Semear(ModalidadeCobranca.Pacote, TurmaBasica);
            Semear(ModalidadeCobranca.Pacote, TurmaMedio);

            await RecusaAsync(() => _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Mensalidade)));
        }

        [Fact]
        public async Task Aluno_com_pacote_de_turma_aceita_avulsa_de_atendimento_individual()
        {
            SemearTurma(TurmaBasica, inicio: Hoje.AddDays(1), fim: Hoje.AddDays(60));
            Semear(ModalidadeCobranca.Pacote, TurmaBasica);

            var resposta = await _servico.CadastrarAsync(AlunoId, Pedido(ModalidadeCobranca.Avulsa));

            Assert.True(resposta.Ativo);
        }

        // ---------- A turma de um pacote nao muda (FR-005) ----------

        [Fact]
        public async Task Turma_do_pacote_nao_pode_ser_alterada_na_edicao()
        {
            SemearTurma(TurmaBasica, inicio: Hoje.AddDays(1), fim: Hoje.AddDays(60));
            SemearTurma(TurmaMedio, inicio: Hoje.AddDays(1), fim: Hoje.AddDays(60));
            var pacote = Semear(ModalidadeCobranca.Pacote, TurmaBasica);

            var paraOutraTurma = await RecusaAsync(() => _servico.AtualizarAsync(AlunoId, pacote.Id, Pedido(ModalidadeCobranca.Pacote, TurmaMedio)));
            var paraIndividual = await RecusaAsync(() => _servico.AtualizarAsync(AlunoId, pacote.Id, Pedido(ModalidadeCobranca.Pacote)));

            Assert.Equal("A turma de um pacote nao pode ser alterada. Inative-o e cadastre outro.", paraOutraTurma);
            Assert.Equal(paraOutraTurma, paraIndividual);
            Assert.Equal(TurmaBasica, pacote.TurmaId);
        }

        [Fact]
        public async Task Editar_o_valor_de_pacote_de_turma_ja_iniciada_continua_possivel()
        {
            SemearTurma(TurmaBasica, inicio: Hoje.AddDays(-5), fim: Hoje.AddDays(30), aluno: true);
            var pacote = Semear(ModalidadeCobranca.Pacote, TurmaBasica);

            var pedido = Pedido(ModalidadeCobranca.Pacote, TurmaBasica) with { Valor = 250m };
            var resposta = await _servico.AtualizarAsync(AlunoId, pacote.Id, pedido);

            Assert.Equal(250m, resposta.Valor);
            Assert.Equal(TurmaBasica, resposta.TurmaId);
        }

        // ---------- Excluir (inativar) nao mexe na matricula (FR-006) ----------

        [Fact]
        public async Task Inativar_o_pacote_nao_desmatricula_o_aluno()
        {
            var turma = SemearTurma(TurmaBasica, inicio: Hoje.AddDays(-5), fim: Hoje.AddDays(30), aluno: true);
            var pacote = Semear(ModalidadeCobranca.Pacote, TurmaBasica, saldo: 4);

            await _servico.ExcluirAsync(AlunoId, pacote.Id);

            Assert.False(pacote.Ativo);
            Assert.Equal(4, pacote.SaldoAulas);
            Assert.Single(turma.AlunosTurma);
            Assert.Equal(0, _turmas.ChamadasDesvincularAluno);
        }

        // ---------- Reativar so com a turma em andamento (FR-021) ----------

        [Fact]
        public async Task Reativar_pacote_de_turma_encerrada_e_recusado_e_depois_de_prorrogar_e_aceito()
        {
            var turma = SemearTurma(TurmaBasica, inicio: Hoje.AddDays(-30), fim: Hoje.AddDays(-1), aluno: true);
            var pacote = Semear(ModalidadeCobranca.Pacote, TurmaBasica, ativo: false, saldo: 3);

            var mensagem = await RecusaAsync(() => _servico.ReativarAsync(AlunoId, pacote.Id));
            Assert.Contains("Prorrogue a data de fim", mensagem);
            Assert.False(pacote.Ativo);

            turma.DataFim = Hoje.AddDays(30); // professora prorrogou a turma
            var resposta = await _servico.ReativarAsync(AlunoId, pacote.Id);

            Assert.True(resposta.Ativo);
            Assert.Equal(3, pacote.SaldoAulas);
        }

        [Fact]
        public async Task Reativar_pacote_de_turma_inativa_e_recusado()
        {
            SemearTurma(TurmaBasica, inicio: Hoje.AddDays(-5), fim: Hoje.AddDays(30), ativa: false);
            var pacote = Semear(ModalidadeCobranca.Pacote, TurmaBasica, ativo: false);

            await RecusaAsync(() => _servico.ReativarAsync(AlunoId, pacote.Id));
        }

        [Fact]
        public async Task Reativar_com_a_turma_iniciada_mas_em_andamento_e_aceito_e_rematricula_o_aluno()
        {
            var turma = SemearTurma(TurmaBasica, inicio: Hoje.AddDays(-5), fim: Hoje.AddDays(30)); // aluno nao esta matriculado
            var pacote = Semear(ModalidadeCobranca.Pacote, TurmaBasica, ativo: false);

            var resposta = await _servico.ReativarAsync(AlunoId, pacote.Id);

            Assert.True(resposta.Ativo);
            Assert.Single(turma.AlunosTurma);
            Assert.Equal(1, _turmas.ChamadasVincularAluno);
        }

        [Fact]
        public async Task Reativar_e_recusado_se_ja_ha_outro_pacote_ativo_da_mesma_turma()
        {
            SemearTurma(TurmaBasica, inicio: Hoje.AddDays(-5), fim: Hoje.AddDays(30), aluno: true);
            var antigo = Semear(ModalidadeCobranca.Pacote, TurmaBasica, ativo: false);
            Semear(ModalidadeCobranca.Pacote, TurmaBasica);

            var mensagem = await RecusaAsync(() => _servico.ReativarAsync(AlunoId, antigo.Id));

            Assert.Equal("O aluno ja tem um pacote ativo desta turma.", mensagem);
        }

        [Fact]
        public async Task Reativar_pacote_de_aulas_individuais_nao_depende_de_turma()
        {
            var pacote = Semear(ModalidadeCobranca.Pacote, ativo: false);

            var resposta = await _servico.ReativarAsync(AlunoId, pacote.Id);

            Assert.True(resposta.Ativo);
            Assert.Equal(0, _turmas.ChamadasVincularAluno);
        }
    }
}
