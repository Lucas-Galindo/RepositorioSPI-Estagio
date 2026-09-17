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

            // Regra assimetrica em relacao ao cadastro: CEP/Rua/Numero so se tornam
            // obrigatorios juntos se pelo menos um deles vier preenchido nesta edicao.
            // Um aluno cadastrado antes desta funcionalidade (sem endereco) continua
            // editavel em outros campos sem ser forcado a preencher endereco agora.
            When(x => x.Cep != null || x.Rua != null || x.Numero != null, () =>
            {
                RuleFor(x => x.Cep)
                    .NotEmpty().WithMessage("O campo CEP e obrigatorio.")
                    .Matches(@"^\d{8}$").WithMessage("O CEP deve conter exatamente 8 digitos numericos.")
                        .When(x => !string.IsNullOrWhiteSpace(x.Cep));

                RuleFor(x => x.Rua)
                    .NotEmpty().WithMessage("O campo Rua e obrigatorio.");

                RuleFor(x => x.Numero)
                    .NotEmpty().WithMessage("O campo Numero e obrigatorio.");
            });

            // Mesma regra assimetrica acima, aplicada ao endereco do responsavel: so
            // obrigatorio quando o aluno e menor de idade, o endereco do responsavel
            // nao e o mesmo do aluno, e pelo menos um dos tres campos veio preenchido.
            When(x => x.EhMenorDeIdade && !x.ResponsavelMesmoEndereco &&
                      (x.ResponsavelCep != null || x.ResponsavelRua != null || x.ResponsavelNumero != null), () =>
            {
                RuleFor(x => x.ResponsavelCep)
                    .NotEmpty().WithMessage("O campo CEP do responsavel e obrigatorio.")
                    .Matches(@"^\d{8}$").WithMessage("O CEP do responsavel deve conter exatamente 8 digitos numericos.")
                        .When(x => !string.IsNullOrWhiteSpace(x.ResponsavelCep));

                RuleFor(x => x.ResponsavelRua)
                    .NotEmpty().WithMessage("O campo Rua do responsavel e obrigatorio.");

                RuleFor(x => x.ResponsavelNumero)
                    .NotEmpty().WithMessage("O campo Numero do responsavel e obrigatorio.");
            });
        }
    }
}
