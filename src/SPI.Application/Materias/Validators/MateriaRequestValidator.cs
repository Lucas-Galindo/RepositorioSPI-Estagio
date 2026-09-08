using FluentValidation;
using SPI.Application.Materias.Dtos;

namespace SPI.Application.Materias.Validators
{
    public class MateriaRequestValidator : AbstractValidator<MateriaRequest>
    {
        public MateriaRequestValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O campo Nome e obrigatorio.")
                .MaximumLength(100);

            RuleFor(x => x.Nivel).MaximumLength(100);
            RuleFor(x => x.Descricao).MaximumLength(255);
        }
    }
}
