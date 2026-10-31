namespace SPI.Application.Turmas.Services
{
    // specs/063: secao "Turmas" do appsettings (registrada em AddInfrastructure).
    public class TurmasOptions
    {
        public const string SectionName = "Turmas";

        // Intervalo da rotina TurmaEncerramentoDispatcherService, que inativa os pacotes das
        // turmas encerradas. A rotina tambem roda uma vez ao iniciar a aplicacao. A operacao e
        // idempotente, entao o intervalo so define o atraso maximo depois da virada do dia.
        public int IntervaloEncerramentoMinutos { get; set; } = 60;
    }
}
