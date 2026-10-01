namespace SPI.Application.Professores.Dtos
{
    public record ConfirmarExclusaoProfessorRequest
    {
        public string Codigo { get; set; } = string.Empty;
    }
}
