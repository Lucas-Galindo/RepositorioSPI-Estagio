namespace SPI.Application.VinculosCobranca.Dtos
{
    // specs/058: progresso de um Pacote -- calculado (ProgressoPacote), nunca gravado.
    public record ProgressoPacoteResponse
    {
        // Nulo quando nao ha total informado nem saldo para estimar.
        public int? Total { get; set; }
        // true quando o total foi estimado como aulas dadas + saldo.
        public bool TotalEstimado { get; set; }
        // Presencas registradas com este pacote.
        public int AulasDadas { get; set; }
        // max(0, total - dadas); nulo sem total.
        public int? AulasRestantes { get; set; }
        // "EmDia", "Atencao", "Esgotado" ou "SemTotal".
        public string Estado { get; set; } = string.Empty;
    }
}
