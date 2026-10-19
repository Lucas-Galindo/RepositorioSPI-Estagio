using SPI.Domain.Enums;

namespace SPI.Application.VinculosCobranca.Dtos
{
    public record VinculoCobrancaResponse
    {
        public int Id { get; set; }
        public int AlunoId { get; set; }
        public int? TurmaId { get; set; }
        // Nulo quando TurmaId e nulo (atendimento individual).
        public string? TurmaNome { get; set; }
        public ModalidadeCobranca Modalidade { get; set; }
        public decimal Valor { get; set; }
        public int? AulasIncluidas { get; set; }
        public int? SaldoAulas { get; set; }
        public int? TotalAulasPacote { get; set; }
        // Somente-leitura, calculados (specs/052) -- nunca aceitos em request.
        public int? AulasDadas { get; set; }
        public int? AulasDadasNoMes { get; set; }
        public bool Ativo { get; set; }
        // specs/056: combinacao herdada da regra antiga que hoje e proibida (ex.:
        // mensalidade + pacote ativos). So a listagem preenche; nunca e resolvida sozinha.
        public string? Conflito { get; set; }
    }
}
