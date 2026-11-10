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
        // Somente-leitura, calculados -- nunca aceitos em request. Progresso: so
        // Pacote (specs/058); AulasDadasNoMes: so Mensalidade (specs/052).
        public ProgressoPacoteResponse? Progresso { get; set; }
        public int? AulasDadasNoMes { get; set; }
        // specs/062: so Semestral/Anual (nulos nas demais). DataFim = inicio + 6 ou 12 meses - 1 dia.
        public DateOnly? DataInicio { get; set; }
        public DateOnly? DataFim { get; set; }
        public List<ParcelaResponse>? Parcelas { get; set; }
        public bool Ativo { get; set; }
        // specs/078: quando a saida do aluno foi registrada com acerto (so pacote inativo). O pacote encerrado assim nao e reativado.
        public DateTime? SaidaRegistradaEm { get; set; }
        // specs/056: combinacao herdada da regra antiga que hoje e proibida (ex.:
        // mensalidade + pacote ativos). So a listagem preenche; nunca e resolvida sozinha.
        public string? Conflito { get; set; }
    }
}
