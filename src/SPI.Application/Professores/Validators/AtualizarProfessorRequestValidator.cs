using FluentValidation;
using SPI.Application.Professores.Dtos;

namespace SPI.Application.Professores.Validators
{
    public class AtualizarProfessorRequestValidator : AbstractValidator<AtualizarProfessorRequest>
    {
        public AtualizarProfessorRequestValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O campo Nome e obrigatorio.")
                .MaximumLength(150);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O campo Email e obrigatorio.")
                .EmailAddress().WithMessage("O campo Email deve conter um e-mail valido.");

            RuleFor(x => x.Telefone).MaximumLength(20);
        }
    }
}
