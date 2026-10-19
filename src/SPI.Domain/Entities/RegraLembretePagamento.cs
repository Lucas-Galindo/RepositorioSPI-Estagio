namespace SPI.Domain.Entities
{
    // specs/060: regra do e-mail semanal de pagamento pendente, editavel na aba
    // Lembretes. Ha uma linha so (Id = 1).
    public class RegraLembretePagamento
    {
        public const int IdUnico = 1;

        public int Id { get; set; }
        public bool Ativo { get; set; }
        // 0 = domingo ... 6 = sabado (mesma convencao de DayOfWeek).
        public int DiaSemana { get; set; }
        public TimeOnly Hora { get; set; }
    }
}
