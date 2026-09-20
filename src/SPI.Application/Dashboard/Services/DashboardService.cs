using SPI.Application.Common.Dtos;
using SPI.Application.Dashboard.Dtos;
using SPI.Domain.Repositories;

namespace SPI.Application.Dashboard.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IRelatorioRepository _relatorioRepository;

        public DashboardService(IRelatorioRepository relatorioRepository)
        {
            _relatorioRepository = relatorioRepository;
        }

        public async Task<DashboardResponse> ObterAsync(DateOnly? periodoInicio, DateOnly? periodoFim, CancellationToken cancellationToken = default)
        {
            // Sem periodo informado, assume o mes corrente (Estoria 11 nao
            // fixa um periodo padrao explicito).
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var inicio = periodoInicio ?? new DateOnly(hoje.Year, hoje.Month, 1);
            var fim = periodoFim ?? inicio.AddMonths(1).AddDays(-1);

            var totalAulas = await _relatorioRepository.ContarAulasAgendadasNoPeriodoAsync(inicio, fim, cancellationToken);
            var totalAlunos = await _relatorioRepository.ContarAlunosAtivosAsync(cancellationToken);
            var alunosAtendidos = await _relatorioRepository.ContarAlunosAtendidosNoPeriodoAsync(inicio, fim, cancellationToken);
            var totalTurmas = await _relatorioRepository.ContarTurmasAtivasAsync(cancellationToken);
            var valorPendente = await _relatorioRepository.ObterValorPendenteAsync(cancellationToken);
            var valorFaturado = await _relatorioRepository.ObterValorFaturadoNoPeriodoAsync(inicio, fim, cancellationToken);
            var proximasAulas = await _relatorioRepository.ObterProximasAulasHojeAsync(cancellationToken);
            var lembretesPendentes = await _relatorioRepository.ContarLembretesPendentesAsync(cancellationToken);

            return new DashboardResponse
            {
                TotalAulasAgendadasNoPeriodo = totalAulas,
                TotalAlunosAtivos = totalAlunos,
                AlunosAtendidosNoPeriodo = alunosAtendidos,
                TotalTurmasAtivas = totalTurmas,
                ValorPendenteRecebimento = valorPendente,
                ValorFaturadoNoPeriodo = valorFaturado,
                LembretesPendentes = lembretesPendentes,
                ProximasAulasHoje = proximasAulas.Select(a => new AulaResumoResponse
                {
                    Id = a.Id,
                    MateriaNome = a.Materia.Nome,
                    TurmaNome = a.Turma?.Nome,
                    DataInicio = a.DataInicio,
                    HoraInicio = a.HoraInicio,
                    HoraFim = a.HoraFim,
                    Status = a.Status
                }).ToList()
            };
        }
    }
}
