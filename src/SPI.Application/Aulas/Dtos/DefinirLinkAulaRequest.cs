namespace SPI.Application.Aulas.Dtos
{
    // specs/067: corpo do PUT /api/aulas/{id}/link.
    public record DefinirLinkAulaRequest
    {
        public string Link { get; set; } = string.Empty;
    }
}
