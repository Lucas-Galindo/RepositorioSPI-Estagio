namespace SPI.Application.Common.Dtos
{
    // Representacao reduzida de Turma, usada dentro de outros DTOs (Aluno, Aula, etc).
    public record TurmaResumoResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}
