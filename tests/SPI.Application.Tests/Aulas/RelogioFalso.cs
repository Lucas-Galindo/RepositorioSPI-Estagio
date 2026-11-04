namespace SPI.Application.Tests.Aulas
{
    // specs/064: TimeProvider de teste com o instante informado em HORA LOCAL de um fuso fixo UTC-3. O
    // fuso do falso nao depende da maquina de quem roda os testes; o sistema real usa o fuso local da maquina.
    // O padrao dos fakes de AulaService e 2100-01-01 12:00: de proposito, toda aula dos testes
    // antigos "ja comecou", para eles nao dependerem do horario. O bloqueio da chamada e testado
    // em RegraChamadaTests e AulaServiceChamadaTests, com um relogio proprio.
    public sealed class RelogioFalso : TimeProvider
    {
        private static readonly TimeZoneInfo FusoDoFalso =
            TimeZoneInfo.CreateCustomTimeZone("UTC-3 (teste)", TimeSpan.FromHours(-3), "UTC-3 (teste)", "UTC-3 (teste)");

        private DateTime _agoraLocal;

        public RelogioFalso(DateTime agoraLocal) => _agoraLocal = agoraLocal;

        public static RelogioFalso Depois() => new(new DateTime(2100, 1, 1, 12, 0, 0));

        public void Definir(DateTime agoraLocal) => _agoraLocal = agoraLocal;

        public override TimeZoneInfo LocalTimeZone => FusoDoFalso;

        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(_agoraLocal.AddHours(3), DateTimeKind.Utc));
    }
}
