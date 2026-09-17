namespace SPI.Application.Anexos.Dtos
{
    /// <summary>
    /// Metadados do anexo de comprovante (nota fiscal/cupom) de uma Conta a
    /// Pagar ou a Receber. Nunca inclui o conteudo binario -- ver
    /// contracts/anexo-comprovante.md.
    /// </summary>
    public record AnexoResponse
    {
        public string NomeOriginal { get; set; } = string.Empty;
        public string TipoMime { get; set; } = string.Empty;
        public int TamanhoBytes { get; set; }
        public DateTime DataUpload { get; set; }
    }
}
