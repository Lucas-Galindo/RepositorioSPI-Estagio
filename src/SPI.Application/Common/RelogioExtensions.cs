namespace SPI.Application.Common
{
    // Regra do usuario (2026-11-04): o horario do sistema e o do lugar onde ele esta instalado (Bahia,
    // Amazonas, Sao Paulo, Argentina...), nao um fuso fixo. O fuso vem do proprio TimeProvider
    // (LocalTimeZone = o da maquina, em producao). A aula guarda data e hora como "hora de parede",
    // sem fuso; por isso o agora tambem sai como hora de parede local (Kind nao especificado).
    // Um lugar so decide o fuso: tudo que compara "agora" com data+hora de aula passa por aqui.
    public static class RelogioExtensions
    {
        public static DateTime AgoraLocal(this TimeProvider relogio) =>
            TimeZoneInfo.ConvertTimeFromUtc(relogio.GetUtcNow().UtcDateTime, relogio.LocalTimeZone);

        // specs/067: converte uma hora de parede local (como data + hora de uma aula) no instante
        // absoluto correspondente, usando o MESMO fuso do relogio. Hora inexistente (salto de horario
        // de verao) avanca uma hora, em vez de falhar.
        public static DateTimeOffset ParaInstanteUtc(this TimeProvider relogio, DateTime horaDeParede)
        {
            var fuso = relogio.LocalTimeZone;
            var hora = DateTime.SpecifyKind(horaDeParede, DateTimeKind.Unspecified);

            if (fuso.IsInvalidTime(hora))
            {
                hora = hora.AddHours(1);
            }

            return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(hora, fuso), TimeSpan.Zero);
        }
    }
}
