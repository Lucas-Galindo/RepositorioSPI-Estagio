namespace SPI.Domain.Entities
{
    public class AulaAluno
    {
        public int AulaId { get; set; }
        public int AlunoId { get; set; }

        // NULL = aula ainda nao realizada; TRUE/FALSE = presenca marcada
        // ao registrar a sessao (Estoria 8).
        public bool? Presente { get; set; }

        public Aula Aula { get; set; } = null!;
        public Aluno Aluno { get; set; } = null!;
    }
}
