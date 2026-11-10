using SPI.Application.VinculosCobranca.Services;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/078: o calculo do acerto e estatico e puro -- testavel sem banco nem relogio.
    public class AcertoSaidaPacoteTests
    {
        [Fact]
        public void Exemplo_da_regra_R100_10_aulas_7_consumidas_devido_70_por_aula_10()
        {
            var r = AcertoSaidaPacote.Calcular(100m, 10, 7, 30m);

            Assert.Equal(10.00m, r.ValorPorAula);
            Assert.Equal(70.00m, r.ValorDevido);
            Assert.Equal(40.00m, r.Diferenca);
            Assert.Equal(0m, r.Excedente);
            Assert.Equal(ConsequenciaAcerto.GerarConta, r.Consequencia);
        }

        [Fact]
        public void Pagou_exatamente_o_devido_fecha_o_acerto()
        {
            var r = AcertoSaidaPacote.Calcular(100m, 10, 7, 70m);

            Assert.Equal(0m, r.Diferenca);
            Assert.Equal(0m, r.Excedente);
            Assert.Equal(ConsequenciaAcerto.Fechado, r.Consequencia);
        }

        [Fact]
        public void Pagou_mais_do_que_o_devido_o_excedente_nao_e_devolvido()
        {
            var r = AcertoSaidaPacote.Calcular(100m, 10, 7, 100m);

            Assert.Equal(70.00m, r.ValorDevido);
            Assert.Equal(0m, r.Diferenca);
            Assert.Equal(30.00m, r.Excedente);
            Assert.Equal(ConsequenciaAcerto.ExcedenteNaoDevolvido, r.Consequencia);
        }

        [Fact]
        public void R100_em_3_aulas_com_2_consumidas_arredonda_uma_vez_para_66_67()
        {
            var r = AcertoSaidaPacote.Calcular(100m, 3, 2, 0m);

            Assert.Equal(66.67m, r.ValorDevido);
            Assert.Equal(33.33m, r.ValorPorAula);
            Assert.Equal(66.67m, r.Diferenca);
        }

        [Fact]
        public void Nenhuma_aula_consumida_devido_zero_e_o_pago_nao_e_devolvido()
        {
            var r = AcertoSaidaPacote.Calcular(100m, 10, 0, 40m);

            Assert.Equal(0m, r.ValorDevido);
            Assert.Equal(0m, r.Diferenca);
            Assert.Equal(40m, r.Excedente);
            Assert.Equal(ConsequenciaAcerto.ExcedenteNaoDevolvido, r.Consequencia);
        }

        [Theory]
        [InlineData(100.00, 3)]
        [InlineData(99.90, 7)]
        [InlineData(250.55, 6)]
        [InlineData(1.00, 3)]
        public void Todas_as_aulas_consumidas_o_devido_e_exatamente_o_valor_do_pacote(double valor, int total)
        {
            var pacote = (decimal)valor;

            var r = AcertoSaidaPacote.Calcular(pacote, total, total, 0m);

            Assert.Equal(pacote, r.ValorDevido);
        }

        [Fact]
        public void Mais_presencas_do_que_o_total_nunca_cobra_alem_do_valor_do_pacote()
        {
            var r = AcertoSaidaPacote.Calcular(100m, 10, 12, 0m);

            Assert.Equal(100m, r.ValorDevido);
        }

        [Theory]
        [InlineData(99.90, 7, 3, 42.81)]   // 99,90 x 3 / 7 = 42,8142...
        [InlineData(250.55, 6, 1, 41.76)]  // 41,7583...
        [InlineData(250.55, 6, 5, 208.79)] // 208,7916...
        [InlineData(10.00, 4, 1, 2.50)]
        [InlineData(10.00, 8, 1, 1.25)]
        [InlineData(0.05, 2, 1, 0.03)]     // 0,025 arredonda para cima (longe do zero)
        public void Devido_arredondado_a_centavos_uma_vez_sobre_o_total(double valor, int total, int consumidas, double esperado)
        {
            var r = AcertoSaidaPacote.Calcular((decimal)valor, total, consumidas, 0m);

            Assert.Equal((decimal)esperado, r.ValorDevido);
        }

        [Theory]
        [InlineData(0, ConsequenciaAcerto.GerarConta)]
        [InlineData(69.99, ConsequenciaAcerto.GerarConta)]
        [InlineData(70, ConsequenciaAcerto.Fechado)]
        [InlineData(70.01, ConsequenciaAcerto.ExcedenteNaoDevolvido)]
        public void Consequencia_nos_limites_do_valor_pago(double pago, ConsequenciaAcerto esperada)
        {
            var r = AcertoSaidaPacote.Calcular(100m, 10, 7, (decimal)pago);

            Assert.Equal(esperada, r.Consequencia);
        }

        [Fact]
        public void Diferenca_e_excedente_nunca_sao_negativos_e_so_um_deles_e_positivo()
        {
            foreach (var pago in new[] { 0m, 10m, 69.99m, 70m, 70.01m, 100m, 500m })
            {
                var r = AcertoSaidaPacote.Calcular(100m, 10, 7, pago);

                Assert.True(r.Diferenca >= 0 && r.Excedente >= 0);
                Assert.True(r.Diferenca == 0 || r.Excedente == 0);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Total_de_aulas_invalido_e_recusado(int total)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AcertoSaidaPacote.Calcular(100m, total, 0, 0m));
        }
    }
}
