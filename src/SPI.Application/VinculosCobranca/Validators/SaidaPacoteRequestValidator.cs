using FluentValidation;
using SPI.Application.VinculosCobranca.Dtos;

namespace SPI.Application.VinculosCobranca.Validators
{
    // specs/078: so o formato. As regras que dependem do banco e do relogio (matricula, pacote ativo, vencimento de hoje
    // em diante, consumo igual ao esperado) ficam no SaidaPacoteService.
    public class SaidaPacoteRequestValidator : AbstractValidator<SaidaPacoteRequest>
    {
        // Teto de sanidade (cabe com folga no decimal(10,2) das colunas de valor).
        private const decimal ValorMaximo = 9999999.99m;

        public SaidaPacoteRequestValidator()
        {
            RuleFor(x => x.ValorPago)
                .GreaterThanOrEqualTo(0).WithMessage("O valor pago nao pode ser negativo.")
                .LessThanOrEqualTo(ValorMaximo).WithMessage("O valor pago excede o limite permitido.")
                .Must(valor => decimal.Round(valor, 2) == valor).WithMessage("O valor pago deve ter no maximo 2 casas decimais.");
        }
    }

    public class ConfirmarSaidaPacoteRequestValidator : AbstractValidator<ConfirmarSaidaPacoteRequest>
    {
        public ConfirmarSaidaPacoteRequestValidator()
        {
            Include(new SaidaPacoteRequestValidator());

            RuleFor(x => x.ConsumidasEsperadas)
                .GreaterThanOrEqualTo(0).WithMessage("As aulas consumidas esperadas nao podem ser negativas.");

            RuleFor(x => x.ValorDevidoEsperado)
                .GreaterThanOrEqualTo(0).WithMessage("O valor devido esperado nao pode ser negativo.");

            RuleFor(x => x.AulasQueSairaoEsperadas)
                .GreaterThanOrEqualTo(0).WithMessage("As aulas esperadas nao podem ser negativas.");
        }
    }
}
