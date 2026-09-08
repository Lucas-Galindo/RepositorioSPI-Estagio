namespace SPI.Domain.Entities
{
    public class FormaPagamento
    {
        public int Id { get; set; }
        public string Forma { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativo { get; set; }

        public ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
        public ICollection<ContaPagar> ContasPagar { get; set; } = new List<ContaPagar>();
    }
}
