namespace SPI.Application.Alunos.Dtos
{
    public record AtualizarAlunoRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string? Cpf { get; set; }
        public string? TelefoneAluno { get; set; }
        public string? TelefoneResponsavel { get; set; }
        public string? Email { get; set; }
        public string? EmailResponsavel { get; set; }
        public decimal ValorAula { get; set; }
        public bool EhMenorDeIdade { get; set; }
    }
}
