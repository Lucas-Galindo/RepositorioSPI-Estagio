using FluentValidation;
using SPI.Application.Auth.Dtos;

namespace SPI.Application.Auth.Validators
{
    public class EsqueciSenhaRequestValidator : AbstractValidator<EsqueciSenhaRequest>
    {
        public EsqueciSenhaRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O campo Email e obrigatorio.")
                .EmailAddress().WithMessage("O campo Email deve conter um e-mail valido.");
        }
    }
}
