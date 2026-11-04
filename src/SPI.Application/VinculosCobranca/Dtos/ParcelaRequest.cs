namespace SPI.Application.VinculosCobranca.Dtos
{
    // specs/062: uma parcela do plano Semestral/Anual, como a professora a montou (a tela sugere parcelas
    // mensais iguais, mas ela ajusta data e valor de cada uma; o servidor revalida tudo).
    public record ParcelaRequest
    {
        public DateOnly Vencimento { get; set; }
        public decimal Valor { get; set; }
    }
}
