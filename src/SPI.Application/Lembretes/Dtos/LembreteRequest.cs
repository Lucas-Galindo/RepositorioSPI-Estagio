namespace SPI.Application.Lembretes.Dtos
{
    public record LembreteRequest
    {
        public int TurmaId { get; set; }

        /// <summary>
        /// Somente "Email" e aceito. WhatsApp e SMS foram descontinuados.
        /// </summary>
        public string Canal { get; set; } = string.Empty;

        public int AntecedenciaHora { get; set; }

        /// <summary>
        /// "Alunos", "Responsaveis" ou "AlunosEResponsaveis".
        /// </summary>
        public string Destinatarios { get; set; } = string.Empty;
    }
}
