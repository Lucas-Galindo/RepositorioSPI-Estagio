namespace SPI.Domain.Entities
{
    public class AulaAluno
    {
        public int AulaId { get; set; }
        public int AlunoId { get; set; }

        // NULL = aula ainda nao realizada; TRUE/FALSE = presenca marcada
        // ao registrar a sessao (Estoria 8).
        public bool? Presente { get; set; }

        // NULL = comportamento normal (presente) ou falta comum -- NUNCA
        // indica bloqueio. So preenchido por RegistrarSessaoAsync quando a
        // presenca e barrada por pacote esgotado (specs/041).
        public string? MotivoNaoRegistro { get; set; }

        public const string MotivoPacoteEsgotado = "PacoteEsgotado";

        public Aula Aula { get; set; } = null!;
        public Aluno Aluno { get; set; } = null!;
    }
}
