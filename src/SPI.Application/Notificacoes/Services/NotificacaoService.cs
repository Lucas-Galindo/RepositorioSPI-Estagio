using SPI.Application.Notificacoes.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Notificacoes.Services
{
    public class NotificacaoService : INotificacaoService
    {
        private const string MensagemAulaPendente = "Você ainda não atualizou o status desta aula.";

        private readonly INotificacaoRepository _notificacaoRepository;
        private readonly IAulaRepository _aulaRepository;
        private readonly NotificacoesOptions _opcoes;

        public NotificacaoService(INotificacaoRepository notificacaoRepository, IAulaRepository aulaRepository, NotificacoesOptions opcoes)
        {
            _notificacaoRepository = notificacaoRepository;
            _aulaRepository = aulaRepository;
            _opcoes = opcoes;
        }

        // specs/059 (research.md R3): unico lugar que decide criar, reativar ou
        // resolver avisos de aula pendente. Roda pela rotina e antes de cada
        // listagem; por isso registrar/cancelar/excluir/editar a aula nao precisa
        // de gancho proprio -- a aula simplesmente deixa de estar pendente.
        public async Task SincronizarAulasPendentesAsync(DateTime agora, int? professorId, CancellationToken cancellationToken = default)
        {
            var pendentes = await _aulaRepository.ListarPendentesDeStatusAsync(agora, _opcoes.AulasPendentesDesde, professorId, cancellationToken);
            var idsPendentes = pendentes.Select(a => a.Id).ToHashSet();

            // Existentes em qualquer estado: uma notificacao resolvida da mesma aula
            // e reativada, nunca duplicada (indice unico tipo + aula).
            var existentesPorAula = (await _notificacaoRepository.ListarPorAulasAsync(TipoNotificacao.AulaPendente, idsPendentes, cancellationToken))
                .ToDictionary(n => n.AulaId!.Value);

            foreach (var aula in pendentes)
            {
                if (!existentesPorAula.TryGetValue(aula.Id, out var existente))
                {
                    await _notificacaoRepository.AdicionarAsync(new Notificacao
                    {
                        ProfessorId = aula.ProfessorId,
                        Tipo = TipoNotificacao.AulaPendente,
                        AulaId = aula.Id,
                        CriadaEm = agora,
                    }, cancellationToken);
                }
                else if (existente.ResolvidaEm is not null)
                {
                    // A aula voltou a ficar pendente (ex.: editada para o futuro e
                    // passou de novo): o mesmo aviso volta como novo e nao lido.
                    existente.ResolvidaEm = null;
                    existente.LidaEm = null;
                    existente.CriadaEm = agora;
                }
            }

            // Ativas cuja aula deixou de estar pendente: realizada, cancelada,
            // excluida ou com termino movido para o futuro.
            foreach (var ativa in await _notificacaoRepository.ListarAtivasPorTipoAsync(TipoNotificacao.AulaPendente, professorId, cancellationToken))
            {
                if (!idsPendentes.Contains(ativa.AulaId!.Value))
                {
                    ativa.ResolvidaEm = agora;
                }
            }

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

        public async Task<List<NotificacaoResponse>> ListarAtivasAsync(int professorId, DateTime agora, CancellationToken cancellationToken = default)
        {
            await SincronizarAulasPendentesAsync(agora, professorId, cancellationToken);

            var notificacoes = await _notificacaoRepository.ListarAtivasPorProfessorAsync(professorId, cancellationToken);
            return notificacoes.Select(Mapear).ToList();
        }

        public async Task MarcarComoLidaAsync(int professorId, int id, DateTime agora, CancellationToken cancellationToken = default)
        {
            var notificacao = await _notificacaoRepository.ObterPorIdAsync(id, cancellationToken);

            // Notificacao de outra professora responde como inexistente.
            if (notificacao is null || notificacao.ProfessorId != professorId)
            {
                throw new NaoEncontradoException("Notificacao nao encontrada.");
            }

            // Abrir marca como lida, mas so a sincronizacao resolve (FR-009).
            if (notificacao.LidaEm is null)
            {
                notificacao.LidaEm = agora;
                await _notificacaoRepository.SalvarAlteracoesAsync(cancellationToken);
            }
        }

        private static NotificacaoResponse Mapear(Notificacao n) => new()
        {
            Id = n.Id,
            Tipo = n.Tipo,
            Mensagem = MensagemAulaPendente,
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
        };

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
