using FluentValidation;
using SPI.Application.Turmas.Dtos;
using SPI.Domain.Enums;

namespace SPI.Application.Turmas.Validators
{
    public class TurmaRequestValidator : AbstractValidator<TurmaRequest>
    {
        // Conjunto de regras so do cadastro (specs/063): a turma NOVA de pacote exige as duas
        // datas. A edicao usa so as regras sem conjunto -- uma turma de pacote migrada pode
        // ficar sem datas ate a professora preenche-las.
        public const string RegrasDeCadastro = "Cadastro";

        public TurmaRequestValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O campo Nome e obrigatorio.")
                .MaximumLength(100);

            RuleFor(x => x.Tipo)
                .IsInEnum().WithMessage("Tipo de turma invalido.");

            // Turma de mensalidade nao tem datas. Com o tipo ausente a decisao fica com o
            // servico, que conhece o tipo atual da turma (no PUT).
            RuleFor(x => x.DataInicio)
                .Null().When(x => x.Tipo == TipoTurma.Mensalidade)
                .WithMessage("Turma de mensalidade nao tem data de inicio.");

            RuleFor(x => x.DataFim)
                .Null().When(x => x.Tipo == TipoTurma.Mensalidade)
                .WithMessage("Turma de mensalidade nao tem data de fim.");

            RuleFor(x => x.DataFim)
                .GreaterThanOrEqualTo(x => x.DataInicio!.Value)
                .When(x => x.DataInicio.HasValue && x.DataFim.HasValue)
                .WithMessage("A data de fim nao pode ser anterior a data de inicio.");

            RuleSet(RegrasDeCadastro, () =>
            {
                RuleFor(x => x.DataInicio)
                    .NotNull().When(x => x.Tipo == TipoTurma.Pacote)
                    .WithMessage("A turma de pacote precisa de data de inicio.");

                RuleFor(x => x.DataFim)
                    .NotNull().When(x => x.Tipo == TipoTurma.Pacote)
                    .WithMessage("A turma de pacote precisa de data de fim.");
            });
        }
    }
}
