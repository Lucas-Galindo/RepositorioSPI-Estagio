using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/037 (US2): exclusao logica (FR-010, SC-003, FR-007) e reativacao
    // (FR-014, incluindo o requisito negativo: uma reativacao em conflito NAO
    // altera o vinculo ativo existente).
    public class VinculoCobrancaServiceExcluirReativarTests
    {
        private const int AlunoId = 1;
        private const int TurmaAId = 10;

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly FakeAlunoRepositoryParaVinculo _alunos = new();
        private readonly VinculoCobrancaService _servico;

        public VinculoCobrancaServiceExcluirReativarTests()
        {
            _alunos.Alunos.Add(new Aluno { Id = AlunoId, Nome = "Ana", Ativo = true });
            var turma = new Turma { Id = TurmaAId, Nome = "Turma A", Ativo = true };
            turma.AlunosTurma.Add(new AlunoTurma { AlunoId = AlunoId, TurmaId = TurmaAId, Turma = turma });
            _turmas.Turmas.Add(turma);
            _vinculos.TurmasConhecidas.Add(turma);
            _servico = new VinculoCobrancaService(_vinculos, _alunos, _turmas);
        }

        private VinculoCobranca Semear(
            int? turmaId, bool ativo, ModalidadeCobranca modalidade = ModalidadeCobranca.Mensalidade,
            decimal valor = 350m, int? aulasIncluidas = null, int alunoId = AlunoId) =>
            _vinculos.Semear(new VinculoCobranca
            {
                AlunoId = alunoId, TurmaId = turmaId, Modalidade = modalidade, Valor = valor,
                AulasIncluidas = aulasIncluidas, Ativo = ativo,
            });

        private void AssertNenhumaEscritaEmAlunoTurma()
        {
            Assert.Equal(0, _turmas.ChamadasVincularAluno);
            Assert.Equal(0, _turmas.ChamadasDesvincularAluno);
        }

        // ---- Excluir ----

        [Fact]
        public async Task Excluir_define_Ativo_false_e_o_registro_permanece()
        {
            var vinculo = Semear(TurmaAId, ativo: true);

            await _servico.ExcluirAsync(AlunoId, vinculo.Id);

            Assert.Single(_vinculos.Vinculos);
            Assert.False(_vinculos.Vinculos[0].Ativo);
            Assert.True(_vinculos.SalvamentosRealizados > 0);
        }

        [Fact]
        public async Task Excluir_e_idempotente()
        {
            var vinculo = Semear(TurmaAId, ativo: false);

            await _servico.ExcluirAsync(AlunoId, vinculo.Id);

            Assert.Single(_vinculos.Vinculos);
            Assert.False(vinculo.Ativo);
        }

        [Fact]
        public async Task Excluir_vinculo_inexistente_ou_de_outro_aluno_lanca_NaoEncontrado()
        {
            var deOutroAluno = Semear(TurmaAId, ativo: true, alunoId: 2);

            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ExcluirAsync(AlunoId, 999));
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ExcluirAsync(AlunoId, deOutroAluno.Id));
            Assert.True(deOutroAluno.Ativo);
        }

        // ---- Reativar ----

        [Fact]
        public async Task Reativar_sem_conflito_volta_a_Ativo_true_e_devolve_a_resposta()
        {
            var vinculo = Semear(TurmaAId, ativo: false, aulasIncluidas: 8);

            var resposta = await _servico.ReativarAsync(AlunoId, vinculo.Id);

            Assert.True(resposta.Ativo);
            Assert.True(vinculo.Ativo);
            Assert.Equal("Turma A", resposta.TurmaNome);
            AssertNenhumaEscritaEmAlunoTurma();
        }

        // FR-014 (MUST NOT): a reativacao em conflito e rejeitada e o vinculo ATIVO
        // existente permanece exatamente como estava; o excluido continua excluido.
        [Fact]
        public async Task Reativar_com_outro_ativo_na_mesma_turma_e_rejeitado_sem_alterar_nenhum_vinculo()
        {
            var excluido = Semear(TurmaAId, ativo: false, ModalidadeCobranca.Avulsa, 80m);
            var ativo = Semear(TurmaAId, ativo: true, ModalidadeCobranca.Mensalidade, 350m, aulasIncluidas: 8);
            var salvamentosAntes = _vinculos.SalvamentosRealizados;

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.ReativarAsync(AlunoId, excluido.Id));

            Assert.Equal("Ja existe um vinculo de cobranca ativo para este aluno nesta turma.", excecao.Message);
            Assert.False(excluido.Ativo);
            Assert.True(ativo.Ativo);
            Assert.Equal(ModalidadeCobranca.Mensalidade, ativo.Modalidade);
            Assert.Equal(350m, ativo.Valor);
            Assert.Equal(8, ativo.AulasIncluidas);
            Assert.Equal(TurmaAId, ativo.TurmaId);
            Assert.Equal(salvamentosAntes, _vinculos.SalvamentosRealizados);
        }

        [Fact]
        public async Task Reativar_com_outro_individual_ativo_e_rejeitado_sem_alterar_nenhum_vinculo()
        {
            var excluido = Semear(null, ativo: false, ModalidadeCobranca.Pacote, 500m);
            var ativo = Semear(null, ativo: true, ModalidadeCobranca.Avulsa, 80m);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.ReativarAsync(AlunoId, excluido.Id));

            Assert.Equal("Ja existe um vinculo de cobranca ativo para este aluno em atendimento individual.", excecao.Message);
            Assert.False(excluido.Ativo);
            Assert.True(ativo.Ativo);
            Assert.Equal(80m, ativo.Valor);
        }

        [Fact]
        public async Task Reativar_libera_apos_o_vinculo_ativo_ser_excluido()
        {
            var excluido = Semear(TurmaAId, ativo: false);
            var ativo = Semear(TurmaAId, ativo: true);

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.ReativarAsync(AlunoId, excluido.Id));
            await _servico.ExcluirAsync(AlunoId, ativo.Id);
            var resposta = await _servico.ReativarAsync(AlunoId, excluido.Id);

            Assert.True(resposta.Ativo);
            Assert.False(ativo.Ativo);
        }

        // Plano R2: a reativacao so reaplica a unicidade (FR-014), nao FR-013.
        [Fact]
        public async Task Reativar_nao_reaplica_a_regra_de_turma_ativa_do_aluno()
        {
            var vinculo = Semear(TurmaAId, ativo: false);
            _turmas.Turmas.Single().Ativo = false;
            _turmas.Turmas.Single().AlunosTurma.Clear();

            var resposta = await _servico.ReativarAsync(AlunoId, vinculo.Id);

            Assert.True(resposta.Ativo);
        }

        [Fact]
        public async Task Reativar_vinculo_ja_ativo_e_idempotente_e_nao_conflita_consigo_mesmo()
        {
            var vinculo = Semear(TurmaAId, ativo: true);
            var salvamentosAntes = _vinculos.SalvamentosRealizados;

            var resposta = await _servico.ReativarAsync(AlunoId, vinculo.Id);

            Assert.True(resposta.Ativo);
            Assert.Equal(salvamentosAntes, _vinculos.SalvamentosRealizados);
        }

        [Fact]
        public async Task Reativar_vinculo_inexistente_ou_de_outro_aluno_lanca_NaoEncontrado()
        {
            var deOutroAluno = Semear(TurmaAId, ativo: false, alunoId: 2);

            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ReativarAsync(AlunoId, 999));
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ReativarAsync(AlunoId, deOutroAluno.Id));
            Assert.False(deOutroAluno.Ativo);
        }
    }
}
