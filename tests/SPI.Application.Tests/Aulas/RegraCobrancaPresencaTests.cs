using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/056 (research.md R1/R2): cada ramo da ordem de decisao de cobranca de uma presenca.
    public class RegraCobrancaPresencaTests
    {
        private const int TurmaDaAula = 10;

        private static VinculoCobranca V(int id, ModalidadeCobranca modalidade, int? turmaId = null, int? saldo = null) =>
            new() { Id = id, AlunoId = 1, Modalidade = modalidade, TurmaId = turmaId, SaldoAulas = saldo, Valor = 100m, Ativo = true };

        private static DecisaoCobranca Decidir(int? turma, params VinculoCobranca[] vinculos) =>
            RegraCobrancaPresenca.Decidir(vinculos, turma);

        [Fact]
        public void Mensalidade_vence_tudo()
        {
            var mensalidade = V(2, ModalidadeCobranca.Mensalidade);

            var d = Decidir(TurmaDaAula, V(1, ModalidadeCobranca.Pacote, saldo: 5), mensalidade, V(3, ModalidadeCobranca.Avulsa, TurmaDaAula));

            Assert.Equal(TipoCobrancaPresenca.Mensalidade, d.Tipo);
            Assert.Same(mensalidade, d.Vinculo);
        }

        [Fact]
        public void Pacote_com_saldo_vence_avulsa_do_contexto()
        {
            var pacote = V(1, ModalidadeCobranca.Pacote, saldo: 3);

            var d = Decidir(TurmaDaAula, pacote, V(2, ModalidadeCobranca.Avulsa, TurmaDaAula));

            Assert.Equal(TipoCobrancaPresenca.Pacote, d.Tipo);
            Assert.Same(pacote, d.Vinculo);
        }

        [Fact]
        public void Pacote_com_saldo_nulo_e_usado_sem_bloquear()
        {
            var d = Decidir(null, V(1, ModalidadeCobranca.Pacote, saldo: null));

            Assert.Equal(TipoCobrancaPresenca.Pacote, d.Tipo);
        }

        [Fact]
        public void Avulsa_do_contexto_sem_pacote_nao_marca_pacote_esgotado()
        {
            var d = Decidir(TurmaDaAula, V(1, ModalidadeCobranca.Avulsa, TurmaDaAula));

            Assert.Equal(TipoCobrancaPresenca.Avulsa, d.Tipo);
            Assert.False(d.PacoteEsgotado);
        }

        [Fact]
        public void Avulsa_individual_vale_so_para_aula_individual()
        {
            var individual = V(1, ModalidadeCobranca.Avulsa, turmaId: null);

            Assert.Equal(TipoCobrancaPresenca.Avulsa, Decidir(null, individual).Tipo);
            Assert.Equal(TipoCobrancaPresenca.SemVinculo, Decidir(TurmaDaAula, individual).Tipo);
        }

        [Fact]
        public void Avulsa_de_outro_contexto_nao_vale()
        {
            var d = Decidir(TurmaDaAula, V(1, ModalidadeCobranca.Avulsa, turmaId: 99));

            Assert.Equal(TipoCobrancaPresenca.SemVinculo, d.Tipo);
            Assert.Null(d.Vinculo);
        }

        [Fact]
        public void Pacote_esgotado_com_avulsa_do_contexto_cai_na_avulsa_com_aviso()
        {
            var avulsa = V(2, ModalidadeCobranca.Avulsa, TurmaDaAula);

            var d = Decidir(TurmaDaAula, V(1, ModalidadeCobranca.Pacote, saldo: 0), avulsa);

            Assert.Equal(TipoCobrancaPresenca.Avulsa, d.Tipo);
            Assert.Same(avulsa, d.Vinculo);
            Assert.True(d.PacoteEsgotado);
        }

        [Fact]
        public void Pacote_esgotado_sem_avulsa_bloqueia()
        {
            var pacote = V(1, ModalidadeCobranca.Pacote, saldo: 0);

            var d = Decidir(TurmaDaAula, pacote);

            Assert.Equal(TipoCobrancaPresenca.Bloqueada, d.Tipo);
            Assert.Same(pacote, d.Vinculo);
        }

        [Fact]
        public void Sem_vinculo()
        {
            var d = Decidir(TurmaDaAula);

            Assert.Equal(TipoCobrancaPresenca.SemVinculo, d.Tipo);
        }

        // Conflito herdado: com dois pacotes, usa o mais antigo que ainda tem saldo.
        [Fact]
        public void Dois_pacotes_usa_o_mais_antigo_com_saldo()
        {
            var antigoEsgotado = V(1, ModalidadeCobranca.Pacote, saldo: 0);
            var antigoComSaldo = V(2, ModalidadeCobranca.Pacote, saldo: 2);
            var novoComSaldo = V(3, ModalidadeCobranca.Pacote, saldo: 5);

            var d = Decidir(TurmaDaAula, novoComSaldo, antigoEsgotado, antigoComSaldo);

            Assert.Same(antigoComSaldo, d.Vinculo);
        }

        [Fact]
        public void Vinculo_inativo_e_ignorado()
        {
            var inativo = V(1, ModalidadeCobranca.Mensalidade);
            inativo.Ativo = false;

            Assert.Equal(TipoCobrancaPresenca.SemVinculo, Decidir(TurmaDaAula, inativo).Tipo);
        }
    }
}
