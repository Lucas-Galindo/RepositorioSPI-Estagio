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
        Pacote = 3,
        // specs/062: plano de 6 meses, com inicio, fim e parcelas; sem turma; cobre todas as aulas do periodo.
        Semestral = 4,
        // specs/062: plano de 12 meses (mesmas regras do Semestral).
        Anual = 5
    }
}
