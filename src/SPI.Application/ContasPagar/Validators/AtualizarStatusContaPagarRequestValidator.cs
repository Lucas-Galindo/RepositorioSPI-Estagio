using FluentValidation;
using SPI.Application.ContasPagar.Dtos;

namespace SPI.Application.ContasPagar.Validators
{
    public class AtualizarStatusContaPagarRequestValidator : AbstractValidator<AtualizarStatusContaPagarRequest>
    {
        private static readonly string[] StatusValidos = { "Pendente", "Pago", "Atrasado", "Cancelado" };

        public AtualizarStatusContaPagarRequestValidator()
        {
            RuleFor(x => x.Status)
                .Must(s => StatusValidos.Contains(s))
                .WithMessage("Status deve ser 'Pendente', 'Pago', 'Atrasado' ou 'Cancelado'.");
        }
    }
}
