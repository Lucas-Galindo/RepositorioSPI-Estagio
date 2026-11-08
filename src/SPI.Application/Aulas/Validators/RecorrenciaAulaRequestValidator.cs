using FluentValidation;
using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;

namespace SPI.Application.Aulas.Validators
{
    // specs/077: formato do pedido de serie. As regras que dependem do relogio e do banco (hoje em diante, limite
    // pela data final, referencias existentes e conflitos) ficam no AulaService.
    public class RecorrenciaAulaRequestValidator : AbstractValidator<RecorrenciaAulaRequest>
    {
        public RecorrenciaAulaRequestValidator()
        {
            this.ExigirMateriaETurmaOuAluno();

            RuleFor(x => x.Horarios)
                .NotEmpty().WithMessage("Informe ao menos um dia da semana com horario.");

            RuleForEach(x => x.Horarios).ChildRules(horario =>
            {
                horario.RuleFor(h => h.DiaSemana)
                    .InclusiveBetween(0, 6).WithMessage("O dia da semana deve ser de 0 (domingo) a 6 (sabado).");
                horario.RuleFor(h => h.HoraFim)
                    .GreaterThan(h => h.HoraInicio).WithMessage("A Hora de Fim deve ser posterior a Hora de Inicio.");
            });

            // Dois horarios no mesmo dia da semana nao podem se sobrepor (a serie criaria aulas em conflito entre si).
            RuleFor(x => x.Horarios)
                .Must(SemSobreposicao).WithMessage("Dois horarios no mesmo dia da semana nao podem se sobrepor.")
                .When(x => x.Horarios is { Count: > 0 });

            // Exatamente um termino: numero de aulas OU data final.
            RuleFor(x => x)
                .Must(x => x.QuantidadeAulas.HasValue ^ x.DataFinal.HasValue)
                .WithMessage("Informe o numero de aulas ou a data final (apenas um dos dois).");

            When(x => x.QuantidadeAulas.HasValue, () =>
            {
                RuleFor(x => x.QuantidadeAulas!.Value)
                    .InclusiveBetween(1, GeradorSerieAulas.LimitePadrao)
                    .WithMessage($"O numero de aulas deve ser de 1 a {GeradorSerieAulas.LimitePadrao}.");
            });

            When(x => x.DataFinal.HasValue, () =>
            {
                RuleFor(x => x.DataFinal!.Value)
                    .GreaterThanOrEqualTo(x => x.DataInicial).WithMessage("A data final nao pode ser anterior a data inicial.");
            });
        }

        private static bool SemSobreposicao(List<HorarioSerieRequest> horarios) =>
            horarios
                .GroupBy(h => h.DiaSemana)
                .All(grupo =>
                {
                    var ordenados = grupo.OrderBy(h => h.HoraInicio).ToList();
                    for (var i = 1; i < ordenados.Count; i++)
                    {
                        if (ordenados[i].HoraInicio < ordenados[i - 1].HoraFim)
                        {
                            return false;
                        }
                    }

                    return true;
                });
    }
}
