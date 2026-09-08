using FluentValidation;
using SPI.Application.Alunos.Dtos;
using SPI.Application.Common;

namespace SPI.Application.Alunos.Validators
{
    public class AtualizarAlunoRequestValidator : AbstractValidator<AtualizarAlunoRequest>
    {
        public AtualizarAlunoRequestValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O campo Nome e obrigatorio.")
                .MaximumLength(150);

            When(x => !string.IsNullOrWhiteSpace(x.Cpf), () =>
            {
                RuleFor(x => x.Cpf!).CpfValido();
            });

            RuleFor(x => x.ValorAula)
                .GreaterThanOrEqualTo(0).WithMessage("O Valor da aula nao pode ser negativo.");

            When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
            {
                RuleFor(x => x.Email!)
                    .EmailAddress().WithMessage("O campo Email deve conter um e-mail valido.");
            });

            When(x => x.EhMenorDeIdade, () =>
            {
                RuleFor(x => x.TelefoneResponsavel)
                    .NotEmpty().WithMessage("Telefone do Responsavel e obrigatorio para aluno menor de idade.");

                RuleFor(x => x.EmailResponsavel)
                    .NotEmpty().WithMessage("Email do Responsavel e obrigatorio para aluno menor de idade.");
            });
        }
    }
}
