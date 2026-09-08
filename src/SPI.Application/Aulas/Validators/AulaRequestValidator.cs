using FluentValidation;
using SPI.Application.Aulas.Dtos;

namespace SPI.Application.Aulas.Validators
{
    public class AulaRequestValidator : AbstractValidator<AulaRequest>
    {
        public AulaRequestValidator()
        {
            RuleFor(x => x.MateriaId)
                .GreaterThan(0).WithMessage("O campo MateriaId e obrigatorio.");

            RuleFor(x => x.HoraFim)
                .GreaterThan(x => x.HoraInicio).WithMessage("A Hora de Fim deve ser posterior a Hora de Inicio.");

            // Aula individual (sem Turma) precisa dizer para qual aluno e; aula
            // de turma nao aceita AlunoId avulso, ja que todos os alunos ativos
            // da turma sao vinculados automaticamente.
            When(x => x.TurmaId is null, () =>
            {
                RuleFor(x => x.AlunoId)
                    .NotNull().WithMessage("AlunoId e obrigatorio quando a aula nao esta vinculada a uma Turma.");
            }).Otherwise(() =>
            {
                RuleFor(x => x.AlunoId)
                    .Null().WithMessage("AlunoId nao deve ser informado quando a aula esta vinculada a uma Turma.");
            });
        }
    }
}
