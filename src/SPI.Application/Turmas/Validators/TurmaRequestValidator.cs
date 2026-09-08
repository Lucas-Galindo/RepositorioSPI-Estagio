using FluentValidation;
using SPI.Application.Turmas.Dtos;

namespace SPI.Application.Turmas.Validators
{
    public class TurmaRequestValidator : AbstractValidator<TurmaRequest>
    {
        public TurmaRequestValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O campo Nome e obrigatorio.")
                .MaximumLength(100);
        }
    }
}
