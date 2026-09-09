using SPI.Application.Pagamentos.Services;
using SPI.Application.Relatorios.Dtos;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Relatorios.Services
{
    public class RelatorioService : IRelatorioService
    {
        private readonly IAulaRepository _aulaRepository;
        private readonly IAlunoRepository _alunoRepository;
        private readonly IMateriaRepository _materiaRepository;
        private readonly IRelatorioRepository _relatorioRepository;
        private readonly IPagamentoService _pagamentoService;

        public RelatorioService(
            IAulaRepository aulaRepository,
            IAlunoRepository alunoRepository,
            IMateriaRepository materiaRepository,
            IRelatorioRepository relatorioRepository,
            IPagamentoService pagamentoService)
        {
            _aulaRepository = aulaRepository;
            _alunoRepository = alunoRepository;
            _materiaRepository = materiaRepository;
            _relatorioRepository = relatorioRepository;
            _pagamentoService = pagamentoService;
        }

        public async Task<List<RelatorioAgendaItem>> ObterAgendaAsync(
            DateOnly? inicio, DateOnly? fim, string? status, int? turmaId, int? alunoId, CancellationToken cancellationToken = default)
        {
            var aulas = await _aulaRepository.ListarAsync(status, turmaId, alunoId, inicio, fim, cancellationToken);

            return aulas.Select(a => new RelatorioAgendaItem
            {
                AulaId = a.Id,
                Data = a.DataInicio,
                HoraInicio = a.HoraInicio,
                HoraFim = a.HoraFim,
                Materia = a.Materia.Nome,
                TurmaOuAluno = a.Turma?.Nome ?? $"Individual - {a.AulaAlunos.FirstOrDefault()?.Aluno.Nome ?? "?"}",
                Status = a.Status
            }).ToList();
        }

        public async Task<RelatorioHistoricoAlunoResponse> ObterHistoricoAlunoAsync(
            int alunoId, DateOnly? inicio, DateOnly? fim, string? status, int? turmaId, CancellationToken cancellationToken = default)
        {
            var aluno = await _alunoRepository.ObterPorIdAsync(alunoId, cancellationToken)
                ?? throw new NaoEncontradoException("Aluno nao encontrado.");

            var aulas = await ObterAgendaAsync(inicio, fim, status, turmaId, alunoId, cancellationToken);

            // Formula da Estoria 13: FREQUENCIA (contador vitalicio do aluno,
            // ver Aluno.Frequencia) dividida pelo total de aulas Realizadas
            // em que ele esteve vinculado dentro do periodo/filtros informados.
            var totalRealizadasNoFiltro = aulas.Count(a => a.Status == "Realizada");
            var percentual = totalRealizadasNoFiltro == 0 ? 0m : Math.Round((decimal)aluno.Frequencia / totalRealizadasNoFiltro * 100, 1);

            return new RelatorioHistoricoAlunoResponse
            {
                AlunoId = aluno.Id,
                AlunoNome = aluno.Nome,
                Ra = aluno.Ra,
                PercentualFrequencia = percentual,
                Aulas = aulas
            };
        }

        public async Task<RelatorioPagamentosResponse> ObterPagamentosAsync(
            int? alunoId, DateOnly? inicio, DateOnly? fim, string? status, CancellationToken cancellationToken = default)
        {
            var pagamentos = await _pagamentoService.ListarAsync(alunoId, status, inicio, fim, cancellationToken);

            var itens = pagamentos.Select(p => new RelatorioPagamentoItem
            {
                PagamentoId = p.Id,
                AlunoNome = p.AlunoNome,
                AulaIds = p.AulaIds,
                Valor = p.ValorFinal,
                DataVencimento = p.DataVencimento,
                DataPagamento = p.DataPagamento,
                Status = p.Status
            }).ToList();

            var totalPendente = itens.Where(i => i.Status is "Pendente" or "Atrasado").Sum(i => i.Valor);

            return new RelatorioPagamentosResponse
            {
                Itens = itens,
                TotalPendenteConsolidado = totalPendente
            };
        }

        public async Task<RelatorioFinanceiroResponse> ObterFinanceiroAsync(
            DateOnly? inicio, DateOnly? fim, int? formaPagamentoId, int? alunoId, int? turmaId, int? materiaId, CancellationToken cancellationToken = default)
        {
            var pagos = await _relatorioRepository.ListarPagosNoPeriodoAsync(inicio, fim, formaPagamentoId, alunoId, turmaId, materiaId, cancellationToken);
            var totalRecebido = pagos.Sum(p => p.ValorFinal);

            // Sem periodo informado, ListarPagosNoPeriodoAsync nao aplica limite
            // (todo o historico); ObterValorPagoNoPeriodoAsync exige datas, entao
            // usamos o intervalo maximo representavel para manter a mesma semantica.
            var despIni = inicio ?? DateOnly.MinValue;
            var despFim = fim ?? DateOnly.MaxValue;
            var totalPago = await _relatorioRepository.ObterValorPagoNoPeriodoAsync(despIni, despFim, cancellationToken);

            var (receitaAVencer, receitaAtrasada) = await _relatorioRepository.ObterReceitasPendentesSegregadasAsync(cancellationToken);
            var (despesaAVencer, despesaAtrasada) = await _relatorioRepository.ObterDespesasPendentesSegregadasAsync(cancellationToken);
            var receitaPendente = receitaAVencer + receitaAtrasada;
            var despesaPendente = despesaAVencer + despesaAtrasada;

            var saldoRealizado = totalRecebido - totalPago;

            return new RelatorioFinanceiroResponse
            {
                TotalRecebido = totalRecebido,
                TotalPago = totalPago,
                SaldoRealizado = saldoRealizado,
                ReceitaPendente = receitaPendente,
                DespesaPendente = despesaPendente,
                SaldoPrevisto = saldoRealizado + (receitaPendente - despesaPendente),
                PorFormaPagamento = pagos
                    .GroupBy(p => p.FormaPagamento?.Forma ?? "—")
                    .Select(g => new RelatorioFinanceiroItem { Chave = g.Key, Total = g.Sum(p => p.ValorFinal) })
                    .OrderByDescending(i => i.Total)
                    .ToList(),
                PorAluno = pagos
                    .GroupBy(p => p.Aluno.Nome)
                    .Select(g => new RelatorioFinanceiroItem { Chave = g.Key, Total = g.Sum(p => p.ValorFinal) })
                    .OrderByDescending(i => i.Total)
                    .ToList(),
                // Turma do aluno no momento do pagamento (primeira, se vinculado
                // a mais de uma); "Atendimento particular" se nao tiver turma.
                PorTurma = pagos
                    .GroupBy(p => p.Aluno.AlunosTurma.Select(at => at.Turma.Nome).FirstOrDefault() ?? "Atendimento particular")
                    .Select(g => new RelatorioFinanceiroItem { Chave = g.Key, Total = g.Sum(p => p.ValorFinal) })
                    .OrderByDescending(i => i.Total)
                    .ToList()
            };
        }

        public async Task<RelatorioPeriodoAgendaResponse> ObterPeriodoAgendaAsync(
            DateOnly inicio, DateOnly fim, int? turmaId, int? materiaId, CancellationToken cancellationToken = default)
        {
            var porDia = await _relatorioRepository.ContarAulasPorDiaAsync(inicio, fim, turmaId, materiaId, cancellationToken);
            var (agendadas, realizadas, canceladas) = await _relatorioRepository.ContarAulasPorStatusAsync(inicio, fim, turmaId, materiaId, cancellationToken);

            var total = agendadas + realizadas + canceladas;
            var taxaOcupacao = total == 0 ? 0m : Math.Round((decimal)realizadas / total * 100, 1);

            return new RelatorioPeriodoAgendaResponse
            {
                AulasPorDia = porDia.Select(x => new AulasPorDiaItem { Data = x.Data, Quantidade = x.Quantidade }).ToList(),
                TotalAgendadas = agendadas,
                TotalRealizadas = realizadas,
                TotalCanceladas = canceladas,
                TaxaOcupacao = taxaOcupacao
            };
        }

        public async Task<List<RelatorioAlunoItem>> ObterAlunosAsync(
            string? nome, int? turmaId, bool? ativo, CancellationToken cancellationToken = default)
        {
            var alunos = await _alunoRepository.ListarAsync(nome, ra: null, turmaId, ativo, cancellationToken);

            return alunos.Select(a => new RelatorioAlunoItem
            {
                Id = a.Id,
                Nome = a.Nome,
                Ra = a.Ra,
                TelefoneAluno = a.TelefoneAluno,
                TelefoneResponsavel = a.TelefoneResponsavel,
                EmailResponsavel = a.EmailResponsavel,
                Turmas = a.AlunosTurma.Select(at => at.Turma.Nome).ToList(),
                ValorAula = a.ValorAula,
                Frequencia = a.Frequencia,
                Ativo = a.Ativo
            }).ToList();
        }

        public async Task<List<RelatorioMateriaItem>> ObterMateriasAsync(
            string? nivel, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default)
        {
            var materias = await _materiaRepository.ListarAsync(nome: null, nivel, ativo: null, cancellationToken);
            var itens = new List<RelatorioMateriaItem>();

            foreach (var materia in materias)
            {
                itens.Add(new RelatorioMateriaItem
                {
                    Id = materia.Id,
                    Nome = materia.Nome,
                    Descricao = materia.Descricao,
                    Nivel = materia.Nivel,
                    QtdAulasNoPeriodo = await _relatorioRepository.ContarAulasDaMateriaNoPeriodoAsync(materia.Id, inicio, fim, cancellationToken),
                    QtdAlunosAtendidos = await _relatorioRepository.ContarAlunosAtendidosDaMateriaNoPeriodoAsync(materia.Id, inicio, fim, cancellationToken)
                });
            }

            return itens;
        }
    }
}
