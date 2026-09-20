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
    }
}
