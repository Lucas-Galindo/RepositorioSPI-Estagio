namespace SPI.Domain.Entities
{
    public class AlunoTurma
    {
        public int AlunoId { get; set; }
        public int TurmaId { get; set; }

        public Aluno Aluno { get; set; } = null!;
        public Turma Turma { get; set; } = null!;
    }
}
