namespace SPI.Application.Common
{
    // Nunca acoplar um provedor especifico (Brevo, etc) aos servicos de
    // dominio -- so a implementacao em SPI.Infrastructure conhece o provedor.
    public interface IEmailSender
    {
        Task EnviarAsync(string destinatario, string assunto, string corpoHtml, CancellationToken cancellationToken = default);
    }
}
