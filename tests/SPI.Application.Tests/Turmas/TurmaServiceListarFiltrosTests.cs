using SPI.Application.Tests.VinculosCobranca;
using SPI.Application.Turmas.Services;
using Xunit;

namespace SPI.Application.Tests.Turmas
{
    // specs/076: os filtros novos da lista de turmas (materia e "sem materia") chegam ao repositorio; sem eles,
    // chegam nulos (os outros consumidores nao mudam de resultado). O filtro SQL e provado na API real.
    public class TurmaServiceListarFiltrosTests
    {
        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly TurmaService _servico;

        public TurmaServiceListarFiltrosTests()
        {
            var vinculos = new FakeVinculoCobrancaRepositoryParaTeste();
            _servico = new TurmaService(_turmas, new FakeAlunoRepositoryParaVinculo(), vinculos, new EncerramentoTurmaPacoteService(vinculos));
        }

        [Fact]
        public async Task Materia_chega_ao_repositorio()
        {
            await _servico.ListarAsync(null, null, materiaId: 3);

            Assert.Equal((3, (bool?)null), _turmas.UltimosFiltrosExtras);
        }

        [Fact]
        public async Task Sem_materia_chega_ao_repositorio()
        {
            await _servico.ListarAsync("A", true, semMateria: true);

            Assert.Equal(((int?)null, (bool?)true), _turmas.UltimosFiltrosExtras);
        }

        [Fact]
        public async Task Sem_os_filtros_novos_o_repositorio_recebe_nulos()
        {
            await _servico.ListarAsync("A", false);

            Assert.Equal(((int?)null, (bool?)null), _turmas.UltimosFiltrosExtras);
        }
    }
}
