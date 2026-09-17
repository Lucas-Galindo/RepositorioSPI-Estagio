using Microsoft.AspNetCore.Http;
using SPI.Application.Anexos.Services;
using Xunit;

namespace SPI.Application.Tests.Anexos
{
    // specs/023: testa o AnexoValidator compartilhado entre ContaPagar e
    // Pagamento -- a peca de logica de negocio de maior risco de regressao
    // silenciosa desta feature (rejeicao por conteudo real do arquivo, nao
    // extensao; rejeicao por tamanho). Nao depende de repositorio, entao
    // nenhum fake e necessario (mesma decisao de specs/022 de nao instalar
    // biblioteca de mock neste projeto de testes).
    public class AnexoValidatorTests
    {
        private static IFormFile CriarArquivo(byte[] conteudo, string nomeArquivo = "arquivo.bin")
        {
            var stream = new MemoryStream(conteudo);
            return new FormFile(stream, 0, conteudo.Length, "arquivo", nomeArquivo);
        }

        [Fact]
        public void Validar_JpegValido_NaoRetornaErros()
        {
            var conteudo = new byte[] { 0xFF, 0xD8, 0xFF, 0x01, 0x02, 0x03, 0x04, 0x05 };
            var erros = new AnexoValidator().Validar(CriarArquivo(conteudo, "nota.jpg"));

            Assert.Empty(erros);
        }

        [Fact]
        public void Validar_PngValido_NaoRetornaErros()
        {
            var conteudo = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02 };
            var erros = new AnexoValidator().Validar(CriarArquivo(conteudo, "nota.png"));

            Assert.Empty(erros);
        }

        [Fact]
        public void Validar_PdfValido_NaoRetornaErros()
        {
            var conteudo = "%PDF-1.4 conteudo qualquer"u8.ToArray();
            var erros = new AnexoValidator().Validar(CriarArquivo(conteudo, "nota.pdf"));

            Assert.Empty(erros);
        }

        [Fact]
        public void Validar_TextoRenomeadoParaPdf_RejeitaPorConteudoReal()
        {
            var conteudo = "isto e um arquivo de texto simples, nao um pdf de verdade"u8.ToArray();
            var erros = new AnexoValidator().Validar(CriarArquivo(conteudo, "nota.pdf"));

            Assert.NotEmpty(erros);
        }

        [Fact]
        public void Validar_ArquivoMaiorQue10MB_Rejeita()
        {
            var conteudo = new byte[AnexoValidator.TamanhoMaximoBytes + 1];
            Array.Copy(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, conteudo, 5); // "%PDF-"

            var erros = new AnexoValidator().Validar(CriarArquivo(conteudo, "nota.pdf"));

            Assert.NotEmpty(erros);
        }

        [Fact]
        public void Validar_ArquivoVazio_RejeitaSemLancarExcecao()
        {
            var erros = new AnexoValidator().Validar(CriarArquivo(Array.Empty<byte>(), "vazio.jpg"));

            Assert.NotEmpty(erros);
        }
    }
}
