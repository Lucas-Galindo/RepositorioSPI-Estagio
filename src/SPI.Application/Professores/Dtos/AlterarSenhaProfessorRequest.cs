namespace SPI.Application.Professores.Dtos
{
    public record AlterarSenhaProfessorRequest
    {
        public string SenhaAtual { get; set; } = string.Empty;
        public string NovaSenha { get; set; } = string.Empty;
    }
}
