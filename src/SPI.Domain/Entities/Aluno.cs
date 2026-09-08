namespace SPI.Domain.Entities
{
    public class Aluno
    {
        public int Id { get; set; }
        public string Ra { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string? Cpf { get; set; }
        public string? TelefoneAluno { get; set; }
        public string? TelefoneResponsavel { get; set; }
        public string? Email { get; set; }
        public string? Senha { get; set; }
        public string? EmailResponsavel { get; set; }
        public decimal ValorAula { get; set; }
        public int Frequencia { get; set; }
        public bool Ativo { get; set; }

        public ICollection<AlunoTurma> AlunosTurma { get; set; } = new List<AlunoTurma>();
        public ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
        public ICollection<AulaAluno> AulaAlunos { get; set; } = new List<AulaAluno>();
    }
}
