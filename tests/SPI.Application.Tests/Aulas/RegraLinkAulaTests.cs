using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/067: uma aula aceita link se estiver ativa, nao cancelada e ainda nao terminou.
    public class RegraLinkAulaTests
    {
        // Aula de 10/11/2026, das 15:00 as 16:00.
        private static Aula Aula(string status = "Agendada", bool ativo = true) => new()
        {
            Id = 1, DataInicio = new DateOnly(2026, 11, 10), HoraInicio = new TimeOnly(15, 0), HoraFim = new TimeOnly(16, 0),
            Status = status, Ativo = ativo,
        };

        [Theory]
        [InlineData(2026, 11, 9, 10, 0, true)]    // aula futura
        [InlineData(2026, 11, 10, 14, 0, true)]   // antes do inicio
        [InlineData(2026, 11, 10, 15, 30, true)]  // em andamento
        [InlineData(2026, 11, 10, 15, 59, true)]  // um minuto antes do fim
        [InlineData(2026, 11, 10, 16, 0, false)]  // instante exato do fim
        [InlineData(2026, 11, 10, 18, 0, false)]  // ja terminou
        public void Aceita_ate_o_fim_da_aula(int ano, int mes, int dia, int hora, int minuto, bool esperado)
        {
            Assert.Equal(esperado, RegraLinkAula.AceitaLink(Aula(), new DateTime(ano, mes, dia, hora, minuto, 0)));
        }

        [Theory]
        [InlineData("Cancelada", true)]
        [InlineData("Agendada", false)] // excluida
        public void Cancelada_ou_excluida_nao_aceita(string status, bool ativo)
        {
            Assert.False(RegraLinkAula.AceitaLink(Aula(status, ativo), new DateTime(2026, 11, 9, 10, 0, 0)));
        }

        [Fact]
        public void Aula_realizada_ainda_em_andamento_continua_aceitando()
        {
            Assert.True(RegraLinkAula.AceitaLink(Aula("Realizada"), new DateTime(2026, 11, 10, 15, 30, 0)));
            Assert.False(RegraLinkAula.AceitaLink(Aula("Realizada"), new DateTime(2026, 11, 10, 16, 30, 0)));
        }
    }
}
