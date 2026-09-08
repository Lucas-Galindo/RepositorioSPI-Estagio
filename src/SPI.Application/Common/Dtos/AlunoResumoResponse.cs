namespace SPI.Application.Common.Dtos
{
    // Representacao reduzida de Aluno, usada dentro de outros DTOs (Turma, Aula, etc).
    public record AlunoResumoResponse
    {
        public int Id { get; set; }
        public string Ra { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
    }
}
