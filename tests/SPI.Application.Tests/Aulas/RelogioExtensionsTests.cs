using SPI.Application.Common;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // Regra do usuario (2026-11-04): o horario do sistema e o do lugar onde ele esta instalado, nao um
    // fuso fixo. O fuso sai do proprio TimeProvider (LocalTimeZone), entao o mesmo instante vira horas
    // diferentes em Sao Paulo (UTC-3), Manaus (UTC-4) ou Buenos Aires (UTC-3).
    public class RelogioExtensionsTests
    {
        private sealed class RelogioEmFuso : TimeProvider
        {
            private readonly DateTimeOffset _utc;
            private readonly TimeZoneInfo _fuso;

            public RelogioEmFuso(DateTimeOffset utc, int horasDeDiferenca)
            {
                _utc = utc;
                _fuso = TimeZoneInfo.CreateCustomTimeZone("teste", TimeSpan.FromHours(horasDeDiferenca), "teste", "teste");
            }

            public override TimeZoneInfo LocalTimeZone => _fuso;
            public override DateTimeOffset GetUtcNow() => _utc;
        }

        private static readonly DateTimeOffset Instante = new(2026, 11, 10, 19, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData(-3, 16)] // Sao Paulo, Bahia, Argentina
        [InlineData(-4, 15)] // Amazonas
        [InlineData(-5, 14)] // Acre
        public void AgoraLocal_segue_o_fuso_do_relogio(int horasDeDiferenca, int horaEsperada)
        {
            var relogio = new RelogioEmFuso(Instante, horasDeDiferenca);

            Assert.Equal(new DateTime(2026, 11, 10, horaEsperada, 0, 0), relogio.AgoraLocal());
        }

        [Fact]
        public void ParaInstanteUtc_usa_o_mesmo_fuso_do_relogio()
        {
            var fimDaAula = new DateTime(2026, 11, 10, 16, 0, 0);

            Assert.Equal(new DateTimeOffset(2026, 11, 10, 19, 0, 0, TimeSpan.Zero), new RelogioEmFuso(Instante, -3).ParaInstanteUtc(fimDaAula));
            Assert.Equal(new DateTimeOffset(2026, 11, 10, 20, 0, 0, TimeSpan.Zero), new RelogioEmFuso(Instante, -4).ParaInstanteUtc(fimDaAula));
        }

        [Fact]
        public void O_relogio_real_usa_o_fuso_da_maquina()
        {
            Assert.Equal(TimeZoneInfo.Local, TimeProvider.System.LocalTimeZone);
        }
    }
}
