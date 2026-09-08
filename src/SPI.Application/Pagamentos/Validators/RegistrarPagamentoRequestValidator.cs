using FluentValidation;
using SPI.Application.Pagamentos.Dtos;

namespace SPI.Application.Pagamentos.Validators
{
    public class RegistrarPagamentoRequestValidator : AbstractValidator<RegistrarPagamentoRequest>
    {
        private static readonly string[] StatusValidos = { "Pendente", "Pago" };

        public RegistrarPagamentoRequestValidator()
        {
            RuleFor(x => x.AlunoId).GreaterThan(0).WithMessage("O campo AlunoId e obrigatorio.");

            RuleFor(x => x.FormaPagamentoId).GreaterThan(0).WithMessage("O campo FormaPagamentoId e obrigatorio.");

            RuleFor(x => x.ValorFinal).GreaterThan(0).WithMessage("O Valor final deve ser maior que zero.");

            RuleFor(x => x.CategoriaReceitaId)
                .GreaterThan(0)
                .When(x => x.CategoriaReceitaId.HasValue)
                .WithMessage("CategoriaReceitaId, quando informado, deve ser maior que zero.");

            RuleFor(x => x.Status)
                .Must(s => StatusValidos.Contains(s))
                .WithMessage("Status deve ser 'Pendente' ou 'Pago'.");
        }
    }
}
