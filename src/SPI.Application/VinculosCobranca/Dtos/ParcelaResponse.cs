namespace SPI.Application.VinculosCobranca.Dtos
{
    // specs/062: uma parcela de plano (e uma conta a receber, ligada ao vinculo). O Status segue o de Contas a
    // Receber: "Pendente", "Pago", "Cancelado" ou "Atrasado" (pendente e vencida).
    public record ParcelaResponse
    {
        public int PagamentoId { get; set; }
        public string? Descricao { get; set; }
        public DateOnly Vencimento { get; set; }
        public decimal Valor { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
