namespace SPI.Application.Relatorios.Dtos
{
    // Estoria 14: Relatorio de Pagamentos.
    public record RelatorioPagamentoItem
    {
        public int PagamentoId { get; set; }
        public string AlunoNome { get; set; } = string.Empty;
        public List<int> AulaIds { get; set; } = new();
        public decimal Valor { get; set; }
        public DateOnly DataVencimento { get; set; }
        public DateOnly? DataPagamento { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public record RelatorioPagamentosResponse
    {
        public List<RelatorioPagamentoItem> Itens { get; set; } = new();

        /// <summary>Soma de Pendente + Atrasado entre os itens retornados.</summary>
        public decimal TotalPendenteConsolidado { get; set; }
    }
}
