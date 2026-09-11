using FluentValidation.TestHelper;
using SPI.Application.Lembretes.Dtos;
using SPI.Application.Lembretes.Validators;
using Xunit;

namespace SPI.Application.Tests.Lembretes
{
    public class LembreteRequestValidatorTests
    {
        private readonly LembreteRequestValidator _validator = new();

        private static LembreteRequest CriarRequestValido(string canal = "Email") => new()
        {
            TurmaId = 1,
            Canal = canal,
            AntecedenciaHora = 24,
            Destinatarios = "Alunos",
        };

        [Theory]
        [InlineData("WhatsApp")]
        [InlineData("SMS")]
        [InlineData("")]
        [InlineData("email")]
        public void Deve_rejeitar_canais_diferentes_de_Email(string canal)
        {
            var request = CriarRequestValido(canal);

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Canal)
                .WithErrorMessage("Canal deve ser 'Email'. WhatsApp e SMS foram descontinuados.");
        }

        [Fact]
        public void Deve_aceitar_canal_Email()
        {
            var request = CriarRequestValido("Email");

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.Canal);
        }

        [Theory]
        [InlineData("Alunos")]
        [InlineData("Responsaveis")]
        [InlineData("AlunosEResponsaveis")]
        public void Deve_aceitar_destinatarios_validos(string destinatarios)
        {
            var request = CriarRequestValido() with { Destinatarios = destinatarios };

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveValidationErrorFor(x => x.Destinatarios);
        }

        [Fact]
        public void Deve_rejeitar_destinatario_invalido()
        {
            var request = CriarRequestValido() with { Destinatarios = "Professores" };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Destinatarios);
        }

        [Fact]
        public void Deve_rejeitar_turmaId_nao_positivo()
        {
            var request = CriarRequestValido() with { TurmaId = 0 };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.TurmaId);
        }

        [Fact]
        public void Deve_rejeitar_antecedencia_nao_positiva()
        {
            var request = CriarRequestValido() with { AntecedenciaHora = 0 };

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.AntecedenciaHora);
        }

        [Fact]
        public void Deve_validar_request_completo_com_sucesso()
        {
            var request = CriarRequestValido();

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
