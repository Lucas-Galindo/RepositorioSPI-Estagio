namespace SPI.Domain.Entities
{
    public class CategoriaDespesa
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; }

        public ICollection<ContaPagar> ContasPagar { get; set; } = new List<ContaPagar>();
    }
}
