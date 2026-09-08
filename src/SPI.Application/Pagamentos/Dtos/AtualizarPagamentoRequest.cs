namespace SPI.Application.Pagamentos.Dtos
{
    /// <summary>
    /// Edicao dos dados de uma Conta a Receber ja existente (nao altera Aluno,
    /// AulaIds nem Status -- isso e feito via registro/atualizacao de status).
    /// Campos nao informados mantem o valor atual (ver PagamentoService.AtualizarAsync).
    /// </summary>
    public record AtualizarPagamentoRequest
    {
        public string? Descricao { get; set; }
        public int? CategoriaReceitaId { get; set; }
        public int? FormaPagamentoId { get; set; }
        public DateOnly? DataVencimento { get; set; }
        public decimal? ValorFinal { get; set; }
        public DateOnly? Competencia { get; set; }
        public string? Observacoes { get; set; }
    }
}
