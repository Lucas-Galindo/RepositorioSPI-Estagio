using Microsoft.AspNetCore.Http;

namespace SPI.Application.Anexos.Services
{
    public class AnexoValidator : IAnexoValidator
    {
        public const int TamanhoMaximoBytes = 10 * 1024 * 1024; // 10MB (FR-003)

        private static readonly byte[] AssinaturaJpeg = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] AssinaturaPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] AssinaturaPdf = { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"

        public List<string> Validar(IFormFile arquivo)
        {
            var erros = new List<string>();

            if (arquivo.Length == 0)
            {
                erros.Add("O arquivo esta vazio ou corrompido. Envie um JPG, PNG ou PDF valido.");
                return erros;
            }

            if (arquivo.Length > TamanhoMaximoBytes)
            {
                erros.Add("O arquivo excede o tamanho maximo permitido de 10MB.");
            }

            if (!ConteudoCorrespondeAFormatoAceito(arquivo))
            {
                erros.Add("Formato de arquivo nao aceito. Envie apenas JPG, PNG ou PDF.");
            }

            return erros;
        }

        /// <summary>
        /// Verifica a assinatura binaria real do arquivo (nao a extensao do nome
        /// nem o Content-Type declarado pelo navegador) -- FR-002, Edge Case de
        /// arquivo renomeado maliciosamente.
        /// </summary>
        private static bool ConteudoCorrespondeAFormatoAceito(IFormFile arquivo)
        {
            var tamanhoNecessario = Math.Max(AssinaturaJpeg.Length, Math.Max(AssinaturaPng.Length, AssinaturaPdf.Length));

            if (arquivo.Length < tamanhoNecessario)
            {
                return false;
            }

            Span<byte> cabecalho = stackalloc byte[tamanhoNecessario];
            using (var stream = arquivo.OpenReadStream())
            {
                var lidos = stream.ReadAtLeast(cabecalho, tamanhoNecessario, throwOnEndOfStream: false);
                if (lidos < tamanhoNecessario)
                {
                    return false;
                }
            }

            return cabecalho.StartsWith(AssinaturaJpeg)
                || cabecalho.StartsWith(AssinaturaPng)
                || cabecalho.StartsWith(AssinaturaPdf);
        }
    }
}
