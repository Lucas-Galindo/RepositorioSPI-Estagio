namespace SPI.Domain.Entities
{
    public class PagamentoAula
    {
        public int PagamentoId { get; set; }
        public int AulaId { get; set; }

        public Pagamento Pagamento { get; set; } = null!;
        public Aula Aula { get; set; } = null!;
    }
}
