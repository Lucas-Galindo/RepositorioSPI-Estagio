using SPI.Application.Common.Dtos;

namespace SPI.Application.Dashboard.Dtos
{
    public record DashboardResponse
    {
        public int TotalAulasAgendadasNoPeriodo { get; set; }
        public int TotalAlunosAtivos { get; set; }
        public int AlunosAtendidosNoPeriodo { get; set; }
        public int TotalTurmasAtivas { get; set; }
        public decimal ValorPendenteRecebimento { get; set; }
        public decimal ValorFaturadoNoPeriodo { get; set; }
        public List<AulaResumoResponse> ProximasAulasHoje { get; set; } = new();
        public int LembretesPendentes { get; set; }
        public IndicadoresFinanceirosResponse Indicadores { get; set; } = new();
        public List<FluxoCaixaMensalItem> FluxoCaixaMensal { get; set; } = new();
    }
}
