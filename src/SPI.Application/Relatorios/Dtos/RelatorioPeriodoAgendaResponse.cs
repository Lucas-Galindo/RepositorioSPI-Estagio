namespace SPI.Application.Relatorios.Dtos
{
    // Estoria 16: Relatorio do Periodo da Agenda (so indicadores agregados,
    // sem listar aula a aula -- isso e o Relatorio de Agenda, Estoria 12).
    public record AulasPorDiaItem
    {
        public DateOnly Data { get; set; }
        public int Quantidade { get; set; }
    }

    public record RelatorioPeriodoAgendaResponse
    {
        public List<AulasPorDiaItem> AulasPorDia { get; set; } = new();
        public int TotalAgendadas { get; set; }
        public int TotalRealizadas { get; set; }
        public int TotalCanceladas { get; set; }

        /// <summary>Percentual de aulas Realizadas sobre o total de aulas do periodo (proxy de ocupacao efetiva da agenda).</summary>
        public decimal TaxaOcupacao { get; set; }
    }
}
