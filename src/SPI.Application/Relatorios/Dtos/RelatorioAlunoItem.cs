namespace SPI.Application.Relatorios.Dtos
{
    // Estoria 17: Relatorio de Alunos.
    public record RelatorioAlunoItem
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Ra { get; set; } = string.Empty;
        public string? TelefoneAluno { get; set; }
        public string? TelefoneResponsavel { get; set; }
        public string? EmailResponsavel { get; set; }
        public List<string> Turmas { get; set; } = new();
        public decimal ValorAula { get; set; }
        public int Frequencia { get; set; }
        public bool Ativo { get; set; }
    }
}
