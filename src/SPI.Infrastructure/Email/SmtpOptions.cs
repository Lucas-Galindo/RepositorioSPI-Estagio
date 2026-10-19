namespace SPI.Infrastructure.Email
{
    // Ligado a secao "Smtp" da configuracao. Host/Porta/Remetente vem do
    // appsettings.json; SmtpUsuario/SmtpChave sao secretos (User Secrets).
    // Padrao atual: Gmail SMTP (specs/049-migrar-email-gmail-smtp).
    public class SmtpOptions
    {
        public const string SectionName = "Smtp";

        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPorta { get; set; } = 587;
        public string SmtpUsuario { get; set; } = string.Empty;
        public string SmtpChave { get; set; } = string.Empty;
        public string RemetenteEmail { get; set; } = string.Empty;
        public string RemetenteNome { get; set; } = "SPI - Sistema para Professoras Independentes";

        // specs/060: em Development, TODO e-mail vai para este endereco (User
        // Secrets, nunca appsettings). Vazio em Development = envio recusado,
        // para nenhum teste chegar a aluno/responsavel real.
        public string? RedirecionarTodosPara { get; set; }
    }
}
