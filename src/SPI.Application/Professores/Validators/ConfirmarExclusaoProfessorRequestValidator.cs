using FluentValidation;
using SPI.Application.Professores.Dtos;

namespace SPI.Application.Professores.Validators
{
    public class ConfirmarExclusaoProfessorRequestValidator : AbstractValidator<ConfirmarExclusaoProfessorRequest>
    {
        public ConfirmarExclusaoProfessorRequestValidator()
        {
            RuleFor(x => x.Codigo)
                .NotEmpty().WithMessage("O campo Codigo e obrigatorio.");
        }
    }
}
