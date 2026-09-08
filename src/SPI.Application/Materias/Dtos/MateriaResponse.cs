namespace SPI.Application.Materias.Dtos
{
    public record MateriaResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Nivel { get; set; }
        public string? Descricao { get; set; }
        public bool Ativo { get; set; }
    }
}
