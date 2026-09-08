namespace SPI.Application.Pagamentos.Dtos
{
    public record PagamentoResponse
    {
        public int Id { get; set; }
        public int AlunoId { get; set; }
        public string AlunoNome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int? CategoriaReceitaId { get; set; }
        public string? CategoriaReceitaNome { get; set; }
        public int? FormaPagamentoId { get; set; }
        public string? FormaPagamentoNome { get; set; }
        public DateOnly DataVencimento { get; set; }
        public DateOnly? DataPagamento { get; set; }
        public DateOnly? Competencia { get; set; }
        public decimal ValorFinal { get; set; }
        public string? Observacoes { get; set; }

        /// <summary>
        /// Status efetivo: se armazenado como "Pendente" e a DataVencimento ja passou,
        /// e exibido como "Atrasado" (calculado, nao persistido -- ver schema).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        public List<int> AulaIds { get; set; } = new();
    }
}
