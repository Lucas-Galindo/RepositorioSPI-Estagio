using FluentValidation;
using SPI.Application.Aulas.Dtos;

namespace SPI.Application.Aulas.Validators
{
    public class AulaRequestValidator : AbstractValidator<AulaRequest>
    {
        public AulaRequestValidator()
        {
            this.ExigirMateriaETurmaOuAluno();

            RuleFor(x => x.HoraFim)
                .GreaterThan(x => x.HoraInicio).WithMessage("A Hora de Fim deve ser posterior a Hora de Inicio.");
        }
    }
}
