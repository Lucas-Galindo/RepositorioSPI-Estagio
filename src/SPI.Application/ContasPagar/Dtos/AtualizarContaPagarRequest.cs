namespace SPI.Application.ContasPagar.Dtos
{
    /// <summary>
    /// Edicao dos dados de uma Conta a Pagar ja existente (nao altera Status --
    /// isso e feito via atualizacao de status). Campos nao informados mantem o
    /// valor atual (ver ContaPagarService.AtualizarAsync).
    /// </summary>
    public record AtualizarContaPagarRequest
    {
        public string? Descricao { get; set; }
        public int? CategoriaDespesaId { get; set; }
        public string? Favorecido { get; set; }
        public decimal? Valor { get; set; }
        public DateOnly? Competencia { get; set; }
        public DateOnly? DataVencimento { get; set; }
        public int? FormaPagamentoId { get; set; }
        public string? Observacoes { get; set; }
    }
}
