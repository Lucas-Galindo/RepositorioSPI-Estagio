using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/052: auditoria de aulas dadas por Vinculo de Cobranca -- US1
    // (total do Pacote), US3 (teto/contagem mensal da Mensalidade) e US4
    // (bloqueio de edicao), reunidas no mesmo arquivo porque compartilham o
    // mesmo setup minimo (sem turma, sem validacao de participacao).
    public class VinculoCobrancaServiceAuditoriaTests
    {
        private const int AlunoId = 1;

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly FakeAlunoRepositoryParaVinculo _alunos = new();
        private readonly FakeAulaRepositoryParaVinculo _aulas = new();
        private readonly VinculoCobrancaService _servico;

        public VinculoCobrancaServiceAuditoriaTests()
        {
            _alunos.Alunos.Add(new Aluno { Id = AlunoId, Nome = "Ana", Ativo = true });
            _servico = new VinculoCobrancaService(_vinculos, _alunos, _turmas, _aulas);
        }

        private VinculoCobranca Semear(
            ModalidadeCobranca modalidade, decimal valor = 350m, int? aulasIncluidas = null,
            int? saldoAulas = null, int? totalAulasPacote = null) =>
            _vinculos.Semear(new VinculoCobranca
            {
                AlunoId = AlunoId, TurmaId = null, Modalidade = modalidade, Valor = valor,
                AulasIncluidas = aulasIncluidas, SaldoAulas = saldoAulas, TotalAulasPacote = totalAulasPacote, Ativo = true,
            });

        private static VinculoCobrancaRequest Request(
            ModalidadeCobranca modalidade, decimal valor = 350m, int? aulasIncluidas = null,
            int? saldoAulas = null, int? totalAulasPacote = null) => new()
        {
            TurmaId = null, Modalidade = modalidade, Valor = valor,
            AulasIncluidas = aulasIncluidas, SaldoAulas = saldoAulas, TotalAulasPacote = totalAulasPacote,
        };

        private void SemearPresencasNoMesCorrente(int vinculoId, int quantidade)
        {
            var agora = DateTime.Now;
            for (var i = 0; i < quantidade; i++)
            {
                _aulas.Presencas.Add((vinculoId, agora.Year, agora.Month));
            }
        }

        // ---------- US1 (T010): AulasDadas do Pacote ----------

        [Fact]
        public async Task Pacote_com_total_e_saldo_informados_calcula_aulas_dadas()
        {
            var vinculo = Semear(ModalidadeCobranca.Pacote, saldoAulas: 7, totalAulasPacote: 10);

            var resposta = await _servico.ObterPorIdAsync(AlunoId, vinculo.Id);

            Assert.Equal(3, resposta.AulasDadas);
        }

        [Fact]
        public async Task Pacote_sem_total_informado_aulas_dadas_fica_nulo_sem_lancar_excecao()
        {
            var vinculo = Semear(ModalidadeCobranca.Pacote, saldoAulas: 7, totalAulasPacote: null);

            var resposta = await _servico.ObterPorIdAsync(AlunoId, vinculo.Id);

            Assert.Null(resposta.AulasDadas);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Mensalidade)]
        [InlineData(ModalidadeCobranca.Avulsa)]
        public async Task Mensalidade_ou_Avulsa_aulas_dadas_do_pacote_sempre_nulo(ModalidadeCobranca modalidade)
        {
            var vinculo = Semear(modalidade, aulasIncluidas: modalidade == ModalidadeCobranca.Mensalidade ? 10 : null);

            var resposta = await _servico.ObterPorIdAsync(AlunoId, vinculo.Id);

            Assert.Null(resposta.AulasDadas);
        }

        // ---------- US3 (T020): AulasDadasNoMes da Mensalidade ----------

        [Fact]
        public async Task Mensalidade_com_teto_conta_presencas_do_mes_corrente()
        {
            var vinculo = Semear(ModalidadeCobranca.Mensalidade, aulasIncluidas: 10);
            SemearPresencasNoMesCorrente(vinculo.Id, 4);

            var resposta = await _servico.ObterPorIdAsync(AlunoId, vinculo.Id);

            Assert.Equal(4, resposta.AulasDadasNoMes);
        }

        [Fact]
        public async Task Mensalidade_conta_so_as_presencas_do_mes_corrente_nao_do_mes_anterior()
        {
            var vinculo = Semear(ModalidadeCobranca.Mensalidade, aulasIncluidas: 10);
            SemearPresencasNoMesCorrente(vinculo.Id, 4);
            var mesAnterior = DateTime.Now.AddMonths(-1);
            _aulas.Presencas.Add((vinculo.Id, mesAnterior.Year, mesAnterior.Month));
            _aulas.Presencas.Add((vinculo.Id, mesAnterior.Year, mesAnterior.Month));

            var resposta = await _servico.ObterPorIdAsync(AlunoId, vinculo.Id);

            Assert.Equal(4, resposta.AulasDadasNoMes);
        }

        [Fact]
        public async Task Mensalidade_sem_teto_ainda_calcula_aulas_dadas_no_mes()
        {
            var vinculo = Semear(ModalidadeCobranca.Mensalidade, aulasIncluidas: null);
            SemearPresencasNoMesCorrente(vinculo.Id, 5);

            var resposta = await _servico.ObterPorIdAsync(AlunoId, vinculo.Id);

            Assert.Equal(5, resposta.AulasDadasNoMes);
            Assert.Null(resposta.AulasIncluidas);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Pacote)]
        [InlineData(ModalidadeCobranca.Avulsa)]
        public async Task Pacote_ou_Avulsa_aulas_dadas_no_mes_sempre_nulo(ModalidadeCobranca modalidade)
        {
            var vinculo = Semear(modalidade, saldoAulas: modalidade == ModalidadeCobranca.Pacote ? 10 : null, totalAulasPacote: modalidade == ModalidadeCobranca.Pacote ? 10 : null);

            var resposta = await _servico.ObterPorIdAsync(AlunoId, vinculo.Id);

            Assert.Null(resposta.AulasDadasNoMes);
        }

        // ---------- US4 (T026): bloqueio de edicao ----------

        [Fact]
        public async Task Pacote_reduzir_total_abaixo_do_ja_dado_e_rejeitado()
        {
            var vinculo = Semear(ModalidadeCobranca.Pacote, saldoAulas: 4, totalAulasPacote: 10); // dado = 6

            var excecao = await Assert.ThrowsAsync<ConflitoException>(
                () => _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(ModalidadeCobranca.Pacote, saldoAulas: 4, totalAulasPacote: 5)));

            Assert.Equal("O novo total de aulas do pacote nao pode ser menor que as aulas ja dadas (6).", excecao.Message);
        }

        [Fact]
        public async Task Pacote_aumentar_total_para_igual_ao_ja_dado_e_aceito_sem_recalcular_saldo()
        {
            var vinculo = Semear(ModalidadeCobranca.Pacote, saldoAulas: 4, totalAulasPacote: 10); // dado = 6

            var resposta = await _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(ModalidadeCobranca.Pacote, saldoAulas: 4, totalAulasPacote: 6));

            Assert.Equal(6, resposta.TotalAulasPacote);
            // specs/052 (/speckit-analyze achado F1): SaldoAulas nunca e
            // recalculado automaticamente a partir do novo total.
            Assert.Equal(4, resposta.SaldoAulas);
        }

        [Fact]
        public async Task Mensalidade_reduzir_teto_abaixo_do_ja_dado_no_mes_e_rejeitado()
        {
            var vinculo = Semear(ModalidadeCobranca.Mensalidade, aulasIncluidas: 10);
            SemearPresencasNoMesCorrente(vinculo.Id, 7);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(
                () => _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(ModalidadeCobranca.Mensalidade, aulasIncluidas: 5)));

            Assert.Equal("O novo teto de aulas da mensalidade nao pode ser menor que as aulas ja dadas neste mes (7).", excecao.Message);
        }

        [Fact]
        public async Task Mensalidade_sem_definir_teto_na_edicao_e_sempre_aceito()
        {
            var vinculo = Semear(ModalidadeCobranca.Mensalidade, aulasIncluidas: 10);
            SemearPresencasNoMesCorrente(vinculo.Id, 7);

            var resposta = await _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(ModalidadeCobranca.Mensalidade, aulasIncluidas: null));

            Assert.Null(resposta.AulasIncluidas);
        }

        // Edge case (/speckit-analyze achado C1): definir um teto pela primeira
        // vez (vinha null) tambem e bloqueado se ja houver mais dado no mes.
        [Fact]
        public async Task Mensalidade_definir_teto_pela_primeira_vez_abaixo_do_ja_dado_e_rejeitado()
        {
            var vinculo = Semear(ModalidadeCobranca.Mensalidade, aulasIncluidas: null);
            SemearPresencasNoMesCorrente(vinculo.Id, 7);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(
                () => _servico.AtualizarAsync(AlunoId, vinculo.Id, Request(ModalidadeCobranca.Mensalidade, aulasIncluidas: 5)));

            Assert.Equal("O novo teto de aulas da mensalidade nao pode ser menor que as aulas ja dadas neste mes (7).", excecao.Message);
        }
    }
}
