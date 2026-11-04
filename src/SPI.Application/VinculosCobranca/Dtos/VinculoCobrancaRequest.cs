using SPI.Domain.Enums;

namespace SPI.Application.VinculosCobranca.Dtos
{
    public record VinculoCobrancaRequest
    {
        // Nulo = atendimento individual (sem turma).
        public int? TurmaId { get; set; }
        public ModalidadeCobranca Modalidade { get; set; }
        public decimal Valor { get; set; }
        // Somente com Modalidade = Mensalidade.
        public int? AulasIncluidas { get; set; }
        // Somente com Modalidade = Pacote.
        public int? SaldoAulas { get; set; }
        // Somente com Modalidade = Pacote.
        public int? TotalAulasPacote { get; set; }
        // specs/062: somente com Modalidade = Semestral ou Anual (obrigatorios nelas, proibidos nas outras). `Valor`
        // e o valor total do periodo (referencia) e pode diferir da soma das parcelas.
        public DateOnly? DataInicio { get; set; }
        public List<ParcelaRequest>? Parcelas { get; set; }
    }
}
