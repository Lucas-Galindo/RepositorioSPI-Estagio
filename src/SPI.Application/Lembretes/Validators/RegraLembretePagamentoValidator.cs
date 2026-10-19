using FluentValidation;
using SPI.Application.Lembretes.Dtos;

namespace SPI.Application.Lembretes.Validators
{
    // specs/060: dia da semana na convencao de DayOfWeek (0 = domingo).
    public class RegraLembretePagamentoValidator : AbstractValidator<RegraLembretePagamentoDto>
    {
        public RegraLembretePagamentoValidator()
        {
            RuleFor(x => x.DiaSemana)
                .InclusiveBetween(0, 6).WithMessage("O dia da semana deve ser de 0 (domingo) a 6 (sabado).");
        }
    }
}
