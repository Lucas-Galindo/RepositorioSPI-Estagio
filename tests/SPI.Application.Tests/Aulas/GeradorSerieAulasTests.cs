using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/077: calculo das datas da serie. GeradorSerieAulas e estatico e puro -- testavel sem banco nem relogio.
    public class GeradorSerieAulasTests
    {
        private static HorarioSerieRequest Horario(DayOfWeek dia, int inicio = 14, int fim = 15) =>
            new() { DiaSemana = (int)dia, HoraInicio = new TimeOnly(inicio, 0), HoraFim = new TimeOnly(fim, 0) };

        // 2026-11-09 e uma segunda-feira.
        private static readonly DateOnly Segunda = new(2026, 11, 9);

        [Fact]
        public void Um_dia_por_semana_por_quantidade_gera_as_proximas_semanas()
        {
            var r = GeradorSerieAulas.Gerar(Segunda, new[] { Horario(DayOfWeek.Tuesday) }, 3, null);

            Assert.False(r.Excedeu);
            Assert.Equal(new[] { new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 17), new DateOnly(2026, 11, 24) }, r.Ocorrencias.Select(o => o.Data));
            Assert.All(r.Ocorrencias, o => Assert.Equal(DayOfWeek.Tuesday, o.DiaSemana));
        }

        [Fact]
        public void Dois_dias_por_semana_saem_em_ordem_de_data_com_cada_horario()
        {
            var r = GeradorSerieAulas.Gerar(Segunda, new[] { Horario(DayOfWeek.Thursday, 10, 11), Horario(DayOfWeek.Tuesday, 14, 15) }, 4, null);

            Assert.Equal(
                new[] { (new DateOnly(2026, 11, 10), 14), (new DateOnly(2026, 11, 12), 10), (new DateOnly(2026, 11, 17), 14), (new DateOnly(2026, 11, 19), 10) },
                r.Ocorrencias.Select(o => (o.Data, o.HoraInicio.Hour)));
        }

        [Fact]
        public void Data_inicial_que_nao_cai_nos_dias_comeca_no_proximo_dia_valido()
        {
            // Quarta 11/11, dia escolhido = segunda -> primeira aula na segunda seguinte (16/11).
            var r = GeradorSerieAulas.Gerar(new DateOnly(2026, 11, 11), new[] { Horario(DayOfWeek.Monday) }, 2, null);

            Assert.Equal(new[] { new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 23) }, r.Ocorrencias.Select(o => o.Data));
        }

        [Fact]
        public void Data_inicial_que_ja_cai_num_dos_dias_entra_na_serie()
        {
            var r = GeradorSerieAulas.Gerar(Segunda, new[] { Horario(DayOfWeek.Monday) }, 2, null);

            Assert.Equal(Segunda, r.Ocorrencias[0].Data);
        }

        [Fact]
        public void Termino_por_data_final_inclui_a_propria_data_final()
        {
            // Terca 10/11, 17/11 e 24/11; data final = 24/11 (terca) entra.
            var r = GeradorSerieAulas.Gerar(Segunda, new[] { Horario(DayOfWeek.Tuesday) }, null, new DateOnly(2026, 11, 24));

            Assert.Equal(3, r.Ocorrencias.Count);
            Assert.Equal(new DateOnly(2026, 11, 24), r.Ocorrencias[^1].Data);
        }

        [Fact]
        public void Data_final_antes_da_primeira_ocorrencia_nao_gera_aulas()
        {
            var r = GeradorSerieAulas.Gerar(Segunda, new[] { Horario(DayOfWeek.Friday) }, null, new DateOnly(2026, 11, 12));

            Assert.Empty(r.Ocorrencias);
            Assert.False(r.Excedeu);
        }

        [Fact]
        public void Dois_horarios_no_mesmo_dia_geram_duas_aulas_no_dia_em_ordem_de_inicio()
        {
            var r = GeradorSerieAulas.Gerar(Segunda, new[] { Horario(DayOfWeek.Tuesday, 16, 17), Horario(DayOfWeek.Tuesday, 9, 10) }, 3, null);

            Assert.Equal(new[] { 9, 16, 9 }, r.Ocorrencias.Select(o => o.HoraInicio.Hour));
            Assert.Equal(new[] { new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 17) }, r.Ocorrencias.Select(o => o.Data));
        }

        [Fact]
        public void A_quantidade_corta_no_meio_do_dia_quando_ele_tem_dois_horarios()
        {
            var r = GeradorSerieAulas.Gerar(Segunda, new[] { Horario(DayOfWeek.Tuesday, 9, 10), Horario(DayOfWeek.Tuesday, 16, 17) }, 3, null);

            Assert.Equal(3, r.Ocorrencias.Count);
            Assert.Equal(new[] { 9, 16, 9 }, r.Ocorrencias.Select(o => o.HoraInicio.Hour));
        }

        [Fact]
        public void A_serie_vira_o_mes_e_o_ano()
        {
            var r = GeradorSerieAulas.Gerar(new DateOnly(2026, 12, 28), new[] { Horario(DayOfWeek.Monday), Horario(DayOfWeek.Thursday) }, 4, null);

            Assert.Equal(
                new[] { new DateOnly(2026, 12, 28), new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 4), new DateOnly(2027, 1, 7) },
                r.Ocorrencias.Select(o => o.Data));
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday)]
        [InlineData(DayOfWeek.Saturday)]
        public void Domingo_e_sabado_funcionam(DayOfWeek dia)
        {
            var r = GeradorSerieAulas.Gerar(Segunda, new[] { Horario(dia) }, 2, null);

            Assert.Equal(2, r.Ocorrencias.Count);
            Assert.All(r.Ocorrencias, o => Assert.Equal(dia, o.DiaSemana));
            Assert.Equal(7, r.Ocorrencias[1].Data.DayNumber - r.Ocorrencias[0].Data.DayNumber);
        }

        [Fact]
        public void Fevereiro_bissexto_e_respeitado()
        {
            var r = GeradorSerieAulas.Gerar(new DateOnly(2028, 2, 26), new[] { Horario(DayOfWeek.Tuesday) }, 2, null);

            // 2028-02-29 e uma terca-feira.
            Assert.Equal(new[] { new DateOnly(2028, 2, 29), new DateOnly(2028, 3, 7) }, r.Ocorrencias.Select(o => o.Data));
        }

        [Fact]
        public void Sessenta_aulas_cabem_no_limite()
        {
            var r = GeradorSerieAulas.Gerar(Segunda, new[] { Horario(DayOfWeek.Monday) }, 60, null);

            Assert.False(r.Excedeu);
            Assert.Equal(60, r.Ocorrencias.Count);
        }

        [Fact]
        public void Sessenta_e_uma_aulas_excedem_o_limite_por_data_final()
        {
            var r = GeradorSerieAulas.Gerar(Segunda, new[] { Horario(DayOfWeek.Monday) }, null, Segunda.AddDays(7 * 60));

            Assert.True(r.Excedeu);
        }

        [Fact]
        public void Data_final_muito_distante_excede_sem_varrer_o_periodo_todo()
        {
            var r = GeradorSerieAulas.Gerar(Segunda, new[] { Horario(DayOfWeek.Monday) }, null, new DateOnly(2100, 1, 1));

            Assert.True(r.Excedeu);
            Assert.Equal(GeradorSerieAulas.LimitePadrao + 1, r.Ocorrencias.Count);
        }

        [Fact]
        public void Sem_horarios_nao_gera_nada()
        {
            var r = GeradorSerieAulas.Gerar(Segunda, Array.Empty<HorarioSerieRequest>(), 5, null);

            Assert.Empty(r.Ocorrencias);
        }
    }
}
