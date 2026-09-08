namespace SPI.Application.ContasPagar.Dtos
{
    public record ContaPagarResponse
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public int CategoriaDespesaId { get; set; }
        public string CategoriaDespesaNome { get; set; } = string.Empty;
        public string? Favorecido { get; set; }
        public decimal Valor { get; set; }
        public DateOnly? Competencia { get; set; }
        public DateOnly DataVencimento { get; set; }
        public DateOnly? DataPagamento { get; set; }
        public int? FormaPagamentoId { get; set; }
        public string? FormaPagamentoNome { get; set; }
        public string? Observacoes { get; set; }

        /// <summary>
        /// Status efetivo: se armazenado como "Pendente" e a DataVencimento ja passou,
        /// e exibido como "Atrasado" (calculado, nao persistido -- mesma convencao de Pagamento).
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}
