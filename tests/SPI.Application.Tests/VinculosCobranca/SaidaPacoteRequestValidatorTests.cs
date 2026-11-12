using FluentValidation.TestHelper;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Validators;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/078: formato do valor informado na saida.
    public class SaidaPacoteRequestValidatorTests
    {
        private readonly SaidaPacoteRequestValidator _validator = new();
        private readonly ConfirmarSaidaPacoteRequestValidator _confirmar = new();

        [Theory]
        [InlineData("0")]
        [InlineData("30")]
        [InlineData("30.50")]
        [InlineData("9999999.99")]
        public void Valores_validos_passam(string valor)
        {
            _validator.TestValidate(new SaidaPacoteRequest { ValorPago = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture) })
                .ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("-0.01")]
        public void Valor_negativo_e_recusado(string valor)
        {
            _validator.TestValidate(new SaidaPacoteRequest { ValorPago = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture) })
                .ShouldHaveValidationErrorFor(x => x.ValorPago);
        }

        [Theory]
        [InlineData("10.005")]
        [InlineData("0.001")]
        public void Mais_de_duas_casas_decimais_e_recusado(string valor)
        {
            _validator.TestValidate(new SaidaPacoteRequest { ValorPago = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture) })
                .ShouldHaveValidationErrorFor(x => x.ValorPago);
        }

        [Fact]
        public void Acima_do_teto_e_recusado()
        {
            _validator.TestValidate(new SaidaPacoteRequest { ValorPago = 10000000m })
                .ShouldHaveValidationErrorFor(x => x.ValorPago);
        }

        [Fact]
        public void A_confirmacao_herda_a_regra_do_valor_pago_e_valida_os_ecos()
        {
            var ok = new ConfirmarSaidaPacoteRequest { ValorPago = 30m, ConsumidasEsperadas = 7, ValorDevidoEsperado = 70m };
            _confirmar.TestValidate(ok).ShouldNotHaveAnyValidationErrors();

            _confirmar.TestValidate(ok with { ValorPago = -5m }).ShouldHaveValidationErrorFor(x => x.ValorPago);
            _confirmar.TestValidate(ok with { ConsumidasEsperadas = -1 }).ShouldHaveValidationErrorFor(x => x.ConsumidasEsperadas);
            _confirmar.TestValidate(ok with { ValorDevidoEsperado = -1m }).ShouldHaveValidationErrorFor(x => x.ValorDevidoEsperado);
            _confirmar.TestValidate(ok with { AulasQueSairaoEsperadas = -1 }).ShouldHaveValidationErrorFor(x => x.AulasQueSairaoEsperadas);
        }
    }
}
