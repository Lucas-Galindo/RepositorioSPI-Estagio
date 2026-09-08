namespace SPI.Domain.Entities
{
    // Sem coluna "ativo": o estado deste registro e controlado exclusivamente
    // pela coluna "status", mesma convencao usada em Pagamento.
    public class ContaPagar
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public int CategoriaDespesaId { get; set; }
        public string? Favorecido { get; set; }
        public decimal Valor { get; set; }
        public DateOnly? Competencia { get; set; }
        public DateOnly DataVencimento { get; set; }
        public DateOnly? DataPagamento { get; set; }
        public int? FormaPagamentoId { get; set; }
        public string Status { get; set; } = "Pendente";
        public string? Observacoes { get; set; }

        public CategoriaDespesa CategoriaDespesa { get; set; } = null!;
        public FormaPagamento? FormaPagamento { get; set; }
    }
}
