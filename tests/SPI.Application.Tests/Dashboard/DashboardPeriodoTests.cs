using SPI.Application.Dashboard.Services;
using Xunit;

namespace SPI.Application.Tests.Dashboard
{
    // specs/071 (US3): o periodo unico do dashboard. ResolverPeriodo e estatica e pura -- testavel sem banco.
    public class DashboardPeriodoTests
    {
        private static readonly DateOnly Hoje = new(2026, 11, 5);

        [Fact]
        public void Sem_nenhum_limite_vale_o_mes_de_hoje()
        {
            var (inicio, fim) = DashboardService.ResolverPeriodo(null, null, Hoje);

            Assert.Equal(new DateOnly(2026, 11, 1), inicio);
            Assert.Equal(new DateOnly(2026, 11, 30), fim);
        }

        [Fact]
        public void So_com_o_inicio_o_fim_e_um_mes_depois_menos_um_dia()
        {
            var (inicio, fim) = DashboardService.ResolverPeriodo(new DateOnly(2026, 9, 10), null, Hoje);

            Assert.Equal(new DateOnly(2026, 9, 10), inicio);
            Assert.Equal(new DateOnly(2026, 10, 9), fim);
        }

        [Fact]
        public void So_com_o_ate_o_inicio_e_o_primeiro_dia_do_mes_do_ate()
        {
            var (inicio, fim) = DashboardService.ResolverPeriodo(null, new DateOnly(2026, 9, 15), Hoje);

            Assert.Equal(new DateOnly(2026, 9, 1), inicio);
            Assert.Equal(new DateOnly(2026, 9, 15), fim);
        }

        [Fact]
        public void So_com_o_ate_no_dia_1_o_periodo_e_aquele_dia()
        {
            var (inicio, fim) = DashboardService.ResolverPeriodo(null, new DateOnly(2026, 9, 1), Hoje);

            Assert.Equal(new DateOnly(2026, 9, 1), inicio);
            Assert.Equal(new DateOnly(2026, 9, 1), fim);
        }

        [Fact]
        public void Com_os_dois_limites_nada_e_alterado()
        {
            var (inicio, fim) = DashboardService.ResolverPeriodo(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Hoje);

            Assert.Equal(new DateOnly(2026, 1, 1), inicio);
            Assert.Equal(new DateOnly(2026, 12, 31), fim);
        }

        [Fact]
        public void So_com_o_ate_em_ano_anterior_nao_depende_de_hoje()
        {
            var (inicio, fim) = DashboardService.ResolverPeriodo(null, new DateOnly(2025, 12, 31), Hoje);

            Assert.Equal(new DateOnly(2025, 12, 1), inicio);
            Assert.Equal(new DateOnly(2025, 12, 31), fim);
        }

        [Fact]
        public void So_com_o_inicio_o_fim_acompanha_o_tamanho_do_mes_seguinte_em_ano_bissexto()
        {
            var (_, fim) = DashboardService.ResolverPeriodo(new DateOnly(2024, 2, 10), null, Hoje);

            Assert.Equal(new DateOnly(2024, 3, 9), fim);
        }
    }
}
