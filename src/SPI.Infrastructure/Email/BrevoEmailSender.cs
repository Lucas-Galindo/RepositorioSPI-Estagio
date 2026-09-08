using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SPI.Application.Common;

namespace SPI.Infrastructure.Email
{
    public class BrevoEmailSender : IEmailSender
    {
        private readonly BrevoOptions _options;

        public BrevoEmailSender(IOptions<BrevoOptions> options)
        {
            _options = options.Value;
        }

        public async Task EnviarAsync(string destinatario, string assunto, string corpoHtml, CancellationToken cancellationToken = default)
        {
            var mensagem = new MimeMessage();
            mensagem.From.Add(new MailboxAddress(_options.RemetenteNome, _options.RemetenteEmail));
            mensagem.To.Add(MailboxAddress.Parse(destinatario));
            mensagem.Subject = assunto;
            mensagem.Body = new BodyBuilder { HtmlBody = corpoHtml }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPorta, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(_options.SmtpUsuario, _options.SmtpChave, cancellationToken);
            await client.SendAsync(mensagem, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
