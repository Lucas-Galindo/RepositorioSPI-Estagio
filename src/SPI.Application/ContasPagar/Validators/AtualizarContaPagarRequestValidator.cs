using FluentValidation;
using SPI.Application.ContasPagar.Dtos;

namespace SPI.Application.ContasPagar.Validators
{
    public class AtualizarContaPagarRequestValidator : AbstractValidator<AtualizarContaPagarRequest>
    {
        public AtualizarContaPagarRequestValidator()
        {
            RuleFor(x => x.Descricao)
                .NotEmpty()
                .MaximumLength(255)
                .When(x => x.Descricao is not null)
                .WithMessage("A Descricao, quando informada, nao pode ser vazia.");

            RuleFor(x => x.CategoriaDespesaId)
                .GreaterThan(0)
                .When(x => x.CategoriaDespesaId.HasValue)
                .WithMessage("CategoriaDespesaId, quando informado, deve ser maior que zero.");

            RuleFor(x => x.FormaPagamentoId)
                .GreaterThan(0)
                .When(x => x.FormaPagamentoId.HasValue)
                .WithMessage("FormaPagamentoId, quando informado, deve ser maior que zero.");

            RuleFor(x => x.Valor)
                .GreaterThan(0)
                .When(x => x.Valor.HasValue)
                .WithMessage("O Valor, quando informado, deve ser maior que zero.");
        }
    }
}
