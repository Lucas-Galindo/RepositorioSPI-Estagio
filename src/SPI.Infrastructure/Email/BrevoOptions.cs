namespace SPI.Infrastructure.Email
{
    // Ligado a secao "Brevo" da configuracao. Host/Porta/Remetente vem do
    // appsettings.json; SmtpUsuario/SmtpChave sao secretos (User Secrets).
    public class BrevoOptions
    {
        public const string SectionName = "Brevo";

        public string SmtpHost { get; set; } = "smtp-relay.brevo.com";
        public int SmtpPorta { get; set; } = 587;
        public string SmtpUsuario { get; set; } = string.Empty;
        public string SmtpChave { get; set; } = string.Empty;
        public string RemetenteEmail { get; set; } = string.Empty;
        public string RemetenteNome { get; set; } = "SPI - Sistema para Professoras Independentes";
    }
}
