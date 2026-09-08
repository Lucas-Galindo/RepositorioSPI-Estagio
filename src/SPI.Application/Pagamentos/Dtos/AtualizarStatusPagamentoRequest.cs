namespace SPI.Application.Pagamentos.Dtos
{
    public record AtualizarStatusPagamentoRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
