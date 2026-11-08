using FluentValidation;
using SPI.Application.Aulas.Dtos;

namespace SPI.Application.Aulas.Validators
{
    public static class ReferenciasDaAulaRules
    {
        // Regras que a aula unica e a serie dividem (uma so fonte, para nao divergirem): materia obrigatoria; aula
        // individual (sem Turma) precisa dizer para qual aluno e; aula de turma nao aceita AlunoId avulso, ja que todos
        // os alunos ativos da turma sao vinculados automaticamente.
        public static void ExigirMateriaETurmaOuAluno<T>(this AbstractValidator<T> validator) where T : IReferenciasDaAula
        {
            validator.RuleFor(x => x.MateriaId)
                .GreaterThan(0).WithMessage("O campo MateriaId e obrigatorio.");

            validator.When(x => x.TurmaId is null, () =>
            {
                validator.RuleFor(x => x.AlunoId)
                    .NotNull().WithMessage("AlunoId e obrigatorio quando a aula nao esta vinculada a uma Turma.");
            }).Otherwise(() =>
            {
                validator.RuleFor(x => x.AlunoId)
                    .Null().WithMessage("AlunoId nao deve ser informado quando a aula esta vinculada a uma Turma.");
            });
        }
    }
}
