namespace SPI.Domain.Enums
{
    // specs/059: tipos de notificacao da professora (gravado como texto). A spec
    // 060 acrescenta aula futura e pagamento pendente.
    public enum TipoNotificacao
    {
        // Aula que terminou e continua "Agendada" (sem registro nem cancelamento).
        AulaPendente,
        // specs/060: aula agendada que comeca em breve (vespera, a partir das 08:00).
        AulaFutura,
        // specs/060: aluno com pagamento vencido sem baixa (uma por aluno).
        PagamentoPendente
    }
}
