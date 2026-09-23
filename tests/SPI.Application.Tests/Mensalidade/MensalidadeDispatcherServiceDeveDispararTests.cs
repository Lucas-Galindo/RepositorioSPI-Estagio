using SPI.Infrastructure.BackgroundServices;
using Xunit;

namespace SPI.Application.Tests.Mensalidade
{
    // specs/039 (US1): condicao de disparo do job -- so no ultimo dia do mes,
    // 23h ou depois (FR-001). Metodo estatico puro, testavel sem mockar
    // DateTime.Now nem instanciar o BackgroundService (research.md R2/R7).
    public class MensalidadeDispatcherServiceDeveDispararTests
    {
        [Theory]
        [InlineData(2026, 9, 15, 10, 0, false)] // dia comum, qualquer hora
        [InlineData(2026, 9, 30, 22, 59, false)] // ultimo dia, um minuto antes da janela
        [InlineData(2026, 9, 30, 23, 0, true)] // ultimo dia, inicio exato da janela
        [InlineData(2026, 9, 30, 23, 59, true)] // ultimo dia, fim da janela
        [InlineData(2026, 10, 1, 0, 0, false)] // dia seguinte, ja nao e mais o ultimo dia
        [InlineData(2026, 9, 1, 23, 30, false)] // primeiro dia do mes, mesmo as 23h
        // Fevereiro bissexto (2028) tem 29 dias.
        [InlineData(2028, 2, 28, 23, 0, false)]
        [InlineData(2028, 2, 29, 23, 0, true)]
        // Fevereiro nao-bissexto (2026) tem 28 dias.
        [InlineData(2026, 2, 28, 23, 0, true)]
        // Mes de 30 dias.
        [InlineData(2026, 4, 30, 23, 0, true)]
        [InlineData(2026, 4, 29, 23, 0, false)]
        // Mes de 31 dias.
        [InlineData(2026, 1, 31, 23, 0, true)]
        [InlineData(2026, 1, 30, 23, 0, false)]
        public void DeveDispararNesteMomento_retorna_o_esperado(int ano, int mes, int dia, int hora, int minuto, bool esperado)
        {
            var agora = new DateTime(ano, mes, dia, hora, minuto, 0);

            var resultado = MensalidadeDispatcherService.DeveDispararNesteMomento(agora);

            Assert.Equal(esperado, resultado);
        }
    }
}
