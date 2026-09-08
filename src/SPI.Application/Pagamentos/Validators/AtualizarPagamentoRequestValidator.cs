using FluentValidation;
using SPI.Application.Pagamentos.Dtos;

namespace SPI.Application.Pagamentos.Validators
{
    public class AtualizarPagamentoRequestValidator : AbstractValidator<AtualizarPagamentoRequest>
    {
        public AtualizarPagamentoRequestValidator()
        {
            RuleFor(x => x.CategoriaReceitaId)
                .GreaterThan(0)
                .When(x => x.CategoriaReceitaId.HasValue)
                .WithMessage("CategoriaReceitaId, quando informado, deve ser maior que zero.");

            RuleFor(x => x.FormaPagamentoId)
                .GreaterThan(0)
                .When(x => x.FormaPagamentoId.HasValue)
                .WithMessage("FormaPagamentoId, quando informado, deve ser maior que zero.");

            RuleFor(x => x.ValorFinal)
                .GreaterThan(0)
                .When(x => x.ValorFinal.HasValue)
                .WithMessage("O Valor final, quando informado, deve ser maior que zero.");
        }
    }
}
