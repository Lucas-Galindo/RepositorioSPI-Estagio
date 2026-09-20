using FluentValidation.TestHelper;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Validators;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/037: regras de validacao do cadastro/edicao de VinculoCobranca
    // (FR-002, FR-003, FR-004). Regras dependentes de repositorio (unicidade,
    // turma elegivel) sao testadas nos testes de servico.
    public class VinculoCobrancaRequestValidatorTests
    {
        private readonly VinculoCobrancaRequestValidator _validator = new();

        private static VinculoCobrancaRequest CriarRequestValido(
            ModalidadeCobranca modalidade = ModalidadeCobranca.Avulsa) => new()
        {
            TurmaId = 1,
            Modalidade = modalidade,
            Valor = 80m,
        };

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-0.01)]
        public void Deve_rejeitar_valor_menor_ou_igual_a_zero(double valor)
        {
            var request = CriarRequestValido() with { Valor = (decimal)valor };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Valor)
                .WithErrorMessage("O Valor deve ser maior que zero.");
        }

        [Fact]
        public void Deve_rejeitar_valor_acima_do_teto_do_campo()
        {
            var request = CriarRequestValido() with { Valor = 100000000m };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Valor);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(350)]
        [InlineData(99999999.99)]
        public void Deve_aceitar_valor_positivo_dentro_do_teto(double valor)
        {
            var request = CriarRequestValido() with { Valor = (decimal)valor };

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.Valor);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(99)]
        public void Deve_rejeitar_modalidade_fora_das_tres_opcoes(int modalidade)
        {
            var request = CriarRequestValido() with { Modalidade = (ModalidadeCobranca)modalidade };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Modalidade);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Avulsa)]
        [InlineData(ModalidadeCobranca.Mensalidade)]
        [InlineData(ModalidadeCobranca.Pacote)]
        public void Deve_aceitar_as_tres_modalidades(ModalidadeCobranca modalidade)
        {
            var request = CriarRequestValido(modalidade);

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.Modalidade);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Avulsa)]
        [InlineData(ModalidadeCobranca.Pacote)]
        public void Deve_rejeitar_aulas_incluidas_fora_da_modalidade_Mensalidade(ModalidadeCobranca modalidade)
        {
            var request = CriarRequestValido(modalidade) with { AulasIncluidas = 8 };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.AulasIncluidas)
                .WithErrorMessage("Aulas incluidas so se aplica a modalidade Mensalidade.");
        }

        [Fact]
        public void Deve_aceitar_aulas_incluidas_com_Mensalidade_e_tambem_sem_informar()
        {
            var comAulas = _validator.TestValidate(CriarRequestValido(ModalidadeCobranca.Mensalidade) with { AulasIncluidas = 8 });
            var semAulas = _validator.TestValidate(CriarRequestValido(ModalidadeCobranca.Mensalidade));

            comAulas.ShouldNotHaveValidationErrorFor(x => x.AulasIncluidas);
            semAulas.ShouldNotHaveValidationErrorFor(x => x.AulasIncluidas);
        }

        [Fact]
        public void Deve_rejeitar_aulas_incluidas_zero_com_Mensalidade()
        {
            var request = CriarRequestValido(ModalidadeCobranca.Mensalidade) with { AulasIncluidas = 0 };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.AulasIncluidas);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Avulsa)]
        [InlineData(ModalidadeCobranca.Mensalidade)]
        public void Deve_rejeitar_saldo_de_aulas_fora_da_modalidade_Pacote(ModalidadeCobranca modalidade)
        {
            var request = CriarRequestValido(modalidade) with { SaldoAulas = 10 };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.SaldoAulas)
                .WithErrorMessage("Saldo de aulas so se aplica a modalidade Pacote.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void Deve_aceitar_saldo_de_aulas_com_Pacote_inclusive_zero(int saldo)
        {
            var request = CriarRequestValido(ModalidadeCobranca.Pacote) with { SaldoAulas = saldo };

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.SaldoAulas);
        }

        [Fact]
        public void Deve_aceitar_Pacote_sem_saldo_informado()
        {
            var result = _validator.TestValidate(CriarRequestValido(ModalidadeCobranca.Pacote));

            result.ShouldNotHaveValidationErrorFor(x => x.SaldoAulas);
        }

        [Fact]
        public void Deve_rejeitar_saldo_de_aulas_negativo_com_Pacote()
        {
            var request = CriarRequestValido(ModalidadeCobranca.Pacote) with { SaldoAulas = -1 };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.SaldoAulas);
        }

        // US2-2: ao trocar Mensalidade -> Pacote na edicao, o campo exclusivo da
        // modalidade anterior deixa de ser aplicavel e MUST ser rejeitado se
        // continuar preenchido.
        [Fact]
        public void Deve_rejeitar_troca_de_Mensalidade_para_Pacote_mantendo_aulas_incluidas()
        {
            var request = CriarRequestValido(ModalidadeCobranca.Pacote) with { AulasIncluidas = 8, SaldoAulas = 10 };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.AulasIncluidas);
            result.ShouldNotHaveValidationErrorFor(x => x.SaldoAulas);
        }

        [Fact]
        public void Deve_aceitar_atendimento_individual_sem_turma()
        {
            var request = CriarRequestValido() with { TurmaId = null };

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
