namespace SPI.Application.ContasPagar.Dtos
{
    public record AtualizarStatusContaPagarRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
