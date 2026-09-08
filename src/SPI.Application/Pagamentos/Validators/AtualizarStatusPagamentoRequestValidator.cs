using FluentValidation;
using SPI.Application.Pagamentos.Dtos;

namespace SPI.Application.Pagamentos.Validators
{
    public class AtualizarStatusPagamentoRequestValidator : AbstractValidator<AtualizarStatusPagamentoRequest>
    {
        private static readonly string[] StatusValidos = { "Pendente", "Pago", "Atrasado", "Cancelado" };

        public AtualizarStatusPagamentoRequestValidator()
        {
            RuleFor(x => x.Status)
                .Must(s => StatusValidos.Contains(s))
                .WithMessage("Status deve ser 'Pendente', 'Pago', 'Atrasado' ou 'Cancelado'.");
        }
    }
}
