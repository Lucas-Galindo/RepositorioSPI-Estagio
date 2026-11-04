using SPI.Application.Aulas.Services;
using SPI.Application.Common;
using SPI.Domain.Entities;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/064: a chamada so e liberada quando o horario atual (Sao Paulo) >= data + hora de inicio,
    // comparados juntos. Os 5 casos de borda da spec, o minuto exato e o fuso.
    public class RegraChamadaTests
    {
        // Aula de 10/11/2026, das 15:00 as 16:00.
        private static Aula Aula(string status = "Agendada", bool ativo = true) => new()
        {
            Id = 1, DataInicio = new DateOnly(2026, 11, 10), HoraInicio = new TimeOnly(15, 0), HoraFim = new TimeOnly(16, 0),
            Status = status, Ativo = ativo,
        };

        [Theory]
        [InlineData(2026, 11, 10, 10, 0, false)]  // hoje, antes do inicio
        [InlineData(2026, 11, 10, 14, 59, false)] // um minuto antes
        [InlineData(2026, 11, 10, 15, 0, true)]   // minuto exato do inicio
        [InlineData(2026, 11, 10, 15, 30, true)]  // hoje, ja iniciada e nao terminada (em andamento)
        [InlineData(2026, 11, 10, 18, 0, true)]   // hoje, ja terminada
        [InlineData(2026, 11, 9, 23, 59, false)]  // aula de amanha
        [InlineData(2026, 11, 11, 8, 0, true)]    // data passada
        public void Liberada_so_quando_o_horario_atual_chega_ao_inicio(int ano, int mes, int dia, int hora, int minuto, bool esperado)
        {
            Assert.Equal(esperado, RegraChamada.Liberada(Aula(), new DateTime(ano, mes, dia, hora, minuto, 0)));
        }

        [Fact]
        public void Um_segundo_antes_do_inicio_ainda_esta_bloqueada()
        {
            Assert.False(RegraChamada.Liberada(Aula(), new DateTime(2026, 11, 10, 14, 59, 59)));
        }

        [Fact]
        public void Aula_com_o_inicio_movido_para_o_futuro_volta_a_ficar_bloqueada()
        {
            var aula = Aula();
            var agora = new DateTime(2026, 11, 10, 15, 30, 0);
            Assert.True(RegraChamada.Liberada(aula, agora));

            aula.HoraInicio = new TimeOnly(18, 0);

            Assert.False(RegraChamada.Liberada(aula, agora));
        }

        [Theory]
        [InlineData("Realizada", true)]
        [InlineData("Cancelada", true)]
        [InlineData("Agendada", false)] // excluida
        public void Aula_que_nao_esta_ativa_e_agendada_nunca_esta_liberada(string status, bool ativo)
        {
            Assert.False(RegraChamada.Liberada(Aula(status, ativo), new DateTime(2026, 11, 11, 8, 0, 0)));
        }

        [Fact]
        public void SegundosParaLiberar_conta_ate_o_inicio_e_some_quando_liberada()
        {
            var aula = Aula();

            Assert.Equal(3600, RegraChamada.SegundosParaLiberar(aula, new DateTime(2026, 11, 10, 14, 0, 0)));
            Assert.Equal(1, RegraChamada.SegundosParaLiberar(aula, new DateTime(2026, 11, 10, 14, 59, 59)));
            Assert.Null(RegraChamada.SegundosParaLiberar(aula, new DateTime(2026, 11, 10, 15, 0, 0)));
            Assert.Null(RegraChamada.SegundosParaLiberar(Aula("Cancelada"), new DateTime(2026, 11, 10, 14, 0, 0)));
            Assert.Null(RegraChamada.SegundosParaLiberar(Aula(ativo: false), new DateTime(2026, 11, 10, 14, 0, 0)));
        }

        [Fact]
        public void Mensagem_de_bloqueio_traz_a_hora_e_a_data_de_inicio()
        {
            Assert.Equal("A chamada ficara disponivel a partir das 15:00 de 10/11/2026.", RegraChamada.MensagemBloqueio(Aula()));
        }

        // --- fuso: a hora de parede e a de Sao Paulo, nao a do servidor ---

        private sealed class RelogioUtc : TimeProvider
        {
            private readonly DateTimeOffset _utc;
            public RelogioUtc(DateTimeOffset utc) => _utc = utc;
            public override DateTimeOffset GetUtcNow() => _utc;
        }

        [Fact]
        public void AgoraEmSaoPaulo_converte_do_utc_para_o_fuso_de_sao_paulo()
        {
            var relogio = new RelogioUtc(new DateTimeOffset(2026, 11, 10, 18, 0, 0, TimeSpan.Zero));

            Assert.Equal(new DateTime(2026, 11, 10, 15, 0, 0), relogio.AgoraEmSaoPaulo());
        }

        [Fact]
        public void AgoraEmSaoPaulo_acompanha_a_virada_do_dia()
        {
            var relogio = new RelogioUtc(new DateTimeOffset(2026, 11, 11, 2, 30, 0, TimeSpan.Zero));

            Assert.Equal(new DateTime(2026, 11, 10, 23, 30, 0), relogio.AgoraEmSaoPaulo());
        }

        [Fact]
        public void RelogioFalso_devolve_exatamente_o_instante_informado_em_sao_paulo()
        {
            Assert.Equal(new DateTime(2026, 11, 10, 15, 0, 0), new RelogioFalso(new DateTime(2026, 11, 10, 15, 0, 0)).AgoraEmSaoPaulo());
        }
    }
}
