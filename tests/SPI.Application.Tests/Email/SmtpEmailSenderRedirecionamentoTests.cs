using SPI.Infrastructure.Email;
using Xunit;

namespace SPI.Application.Tests.Email
{
    // specs/060 (US6, FR-015): em desenvolvimento nenhum e-mail chega a
    // aluno/responsavel real.
    public class SmtpEmailSenderRedirecionamentoTests
    {
        private const string Teste = "teste@exemplo.dev";

        [Fact]
        public void Em_desenvolvimento_redireciona_para_o_endereco_de_teste_com_o_original_no_assunto()
        {
            var (para, assunto) = SmtpEmailSender.ResolverEnvio("aluno@real.com", "Pagamento em aberto", ehDesenvolvimento: true, Teste);

            Assert.Equal(Teste, para);
            Assert.Equal("[DEV → aluno@real.com] Pagamento em aberto", assunto);
        }

        // FR-015 (MUST NOT): sem endereco de teste, em desenvolvimento nada sai.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Em_desenvolvimento_sem_endereco_de_teste_recusa_o_envio(string? redirecionar)
        {
            Assert.Throws<InvalidOperationException>(() =>
                SmtpEmailSender.ResolverEnvio("aluno@real.com", "Assunto", ehDesenvolvimento: true, redirecionar));
        }

        [Fact]
        public void Fora_de_desenvolvimento_envia_ao_destinatario_real_sem_mudar_o_assunto()
        {
            var (para, assunto) = SmtpEmailSender.ResolverEnvio("aluno@real.com", "Assunto", ehDesenvolvimento: false, Teste);

            Assert.Equal("aluno@real.com", para);
            Assert.Equal("Assunto", assunto);
        }
    }
}
