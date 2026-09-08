namespace SPI.Domain.Entities
{
    public class Turma
    {
        public int Id { get; set; }
        public int ProfessorId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; }

        public Professor Professor { get; set; } = null!;
        public ICollection<AlunoTurma> AlunosTurma { get; set; } = new List<AlunoTurma>();
        public ICollection<Aula> Aulas { get; set; } = new List<Aula>();
        public ICollection<Lembrete> Lembretes { get; set; } = new List<Lembrete>();
    }
}
