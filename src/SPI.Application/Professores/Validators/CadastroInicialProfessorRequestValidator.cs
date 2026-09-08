using FluentValidation;
using SPI.Application.Common;
using SPI.Application.Professores.Dtos;

namespace SPI.Application.Professores.Validators
{
    public class CadastroInicialProfessorRequestValidator : AbstractValidator<CadastroInicialProfessorRequest>
    {
        public CadastroInicialProfessorRequestValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O campo Nome e obrigatorio.")
                .MaximumLength(150);

            RuleFor(x => x.Cpf).CpfValido();

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O campo Email e obrigatorio.")
                .EmailAddress().WithMessage("O campo Email deve conter um e-mail valido.");

            RuleFor(x => x.Senha).SenhaForte();

            RuleFor(x => x.Telefone).MaximumLength(20);
        }
    }
}
