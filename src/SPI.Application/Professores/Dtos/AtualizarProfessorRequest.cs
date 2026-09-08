namespace SPI.Application.Professores.Dtos
{
    public record AtualizarProfessorRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
    }
}
