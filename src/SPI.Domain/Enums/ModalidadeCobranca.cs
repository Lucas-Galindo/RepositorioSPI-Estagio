namespace SPI.Domain.Enums
{
    /// <summary>
    /// Modalidade de cobranca de um VinculoCobranca. Persistida como texto
    /// (mesmo padrao de aula.status) e trafegada como string no JSON.
    /// </summary>
    public enum ModalidadeCobranca
    {
        Avulsa = 1,
        Mensalidade = 2,
        Pacote = 3
    }
}
