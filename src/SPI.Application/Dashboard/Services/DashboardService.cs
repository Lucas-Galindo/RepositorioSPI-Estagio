using SPI.Application.Common.Dtos;
using SPI.Application.Dashboard.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Repositories;

namespace SPI.Application.Dashboard.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IRelatorioRepository _relatorioRepository;
        private readonly IVinculoCobrancaRepository _vinculoCobrancaRepository;
        private readonly IAulaRepository _aulaRepository;

        public DashboardService(IRelatorioRepository relatorioRepository, IVinculoCobrancaRepository vinculoCobrancaRepository, IAulaRepository aulaRepository)
        {
            _relatorioRepository = relatorioRepository;
            _vinculoCobrancaRepository = vinculoCobrancaRepository;
            _aulaRepository = aulaRepository;
        }

        public async Task<DashboardResponse> ObterAsync(DateOnly? periodoInicio, DateOnly? periodoFim, int? alunoId, CancellationToken cancellationToken = default)
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
            // specs/058: pacotes (ativos e inativos) e as aulas dadas de cada um,
            // no total e no periodo -- duas consultas em lote.
            var pacotes = await _vinculoCobrancaRepository.ListarPacotesAsync(alunoId, cancellationToken);
            var idsPacote = pacotes.Select(p => p.Id).ToList();
            var presencas = await _aulaRepository.ContarPresencasPorVinculosAsync(idsPacote, null, null, cancellationToken);
            var presencasNoPeriodo = await _aulaRepository.ContarPresencasPorVinculosAsync(idsPacote, inicio, fim, cancellationToken);

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
                Pacotes = MontarResumoPacotes(pacotes, presencas, presencasNoPeriodo.Values.Sum())
            };
        }

        // specs/058: visao de negocio dos pacotes. Estatica e pura (testavel sem
        // banco). O estado de cada pacote vem de ProgressoPacote -- o mesmo
        // calculo da ficha do aluno, para os numeros baterem (FR-004).
        public static PacotesResumoResponse MontarResumoPacotes(
            IEnumerable<VinculoCobranca> pacotes, IReadOnlyDictionary<int, int> presencasPorPacote, int aulasDadasNoPeriodo)
        {
            var calculados = pacotes
                .Where(p => p.Modalidade == ModalidadeCobranca.Pacote)
                .Select(p => (Pacote: p, Progresso: ProgressoPacote.Calcular(p, presencasPorPacote.GetValueOrDefault(p.Id))))
                .ToList();

            // "Em uso": pacote ativo de aluno ativo -- o que a professora ainda acompanha.
            var emUso = calculados.Where(c => c.Pacote.Ativo && c.Pacote.Aluno.Ativo).ToList();

            return new PacotesResumoResponse
            {
                Ativos = emUso.Count(c => c.Progresso.Estado != ProgressoPacote.Esgotado),
                Concluidos = calculados.Count(c => c.Progresso.Estado == ProgressoPacote.Esgotado),
                ProximosDoFim = emUso.Count(c => c.Progresso.Estado == ProgressoPacote.Atencao),
                AulasDadas = emUso.Sum(c => c.Progresso.AulasDadas),
                AulasRestantes = emUso.Sum(c => c.Progresso.AulasRestantes ?? 0),
                AulasDadasNoPeriodo = aulasDadasNoPeriodo,
                Renovacao = emUso
                    .Where(c => c.Progresso.Estado is ProgressoPacote.Esgotado or ProgressoPacote.Atencao)
                    .OrderBy(c => c.Progresso.Estado == ProgressoPacote.Esgotado ? 0 : 1)
                    .ThenBy(c => c.Progresso.AulasRestantes)
                    .ThenBy(c => c.Pacote.Aluno.Nome)
                    .Select(c => new PacoteEmAtencaoResponse
                    {
                        VinculoId = c.Pacote.Id,
                        AlunoId = c.Pacote.AlunoId,
                        AlunoNome = c.Pacote.Aluno.Nome,
                        Progresso = c.Progresso,
                    })
                    .ToList(),
            };
        }
    }
}
