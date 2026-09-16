namespace SPI.Application.Relatorios.Dtos
{
    // Substitui o card "Gargalo de caixa" na "Visao de Indicadores" (specs/022):
    // um lancamento individual (entrada ou saida) do periodo filtrado, em vez de
    // apenas o maior valor de cada tipo.
    public record LancamentoIndicadorItem
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public DateOnly DataVencimento { get; set; }
        public decimal Valor { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
