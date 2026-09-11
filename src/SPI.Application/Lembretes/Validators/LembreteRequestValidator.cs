using FluentValidation;
using SPI.Application.Lembretes.Dtos;

namespace SPI.Application.Lembretes.Validators
{
    public class LembreteRequestValidator : AbstractValidator<LembreteRequest>
    {
        private static readonly string[] CanaisValidos = { "Email" };
        private static readonly string[] DestinatariosValidos = { "Alunos", "Responsaveis", "AlunosEResponsaveis" };

        public LembreteRequestValidator()
        {
            RuleFor(x => x.TurmaId)
                .GreaterThan(0).WithMessage("O campo TurmaId e obrigatorio.");

            RuleFor(x => x.Canal)
                .Must(c => CanaisValidos.Contains(c))
                .WithMessage("Canal deve ser 'Email'. WhatsApp e SMS foram descontinuados.");

            RuleFor(x => x.AntecedenciaHora)
                .GreaterThan(0).WithMessage("A Antecedencia deve ser maior que zero (em horas).");

            RuleFor(x => x.Destinatarios)
                .Must(d => DestinatariosValidos.Contains(d))
                .WithMessage("Destinatarios deve ser 'Alunos', 'Responsaveis' ou 'AlunosEResponsaveis'.");
        }
    }
}
