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

        /// <summary>
        /// Vinculos Pacote ativos com saldo informado <= 2 (specs/041). Vazio
        /// quando ninguem precisa de atencao; esgotados primeiro.
        /// </summary>
        public List<PacoteEmAtencaoResponse> PacotesEmAtencao { get; set; } = new();
    }
}
