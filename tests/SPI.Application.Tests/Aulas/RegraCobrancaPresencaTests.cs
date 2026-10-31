using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/056 (research.md R1/R2) + specs/063 (research.md R5): cada ramo da ordem de
    // decisao de cobranca de uma presenca. Desde a spec 063 o pacote so vale no SEU grupo:
    // o pacote de uma turma, em aula dessa turma; o pacote de aulas individuais (sem turma),
    // em aula individual.
    public class RegraCobrancaPresencaTests
    {
        private const int TurmaDaAula = 10;
        private const int OutraTurma = 99;

        private static VinculoCobranca V(int id, ModalidadeCobranca modalidade, int? turmaId = null, int? saldo = null) =>
            new() { Id = id, AlunoId = 1, Modalidade = modalidade, TurmaId = turmaId, SaldoAulas = saldo, Valor = 100m, Ativo = true };

        private static DecisaoCobranca Decidir(int? turma, params VinculoCobranca[] vinculos) =>
            RegraCobrancaPresenca.Decidir(vinculos, turma);

        [Fact]
        public void Mensalidade_vence_tudo()
        {
            var mensalidade = V(2, ModalidadeCobranca.Mensalidade);

            var d = Decidir(TurmaDaAula, V(1, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 5), mensalidade, V(3, ModalidadeCobranca.Avulsa, TurmaDaAula));

            Assert.Equal(TipoCobrancaPresenca.Mensalidade, d.Tipo);
            Assert.Same(mensalidade, d.Vinculo);
        }

        [Fact]
        public void Pacote_da_turma_com_saldo_vence_avulsa_do_contexto()
        {
            var pacote = V(1, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 3);

            var d = Decidir(TurmaDaAula, pacote, V(2, ModalidadeCobranca.Avulsa, TurmaDaAula));

            Assert.Equal(TipoCobrancaPresenca.Pacote, d.Tipo);
            Assert.Same(pacote, d.Vinculo);
        }

        [Fact]
        public void Pacote_com_saldo_nulo_e_usado_sem_bloquear()
        {
            var d = Decidir(TurmaDaAula, V(1, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: null));

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
            var d = Decidir(TurmaDaAula, V(1, ModalidadeCobranca.Avulsa, turmaId: OutraTurma));

            Assert.Equal(TipoCobrancaPresenca.SemVinculo, d.Tipo);
            Assert.Null(d.Vinculo);
        }

        [Fact]
        public void Pacote_da_turma_esgotado_com_avulsa_do_contexto_cai_na_avulsa_com_aviso()
        {
            var avulsa = V(2, ModalidadeCobranca.Avulsa, TurmaDaAula);

            var d = Decidir(TurmaDaAula, V(1, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 0), avulsa);

            Assert.Equal(TipoCobrancaPresenca.Avulsa, d.Tipo);
            Assert.Same(avulsa, d.Vinculo);
            Assert.True(d.PacoteEsgotado);
        }

        [Fact]
        public void Pacote_da_turma_esgotado_sem_avulsa_bloqueia()
        {
            var pacote = V(1, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 0);

            var d = Decidir(TurmaDaAula, pacote);

            Assert.Equal(TipoCobrancaPresenca.Bloqueada, d.Tipo);
            Assert.Same(pacote, d.Vinculo);
        }

        [Fact]
        public void Pacote_individual_esgotado_sem_avulsa_bloqueia_em_aula_individual()
        {
            var pacote = V(1, ModalidadeCobranca.Pacote, turmaId: null, saldo: 0);

            var d = Decidir(null, pacote);

            Assert.Equal(TipoCobrancaPresenca.Bloqueada, d.Tipo);
        }

        [Fact]
        public void Sem_vinculo()
        {
            var d = Decidir(TurmaDaAula);

            Assert.Equal(TipoCobrancaPresenca.SemVinculo, d.Tipo);
        }

        // Conflito herdado (nao deveria existir mais de um pacote do mesmo grupo): usa o mais
        // antigo que ainda tem saldo.
        [Fact]
        public void Dois_pacotes_do_mesmo_grupo_herdados_usam_o_mais_antigo_com_saldo()
        {
            var antigoEsgotado = V(1, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 0);
            var antigoComSaldo = V(2, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 2);
            var novoComSaldo = V(3, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 5);

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

        // ---- specs/063: o pacote so vale no seu grupo ----

        [Fact]
        public void Pacote_de_turma_nao_vale_em_aula_de_outra_turma_e_cobra_avulso()
        {
            var d = Decidir(OutraTurma, V(1, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 5));

            Assert.Equal(TipoCobrancaPresenca.SemVinculo, d.Tipo);
            Assert.Null(d.Vinculo);
        }

        [Fact]
        public void Pacote_de_turma_nao_vale_em_aula_individual()
        {
            var d = Decidir(null, V(1, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 5));

            Assert.Equal(TipoCobrancaPresenca.SemVinculo, d.Tipo);
            Assert.Null(d.Vinculo);
        }

        [Fact]
        public void Pacote_de_turma_em_aula_individual_cai_na_avulsa_individual()
        {
            var avulsaIndividual = V(2, ModalidadeCobranca.Avulsa, turmaId: null);

            var d = Decidir(null, V(1, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 5), avulsaIndividual);

            Assert.Equal(TipoCobrancaPresenca.Avulsa, d.Tipo);
            Assert.Same(avulsaIndividual, d.Vinculo);
            Assert.False(d.PacoteEsgotado);
        }

        [Fact]
        public void Pacote_individual_nao_vale_em_aula_de_turma()
        {
            var d = Decidir(TurmaDaAula, V(1, ModalidadeCobranca.Pacote, turmaId: null, saldo: 5));

            Assert.Equal(TipoCobrancaPresenca.SemVinculo, d.Tipo);
            Assert.Null(d.Vinculo);
        }

        [Fact]
        public void Pacote_individual_vale_em_aula_individual()
        {
            var pacote = V(1, ModalidadeCobranca.Pacote, turmaId: null, saldo: 5);

            var d = Decidir(null, pacote);

            Assert.Equal(TipoCobrancaPresenca.Pacote, d.Tipo);
            Assert.Same(pacote, d.Vinculo);
        }

        [Fact]
        public void Pacote_fora_do_grupo_esgotado_nao_bloqueia_nem_avisa()
        {
            var esgotadoDeOutraTurma = V(1, ModalidadeCobranca.Pacote, OutraTurma, saldo: 0);
            var avulsa = V(2, ModalidadeCobranca.Avulsa, TurmaDaAula);

            Assert.Equal(TipoCobrancaPresenca.SemVinculo, Decidir(TurmaDaAula, esgotadoDeOutraTurma).Tipo);

            var comAvulsa = Decidir(TurmaDaAula, esgotadoDeOutraTurma, avulsa);
            Assert.Equal(TipoCobrancaPresenca.Avulsa, comAvulsa.Tipo);
            Assert.False(comAvulsa.PacoteEsgotado);
        }

        [Fact]
        public void Aluno_com_pacotes_de_varias_turmas_consome_o_da_turma_da_aula()
        {
            var basica = V(1, ModalidadeCobranca.Pacote, turmaId: 10, saldo: 4);
            var medio = V(2, ModalidadeCobranca.Pacote, turmaId: 20, saldo: 4);
            var individual = V(3, ModalidadeCobranca.Pacote, turmaId: null, saldo: 4);

            Assert.Same(basica, Decidir(10, basica, medio, individual).Vinculo);
            Assert.Same(medio, Decidir(20, basica, medio, individual).Vinculo);
            Assert.Same(individual, Decidir(null, basica, medio, individual).Vinculo);
            Assert.Equal(TipoCobrancaPresenca.SemVinculo, Decidir(30, basica, medio, individual).Tipo);
        }
    }
}
