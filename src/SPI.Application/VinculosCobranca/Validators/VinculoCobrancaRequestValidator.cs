using FluentValidation;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Domain.Enums;

namespace SPI.Application.VinculosCobranca.Validators
{
    public class VinculoCobrancaRequestValidator : AbstractValidator<VinculoCobrancaRequest>
    {
        // Limite do tipo decimal(10,2) da coluna vinculo_cobranca.valor.
        private const decimal ValorMaximo = 99999999.99m;

        public VinculoCobrancaRequestValidator()
        {
            RuleFor(x => x.Modalidade)
                .IsInEnum().WithMessage("Modalidade deve ser Avulsa, Mensalidade ou Pacote.");

            RuleFor(x => x.Valor)
                .GreaterThan(0).WithMessage("O Valor deve ser maior que zero.")
                .LessThanOrEqualTo(ValorMaximo).WithMessage("O Valor excede o limite permitido.");

            // specs/056: Mensalidade e Pacote sao do aluno (valem em qualquer aula);
            // so a Avulsa e por contexto e pode ter turma.
            RuleFor(x => x.TurmaId)
                .Null()
                .When(x => x.Modalidade is ModalidadeCobranca.Mensalidade or ModalidadeCobranca.Pacote)
                .WithMessage("Mensalidade e Pacote valem para todas as aulas do aluno e nao tem turma.");

            When(x => x.AulasIncluidas.HasValue, () =>
            {
                RuleFor(x => x.AulasIncluidas)
                    .Must((request, _) => request.Modalidade == ModalidadeCobranca.Mensalidade)
                    .WithMessage("Aulas incluidas so se aplica a modalidade Mensalidade.");

                RuleFor(x => x.AulasIncluidas)
                    .GreaterThanOrEqualTo(1).WithMessage("Aulas incluidas deve ser no minimo 1.");
            });

            When(x => x.SaldoAulas.HasValue, () =>
            {
                RuleFor(x => x.SaldoAulas)
                    .Must((request, _) => request.Modalidade == ModalidadeCobranca.Pacote)
                    .WithMessage("Saldo de aulas so se aplica a modalidade Pacote.");

                RuleFor(x => x.SaldoAulas)
                    .GreaterThanOrEqualTo(0).WithMessage("Saldo de aulas nao pode ser negativo.");
            });

            // specs/052
            When(x => x.TotalAulasPacote.HasValue, () =>
            {
                RuleFor(x => x.TotalAulasPacote)
                    .Must((request, _) => request.Modalidade == ModalidadeCobranca.Pacote)
                    .WithMessage("Total de aulas do pacote so se aplica a modalidade Pacote.");

                RuleFor(x => x.TotalAulasPacote)
                    .GreaterThanOrEqualTo(1).WithMessage("Total de aulas do pacote deve ser no minimo 1.");
            });
        }
    }
}
