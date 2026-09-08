namespace SPI.Application.Materias.Dtos
{
    public record MateriaRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string? Nivel { get; set; }
        public string? Descricao { get; set; }
    }
}
