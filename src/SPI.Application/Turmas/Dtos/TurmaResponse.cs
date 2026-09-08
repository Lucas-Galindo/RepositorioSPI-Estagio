using SPI.Application.Common.Dtos;

namespace SPI.Application.Turmas.Dtos
{
    public record TurmaResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int ProfessorId { get; set; }
        public bool Ativo { get; set; }
        public List<AlunoResumoResponse> Alunos { get; set; } = new();
    }
}
