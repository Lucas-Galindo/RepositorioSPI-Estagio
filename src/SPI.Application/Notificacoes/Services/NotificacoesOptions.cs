namespace SPI.Application.Notificacoes.Services
{
    // specs/059: secao "Notificacoes" do appsettings (registrada em AddInfrastructure).
    public class NotificacoesOptions
    {
        public const string SectionName = "Notificacoes";

        // Aulas que terminaram antes disso nao geram aviso (decisao do usuario:
        // so aulas depois da entrada no ar da funcionalidade).
        public DateTime AulasPendentesDesde { get; set; } = new(2026, 10, 19);

        // Intervalo da rotina NotificacaoDispatcherService.
        public int IntervaloVerificacaoSegundos { get; set; } = 300;
    }
}
