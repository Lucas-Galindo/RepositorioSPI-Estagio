namespace SPI.Application.Lembretes.Dtos
{
    public record LembreteResponse
    {
        public int Id { get; set; }
        public int TurmaId { get; set; }
        public string TurmaNome { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime HoraProgramada { get; set; }
        public string Destinatarios { get; set; } = string.Empty;
        public string Canal { get; set; } = string.Empty;
        public int AntecedenciaHora { get; set; }
        public bool Ativo { get; set; }
    }
}
