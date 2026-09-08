namespace SPI.Domain.Entities
{
    public class Materia
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? Nivel { get; set; }
        public bool Ativo { get; set; }

        public ICollection<Aula> Aulas { get; set; } = new List<Aula>();
    }
}
