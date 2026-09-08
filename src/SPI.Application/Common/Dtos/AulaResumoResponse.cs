namespace SPI.Application.Common.Dtos
{
    // Representacao reduzida de Aula, usada no Dashboard e em relatorios.
    public record AulaResumoResponse
    {
        public int Id { get; set; }
        public string MateriaNome { get; set; } = string.Empty;
        public string? TurmaNome { get; set; }
        public DateOnly DataInicio { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFim { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
