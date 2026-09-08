namespace SPI.Domain.Entities
{
    public class Professor
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public bool Ativo { get; set; }

        public ICollection<Turma> Turmas { get; set; } = new List<Turma>();
        public ICollection<Aula> Aulas { get; set; } = new List<Aula>();
    }
}
