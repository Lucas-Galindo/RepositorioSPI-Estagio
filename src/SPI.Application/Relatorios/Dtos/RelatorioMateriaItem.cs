namespace SPI.Application.Relatorios.Dtos
{
    // Estoria 18: Relatorio de Materias.
    public record RelatorioMateriaItem
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? Nivel { get; set; }
        public int QtdAulasNoPeriodo { get; set; }
        public int QtdAlunosAtendidos { get; set; }
    }
}
