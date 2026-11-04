using FluentValidation;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Domain.Enums;

namespace SPI.Application.VinculosCobranca.Validators
{
    public class VinculoCobrancaRequestValidator : AbstractValidator<VinculoCobrancaRequest>
    {
        // Limite do tipo decimal(10,2) da coluna vinculo_cobranca.valor.
        private const decimal ValorMaximo = 99999999.99m;

        // specs/062: de 1 a 24 parcelas por plano (limite contra erro de digitacao).
        private const int MaximoDeParcelas = 24;

        public VinculoCobrancaRequestValidator()
        {
            RuleFor(x => x.Modalidade)
                .IsInEnum().WithMessage("Modalidade deve ser Avulsa, Mensalidade, Pacote, Semestral ou Anual.");

            RuleFor(x => x.Valor)
                .GreaterThan(0).WithMessage("O Valor deve ser maior que zero.")
                .LessThanOrEqualTo(ValorMaximo).WithMessage("O Valor excede o limite permitido.");

            // specs/056 + specs/063: a Mensalidade e do aluno (vale em qualquer aula) e nao tem
            // turma. A Avulsa e por contexto e o Pacote pertence a um grupo -- uma turma de
            // pacote ou, sem turma, as aulas individuais --, entao ambos podem ter turma.
            RuleFor(x => x.TurmaId)
                .Null()
                .When(x => x.Modalidade == ModalidadeCobranca.Mensalidade)
                .WithMessage("A Mensalidade vale para todas as aulas do aluno e nao tem turma.");

            // specs/062: o plano Semestral/Anual tambem vale para todas as aulas do aluno: sem turma.
            RuleFor(x => x.TurmaId)
                .Null()
                .When(x => x.Modalidade.ComPeriodo())
                .WithMessage("A semestralidade e a anuidade valem para todas as aulas do aluno e nao tem turma.");

            // specs/062: Semestral/Anual exigem o inicio e de 1 a 24 parcelas (valor > 0, um vencimento por mes);
            // as outras modalidades nao aceitam inicio nem parcelas. A soma das parcelas pode diferir do Valor
            // total (FR-005): so o servidor valida o que importa, a tela mostra a diferenca como aviso.
            When(x => x.Modalidade.ComPeriodo(), () =>
            {
                RuleFor(x => x.DataInicio)
                    .NotNull().WithMessage("A data de inicio e obrigatoria para semestralidade e anuidade.");

                RuleFor(x => x.Parcelas)
                    .NotEmpty().WithMessage("Informe ao menos uma parcela.")
                    .Must(p => p is null || p.Count <= MaximoDeParcelas).WithMessage($"O plano aceita no maximo {MaximoDeParcelas} parcelas.");

                RuleForEach(x => x.Parcelas).ChildRules(parcela =>
                {
                    parcela.RuleFor(p => p.Valor)
                        .GreaterThan(0).WithMessage("O valor de cada parcela deve ser maior que zero.")
                        .LessThanOrEqualTo(ValorMaximo).WithMessage("O valor de uma parcela excede o limite permitido.");
                });

                RuleFor(x => x.Parcelas)
                    .Must(p => p is null || p.Select(x => (x.Vencimento.Year, x.Vencimento.Month)).Distinct().Count() == p.Count)
                    .WithMessage("As parcelas devem vencer em meses diferentes (no maximo uma por mes).");
            });

            When(x => !x.Modalidade.ComPeriodo(), () =>
            {
                RuleFor(x => x.DataInicio)
                    .Null().WithMessage("A data de inicio so se aplica a semestralidade e anuidade.");

                RuleFor(x => x.Parcelas)
                    .Must(p => p is null || p.Count == 0).WithMessage("Parcelas so se aplicam a semestralidade e anuidade.");
            });

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
