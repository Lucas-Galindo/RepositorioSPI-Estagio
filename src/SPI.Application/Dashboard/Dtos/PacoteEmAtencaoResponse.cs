namespace SPI.Application.Dashboard.Dtos
{
    /// <summary>
    /// Item do painel de Pacotes em Atencao (specs/041) -- visao derivada do
    /// SaldoAulas atual de um VinculoCobranca Pacote, nunca persistida.
    /// </summary>
    public record PacoteEmAtencaoResponse
    {
        public int VinculoId { get; set; }
        public int AlunoId { get; set; }
        public string AlunoNome { get; set; } = string.Empty;
        public string Contexto { get; set; } = string.Empty;
        public int SaldoAulas { get; set; }

        /// <summary>"Esgotado" (saldo 0) ou "Atencao" (saldo 1 ou 2).</summary>
        public string Estado { get; set; } = string.Empty;
    }
}
