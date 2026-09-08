namespace SPI.Application.Lembretes.Dtos
{
    public record LembreteRequest
    {
        public int TurmaId { get; set; }

        /// <summary>
        /// "Email", "WhatsApp" ou "SMS". Somente "Email" e efetivamente
        /// disparado nesta fase (Estoria 19); os demais ficam registrados.
        /// </summary>
        public string Canal { get; set; } = string.Empty;

        public int AntecedenciaHora { get; set; }

        /// <summary>
        /// "Alunos", "Responsaveis" ou "AlunosEResponsaveis".
        /// </summary>
        public string Destinatarios { get; set; } = string.Empty;
    }
}
