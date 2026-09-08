using FluentValidation;
using SPI.Application.Auth.Dtos;

namespace SPI.Application.Auth.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Login)
                .NotEmpty().WithMessage("O campo Login e obrigatorio.");

            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("O campo Senha e obrigatorio.");
        }
    }
}
