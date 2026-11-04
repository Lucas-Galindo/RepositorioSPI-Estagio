using SPI.Application.Notificacoes.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Notificacoes.Services
{
    public class NotificacaoService : INotificacaoService
    {
        private readonly INotificacaoRepository _notificacaoRepository;
        private readonly IAulaRepository _aulaRepository;
        private readonly IPagamentoRepository _pagamentoRepository;
        private readonly IProfessorRepository _professorRepository;
        private readonly NotificacoesOptions _opcoes;
        private readonly IVinculoCobrancaRepository? _vinculoRepository;

        public NotificacaoService(
            INotificacaoRepository notificacaoRepository,
            IAulaRepository aulaRepository,
            IPagamentoRepository pagamentoRepository,
            IProfessorRepository professorRepository,
            NotificacoesOptions opcoes,
            IVinculoCobrancaRepository? vinculoRepository = null)
        {
            _vinculoRepository = vinculoRepository;
            _notificacaoRepository = notificacaoRepository;
            _aulaRepository = aulaRepository;
            _pagamentoRepository = pagamentoRepository;
            _professorRepository = professorRepository;
            _opcoes = opcoes;
        }

        // specs/059/060 (research.md R2/R3 da 059; R2 da 060): unico lugar que decide
        // criar, reativar ou resolver notificacoes. Roda pela rotina e antes de cada
        // listagem -- registrar/cancelar/remarcar uma aula ou dar baixa num pagamento
        // nao precisa de gancho proprio: a referencia simplesmente sai do conjunto.
        public async Task SincronizarAsync(DateTime agora, int? professorId, CancellationToken cancellationToken = default)
        {
            var pendentes = await _aulaRepository.ListarPendentesDeStatusAsync(agora, professorId, cancellationToken);
            await SincronizarPorAulaAsync(TipoNotificacao.AulaPendente, pendentes, agora, professorId, cancellationToken);

            var futuras = await _aulaRepository.ListarNaJanelaDeAvisoAsync(agora, _opcoes.AulaFuturaDiasAntes, _opcoes.AulaFuturaHora, professorId, cancellationToken);
            await SincronizarPorAulaAsync(TipoNotificacao.AulaFutura, futuras, agora, professorId, cancellationToken);

            await SincronizarPagamentosAsync(agora, professorId, cancellationToken);
            await SincronizarRenovacoesAsync(agora, professorId, cancellationToken);

            try
            {
                await _notificacaoRepository.SalvarAlteracoesAsync(cancellationToken);
            }
            catch (ConflitoException)
            {
                // Outra sincronizacao (rotina x abertura do sistema) gravou a mesma
                // notificacao primeiro. Nada se perde: a proxima sincronizacao, em
                // novo escopo, reaplica o que faltou.
            }
        }

        // Uma notificacao por aula para o tipo: cria para aula nova no conjunto,
        // reativa a resolvida que voltou ao conjunto, resolve a ativa que saiu dele.
        private async Task SincronizarPorAulaAsync(TipoNotificacao tipo, List<Aula> aulas, DateTime agora, int? professorId, CancellationToken cancellationToken)
        {
            var idsNoConjunto = aulas.Select(a => a.Id).ToHashSet();
            var existentesPorAula = (await _notificacaoRepository.ListarPorAulasAsync(tipo, idsNoConjunto, cancellationToken))
                .ToDictionary(n => n.AulaId!.Value);

            foreach (var aula in aulas)
            {
                await CriarOuReativarAsync(existentesPorAula.GetValueOrDefault(aula.Id),
                    () => new Notificacao { ProfessorId = aula.ProfessorId, Tipo = tipo, AulaId = aula.Id, CriadaEm = agora },
                    agora, cancellationToken);
            }

            foreach (var ativa in await _notificacaoRepository.ListarAtivasPorTipoAsync(tipo, professorId, cancellationToken))
            {
                if (!idsNoConjunto.Contains(ativa.AulaId!.Value))
                {
                    ativa.ResolvidaEm = agora;
                }
            }
        }

        // specs/060: uma notificacao por (professora, aluno) com pagamento vencido sem
        // baixa. Sistema single-tenant: na rotina a professora e a unica cadastrada.
        private async Task SincronizarPagamentosAsync(DateTime agora, int? professorId, CancellationToken cancellationToken)
        {
            var destinataria = professorId ?? (await _professorRepository.ObterUnicaAsync(cancellationToken))?.Id;
            if (destinataria is null)
            {
                return;
            }

            var hoje = DateOnly.FromDateTime(agora);
            var alunosComPendencia = (await _pagamentoRepository.ListarPendentesVencidosAsync(hoje, cancellationToken))
                .Select(p => p.AlunoId).ToHashSet();
            var existentesPorAluno = (await _notificacaoRepository.ListarPorAlunosAsync(TipoNotificacao.PagamentoPendente, destinataria.Value, alunosComPendencia, cancellationToken))
                .ToDictionary(n => n.AlunoId!.Value);

            foreach (var alunoId in alunosComPendencia)
            {
                await CriarOuReativarAsync(existentesPorAluno.GetValueOrDefault(alunoId),
                    () => new Notificacao { ProfessorId = destinataria.Value, Tipo = TipoNotificacao.PagamentoPendente, AlunoId = alunoId, CriadaEm = agora },
                    agora, cancellationToken);
            }

            foreach (var ativa in await _notificacaoRepository.ListarAtivasPorTipoAsync(TipoNotificacao.PagamentoPendente, destinataria, cancellationToken))
            {
                if (!alunosComPendencia.Contains(ativa.AlunoId!.Value))
                {
                    ativa.ResolvidaEm = agora;
                }
            }
        }

        // specs/062: uma notificacao por (professora, aluno ativo) cujo plano Semestral/Anual mais recente (maior data de
        // fim) termina a ate RenovacaoDiasAntes dias ou ja terminou. Um plano de renovacao com inicio depois do fim do
        // atual passa a ser o de maior fim e tira o aluno do conjunto; excluir o plano tambem. Mesma trava de unicidade
        // (tipo, professora, aluno) das demais: nunca duplica entre verificacoes.
        private async Task SincronizarRenovacoesAsync(DateTime agora, int? professorId, CancellationToken cancellationToken)
        {
            if (_vinculoRepository is null)
            {
                return;
            }

            var destinataria = professorId ?? (await _professorRepository.ObterUnicaAsync(cancellationToken))?.Id;
            if (destinataria is null)
            {
                return;
            }

            var limite = DateOnly.FromDateTime(agora).AddDays(_opcoes.RenovacaoDiasAntes);
            var alunosComRenovacao = (await _vinculoRepository.ListarPlanosAtivosAsync(cancellationToken))
                .Where(p => p.Aluno is { Ativo: true } && p.DataFim.HasValue)
                .GroupBy(p => p.AlunoId)
                .Where(g => g.Max(p => p.DataFim!.Value) <= limite)
                .Select(g => g.Key)
                .ToHashSet();

            var existentesPorAluno = (await _notificacaoRepository.ListarPorAlunosAsync(TipoNotificacao.RenovacaoPlano, destinataria.Value, alunosComRenovacao, cancellationToken))
                .ToDictionary(n => n.AlunoId!.Value);

            foreach (var alunoId in alunosComRenovacao)
            {
                await CriarOuReativarAsync(existentesPorAluno.GetValueOrDefault(alunoId),
                    () => new Notificacao { ProfessorId = destinataria.Value, Tipo = TipoNotificacao.RenovacaoPlano, AlunoId = alunoId, CriadaEm = agora },
                    agora, cancellationToken);
            }

            foreach (var ativa in await _notificacaoRepository.ListarAtivasPorTipoAsync(TipoNotificacao.RenovacaoPlano, destinataria, cancellationToken))
            {
                if (!alunosComRenovacao.Contains(ativa.AlunoId!.Value))
                {
                    ativa.ResolvidaEm = agora;
                }
            }
        }

        // Sem notificacao: cria. Resolvida: a referencia voltou a precisar de aviso,
        // entao a MESMA notificacao volta como nova e nao lida (nunca duplica).
        private async Task CriarOuReativarAsync(Notificacao? existente, Func<Notificacao> nova, DateTime agora, CancellationToken cancellationToken)
        {
            if (existente is null)
            {
                await _notificacaoRepository.AdicionarAsync(nova(), cancellationToken);
            }
            else if (existente.ResolvidaEm is not null)
            {
                existente.ResolvidaEm = null;
                existente.LidaEm = null;
                existente.CriadaEm = agora;
            }
        }

        public async Task<List<NotificacaoResponse>> ListarAtivasAsync(int professorId, DateTime agora, CancellationToken cancellationToken = default)
        {
            await SincronizarAsync(agora, professorId, cancellationToken);

            var notificacoes = await _notificacaoRepository.ListarAtivasPorProfessorAsync(professorId, cancellationToken);

            // Pendencias atuais por aluno, para o detalhe das notificacoes de pagamento.
            var hoje = DateOnly.FromDateTime(agora);
            var pendenciasPorAluno = notificacoes.Any(n => n.Tipo == TipoNotificacao.PagamentoPendente)
                ? (await _pagamentoRepository.ListarPendentesVencidosAsync(hoje, cancellationToken)).ToLookup(p => p.AlunoId)
                : Enumerable.Empty<Pagamento>().ToLookup(p => p.AlunoId);

            // specs/062: plano de maior fim de cada aluno com aviso de renovacao (uma consulta so).
            var planoPorAluno = notificacoes.Any(n => n.Tipo == TipoNotificacao.RenovacaoPlano) && _vinculoRepository is not null
                ? (await _vinculoRepository.ListarPlanosAtivosAsync(cancellationToken))
                    .Where(p => p.DataFim.HasValue)
                    .GroupBy(p => p.AlunoId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.DataFim).First())
                : new Dictionary<int, VinculoCobranca>();

            return notificacoes.Select(n => Mapear(n, pendenciasPorAluno, planoPorAluno, hoje)).ToList();
        }

        public async Task MarcarComoLidaAsync(int professorId, int id, DateTime agora, CancellationToken cancellationToken = default)
        {
            var notificacao = await _notificacaoRepository.ObterPorIdAsync(id, cancellationToken);

            // Notificacao de outra professora responde como inexistente.
            if (notificacao is null || notificacao.ProfessorId != professorId)
            {
                throw new NaoEncontradoException("Notificacao nao encontrada.");
            }

            // Abrir marca como lida, mas so a sincronizacao resolve (FR-009 da 059).
            if (notificacao.LidaEm is null)
            {
                notificacao.LidaEm = agora;
                await _notificacaoRepository.SalvarAlteracoesAsync(cancellationToken);
            }
        }

        private static NotificacaoResponse Mapear(Notificacao n, ILookup<int, Pagamento> pendenciasPorAluno, IReadOnlyDictionary<int, VinculoCobranca> planoPorAluno, DateOnly hoje) => new()
        {
            Id = n.Id,
            Tipo = n.Tipo,
            Mensagem = n.Tipo switch
            {
                TipoNotificacao.AulaPendente => "Esta aula já terminou e está sem frequência registrada. Registre a chamada para corrigir.",
                TipoNotificacao.AulaFutura => "Você tem uma aula agendada em breve.",
                TipoNotificacao.RenovacaoPlano => MensagemDeRenovacao(n, planoPorAluno, hoje),
                _ => $"{n.Aluno?.Nome} tem pagamento vencido sem baixa.",
            },
            CriadaEm = n.CriadaEm,
            Lida = n.LidaEm is not null,
            Aula = n.Aula is null ? null : new NotificacaoAulaResponse
            {
                Id = n.Aula.Id,
                Data = n.Aula.DataInicio,
                HoraInicio = n.Aula.HoraInicio,
                HoraFim = n.Aula.HoraFim,
                MateriaNome = n.Aula.Materia.Nome,
                Contexto = Contexto(n.Aula),
            },
            Renovacao = n.Tipo != TipoNotificacao.RenovacaoPlano || n.AlunoId is null || !planoPorAluno.TryGetValue(n.AlunoId.Value, out var plano)
                ? null
                : new NotificacaoRenovacaoResponse
                {
                    AlunoId = n.AlunoId.Value,
                    AlunoNome = n.Aluno?.Nome ?? string.Empty,
                    Modalidade = plano.Modalidade.ToString(),
                    DataFim = plano.DataFim!.Value,
                },
            Pagamento = n.Tipo != TipoNotificacao.PagamentoPendente || n.AlunoId is null ? null : new NotificacaoPagamentoResponse
            {
                AlunoId = n.AlunoId.Value,
                AlunoNome = n.Aluno?.Nome ?? string.Empty,
                Pendencias = pendenciasPorAluno[n.AlunoId.Value].Select(p => new PendenciaPagamentoResponse
                {
                    PagamentoId = p.Id,
                    Descricao = p.Descricao ?? p.VinculoCobranca?.Modalidade.ToString() ?? "Pagamento",
                    Valor = p.ValorFinal,
                    Vencimento = p.DataVencimento,
                    DiasAtraso = hoje.DayNumber - p.DataVencimento.DayNumber,
                }).ToList(),
            },
        };

        private static string MensagemDeRenovacao(Notificacao n, IReadOnlyDictionary<int, VinculoCobranca> planoPorAluno, DateOnly hoje)
        {
            if (n.AlunoId is null || !planoPorAluno.TryGetValue(n.AlunoId.Value, out var plano) || !plano.DataFim.HasValue)
            {
                return $"O plano de {n.Aluno?.Nome} precisa de renovação.";
            }

            var verbo = plano.DataFim.Value < hoje ? "terminou" : "termina";
            return $"O plano {plano.Modalidade} de {n.Aluno?.Nome} {verbo} em {plano.DataFim.Value:dd/MM/yyyy}.";
        }

        // Turma da aula; na aula individual, "Atendimento individual" com o aluno.
        private static string Contexto(Aula aula)
        {
            if (aula.Turma is not null)
            {
                return aula.Turma.Nome;
            }

            var aluno = aula.AulaAlunos.FirstOrDefault()?.Aluno.Nome;
            return aluno is null ? "Atendimento individual" : $"Atendimento individual · {aluno}";
        }
    }
}
