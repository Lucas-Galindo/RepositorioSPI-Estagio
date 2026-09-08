using System.Text.RegularExpressions;
using FluentValidation;

namespace SPI.Application.Common
{
    // Regra compartilhada (Estoria 1): senha com no minimo 8 caracteres e alfanumerica.
    public static class ValidationExtensions
    {
        private static readonly Regex Alfanumerica = new(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);

        public static IRuleBuilderOptions<T, string> SenhaForte<T>(this IRuleBuilder<T, string> ruleBuilder) =>
            ruleBuilder
                .NotEmpty().WithMessage("O campo Senha e obrigatorio.")
                .MinimumLength(8).WithMessage("A senha deve ter no minimo 8 caracteres.")
                .Must(senha => Alfanumerica.IsMatch(senha)).WithMessage("A senha deve ser alfanumerica.");

        public static IRuleBuilderOptions<T, string> CpfValido<T>(this IRuleBuilder<T, string> ruleBuilder) =>
            ruleBuilder
                .NotEmpty().WithMessage("O campo CPF e obrigatorio.")
                .Must(CpfValidator.EhValido).WithMessage("O CPF informado e invalido.");
    }
}
