using FluentValidation;
using SPI.Application.ContasPagar.Dtos;

namespace SPI.Application.ContasPagar.Validators
{
    public class RegistrarContaPagarRequestValidator : AbstractValidator<RegistrarContaPagarRequest>
    {
        private static readonly string[] StatusValidos = { "Pendente", "Pago" };

        public RegistrarContaPagarRequestValidator()
        {
            RuleFor(x => x.Descricao).NotEmpty().MaximumLength(255).WithMessage("A Descricao e obrigatoria.");

            RuleFor(x => x.CategoriaDespesaId).GreaterThan(0).WithMessage("O campo CategoriaDespesaId e obrigatorio.");

            RuleFor(x => x.Valor).GreaterThan(0).WithMessage("O Valor deve ser maior que zero.");

            RuleFor(x => x.FormaPagamentoId)
                .GreaterThan(0)
                .When(x => x.FormaPagamentoId.HasValue)
                .WithMessage("FormaPagamentoId, quando informado, deve ser maior que zero.");

            RuleFor(x => x.Status)
                .Must(s => StatusValidos.Contains(s))
                .WithMessage("Status deve ser 'Pendente' ou 'Pago'.");
        }
    }
}
