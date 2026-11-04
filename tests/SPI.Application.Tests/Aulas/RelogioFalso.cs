namespace SPI.Application.Tests.Aulas
{
    // specs/064: TimeProvider de teste com o instante informado em HORA DE SAO PAULO (UTC-3).
    // O padrao dos fakes de AulaService e 2100-01-01 12:00: de proposito, toda aula dos testes
    // antigos "ja comecou", para eles nao dependerem do horario. O bloqueio da chamada e testado
    // em RegraChamadaTests e AulaServiceChamadaTests, com um relogio proprio.
    public sealed class RelogioFalso : TimeProvider
    {
        private DateTime _agoraSp;

        public RelogioFalso(DateTime agoraEmSaoPaulo) => _agoraSp = agoraEmSaoPaulo;

        public static RelogioFalso Depois() => new(new DateTime(2100, 1, 1, 12, 0, 0));

        public void Definir(DateTime agoraEmSaoPaulo) => _agoraSp = agoraEmSaoPaulo;

        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(_agoraSp.AddHours(3), DateTimeKind.Utc));
    }
}
