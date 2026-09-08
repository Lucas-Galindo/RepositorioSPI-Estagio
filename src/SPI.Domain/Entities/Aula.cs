namespace SPI.Domain.Entities
{
    public class Aula
    {
        public int Id { get; set; }
        public string? Descricao { get; set; }
        public int MateriaId { get; set; }
        public int ProfessorId { get; set; }
        public int? TurmaId { get; set; }
        public DateOnly DataInicio { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFim { get; set; }
        public string Status { get; set; } = "Agendada";
        public bool Ativo { get; set; }

        public Materia Materia { get; set; } = null!;
        public Professor Professor { get; set; } = null!;
        public Turma? Turma { get; set; }
        public ICollection<PagamentoAula> PagamentosAula { get; set; } = new List<PagamentoAula>();
        public ICollection<AulaAluno> AulaAlunos { get; set; } = new List<AulaAluno>();
    }
}
