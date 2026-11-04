namespace SPI.Application.Notificacoes.Services
{
    // specs/059: secao "Notificacoes" do appsettings (registrada em AddInfrastructure).
    public class NotificacoesOptions
    {
        public const string SectionName = "Notificacoes";

        // Intervalo da rotina NotificacaoDispatcherService.
        public int IntervaloVerificacaoSegundos { get; set; } = 300;

        // specs/060: o aviso de aula futura aparece AulaFuturaDiasAntes dias antes
        // da aula, a partir de AulaFuturaHora (decisao do usuario: ajustavel sem codigo).
        public int AulaFuturaDiasAntes { get; set; } = 1;
        public TimeOnly AulaFuturaHora { get; set; } = new(8, 0);

        // specs/062: o aviso de renovacao do plano Semestral/Anual aparece quando o fim esta a ate este numero
        // de dias (e continua depois do fim, ate renovar ou excluir).
        public int RenovacaoDiasAntes { get; set; } = 15;
    }
}
