namespace SPI.Application.Common
{
    // specs/064: o sistema decide "a aula ja comecou/terminou" no fuso de Sao Paulo, nao no do
    // servidor (que pode estar em outro). A aula guarda data e hora como "hora de parede", sem
    // fuso; por isso o agora tambem sai como hora de parede de Sao Paulo (Kind nao especificado).
    public static class RelogioExtensions
    {
        private static readonly Lazy<TimeZoneInfo> FusoSaoPaulo = new(ResolverFuso);

        public static DateTime AgoraEmSaoPaulo(this TimeProvider relogio) =>
            TimeZoneInfo.ConvertTimeFromUtc(relogio.GetUtcNow().UtcDateTime, FusoSaoPaulo.Value);

        // Tenta o id IANA (Linux e Windows com ICU) e depois o id do Windows. Se nenhum resolver
        // (ex.: conteiner minimo sem tzdata) falha alto: decidir "ja comecou" com o fuso errado
        // liberaria a chamada na hora errada.
        private static TimeZoneInfo ResolverFuso()
        {
            foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
            }

            throw new InvalidOperationException("Nao foi possivel resolver o fuso horario America/Sao_Paulo neste servidor (instale o tzdata).");
        }
    }
}
