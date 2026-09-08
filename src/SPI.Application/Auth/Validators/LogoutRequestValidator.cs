using FluentValidation;
using SPI.Application.Auth.Dtos;

namespace SPI.Application.Auth.Validators
{
    public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
    {
        public LogoutRequestValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("O campo RefreshToken e obrigatorio.");
        }
    }
}
