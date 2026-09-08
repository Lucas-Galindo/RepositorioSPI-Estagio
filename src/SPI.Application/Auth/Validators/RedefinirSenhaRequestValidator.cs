using FluentValidation;
using SPI.Application.Auth.Dtos;
using SPI.Application.Common;

namespace SPI.Application.Auth.Validators
{
    public class RedefinirSenhaRequestValidator : AbstractValidator<RedefinirSenhaRequest>
    {
        public RedefinirSenhaRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("O campo Token e obrigatorio.");

            RuleFor(x => x.NovaSenha).SenhaForte();
        }
    }
}
