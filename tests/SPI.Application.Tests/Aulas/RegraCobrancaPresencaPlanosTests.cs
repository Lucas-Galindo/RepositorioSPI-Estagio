using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/062 (FR-009, FR-010): a regra de cobranca por presenca continua num lugar so e agora recebe a data da aula
    // para decidir se um plano Semestral/Anual vale.
    public class RegraCobrancaPresencaPlanosTests
    {
        private static readonly DateOnly Inicio = new(2026, 11, 1);
        private static readonly DateOnly Fim = new(2027, 4, 30);

        private static VinculoCobranca Plano(int id = 1, ModalidadeCobranca modalidade = ModalidadeCobranca.Semestral, DateOnly? inicio = null, DateOnly? fim = null) =>
            new() { Id = id, AlunoId = 1, Modalidade = modalidade, Valor = 1800m, DataInicio = inicio ?? Inicio, DataFim = fim ?? Fim, Ativo = true };

        private static VinculoCobranca Avulsa(int id = 9) =>
            new() { Id = id, AlunoId = 1, Modalidade = ModalidadeCobranca.Avulsa, Valor = 80m, Ativo = true };

        [Theory]
        [InlineData(2026, 11, 1, true)]
        [InlineData(2027, 1, 15, true)]
        [InlineData(2027, 4, 30, true)]
        [InlineData(2026, 10, 31, false)]
        [InlineData(2027, 5, 1, false)]
        public void Semestral_vale_so_dentro_do_periodo(int ano, int mes, int dia, bool vale)
        {
            var d = RegraCobrancaPresenca.Decidir(new[] { Plano(), Avulsa() }, null, new DateOnly(ano, mes, dia));

            Assert.Equal(vale ? TipoCobrancaPresenca.PlanoPeriodico : TipoCobrancaPresenca.Avulsa, d.Tipo);
        }

        [Fact]
        public void Anual_vale_dentro_do_periodo_e_vence_pacote_herdado()
        {
            var pacote = new VinculoCobranca { Id = 5, AlunoId = 1, Modalidade = ModalidadeCobranca.Pacote, SaldoAulas = 3, Valor = 300m, Ativo = true };
            var plano = Plano(modalidade: ModalidadeCobranca.Anual, fim: new DateOnly(2027, 10, 31));

            var d = RegraCobrancaPresenca.Decidir(new[] { pacote, plano }, null, new DateOnly(2027, 8, 1));

            Assert.Equal(TipoCobrancaPresenca.PlanoPeriodico, d.Tipo);
            Assert.Same(plano, d.Vinculo);
        }

        [Fact]
        public void Mensalidade_continua_valendo_em_qualquer_data()
        {
            var mensalidade = new VinculoCobranca { Id = 3, AlunoId = 1, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 250m, Ativo = true };

            Assert.Equal(TipoCobrancaPresenca.PlanoPeriodico, RegraCobrancaPresenca.Decidir(new[] { mensalidade }, null, new DateOnly(2020, 1, 1)).Tipo);
            Assert.Equal(TipoCobrancaPresenca.PlanoPeriodico, RegraCobrancaPresenca.Decidir(new[] { mensalidade }, null, new DateOnly(2090, 1, 1)).Tipo);
        }

        [Fact]
        public void Com_renovacao_antecipada_vale_o_plano_do_periodo_da_aula()
        {
            var atual = Plano(id: 1);
            var proximo = Plano(id: 2, inicio: new DateOnly(2027, 5, 1), fim: new DateOnly(2027, 10, 31));

            Assert.Same(atual, RegraCobrancaPresenca.Decidir(new[] { atual, proximo }, null, new DateOnly(2027, 4, 30)).Vinculo);
            Assert.Same(proximo, RegraCobrancaPresenca.Decidir(new[] { atual, proximo }, null, new DateOnly(2027, 5, 1)).Vinculo);
        }

        [Fact]
        public void Plano_inativo_e_ignorado()
        {
            var inativo = Plano();
            inativo.Ativo = false;

            var d = RegraCobrancaPresenca.Decidir(new[] { inativo }, null, new DateOnly(2026, 12, 1));

            Assert.Equal(TipoCobrancaPresenca.SemVinculo, d.Tipo);
        }
    }
}
