using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;
using SPI.Application.Common;

namespace SPI.Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;
        private readonly bool _ehDesenvolvimento;

        public SmtpEmailSender(IOptions<SmtpOptions> options, IHostEnvironment ambiente)
        {
            _options = options.Value;
            _ehDesenvolvimento = ambiente.IsDevelopment();
        }

        public async Task EnviarAsync(string destinatario, string assunto, string corpoHtml, CancellationToken cancellationToken = default)
        {
            // Todo envio passa por aqui: lembrete de aula, de mensalidade, de
            // pagamento pendente e recuperacao de senha.
            var (para, assuntoFinal) = ResolverEnvio(destinatario, assunto, _ehDesenvolvimento, _options.RedirecionarTodosPara);

            var mensagem = new MimeMessage();
            mensagem.From.Add(new MailboxAddress(_options.RemetenteNome, _options.RemetenteEmail));
            mensagem.To.Add(MailboxAddress.Parse(para));
            mensagem.Subject = assuntoFinal;
            mensagem.Body = new BodyBuilder { HtmlBody = corpoHtml }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPorta, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(_options.SmtpUsuario, _options.SmtpChave, cancellationToken);
            await client.SendAsync(mensagem, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }

        // specs/060 (research.md R5): em Development nenhum e-mail chega a um
        // destinatario real -- vai para o endereco de teste, com o original no
        // assunto; sem endereco de teste configurado, o envio e recusado. Fora de
        // Development, nada muda. Estatico e puro para ser testavel sem SMTP.
        public static (string Para, string Assunto) ResolverEnvio(string destinatario, string assunto, bool ehDesenvolvimento, string? redirecionarPara)
        {
            if (!ehDesenvolvimento)
            {
                return (destinatario, assunto);
            }

            if (string.IsNullOrWhiteSpace(redirecionarPara))
            {
                throw new InvalidOperationException(
                    "Envio de e-mail recusado em Development: configure Smtp:RedirecionarTodosPara (User Secrets) com o endereco de teste.");
            }

            return (redirecionarPara, $"[DEV → {destinatario}] {assunto}");
        }
    }
}
