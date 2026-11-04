using FluentValidation.TestHelper;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Validators;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/062: o plano Semestral/Anual exige o inicio e de 1 a 24 parcelas (valor > 0, uma por mes); as outras
    // modalidades nao aceitam inicio nem parcelas; nenhum plano tem turma.
    public class VinculoCobrancaRequestValidatorPlanosTests
    {
        private readonly VinculoCobrancaRequestValidator _validator = new();

        private static ParcelaRequest Parcela(int ano, int mes, decimal valor = 300m) => new() { Vencimento = new DateOnly(ano, mes, 5), Valor = valor };

        private static VinculoCobrancaRequest PlanoValido(ModalidadeCobranca modalidade = ModalidadeCobranca.Semestral) => new()
        {
            Modalidade = modalidade,
            Valor = 1800m,
            DataInicio = new DateOnly(2026, 11, 1),
            Parcelas = new List<ParcelaRequest> { Parcela(2026, 11), Parcela(2026, 12) },
        };

        [Theory]
        [InlineData(ModalidadeCobranca.Semestral)]
        [InlineData(ModalidadeCobranca.Anual)]
        public void Plano_valido_passa(ModalidadeCobranca modalidade)
        {
            _validator.TestValidate(PlanoValido(modalidade)).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void A_soma_das_parcelas_diferente_do_total_nao_e_erro()
        {
            var request = PlanoValido() with { Valor = 5000m };

            _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Plano_sem_inicio_e_recusado()
        {
            _validator.TestValidate(PlanoValido() with { DataInicio = null }).ShouldHaveValidationErrorFor(x => x.DataInicio);
        }

        [Fact]
        public void Plano_sem_parcela_e_recusado()
        {
            _validator.TestValidate(PlanoValido() with { Parcelas = new List<ParcelaRequest>() }).ShouldHaveValidationErrorFor(x => x.Parcelas);
            _validator.TestValidate(PlanoValido() with { Parcelas = null }).ShouldHaveValidationErrorFor(x => x.Parcelas);
        }

        [Fact]
        public void Vinte_e_quatro_parcelas_passam_e_vinte_e_cinco_nao()
        {
            ParcelaRequest[] Meses(int quantidade) => Enumerable.Range(0, quantidade)
                .Select(i => new ParcelaRequest { Vencimento = new DateOnly(2026, 11, 5).AddMonths(i), Valor = 100m }).ToArray();

            _validator.TestValidate(PlanoValido() with { Parcelas = Meses(24).ToList() }).ShouldNotHaveAnyValidationErrors();

            var resultado = _validator.TestValidate(PlanoValido() with { Parcelas = Meses(25).ToList() });
            resultado.ShouldHaveValidationErrorFor(x => x.Parcelas).WithErrorMessage("O plano aceita no maximo 24 parcelas.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Parcela_com_valor_menor_ou_igual_a_zero_e_recusada(double valor)
        {
            var request = PlanoValido() with { Parcelas = new List<ParcelaRequest> { Parcela(2026, 11, (decimal)valor) } };

            _validator.TestValidate(request).ShouldHaveValidationErrorFor("Parcelas[0].Valor");
        }

        [Fact]
        public void Duas_parcelas_no_mesmo_mes_sao_recusadas()
        {
            var request = PlanoValido() with { Parcelas = new List<ParcelaRequest> { Parcela(2026, 11), new() { Vencimento = new DateOnly(2026, 11, 25), Valor = 300m } } };

            _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Parcelas)
                .WithErrorMessage("As parcelas devem vencer em meses diferentes (no maximo uma por mes).");
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Semestral)]
        [InlineData(ModalidadeCobranca.Anual)]
        public void Plano_com_turma_e_recusado(ModalidadeCobranca modalidade)
        {
            _validator.TestValidate(PlanoValido(modalidade) with { TurmaId = 3 }).ShouldHaveValidationErrorFor(x => x.TurmaId);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Avulsa)]
        [InlineData(ModalidadeCobranca.Mensalidade)]
        [InlineData(ModalidadeCobranca.Pacote)]
        public void Inicio_e_parcelas_nao_se_aplicam_as_outras_modalidades(ModalidadeCobranca modalidade)
        {
            var comInicio = new VinculoCobrancaRequest { Modalidade = modalidade, Valor = 100m, DataInicio = new DateOnly(2026, 11, 1) };
            var comParcelas = new VinculoCobrancaRequest { Modalidade = modalidade, Valor = 100m, Parcelas = new List<ParcelaRequest> { Parcela(2026, 11) } };

            _validator.TestValidate(comInicio).ShouldHaveValidationErrorFor(x => x.DataInicio);
            _validator.TestValidate(comParcelas).ShouldHaveValidationErrorFor(x => x.Parcelas);
        }

        [Fact]
        public void As_outras_modalidades_sem_inicio_nem_parcelas_continuam_validas()
        {
            _validator.TestValidate(new VinculoCobrancaRequest { Modalidade = ModalidadeCobranca.Avulsa, Valor = 80m }).ShouldNotHaveAnyValidationErrors();
            _validator.TestValidate(new VinculoCobrancaRequest { Modalidade = ModalidadeCobranca.Mensalidade, Valor = 250m, Parcelas = new List<ParcelaRequest>() }).ShouldNotHaveAnyValidationErrors();
        }
    }
}
