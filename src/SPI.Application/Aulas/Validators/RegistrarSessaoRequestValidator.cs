using FluentValidation;
using SPI.Application.Aulas.Dtos;

namespace SPI.Application.Aulas.Validators
{
    public class RegistrarSessaoRequestValidator : AbstractValidator<RegistrarSessaoRequest>
    {
        public RegistrarSessaoRequestValidator()
        {
            RuleFor(x => x.Presencas)
                .NotEmpty().WithMessage("Informe a presenca de pelo menos um aluno.");
        }
    }
}
