using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/037 (US1): cadastro de VinculoCobranca -- FR-001, FR-005, FR-006,
    // FR-007 e FR-013 (incluindo o requisito negativo: o vinculo NUNCA cria nem
    // altera a participacao do aluno em uma turma).
    public class VinculoCobrancaServiceCadastrarTests
    {
        private const int AlunoId = 1;
        private const int TurmaDoAlunoId = 10;

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly FakeAlunoRepositoryParaVinculo _alunos = new();
        private readonly VinculoCobrancaService _servico;

        public VinculoCobrancaServiceCadastrarTests()
        {
            _alunos.Alunos.Add(new Aluno { Id = AlunoId, Nome = "Ana", Ativo = true });
            AdicionarTurma(TurmaDoAlunoId, "Turma A", ativa: true, alunoParticipa: true);
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

        private static VinculoCobrancaRequest Request(
            int? turmaId = TurmaDoAlunoId,
            ModalidadeCobranca modalidade = ModalidadeCobranca.Mensalidade,
            decimal valor = 350m,
            int? aulasIncluidas = null,
            int? saldoAulas = null) => new()
        {
            TurmaId = turmaId,
            Modalidade = modalidade,
            Valor = valor,
            AulasIncluidas = aulasIncluidas,
            SaldoAulas = saldoAulas,
        };

        private void AssertNenhumaEscritaEmAlunoTurma()
        {
            Assert.Equal(0, _turmas.ChamadasVincularAluno);
            Assert.Equal(0, _turmas.ChamadasDesvincularAluno);
        }

        [Fact]
        public async Task Cadastra_vinculo_de_Mensalidade_em_turma_do_aluno_como_ativo()
        {
            var resposta = await _servico.CadastrarAsync(
                AlunoId, Request(modalidade: ModalidadeCobranca.Mensalidade, valor: 350m, aulasIncluidas: 8));

            Assert.True(resposta.Id > 0);
            Assert.Equal(AlunoId, resposta.AlunoId);
            Assert.Equal(TurmaDoAlunoId, resposta.TurmaId);
            Assert.Equal("Turma A", resposta.TurmaNome);
            Assert.Equal(ModalidadeCobranca.Mensalidade, resposta.Modalidade);
            Assert.Equal(350m, resposta.Valor);
            Assert.Equal(8, resposta.AulasIncluidas);
            Assert.Null(resposta.SaldoAulas);
            Assert.True(resposta.Ativo);
            Assert.Single(_vinculos.Vinculos);
            Assert.True(_vinculos.SalvamentosRealizados > 0);
            AssertNenhumaEscritaEmAlunoTurma();
        }

        [Fact]
        public async Task Cadastra_vinculo_de_atendimento_individual_com_turma_nula()
        {
            var resposta = await _servico.CadastrarAsync(AlunoId, Request(turmaId: null, modalidade: ModalidadeCobranca.Avulsa, valor: 80m));

            Assert.Null(resposta.TurmaId);
            Assert.Null(resposta.TurmaNome);
            Assert.True(resposta.Ativo);
            AssertNenhumaEscritaEmAlunoTurma();
        }

        [Fact]
        public async Task Cadastra_Pacote_com_saldo_e_sem_aulas_incluidas()
        {
            var resposta = await _servico.CadastrarAsync(
                AlunoId, Request(turmaId: null, modalidade: ModalidadeCobranca.Pacote, valor: 500m, saldoAulas: 10));

            Assert.Equal(ModalidadeCobranca.Pacote, resposta.Modalidade);
            Assert.Equal(10, resposta.SaldoAulas);
            Assert.Null(resposta.AulasIncluidas);
        }

        [Fact]
        public async Task Rejeita_segundo_vinculo_ativo_na_mesma_turma()
        {
            await _servico.CadastrarAsync(AlunoId, Request());

            var excecao = await Assert.ThrowsAsync<ConflitoException>(
                () => _servico.CadastrarAsync(AlunoId, Request(modalidade: ModalidadeCobranca.Avulsa, valor: 80m)));

            Assert.Equal("Ja existe um vinculo de cobranca ativo para este aluno nesta turma.", excecao.Message);
            Assert.Single(_vinculos.Vinculos);
            AssertNenhumaEscritaEmAlunoTurma();
        }

        [Fact]
        public async Task Rejeita_segundo_vinculo_ativo_de_atendimento_individual()
        {
            await _servico.CadastrarAsync(AlunoId, Request(turmaId: null, modalidade: ModalidadeCobranca.Avulsa, valor: 80m));

            var excecao = await Assert.ThrowsAsync<ConflitoException>(
                () => _servico.CadastrarAsync(AlunoId, Request(turmaId: null, modalidade: ModalidadeCobranca.Pacote, valor: 500m)));

            Assert.Equal("Ja existe um vinculo de cobranca ativo para este aluno em atendimento individual.", excecao.Message);
            Assert.Single(_vinculos.Vinculos);
        }

        [Fact]
        public async Task Permite_novo_vinculo_quando_o_anterior_da_mesma_combinacao_esta_excluido()
        {
            _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaDoAlunoId, Modalidade = ModalidadeCobranca.Avulsa, Valor = 80m, Ativo = false });
            _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, TurmaId = null, Modalidade = ModalidadeCobranca.Avulsa, Valor = 80m, Ativo = false });

            var naTurma = await _servico.CadastrarAsync(AlunoId, Request());
            var individual = await _servico.CadastrarAsync(AlunoId, Request(turmaId: null, modalidade: ModalidadeCobranca.Pacote, valor: 500m));

            Assert.True(naTurma.Ativo);
            Assert.True(individual.Ativo);
            Assert.Equal(4, _vinculos.Vinculos.Count);
        }

        [Fact]
        public async Task Vinculo_de_outro_aluno_na_mesma_turma_nao_conflita()
        {
            _vinculos.Semear(new VinculoCobranca { AlunoId = 99, TurmaId = TurmaDoAlunoId, Modalidade = ModalidadeCobranca.Avulsa, Valor = 80m, Ativo = true });

            var resposta = await _servico.CadastrarAsync(AlunoId, Request());

            Assert.True(resposta.Ativo);
        }

        [Fact]
        public async Task Rejeita_aluno_inexistente()
        {
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.CadastrarAsync(999, Request()));

            Assert.Empty(_vinculos.Vinculos);
        }

        [Fact]
        public async Task Rejeita_turma_inexistente()
        {
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.CadastrarAsync(AlunoId, Request(turmaId: 999)));

            Assert.Empty(_vinculos.Vinculos);
            AssertNenhumaEscritaEmAlunoTurma();
        }

        [Fact]
        public async Task Rejeita_turma_inativa_mesmo_com_o_aluno_participando()
        {
            AdicionarTurma(20, "Turma Inativa", ativa: false, alunoParticipa: true);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.CadastrarAsync(AlunoId, Request(turmaId: 20)));

            Assert.Equal("A turma informada esta inativa ou o aluno nao participa dela.", excecao.Message);
            Assert.Empty(_vinculos.Vinculos);
            AssertNenhumaEscritaEmAlunoTurma();
        }

        // FR-013 (MUST NOT): mesmo quando a turma e ativa, se o aluno nao participa
        // dela o vinculo e rejeitado e a participacao NAO e criada como efeito colateral.
        [Fact]
        public async Task Rejeita_turma_ativa_da_qual_o_aluno_nao_participa_sem_criar_participacao()
        {
            var turmaAlheia = AdicionarTurma(30, "Turma B", ativa: true, alunoParticipa: false);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _servico.CadastrarAsync(AlunoId, Request(turmaId: 30)));

            Assert.Equal("A turma informada esta inativa ou o aluno nao participa dela.", excecao.Message);
            Assert.Empty(_vinculos.Vinculos);
            Assert.Empty(turmaAlheia.AlunosTurma);
            AssertNenhumaEscritaEmAlunoTurma();
        }
    }
}
