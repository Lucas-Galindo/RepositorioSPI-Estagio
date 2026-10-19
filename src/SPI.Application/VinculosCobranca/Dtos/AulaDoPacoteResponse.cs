namespace SPI.Application.VinculosCobranca.Dtos
{
    // specs/058: aula que contou como "dada" num pacote (lista do detalhe do pacote).
    public record AulaDoPacoteResponse
    {
        public int AulaId { get; set; }
        public DateOnly Data { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFim { get; set; }
        public string MateriaNome { get; set; } = string.Empty;
        // Nome da turma ou "Atendimento individual".
        public string Contexto { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
