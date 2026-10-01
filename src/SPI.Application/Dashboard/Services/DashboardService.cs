using SPI.Application.Common.Dtos;
using SPI.Application.Dashboard.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Repositories;

namespace SPI.Application.Dashboard.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IRelatorioRepository _relatorioRepository;
        private readonly IVinculoCobrancaRepository _vinculoCobrancaRepository;

        public DashboardService(IRelatorioRepository relatorioRepository, IVinculoCobrancaRepository vinculoCobrancaRepository)
        {
            _relatorioRepository = relatorioRepository;
            _vinculoCobrancaRepository = vinculoCobrancaRepository;
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
            var vinculosPacote = await _vinculoCobrancaRepository.ListarAtivosPorModalidadeAsync(ModalidadeCobranca.Pacote, cancellationToken);

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
                }).ToList(),
                PacotesEmAtencao = MontarPacotesEmAtencao(vinculosPacote)
            };
        }

        // specs/041 (US1): unica dona da regra de elegibilidade do painel --
        // estatica e pura, testavel sem banco (DashboardPacotesEmAtencaoTests).
        // Nao confia no filtro do chamador: repete Ativo && Pacote aqui mesmo,
        // para que a regra inteira tenha um dono so (Principio II).
        public static List<PacoteEmAtencaoResponse> MontarPacotesEmAtencao(IEnumerable<VinculoCobranca> vinculos) =>
            vinculos
                .Where(v => v.Ativo && v.Modalidade == ModalidadeCobranca.Pacote && v.SaldoAulas is >= 0 and <= VinculoCobranca.LimiteAlertaPacote && v.Aluno.Ativo)
                .Select(v => new PacoteEmAtencaoResponse
                {
                    VinculoId = v.Id,
                    AlunoId = v.AlunoId,
                    AlunoNome = v.Aluno.Nome,
                    Contexto = v.TurmaId.HasValue ? v.Turma!.Nome : "Atendimento individual",
                    SaldoAulas = v.SaldoAulas!.Value,
                    Estado = v.SaldoAulas == 0 ? "Esgotado" : "Atencao"
                })
                .OrderBy(p => p.Estado == "Esgotado" ? 0 : 1)
                .ThenBy(p => p.SaldoAulas)
                .ThenBy(p => p.AlunoNome)
                .ToList();
    }
}
