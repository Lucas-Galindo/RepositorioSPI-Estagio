namespace SPI.Application.Relatorios.Dtos
{
    // Estoria 13: Relatorio de Historico do Aluno.
    public record RelatorioHistoricoAlunoResponse
    {
        public int AlunoId { get; set; }
        public string AlunoNome { get; set; } = string.Empty;
        public string Ra { get; set; } = string.Empty;

        /// <summary>Frequencia dividida pelo total de aulas Realizadas em que o aluno esteve vinculado, no periodo/filtros informados.</summary>
        public decimal PercentualFrequencia { get; set; }

        public List<RelatorioAgendaItem> Aulas { get; set; } = new();
    }
}
