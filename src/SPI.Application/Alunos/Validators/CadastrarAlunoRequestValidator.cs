using FluentValidation;
using SPI.Application.Alunos.Dtos;
using SPI.Application.Common;

namespace SPI.Application.Alunos.Validators
{
    public class CadastrarAlunoRequestValidator : AbstractValidator<CadastrarAlunoRequest>
    {
        public CadastrarAlunoRequestValidator()
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

            When(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Senha), () =>
            {
                RuleFor(x => x.Email!)
                    .NotEmpty().WithMessage("O campo Email e obrigatorio quando a Senha e informada.")
                    .EmailAddress().WithMessage("O campo Email deve conter um e-mail valido.");

                RuleFor(x => x.Senha!).SenhaForte();
            });

            // Estoria 2, fluxo alternativo: aluno menor de idade exige
            // Telefone e Email do Responsavel (schema nao tem data de
            // nascimento; a professora informa a condicao no cadastro).
            When(x => x.EhMenorDeIdade, () =>
            {
                RuleFor(x => x.TelefoneResponsavel)
                    .NotEmpty().WithMessage("Telefone do Responsavel e obrigatorio para aluno menor de idade.");

                RuleFor(x => x.EmailResponsavel)
                    .NotEmpty().WithMessage("Email do Responsavel e obrigatorio para aluno menor de idade.")
                    .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.EmailResponsavel))
                        .WithMessage("O campo Email do Responsavel deve conter um e-mail valido.");
            });
        }
    }
}
