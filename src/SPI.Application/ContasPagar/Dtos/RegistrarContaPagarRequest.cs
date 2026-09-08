namespace SPI.Application.ContasPagar.Dtos
{
    public record RegistrarContaPagarRequest
    {
        public string Descricao { get; set; } = string.Empty;
        public int CategoriaDespesaId { get; set; }
        public string? Favorecido { get; set; }
        public decimal Valor { get; set; }
        public DateOnly? Competencia { get; set; }
        public DateOnly DataVencimento { get; set; }
        public int? FormaPagamentoId { get; set; }
        public string? Observacoes { get; set; }

        /// <summary>
        /// "Pendente" (padrao) ou "Pago" quando o pagamento ja ocorreu no ato do registro.
        /// </summary>
        public string Status { get; set; } = "Pendente";
    }
}
