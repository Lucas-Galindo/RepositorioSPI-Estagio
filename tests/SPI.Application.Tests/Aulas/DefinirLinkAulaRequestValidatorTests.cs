using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Validators;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/067 (FR-003): so enderecos http/https, sem espacos, ate 500 caracteres.
    public class DefinirLinkAulaRequestValidatorTests
    {
        private static readonly DefinirLinkAulaRequestValidator Validador = new();

        private static bool Valido(string link) => Validador.Validate(new DefinirLinkAulaRequest { Link = link }).IsValid;

        [Theory]
        [InlineData("https://meet.google.com/abc-defg-hij")]
        [InlineData("http://exemplo.com/sala")]
        [InlineData("https://zoom.us/j/123456789?pwd=abcDEF")]
        [InlineData("HTTPS://EXEMPLO.COM/SALA")]
        public void Aceita_enderecos_http_e_https(string link) => Assert.True(Valido(link));

        [Theory]
        [InlineData("ftp://exemplo.com/x")]
        [InlineData("javascript:alert(1)")]
        [InlineData("data:text/html,<script>alert(1)</script>")]
        [InlineData("file:///c:/segredo.txt")]
        [InlineData("mailto:a@b.com")]
        [InlineData("abc")]
        [InlineData("meet.google.com/abc")]
        [InlineData("//exemplo.com/x")]
        [InlineData("https://")]
        [InlineData("")]
        [InlineData("   ")]
        public void Recusa_o_que_nao_e_http_ou_https(string link) => Assert.False(Valido(link));

        [Theory]
        [InlineData("https://exemplo.com/a b")]
        [InlineData(" https://exemplo.com/x")]
        [InlineData("https://exemplo.com/x ")]
        [InlineData("https://exemplo.com/\tx")]
        public void Recusa_espacos(string link) => Assert.False(Valido(link));

        [Fact]
        public void Limite_de_500_caracteres()
        {
            var com500 = "https://exemplo.com/" + new string('a', 500 - "https://exemplo.com/".Length);

            Assert.Equal(500, com500.Length);
            Assert.True(Valido(com500));
            Assert.False(Valido(com500 + "a"));
        }
    }
}
