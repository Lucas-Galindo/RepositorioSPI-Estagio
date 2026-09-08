using SPI.Application.Common.Dtos;

namespace SPI.Application.Alunos.Dtos
{
    public record AlunoResponse
    {
        public int Id { get; set; }
        public string Ra { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string? Cpf { get; set; }
        public string? TelefoneAluno { get; set; }
        public string? TelefoneResponsavel { get; set; }
        public string? Email { get; set; }
        public string? EmailResponsavel { get; set; }
        public decimal ValorAula { get; set; }
        public int Frequencia { get; set; }
        public bool Ativo { get; set; }
        public List<TurmaResumoResponse> Turmas { get; set; } = new();
    }
}
