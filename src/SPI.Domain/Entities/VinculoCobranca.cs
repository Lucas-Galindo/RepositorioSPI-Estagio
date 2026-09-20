using SPI.Domain.Enums;

namespace SPI.Domain.Entities
{
    /// <summary>
    /// Configuracao de cobranca de um aluno em um contexto especifico: uma
    /// turma, ou atendimento individual quando <see cref="TurmaId"/> e nulo.
    /// Nesta fatia e apenas cadastro -- nenhum processo automatico o consome.
    /// </summary>
    public class VinculoCobranca
    {
        public int Id { get; set; }
        public int AlunoId { get; set; }
        public int? TurmaId { get; set; }
        public ModalidadeCobranca Modalidade { get; set; }
        public decimal Valor { get; set; }
        public int? AulasIncluidas { get; set; }
        public int? SaldoAulas { get; set; }
        public bool Ativo { get; set; }

        public Aluno Aluno { get; set; } = null!;
        public Turma? Turma { get; set; }
    }
}
