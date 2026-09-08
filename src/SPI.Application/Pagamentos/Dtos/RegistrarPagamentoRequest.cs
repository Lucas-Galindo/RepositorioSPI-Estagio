namespace SPI.Application.Pagamentos.Dtos
{
    public record RegistrarPagamentoRequest
    {
        public int AlunoId { get; set; }
        public string? Descricao { get; set; }
        public int? CategoriaReceitaId { get; set; }
        public List<int> AulaIds { get; set; } = new();
        public DateOnly DataVencimento { get; set; }
        public DateOnly? Competencia { get; set; }
        public decimal ValorFinal { get; set; }
        public int FormaPagamentoId { get; set; }
        public string? Observacoes { get; set; }

        /// <summary>
        /// "Pendente" (padrao) ou "Pago" quando o recebimento ja ocorreu no ato do registro.
        /// </summary>
        public string Status { get; set; } = "Pendente";
    }
}
