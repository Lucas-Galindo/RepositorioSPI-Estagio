using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/037 (US2): edicao de VinculoCobranca -- FR-008 (revalida FR-002 a
    // FR-006), FR-013 na edicao (so quando a Turma muda) e o requisito negativo
    // de que o vinculo nunca cria/altera a participacao do aluno em turma.
    public class VinculoCobrancaServiceAtualizarTests
    {
        private const int AlunoId = 1;
        private const int TurmaAId = 10;
        private const int TurmaBId = 11;

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly FakeAlunoRepositoryParaVinculo _alunos = new();
        private readonly VinculoCobrancaService _servico;

        public VinculoCobrancaServiceAtualizarTests()
        {
            _alunos.Alunos.Add(new Aluno { Id = AlunoId, Nome = "Ana", Ativo = true });
            AdicionarTurma(TurmaAId, "Turma A", ativa: true, alunoParticipa: true);
            AdicionarTurma(TurmaBId, "Turma B", ativa: true, alunoParticipa: true);
            _servico = new VinculoCobrancaService(_vinculos, _alunos, _turmas);
        }

        private Turma AdicionarTurma(int id, string nome, bool ativa, bool alunoParticipa)
        {
            var turma = new Turma { Id = id, Nome = nome, Ativo = ativa };
            if (alunoParticipa)
            {
                turma.AlunosTurma.Add(new AlunoTurma { AlunoId = AlunoId, TurmaId = id, Turma = turma });
            }

            _turmas.Turmas.Add(turma);
            _vinculos.TurmasConhecidas.Add(turma);
            return turma;
        }

        private VinculoCobranca Semear(
            int? turmaId, ModalidadeCobranca modalidade = ModalidadeCobranca.Mensalidade, decimal valor = 350m,
            int? aulasIncluidas = null, int? saldoAulas = null, bool ativo = true, int alunoId = AlunoId) =>
            _vinculos.Semear(new VinculoCobranca
            {
                AlunoId = alunoId, TurmaId = turmaId, Modalidade = modalidade, Valor = valor,
                AulasIncluidas = aulasIncluidas, SaldoAulas = saldoAulas, Ativo = ativo,
            });

        private static VinculoCobrancaRequest Request(
            int? turmaId, ModalidadeCobranca modalidade = ModalidadeCobranca.Mensalidade, decimal valor = 350m,
            int? aulasIncluidas = null, int? saldoAulas = null) => new()
        {
            TurmaId = turmaId, Modalidade = modalidade, Valor = valor, AulasIncluidas = aulasIncluidas, SaldoAulas = saldoAulas,
        };

        private void AssertNenhumaEscritaEmAlunoTurma()
        {
            Assert.Equal(0, _turmas.ChamadasVincularAluno);
            Assert.Equal(0, _turmas.ChamadasDesvincularAluno);
        }

        [Fact]
        public async Task Editar_o_valor_persiste_e_devolve_a_resposta_atualizada()
        {
            var vinculo = Semear(TurmaAId, aulasIncluidas: 8);

            var resposta = await _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(TurmaAId, valor: 400m, aulasIncluidas: 8));

            Assert.Equal(400m, resposta.Valor);
            Assert.Equal(400m, _vinculos.Vinculos.Single().Valor);
            Assert.True(_vinculos.SalvamentosRealizados > 0);
            AssertNenhumaEscritaEmAlunoTurma();
        }

        [Fact]
        public async Task Trocar_Mensalidade_por_Pacote_persiste_com_saldo_e_sem_aulas_incluidas()
        {
            var vinculo = Semear(TurmaAId, aulasIncluidas: 8);

            var resposta = await _servico.AtualizarAsync(
                AlunoId, vinculo.Id, Request(TurmaAId, ModalidadeCobranca.Pacote, 500m, aulasIncluidas: null, saldoAulas: 10));

            Assert.Equal(ModalidadeCobranca.Pacote, resposta.Modalidade);
            Assert.Equal(10, resposta.SaldoAulas);
            Assert.Null(resposta.AulasIncluidas);
        }

        [Fact]
        public async Task Mudar_para_turma_que_ja_tem_outro_vinculo_ativo_do_aluno_e_rejeitado()
        {
            var alvo = Semear(TurmaAId);
            Semear(TurmaBId);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(
                () => _servico.AtualizarAsync(AlunoId, alvo.Id, Request(TurmaBId)));

            Assert.Equal("Ja existe um vinculo de cobranca ativo para este aluno nesta turma.", excecao.Message);
            Assert.Equal(TurmaAId, alvo.TurmaId);
            AssertNenhumaEscritaEmAlunoTurma();
        }

        [Fact]
        public async Task Mudar_para_atendimento_individual_quando_ja_existe_individual_ativo_e_rejeitado()
        {
            var alvo = Semear(TurmaAId);
            Semear(null, ModalidadeCobranca.Avulsa, 80m);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(
                () => _servico.AtualizarAsync(AlunoId, alvo.Id, Request(null, ModalidadeCobranca.Avulsa, 80m)));

            Assert.Equal("Ja existe um vinculo de cobranca ativo para este aluno em atendimento individual.", excecao.Message);
            Assert.Equal(TurmaAId, alvo.TurmaId);
        }

        [Fact]
        public async Task Mudar_para_turma_livre_do_aluno_e_permitido()
        {
            var vinculo = Semear(TurmaAId);

            var resposta = await _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(TurmaBId));

            Assert.Equal(TurmaBId, resposta.TurmaId);
            Assert.Equal("Turma B", resposta.TurmaNome);
        }

        [Fact]
        public async Task Mudar_para_turma_inativa_e_rejeitado()
        {
            var vinculo = Semear(TurmaAId);
            AdicionarTurma(20, "Turma Inativa", ativa: false, alunoParticipa: true);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(20)));

            Assert.Equal("A turma informada esta inativa ou o aluno nao participa dela.", excecao.Message);
            Assert.Equal(TurmaAId, vinculo.TurmaId);
        }

        [Fact]
        public async Task Mudar_para_turma_da_qual_o_aluno_nao_participa_e_rejeitado_sem_criar_participacao()
        {
            var vinculo = Semear(TurmaAId);
            var turmaAlheia = AdicionarTurma(30, "Turma C", ativa: true, alunoParticipa: false);

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(30)));

            Assert.Empty(turmaAlheia.AlunosTurma);
            Assert.Equal(TurmaAId, vinculo.TurmaId);
            AssertNenhumaEscritaEmAlunoTurma();
        }

        // Plano R2: turma INALTERADA nao e revalidada -- editar so o Valor de um vinculo
        // cuja turma foi desativada (ou da qual o aluno saiu) depois continua possivel.
        [Fact]
        public async Task Turma_inalterada_nao_e_revalidada_mesmo_com_turma_inativada_depois()
        {
            var vinculo = Semear(TurmaAId);
            _turmas.Turmas.Single(t => t.Id == TurmaAId).Ativo = false;

            var resposta = await _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(TurmaAId, valor: 420m));

            Assert.Equal(420m, resposta.Valor);
            AssertNenhumaEscritaEmAlunoTurma();
        }

        [Fact]
        public async Task Turma_inalterada_nao_e_revalidada_mesmo_com_aluno_removido_da_turma_depois()
        {
            var vinculo = Semear(TurmaAId);
            _turmas.Turmas.Single(t => t.Id == TurmaAId).AlunosTurma.Clear();

            var resposta = await _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(TurmaAId, valor: 420m));

            Assert.Equal(420m, resposta.Valor);
        }

        [Fact]
        public async Task Verificacao_de_unicidade_na_edicao_ignora_o_proprio_vinculo()
        {
            var vinculo = Semear(TurmaAId);

            var resposta = await _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(TurmaAId, valor: 380m));

            Assert.Equal(380m, resposta.Valor);
        }

        [Fact]
        public async Task Editar_vinculo_excluido_exige_reativar_antes()
        {
            var vinculo = Semear(TurmaAId, ativo: false);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(
                () => _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(TurmaAId, valor: 999m)));

            Assert.Equal("Reative o vinculo antes de edita-lo.", excecao.Message);
            Assert.Equal(350m, vinculo.Valor);
        }

        [Fact]
        public async Task Vinculo_inexistente_ou_de_outro_aluno_lanca_NaoEncontrado()
        {
            var deOutroAluno = Semear(TurmaAId, alunoId: 2);

            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.AtualizarAsync(AlunoId, 999, Request(TurmaAId)));
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.AtualizarAsync(AlunoId, deOutroAluno.Id, Request(TurmaAId)));
        }
    }
}
