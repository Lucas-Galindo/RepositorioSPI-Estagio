using FluentValidation;
using SPI.Application.Aulas.Dtos;

namespace SPI.Application.Aulas.Validators
{
    // specs/067 (FR-003): o servidor decide o que e um link valido -- a tela so repassa o texto.
    // So http e https: nada de javascript:, data:, ftp: etc. (o link vai clicavel na tela e no e-mail).
    public class DefinirLinkAulaRequestValidator : AbstractValidator<DefinirLinkAulaRequest>
    {
        public const int TamanhoMaximo = 500;

        public DefinirLinkAulaRequestValidator()
        {
            RuleFor(x => x.Link)
                .NotEmpty().WithMessage("O link da aula e obrigatorio.")
                .MaximumLength(TamanhoMaximo).WithMessage($"O link da aula deve ter no maximo {TamanhoMaximo} caracteres.")
                .Must(link => !link.Any(char.IsWhiteSpace)).WithMessage("O link da aula nao pode conter espacos.")
                .Must(EhEnderecoHttp).WithMessage("O link da aula deve ser um endereco http ou https valido.");
        }

        private static bool EhEnderecoHttp(string link) =>
            Uri.TryCreate(link, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host);
    }
}
