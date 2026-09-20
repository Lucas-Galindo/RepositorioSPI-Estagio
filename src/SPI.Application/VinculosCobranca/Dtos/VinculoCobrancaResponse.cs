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
        public bool Ativo { get; set; }
    }
}
