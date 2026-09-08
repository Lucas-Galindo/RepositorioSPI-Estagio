using FluentValidation;
using SPI.Application.Auth.Dtos;

namespace SPI.Application.Auth.Validators
{
    public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
    {
        public RefreshRequestValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("O campo RefreshToken e obrigatorio.");
        }
    }
}
