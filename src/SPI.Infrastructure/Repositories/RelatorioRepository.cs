using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class RelatorioRepository : IRelatorioRepository
    {
        private readonly SpiDbContext _dbContext;

        public RelatorioRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<int> ContarAulasAgendadasNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default) =>
            _dbContext.Aulas.CountAsync(a =>
                a.Ativo && a.Status == "Agendada" && a.DataInicio >= inicio && a.DataInicio <= fim, cancellationToken);

        public Task<int> ContarAlunosAtivosAsync(CancellationToken cancellationToken = default) =>
            _dbContext.Alunos.CountAsync(a => a.Ativo, cancellationToken);

        public Task<int> ContarTurmasAtivasAsync(CancellationToken cancellationToken = default) =>
            _dbContext.Turmas.CountAsync(t => t.Ativo, cancellationToken);

        public async Task<decimal> ObterValorPendenteAsync(CancellationToken cancellationToken = default)
        {
            // "Atrasado" nao e persistido (e um Pendente vencido); somar por
            // status "Pendente" ja cobre os dois casos do relatorio.
            var pagamentos = await _dbContext.Pagamentos
                .Where(p => p.Status == "Pendente")
                .Select(p => p.ValorFinal)
                .ToListAsync(cancellationToken);

            return pagamentos.Sum();
        }

        public async Task<decimal> ObterValorFaturadoNoPeriodoAsync(
            DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default,
            int? turmaId = null, int? materiaId = null, int? alunoId = null,
            string? turmaNome = null, string? materiaNome = null, string? alunoBusca = null)
        {
            var query = AplicarFiltroReceitaPorNome(
                AplicarFiltroReceita(
                    _dbContext.Pagamentos.Where(p => p.Status == "Pago" && p.DataPagamento != null && p.DataPagamento >= inicio && p.DataPagamento <= fim),
                    turmaId, materiaId, alunoId),
                turmaNome, materiaNome, alunoBusca);
            var pagamentos = await query
                .Select(p => p.ValorFinal)
                .ToListAsync(cancellationToken);

            return pagamentos.Sum();
        }

        public async Task<decimal> ObterValorAPagarAsync(CancellationToken cancellationToken = default)
        {
            // Mesma convencao de ObterValorPendenteAsync: "Atrasado" nao e
            // persistido (e um Pendente vencido), entao somar por status
            // "Pendente" ja cobre os dois casos.
            var contas = await _dbContext.ContasPagar
                .Where(c => c.Status == "Pendente")
                .Select(c => c.Valor)
                .ToListAsync(cancellationToken);

            return contas.Sum();
        }

        public async Task<decimal> ObterValorPagoNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default)
        {
            var contas = await _dbContext.ContasPagar
                .Where(c => c.Status == "Pago" && c.DataPagamento != null && c.DataPagamento >= inicio && c.DataPagamento <= fim)
                .Select(c => c.Valor)
                .ToListAsync(cancellationToken);

            return contas.Sum();
        }

        public async Task<(decimal AVencer, decimal Atrasado)> ObterReceitasPendentesSegregadasAsync(
            CancellationToken cancellationToken = default, string? turmaNome = null, string? materiaNome = null, string? alunoBusca = null)
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var query = AplicarFiltroReceitaPorNome(
                _dbContext.Pagamentos.Where(p => p.Status == "Pendente"), turmaNome, materiaNome, alunoBusca);
            var pendentes = await query
                .Select(p => new { p.ValorFinal, p.DataVencimento })
                .ToListAsync(cancellationToken);

            return (
                pendentes.Where(p => p.DataVencimento >= hoje).Sum(p => p.ValorFinal),
                pendentes.Where(p => p.DataVencimento < hoje).Sum(p => p.ValorFinal));
        }

        public async Task<(decimal AVencer, decimal Atrasado)> ObterDespesasPendentesSegregadasAsync(CancellationToken cancellationToken = default)
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var pendentes = await _dbContext.ContasPagar
                .Where(c => c.Status == "Pendente")
                .Select(c => new { c.Valor, c.DataVencimento })
                .ToListAsync(cancellationToken);

            return (
                pendentes.Where(c => c.DataVencimento >= hoje).Sum(c => c.Valor),
                pendentes.Where(c => c.DataVencimento < hoje).Sum(c => c.Valor));
        }

        public Task<List<Pagamento>> ObterProximasContasAReceberAsync(
            int dias, CancellationToken cancellationToken = default, string? turmaNome = null, string? materiaNome = null, string? alunoBusca = null)
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var limite = hoje.AddDays(dias);

            var query = AplicarFiltroReceitaPorNome(
                _dbContext.Pagamentos.Include(p => p.Aluno).Where(p => p.Status == "Pendente" && p.DataVencimento >= hoje && p.DataVencimento <= limite),
                turmaNome, materiaNome, alunoBusca);

            return query.OrderBy(p => p.DataVencimento).ToListAsync(cancellationToken);
        }

        public Task<List<ContaPagar>> ObterProximasContasAPagarAsync(int dias, CancellationToken cancellationToken = default)
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var limite = hoje.AddDays(dias);

            return _dbContext.ContasPagar
                .Where(c => c.Status == "Pendente" && c.DataVencimento >= hoje && c.DataVencimento <= limite)
                .OrderBy(c => c.DataVencimento)
                .ToListAsync(cancellationToken);
        }

        public Task<List<Aula>> ObterProximasAulasHojeAsync(CancellationToken cancellationToken = default)
        {
            var hoje = DateOnly.FromDateTime(DateTime.Now);

            return _dbContext.Aulas
                .Include(a => a.Materia)
                .Include(a => a.Turma)
                .Where(a => a.Ativo && a.Status == "Agendada" && a.DataInicio == hoje)
                .OrderBy(a => a.HoraInicio)
                .ToListAsync(cancellationToken);
        }

        public Task<int> ContarLembretesPendentesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.Lembretes.CountAsync(l => l.Ativo && l.Status == "Pendente", cancellationToken);

        public async Task<List<(DateOnly Data, int Quantidade)>> ContarAulasPorDiaAsync(
            DateOnly inicio, DateOnly fim, int? turmaId, int? materiaId, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Aulas.Where(a => a.Ativo && a.DataInicio >= inicio && a.DataInicio <= fim);

            if (turmaId.HasValue)
            {
                query = query.Where(a => a.TurmaId == turmaId.Value);
            }
            if (materiaId.HasValue)
            {
                query = query.Where(a => a.MateriaId == materiaId.Value);
            }

            var agrupado = await query
                .GroupBy(a => a.DataInicio)
                .Select(g => new { Data = g.Key, Quantidade = g.Count() })
                .ToListAsync(cancellationToken);

            return agrupado.OrderBy(x => x.Data).Select(x => (x.Data, x.Quantidade)).ToList();
        }

        public async Task<(int Agendadas, int Realizadas, int Canceladas)> ContarAulasPorStatusAsync(
            DateOnly inicio, DateOnly fim, int? turmaId, int? materiaId, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Aulas.Where(a => a.Ativo && a.DataInicio >= inicio && a.DataInicio <= fim);

            if (turmaId.HasValue)
            {
                query = query.Where(a => a.TurmaId == turmaId.Value);
            }
            if (materiaId.HasValue)
            {
                query = query.Where(a => a.MateriaId == materiaId.Value);
            }

            var status = await query.Select(a => a.Status).ToListAsync(cancellationToken);

            return (
                status.Count(s => s == "Agendada"),
                status.Count(s => s == "Realizada"),
                status.Count(s => s == "Cancelada"));
        }

        public Task<int> ContarAulasDaMateriaNoPeriodoAsync(int materiaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Aulas.Where(a => a.Ativo && a.MateriaId == materiaId);

            if (inicio.HasValue)
            {
                query = query.Where(a => a.DataInicio >= inicio.Value);
            }
            if (fim.HasValue)
            {
                query = query.Where(a => a.DataInicio <= fim.Value);
            }

            return query.CountAsync(cancellationToken);
        }

        public Task<int> ContarAlunosAtendidosDaMateriaNoPeriodoAsync(int materiaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.AulaAlunos.Where(aa => aa.Aula.Ativo && aa.Aula.MateriaId == materiaId);

            if (inicio.HasValue)
            {
                query = query.Where(aa => aa.Aula.DataInicio >= inicio.Value);
            }
            if (fim.HasValue)
            {
                query = query.Where(aa => aa.Aula.DataInicio <= fim.Value);
            }

            return query.Select(aa => aa.AlunoId).Distinct().CountAsync(cancellationToken);
        }

        public Task<List<Pagamento>> ListarPagosNoPeriodoAsync(
            DateOnly? inicio, DateOnly? fim, int? formaPagamentoId, int? alunoId, int? turmaId, int? materiaId, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Pagamentos
                .Include(p => p.Aluno).ThenInclude(a => a.AlunosTurma).ThenInclude(at => at.Turma)
                .Include(p => p.FormaPagamento)
                .Where(p => p.Status == "Pago");

            if (inicio.HasValue)
            {
                query = query.Where(p => p.DataPagamento != null && p.DataPagamento >= inicio.Value);
            }
            if (fim.HasValue)
            {
                query = query.Where(p => p.DataPagamento != null && p.DataPagamento <= fim.Value);
            }
            if (formaPagamentoId.HasValue)
            {
                query = query.Where(p => p.FormaPagamentoId == formaPagamentoId.Value);
            }
            if (alunoId.HasValue)
            {
                query = query.Where(p => p.AlunoId == alunoId.Value);
            }
            if (turmaId.HasValue)
            {
                query = query.Where(p => p.Aluno.AlunosTurma.Any(at => at.TurmaId == turmaId.Value));
            }
            if (materiaId.HasValue)
            {
                query = query.Where(p => p.PagamentosAula.Any(pa => pa.Aula.MateriaId == materiaId.Value));
            }

            return query.OrderBy(p => p.DataPagamento).ToListAsync(cancellationToken);
        }

        public Task<int> ContarAlunosAtendidosNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default) =>
            _dbContext.AulaAlunos
                .Where(aa => aa.Aula.Status == "Realizada" && aa.Aula.DataInicio >= inicio && aa.Aula.DataInicio <= fim)
                .Select(aa => aa.AlunoId)
                .Distinct()
                .CountAsync(cancellationToken);

        // Filtro compartilhado por turma/materia/aluno nos indicadores financeiros
        // filtrados (Sprint 3 da evolucao do Financeiro): so se aplica ao lado da
        // receita (Pagamento) -- despesas (ContaPagar) nao tem nenhuma ligacao com
        // aluno/turma/materia, entao nunca sao filtradas por esses criterios.
        private static IQueryable<Pagamento> AplicarFiltroReceita(
            IQueryable<Pagamento> query, int? turmaId, int? materiaId, int? alunoId)
        {
            if (alunoId.HasValue)
            {
                query = query.Where(p => p.AlunoId == alunoId.Value);
            }
            if (turmaId.HasValue)
            {
                query = query.Where(p => p.Aluno.AlunosTurma.Any(at => at.TurmaId == turmaId.Value));
            }
            if (materiaId.HasValue)
            {
                query = query.Where(p => p.PagamentosAula.Any(pa => pa.Aula.MateriaId == materiaId.Value));
            }
            return query;
        }

        // Variante por nome/RA (Sprint 4.2 da evolucao do Financeiro, Visao
        // Geral): a professora busca por texto em vez de escolher de um
        // dropdown com Id. Mesma regra de so filtrar o lado da receita.
        private static IQueryable<Pagamento> AplicarFiltroReceitaPorNome(
            IQueryable<Pagamento> query, string? turmaNome, string? materiaNome, string? alunoBusca)
        {
            if (!string.IsNullOrWhiteSpace(alunoBusca))
            {
                query = query.Where(p => p.Aluno.Nome.Contains(alunoBusca) || p.Aluno.Ra.Contains(alunoBusca));
            }
            if (!string.IsNullOrWhiteSpace(turmaNome))
            {
                query = query.Where(p => p.Aluno.AlunosTurma.Any(at => at.Turma.Nome.Contains(turmaNome)));
            }
            if (!string.IsNullOrWhiteSpace(materiaNome))
            {
                query = query.Where(p => p.PagamentosAula.Any(pa => pa.Aula.Materia.Nome.Contains(materiaNome)));
            }
            return query;
        }

        public async Task<(decimal TotalVencidoNoPeriodo, decimal ValorInadimplente)> ObterInadimplenciaNoPeriodoAsync(
            DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default,
            int? turmaId = null, int? materiaId = null, int? alunoId = null)
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var query = AplicarFiltroReceita(
                _dbContext.Pagamentos.Where(p => p.Status != "Cancelado" && p.DataVencimento >= inicio && p.DataVencimento <= fim),
                turmaId, materiaId, alunoId);
            var contas = await query
                .Select(p => new { p.ValorFinal, p.Status, p.DataVencimento })
                .ToListAsync(cancellationToken);

            var total = contas.Sum(c => c.ValorFinal);
            var inadimplente = contas
                .Where(c => c.Status == "Pendente" && c.DataVencimento < hoje)
                .Sum(c => c.ValorFinal);

            return (total, inadimplente);
        }

        public async Task<List<(DateOnly DataVencimento, DateOnly DataPagamento)>> ObterPagamentosComAtrasoNoPeriodoAsync(
            DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default,
            int? turmaId = null, int? materiaId = null, int? alunoId = null)
        {
            var query = AplicarFiltroReceita(
                _dbContext.Pagamentos.Where(p => p.Status == "Pago"
                    && p.DataPagamento != null
                    && p.DataPagamento >= inicio && p.DataPagamento <= fim
                    && p.DataPagamento > p.DataVencimento),
                turmaId, materiaId, alunoId);
            var pagos = await query
                .Select(p => new { p.DataVencimento, DataPagamento = p.DataPagamento!.Value })
                .ToListAsync(cancellationToken);

            return pagos.Select(p => (p.DataVencimento, p.DataPagamento)).ToList();
        }

        public async Task<List<(int Dia, decimal Valor)>> ObterEntradasPorDiaDoMesAsync(
            DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default,
            int? turmaId = null, int? materiaId = null, int? alunoId = null)
        {
            var query = AplicarFiltroReceita(
                _dbContext.Pagamentos.Where(p => p.Status != "Cancelado" && p.DataVencimento >= inicio && p.DataVencimento <= fim),
                turmaId, materiaId, alunoId);
            var pagamentos = await query
                .Select(p => new { p.DataVencimento, p.ValorFinal })
                .ToListAsync(cancellationToken);

            return pagamentos
                .GroupBy(p => p.DataVencimento.Day)
                .Select(g => (g.Key, g.Sum(p => p.ValorFinal)))
                .OrderBy(x => x.Key)
                .ToList();
        }

        public async Task<List<(int Dia, decimal Valor)>> ObterSaidasPorDiaDoMesAsync(
            DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default)
        {
            var contas = await _dbContext.ContasPagar
                .Where(c => c.Status != "Cancelado" && c.DataVencimento >= inicio && c.DataVencimento <= fim)
                .Select(c => new { c.DataVencimento, c.Valor })
                .ToListAsync(cancellationToken);

            return contas
                .GroupBy(c => c.DataVencimento.Day)
                .Select(g => (g.Key, g.Sum(c => c.Valor)))
                .OrderBy(x => x.Key)
                .ToList();
        }
    }
}
