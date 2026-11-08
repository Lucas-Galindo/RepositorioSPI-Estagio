using SPI.Application.Aulas.Dtos;

namespace SPI.Application.Aulas.Services
{
    // specs/077: uma data da serie (ainda sem conflito avaliado).
    public record OcorrenciaSerie(DateOnly Data, DayOfWeek DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFim);

    // Excedeu = a serie passaria do limite; nesse caso a geracao para na primeira ocorrencia alem do limite.
    public record ResultadoGeracao(IReadOnlyList<OcorrenciaSerie> Ocorrencias, bool Excedeu);

    // specs/077: calcula as datas de uma serie semanal. Estatica e pura (sem banco, sem relogio e sem DateTime:
    // so DateOnly, entao nenhum fuso desloca o dia) -- testavel sem infraestrutura. E o unico lugar que decide
    // quais datas a serie tem: a previa e a criacao usam o mesmo calculo.
    public static class GeradorSerieAulas
    {
        public const int LimitePadrao = 60;

        public static ResultadoGeracao Gerar(
            DateOnly dataInicial,
            IReadOnlyList<HorarioSerieRequest> horarios,
            int? quantidade,
            DateOnly? dataFinal,
            int limite = LimitePadrao)
        {
            var ocorrencias = new List<OcorrenciaSerie>();
            var porDia = horarios
                .GroupBy(h => h.DiaSemana)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.HoraInicio).ToList());

            if (porDia.Count == 0)
            {
                return new ResultadoGeracao(ocorrencias, false);
            }

            var data = dataInicial;
            while (!dataFinal.HasValue || data <= dataFinal.Value)
            {
                if (porDia.TryGetValue((int)data.DayOfWeek, out var doDia))
                {
                    foreach (var horario in doDia)
                    {
                        if (quantidade.HasValue && ocorrencias.Count >= quantidade.Value)
                        {
                            return new ResultadoGeracao(ocorrencias, false);
                        }

                        ocorrencias.Add(new OcorrenciaSerie(data, data.DayOfWeek, horario.HoraInicio, horario.HoraFim));

                        // Passou do limite: para na hora, sem varrer um periodo enorme.
                        if (ocorrencias.Count > limite)
                        {
                            return new ResultadoGeracao(ocorrencias, true);
                        }
                    }
                }

                if (quantidade.HasValue && ocorrencias.Count >= quantidade.Value)
                {
                    break;
                }

                if (data == DateOnly.MaxValue)
                {
                    break;
                }

                data = data.AddDays(1);
            }

            return new ResultadoGeracao(ocorrencias, false);
        }
    }
}
