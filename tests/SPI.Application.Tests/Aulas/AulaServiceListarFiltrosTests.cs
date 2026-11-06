using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/074: os filtros novos da lista de aulas (materia e horario de inicio) chegam ao repositorio; sem eles,
    // chegam nulos (os outros consumidores nao mudam de resultado). O filtro SQL e provado na API real.
    public class AulaServiceListarFiltrosTests
    {
        private static CenarioCadastroAula NovoCenario() => new(CenarioCadastroAula.NovoAluno(1, "Ana"));

        [Fact]
        public async Task Materia_e_horarios_chegam_ao_repositorio()
        {
            var cenario = NovoCenario();

            await cenario.Servico.ListarAsync(null, null, null, null, null,
                materiaId: 3, horaInicioDe: new TimeOnly(9, 0), horaInicioAte: new TimeOnly(12, 0));

            Assert.Equal((3, new TimeOnly(9, 0), new TimeOnly(12, 0)), cenario.Aulas.UltimosFiltrosExtras);
        }

        [Fact]
        public async Task Sem_os_filtros_novos_o_repositorio_recebe_nulos()
        {
            var cenario = NovoCenario();

            await cenario.Servico.ListarAsync("Agendada", 10, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

            Assert.Equal((null, null, null), cenario.Aulas.UltimosFiltrosExtras);
        }

        [Fact]
        public async Task So_um_limite_de_horario_chega_sozinho()
        {
            var cenario = NovoCenario();

            await cenario.Servico.ListarAsync(null, null, null, null, null, horaInicioAte: new TimeOnly(10, 0));

            Assert.Equal((null, null, new TimeOnly(10, 0)), cenario.Aulas.UltimosFiltrosExtras);
        }
    }
}
