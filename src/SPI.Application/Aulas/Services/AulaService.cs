using System.Globalization;
using SPI.Application.Aulas.Dtos;
using SPI.Application.Common;
using SPI.Application.Lembretes.Services;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Aulas.Services
{
    public class AulaService : IAulaService
    {
        private readonly IAulaRepository _aulaRepository;
        private readonly IAlunoRepository _alunoRepository;
        private readonly ITurmaRepository _turmaRepository;
        private readonly IMateriaRepository _materiaRepository;
        private readonly ILembreteService _lembreteService;
        private readonly IPagamentoRepository _pagamentoRepository;
        private readonly ICategoriaReceitaRepository _categoriaReceitaRepository;
        private readonly IVinculoCobrancaRepository _vinculoCobrancaRepository;
        private readonly TimeProvider _relogio;
        private readonly ILinkAulaTemporario? _links;
        private readonly IResumoCobrancaService? _cobranca;

        public AulaService(
            IAulaRepository aulaRepository,
            IAlunoRepository alunoRepository,
            ITurmaRepository turmaRepository,
            IMateriaRepository materiaRepository,
            ILembreteService lembreteService,
            IPagamentoRepository pagamentoRepository,
            ICategoriaReceitaRepository categoriaReceitaRepository,
            IVinculoCobrancaRepository vinculoCobrancaRepository,
            TimeProvider relogio,
            ILinkAulaTemporario? links = null,
            IResumoCobrancaService? cobranca = null)
        {
            _cobranca = cobranca;
            _aulaRepository = aulaRepository;
            _alunoRepository = alunoRepository;
            _turmaRepository = turmaRepository;
            _materiaRepository = materiaRepository;
            _lembreteService = lembreteService;
            _pagamentoRepository = pagamentoRepository;
            _categoriaReceitaRepository = categoriaReceitaRepository;
            _vinculoCobrancaRepository = vinculoCobrancaRepository;
            _relogio = relogio;
            _links = links;
        }

        public async Task<List<AulaResponse>> ListarAsync(
            string? status,
            int? turmaId,
            int? alunoId,
            DateOnly? dataInicio,
            DateOnly? dataFim,
            CancellationToken cancellationToken = default,
            int? materiaId = null, TimeOnly? horaInicioDe = null, TimeOnly? horaInicioAte = null)
        {
            var aulas = await _aulaRepository.ListarAsync(status, turmaId, alunoId, dataInicio, dataFim, cancellationToken, materiaId, horaInicioDe, horaInicioAte);
            // Uma leitura do relogio para a lista inteira: todas as aulas usam o mesmo instante.
            var agora = _relogio.AgoraLocal();
            return aulas.Select(a => MapearEm(a, agora)).ToList();
        }

        public async Task<AulaResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var aula = await _aulaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aula nao encontrada.");

            return await MapearComCobrancaAsync(aula, cancellationToken);
        }

        public async Task<AulaResponse> CadastrarAsync(int professorId, AulaRequest request, CancellationToken cancellationToken = default)
        {
            await ValidarReferenciasAsync(request.MateriaId, request.TurmaId, request.AlunoId, cancellationToken);
            await ValidarConflitoAsync(professorId, request, ignorarAulaId: null, cancellationToken);

            // specs/064: toda aula nasce "Agendada", inclusive a de data passada. A chamada so se faz
            // nos detalhes da aula, depois do inicio (RegistrarSessaoAsync); criar nao registra
            // frequencia, nao atualiza a frequencia dos alunos e nao gera cobranca.
            var aula = NovaAulaAgendada(professorId, request.Descricao, request.MateriaId, request.TurmaId, request.DataInicio, request.HoraInicio, request.HoraFim);

            await _aulaRepository.AdicionarAsync(aula, cancellationToken);
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            var alunoIds = await ObterAlunoIdsDaNovaAulaAsync(request.TurmaId, request.AlunoId, cancellationToken);

            await _aulaRepository.DefinirAlunosAsync(aula.Id, alunoIds, cancellationToken);
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            // Estoria 5: a aula nova muda a "proxima aula" da turma, entao os
            // lembretes precisam recalcular.
            if (request.TurmaId.HasValue)
            {
                await _lembreteService.RecalcularAsync(request.TurmaId.Value, cancellationToken);
            }

            // Recarrega com Materia e AulaAlunos.Aluno para montar a resposta.
            var aulaCompleta = (await _aulaRepository.ObterPorIdAsync(aula.Id, cancellationToken))!;

            return Mapear(aulaCompleta);
        }

        // specs/077: previa da serie. Calcula as datas e marca as que conflitam; nao grava nada.
        public async Task<PreviaSerieResponse> PreVisualizarSerieAsync(int professorId, RecorrenciaAulaRequest request, CancellationToken cancellationToken = default)
        {
            var avaliadas = await AvaliarSerieAsync(professorId, request, cancellationToken);
            var conflitos = avaliadas.Count(a => a.Conflito);

            return new PreviaSerieResponse
            {
                Total = avaliadas.Count,
                Limite = GeradorSerieAulas.LimitePadrao,
                Conflitos = conflitos,
                PodeCriar = conflitos == 0,
                Aulas = avaliadas
                    .Select(a => new OcorrenciaSerieResponse
                    {
                        Data = a.Ocorrencia.Data,
                        DiaSemana = (int)a.Ocorrencia.DiaSemana,
                        HoraInicio = a.Ocorrencia.HoraInicio,
                        HoraFim = a.Ocorrencia.HoraFim,
                        Conflito = a.Conflito,
                    })
                    .ToList(),
            };
        }

        // specs/077: cria a serie inteira ou nada. Qualquer data em conflito recusa tudo (FR-005); as aulas ja vao com
        // os alunos preenchidos e UM unico salvamento as grava (o EF usa uma transacao so), entao nao ha serie pela metade.
        // Cada aula e uma aula comum "Agendada" (specs/064), sem vinculo com as demais.
        public async Task<SerieCriadaResponse> CadastrarSerieAsync(int professorId, RecorrenciaAulaRequest request, CancellationToken cancellationToken = default)
        {
            var avaliadas = await AvaliarSerieAsync(professorId, request, cancellationToken);

            var emConflito = avaliadas.Where(a => a.Conflito).Select(a => a.Ocorrencia).ToList();
            if (emConflito.Count > 0)
            {
                throw new ConflitoException(MensagemDeConflitoDaSerie(emConflito));
            }

            var alunoIds = await ObterAlunoIdsDaNovaAulaAsync(request.TurmaId, request.AlunoId, cancellationToken);

            var novas = new List<Aula>();
            foreach (var (o, _) in avaliadas)
            {
                var aula = NovaAulaAgendada(professorId, request.Descricao, request.MateriaId, request.TurmaId, o.Data, o.HoraInicio, o.HoraFim);
                foreach (var alunoId in alunoIds)
                {
                    aula.AulaAlunos.Add(new AulaAluno { AlunoId = alunoId });
                }

                await _aulaRepository.AdicionarAsync(aula, cancellationToken);
                novas.Add(aula);
            }

            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            // As aulas novas mudam a "proxima aula" da turma: recalcula os lembretes uma vez so.
            if (request.TurmaId.HasValue)
            {
                await _lembreteService.RecalcularAsync(request.TurmaId.Value, cancellationToken);
            }

            // Recarrega cada aula com Materia e AulaAlunos.Aluno para montar a resposta (como o cadastro unico).
            var respostas = new List<AulaResponse>();
            foreach (var aula in novas)
            {
                respostas.Add(Mapear((await _aulaRepository.ObterPorIdAsync(aula.Id, cancellationToken))!));
            }

            return new SerieCriadaResponse { Quantidade = respostas.Count, Aulas = respostas };
        }

        // Regras da serie que dependem do relogio e do banco: so de hoje em diante, referencias existentes, no maximo
        // 60 aulas e conflito por data. As regras de formato ficam no RecorrenciaAulaRequestValidator.
        private async Task<List<(OcorrenciaSerie Ocorrencia, bool Conflito)>> AvaliarSerieAsync(
            int professorId, RecorrenciaAulaRequest request, CancellationToken cancellationToken)
        {
            if (request.DataInicial < DateOnly.FromDateTime(_relogio.AgoraLocal()))
            {
                throw new ConflitoException("A serie so pode comecar de hoje em diante.");
            }

            await ValidarReferenciasAsync(request.MateriaId, request.TurmaId, request.AlunoId, cancellationToken);

            var gerado = GeradorSerieAulas.Gerar(request.DataInicial, request.Horarios, request.QuantidadeAulas, request.DataFinal);
            if (gerado.Excedeu)
            {
                throw new ConflitoException($"A serie teria mais de {GeradorSerieAulas.LimitePadrao} aulas (maximo {GeradorSerieAulas.LimitePadrao}). Reduza o termino.");
            }

            if (gerado.Ocorrencias.Count == 0)
            {
                throw new ConflitoException("Nenhuma aula cai nos dias e no periodo informados.");
            }

            var avaliadas = new List<(OcorrenciaSerie Ocorrencia, bool Conflito)>();
            foreach (var o in gerado.Ocorrencias)
            {
                var conflito = await _aulaRepository.ExisteConflitoAsync(professorId, o.Data, o.HoraInicio, o.HoraFim, null, cancellationToken);
                avaliadas.Add((o, conflito));
            }

            return avaliadas;
        }

        // Lista ate 10 datas para a professora saber o que ajustar; o resto vira "e mais N".
        private static string MensagemDeConflitoDaSerie(IReadOnlyList<OcorrenciaSerie> emConflito)
        {
            const int maximoListado = 10;
            var datas = string.Join(", ", emConflito.Take(maximoListado).Select(o =>
                $"{o.Data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} {o.HoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture)}"));
            var resto = emConflito.Count > maximoListado ? $" e mais {emConflito.Count - maximoListado}" : string.Empty;

            return $"Ja existe aula nesse horario em: {datas}{resto}. Nenhuma aula foi criada.";
        }

        // specs/064: toda aula nasce "Agendada", inclusive a de data passada; criar nao registra frequencia nem cobra.
        private static Aula NovaAulaAgendada(int professorId, string? descricao, int materiaId, int? turmaId, DateOnly data, TimeOnly horaInicio, TimeOnly horaFim) => new()
        {
            Descricao = descricao,
            MateriaId = materiaId,
            ProfessorId = professorId,
            TurmaId = turmaId,
            DataInicio = data,
            HoraInicio = horaInicio,
            HoraFim = horaFim,
            Status = "Agendada",
            Ativo = true,
        };

        // Aula de turma: os alunos ativos da turma naquele momento; aula individual: o aluno escolhido.
        private async Task<List<int>> ObterAlunoIdsDaNovaAulaAsync(int? turmaId, int? alunoId, CancellationToken cancellationToken) =>
            turmaId.HasValue
                ? await _aulaRepository.ObterAlunosAtivosDaTurmaAsync(turmaId.Value, cancellationToken)
                : new List<int> { alunoId!.Value };

        public async Task<AulaResponse> AtualizarAsync(int id, AulaRequest request, CancellationToken cancellationToken = default)
        {
            var aula = await _aulaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aula nao encontrada.");

            if (aula.Status == "Realizada")
            {
                return await AtualizarDescricaoDeAulaRealizadaAsync(aula, request, cancellationToken);
            }

            var turmaIdAntiga = aula.TurmaId;

            await ValidarReferenciasAsync(request.MateriaId, request.TurmaId, request.AlunoId, cancellationToken);
            await ValidarConflitoAsync(aula.ProfessorId, request, ignorarAulaId: id, cancellationToken);

            aula.Descricao = request.Descricao;
            aula.MateriaId = request.MateriaId;
            aula.TurmaId = request.TurmaId;
            aula.DataInicio = request.DataInicio;
            aula.HoraInicio = request.HoraInicio;
            aula.HoraFim = request.HoraFim;

            // Recalcula os alunos vinculados (turma inteira ou o aluno individual
            // informado). specs/056: alunos de reposicao sobrevivem a edicao --
            // exceto se a aula virou individual, que nao aceita extras (FR-017).
            // Um aluno de reposicao que agora e matriculado na turma so perde a
            // marca de reposicao, sem ser removido e recriado (nem duplicado).
            var matriculados = request.TurmaId.HasValue
                ? await _aulaRepository.ObterAlunosAtivosDaTurmaAsync(request.TurmaId.Value, cancellationToken)
                : new List<int> { request.AlunoId!.Value };

            foreach (var participante in aula.AulaAlunos.ToList())
            {
                if (!participante.Reposicao || !request.TurmaId.HasValue)
                {
                    aula.AulaAlunos.Remove(participante);
                }
                else if (matriculados.Contains(participante.AlunoId))
                {
                    participante.Reposicao = false;
                }
            }

            // A remocao precisa ir ao banco antes de recriar os matriculados:
            // (aula_id, aluno_id) e a chave da tabela.
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            // specs/067: o link acompanha o novo fim da aula (se o novo fim ja passou, e descartado).
            if (_links?.Obter(id) is { } linkAtual)
            {
                _links.Definir(id, linkAtual, RegraLinkAula.FimDaAula(aula));
            }

            var jaParticipam = aula.AulaAlunos.Select(aa => aa.AlunoId).ToHashSet();
            await _aulaRepository.DefinirAlunosAsync(aula.Id, matriculados.Where(alunoId => !jaParticipam.Contains(alunoId)), cancellationToken);
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            // Recalcula a turma nova e, se mudou, tambem a antiga (a "proxima
            // aula" dela pode ter sido justamente esta que saiu).
            if (request.TurmaId.HasValue)
            {
                await _lembreteService.RecalcularAsync(request.TurmaId.Value, cancellationToken);
            }
            if (turmaIdAntiga.HasValue && turmaIdAntiga != request.TurmaId)
            {
                await _lembreteService.RecalcularAsync(turmaIdAntiga.Value, cancellationToken);
            }

            var aulaCompleta = await _aulaRepository.ObterPorIdAsync(id, cancellationToken);
            return await MapearComCobrancaAsync(aulaCompleta!, cancellationToken);
        }

        // specs/056 (reposicao): a professora coloca um aluno de fora da turma numa
        // aula ainda agendada. Ele participa como os demais (presenca e cobranca
        // pelos vinculos dele), mas fica marcado como reposicao.
        public async Task<AulaResponse> AdicionarParticipanteAsync(int aulaId, int alunoId, CancellationToken cancellationToken = default)
        {
            var aula = await ObterAgendadaAsync(aulaId, cancellationToken);

            if (!aula.TurmaId.HasValue)
            {
                throw new ConflitoException("Aula individual nao aceita alunos de reposicao.");
            }

            var aluno = await _alunoRepository.ObterPorIdAsync(alunoId, cancellationToken)
                ?? throw new NaoEncontradoException("Aluno nao encontrado.");

            if (!aluno.Ativo)
            {
                throw new ConflitoException("O aluno esta inativo.");
            }

            if (aula.AulaAlunos.Any(aa => aa.AlunoId == alunoId))
            {
                throw new ConflitoException("O aluno ja participa desta aula.");
            }

            aula.AulaAlunos.Add(new AulaAluno { AulaId = aula.Id, AlunoId = alunoId, Aluno = aluno, Reposicao = true });
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            var aulaCompleta = await _aulaRepository.ObterPorIdAsync(aulaId, cancellationToken);
            return await MapearComCobrancaAsync(aulaCompleta!, cancellationToken);
        }

        // Remove so participantes de reposicao: os matriculados saem da aula pela
        // turma, nunca individualmente (FR-016).
        public async Task<AulaResponse> RemoverParticipanteAsync(int aulaId, int alunoId, CancellationToken cancellationToken = default)
        {
            var aula = await ObterAgendadaAsync(aulaId, cancellationToken);

            var participante = aula.AulaAlunos.FirstOrDefault(aa => aa.AlunoId == alunoId)
                ?? throw new ConflitoException("O aluno nao participa desta aula.");

            if (!participante.Reposicao)
            {
                throw new ConflitoException("So alunos de reposicao podem ser removidos da aula; este aluno e matriculado na turma.");
            }

            aula.AulaAlunos.Remove(participante);
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            var aulaCompleta = await _aulaRepository.ObterPorIdAsync(aulaId, cancellationToken);
            return await MapearComCobrancaAsync(aulaCompleta!, cancellationToken);
        }

        private async Task<Aula> ObterAgendadaAsync(int aulaId, CancellationToken cancellationToken)
        {
            var aula = await _aulaRepository.ObterPorIdAsync(aulaId, cancellationToken);
            if (aula is null || !aula.Ativo)
            {
                throw new NaoEncontradoException("Aula nao encontrada.");
            }

            if (aula.Status != "Agendada")
            {
                throw new ConflitoException("Somente aulas agendadas aceitam mudanca de participantes.");
            }

            return aula;
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var aula = await _aulaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aula nao encontrada.");

            aula.Ativo = false;
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);
            _links?.Remover(id);

            if (aula.TurmaId.HasValue)
            {
                // Mesmo motivo de CancelarAsync (Estoria 5): uma aula excluida
                // tambem sai da busca por "proxima aula" (ObterProximaAulaDaTurmaAsync
                // filtra por Ativo), entao os lembretes da turma precisam recalcular.
                await _lembreteService.RecalcularAsync(aula.TurmaId.Value, cancellationToken);
            }
        }

        public async Task<AulaResponse> RegistrarSessaoAsync(int id, RegistrarSessaoRequest request, CancellationToken cancellationToken = default)
        {
            var aula = await _aulaRepository.ObterPorIdAsync(id, cancellationToken);

            // Aula excluida nao tem chamada (specs/064): responde como inexistente.
            if (aula is null || !aula.Ativo)
            {
                throw new NaoEncontradoException("Aula nao encontrada.");
            }

            if (aula.Status != "Agendada")
            {
                throw new ConflitoException("Somente aulas com status 'Agendada' podem ter a sessao registrada.");
            }

            // specs/064 (FR-006): o servidor decide, no fuso de Sao Paulo, se a aula ja comecou --
            // mesmo que a tela seja burlada. Nada e gravado antes desta recusa.
            if (!RegraChamada.Liberada(aula, _relogio.AgoraLocal()))
            {
                throw new ConflitoException(RegraChamada.MensagemBloqueio(aula));
            }

            var avisos = await AplicarPresencasAsync(aula, request.Presencas, cancellationToken);

            var resposta = await MapearComCobrancaAsync(aula, cancellationToken);
            resposta.Avisos = avisos;
            return resposta;
        }

        // Unico ponto que transforma as presencas informadas em uma aula
        // "Realizada" (specs/055, research.md R1). Desde a spec 064 so
        // RegistrarSessaoAsync o chama: a criacao da aula nunca registra frequencia. Grava
        // tambem quando a chamada foi feita (FrequenciaRegistradaEm). Devolve os avisos a
        // mostrar para a professora.
        private async Task<List<string>> AplicarPresencasAsync(Aula aula, Dictionary<int, bool> presencas, CancellationToken cancellationToken)
        {
            var avisos = new List<string>();

            // Decisao de cobranca de cada aluno presente (specs/056), tomada uma vez
            // aqui e so executada depois, em GerarContasAReceberAsync.
            var decisoes = new Dictionary<int, DecisaoCobranca>();

            foreach (var aulaAluno in aula.AulaAlunos)
            {
                if (!presencas.TryGetValue(aulaAluno.AlunoId, out var presente))
                {
                    throw new ConflitoException($"Presenca do aluno {aulaAluno.AlunoId} nao foi informada.");
                }

                // Falta nunca cobra nem consome pacote.
                if (!presente)
                {
                    aulaAluno.Presente = false;
                    continue;
                }

                var vinculosAtivos = await _vinculoCobrancaRepository.ListarPorAlunoAsync(aulaAluno.AlunoId, ativo: true, cancellationToken);
                var decisao = RegraCobrancaPresenca.Decidir(vinculosAtivos, aula.TurmaId, aula.DataInicio);

                // specs/041: pacote esgotado sem Avulsa para cair barra so este aluno --
                // a aula continua Realizada para os demais. O motivo fica gravado para
                // distinguir de uma falta comum no historico.
                if (decisao.Tipo == TipoCobrancaPresenca.Bloqueada)
                {
                    aulaAluno.Presente = false;
                    aulaAluno.MotivoNaoRegistro = AulaAluno.MotivoPacoteEsgotado;
                    avisos.Add($"Pacote esgotado: {aulaAluno.Aluno.Nome} não teve a presença registrada. Renove o pacote (edite o Vínculo de Cobrança) para liberar novos registros.");
                    continue;
                }

                if (decisao.PacoteEsgotado)
                {
                    avisos.Add($"Pacote esgotado: {aulaAluno.Aluno.Nome} foi cobrado como aula avulsa. Renove o pacote para voltar a usar o saldo.");
                }

                aulaAluno.Presente = true;
                aulaAluno.Aluno.Frequencia += 1;
                decisoes[aulaAluno.AlunoId] = decisao;
            }

            aula.Status = "Realizada";
            aula.FrequenciaRegistradaEm = _relogio.AgoraLocal();
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            if (aula.TurmaId.HasValue)
            {
                // Mesmo motivo de CancelarAsync/ExcluirAsync (Estoria 5): uma aula
                // Realizada tambem sai da busca por "proxima aula" -- se a turma
                // ja tinha outra aula futura agendada, o lembrete precisa apontar
                // para ela em vez de continuar "Enviado" para a aula que acabou de
                // acontecer.
                await _lembreteService.RecalcularAsync(aula.TurmaId.Value, cancellationToken);
            }

            await GerarContasAReceberAsync(aula, decisoes, cancellationToken);

            return avisos;
        }

        // specs/055 (US2): de uma aula ja Realizada, so a Descricao pode mudar (para
        // anotar o que de fato foi lecionado). Qualquer outro campo diferente do
        // atual continua recusado, como antes desta feature. Como nada de
        // data/horario/turma muda aqui, nao ha conflito de horario, lista de alunos
        // ou lembrete a recalcular.
        private async Task<AulaResponse> AtualizarDescricaoDeAulaRealizadaAsync(Aula aula, AulaRequest request, CancellationToken cancellationToken)
        {
            // Aula individual nao tem turma: o aluno dela e o unico participante.
            var tentaMudarOutroCampo = request.MateriaId != aula.MateriaId
                || request.TurmaId != aula.TurmaId
                || (!aula.TurmaId.HasValue && request.AlunoId != aula.AulaAlunos.FirstOrDefault()?.AlunoId)
                || request.DataInicio != aula.DataInicio
                || request.HoraInicio != aula.HoraInicio
                || request.HoraFim != aula.HoraFim;

            if (tentaMudarOutroCampo)
            {
                throw new ConflitoException("Uma aula ja realizada so permite editar a descricao.");
            }

            aula.Descricao = request.Descricao;
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);
            return await MapearComCobrancaAsync(aula, cancellationToken);
        }

        // Executa o efeito financeiro ja decidido (RegraCobrancaPresenca) para cada
        // presenca confirmada. Decisao de negocio da evolucao do Financeiro (Sprint 4):
        // a cobranca nasce Pendente, com forma de pagamento em aberto ate o recebimento.
        private async Task GerarContasAReceberAsync(Aula aula, IReadOnlyDictionary<int, DecisaoCobranca> decisoes, CancellationToken cancellationToken)
        {
            var categoriaNome = aula.TurmaId.HasValue ? "Aula em turma" : "Aula particular";
            var categoria = await _categoriaReceitaRepository.ObterPorNomeAsync(categoriaNome, cancellationToken);
            var competencia = new DateOnly(aula.DataInicio.Year, aula.DataInicio.Month, 1);

            foreach (var aulaAluno in aula.AulaAlunos.Where(v => decisoes.ContainsKey(v.AlunoId)))
            {
                var decisao = decisoes[aulaAluno.AlunoId];

                // specs/052: grava qual vinculo foi aplicado nesta presenca (auditoria
                // de aulas dadas por vinculo); nulo quando cobrada pelo Valor da aula.
                aulaAluno.VinculoCobrancaId = decisao.Vinculo?.Id;

                switch (decisao.Tipo)
                {
                    case TipoCobrancaPresenca.SemVinculo:
                    case TipoCobrancaPresenca.Avulsa:
                        // Mesma cobranca nos dois casos -- so muda a fonte do valor.
                        var pagamento = new Pagamento
                        {
                            AlunoId = aulaAluno.AlunoId,
                            Descricao = $"{aula.Materia.Nome} - {aula.DataInicio:dd/MM/yyyy}",
                            CategoriaReceitaId = categoria?.Id,
                            DataVencimento = aula.DataInicio.AddDays(5),
                            Competencia = competencia,
                            ValorFinal = decisao.Vinculo?.Valor ?? aulaAluno.Aluno.ValorAula,
                            Status = "Pendente"
                        };

                        await _pagamentoRepository.AdicionarAsync(pagamento, cancellationToken);
                        await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
                        await _pagamentoRepository.VincularAulaAsync(pagamento.Id, aula.Id, cancellationToken);
                        break;

                    case TipoCobrancaPresenca.PlanoPeriodico:
                        // O plano ja cobre a aula: a Mensalidade e cobrada todo mes pelo job (specs/039) e o
                        // Semestral/Anual, pelas parcelas geradas no cadastro (specs/062). A presenca so fica ligada ao plano.
                        break;

                    case TipoCobrancaPresenca.Pacote:
                        // Consome uma aula do saldo; um saldo nunca informado (nulo) fica como esta.
                        if (decisao.Vinculo!.SaldoAulas is > 0)
                        {
                            decisao.Vinculo.SaldoAulas -= 1;
                            await _vinculoCobrancaRepository.SalvarAlteracoesAsync(cancellationToken);
                        }
                        break;
                }
            }

            await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        public async Task<AulaResponse> CancelarAsync(int id, CancellationToken cancellationToken = default)
        {
            var aula = await _aulaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aula nao encontrada.");

            if (aula.Status == "Realizada")
            {
                throw new ConflitoException("Uma aula ja realizada nao pode ser cancelada.");
            }

            aula.Status = "Cancelada";
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);
            _links?.Remover(id);

            if (aula.TurmaId.HasValue)
            {
                // Se a aula cancelada era a "proxima aula" da turma, a hora
                // programada dos lembretes precisa apontar para a seguinte.
                await _lembreteService.RecalcularAsync(aula.TurmaId.Value, cancellationToken);
            }

            return await MapearComCobrancaAsync(aula, cancellationToken);
        }

        // specs/067: so aula ativa, nao cancelada e que ainda nao terminou aceita link; a regra e uma so
        // (RegraLinkAula) e vale tambem para a resposta (AceitaLinkAula). O link nunca e gravado em banco.
        public async Task<AulaResponse> DefinirLinkAsync(int id, string link, CancellationToken cancellationToken = default)
        {
            var aula = await ObterAulaAtivaAsync(id, cancellationToken);
            var agora = _relogio.AgoraLocal();

            if (!RegraLinkAula.AceitaLink(aula, agora))
            {
                throw new ConflitoException("Esta aula nao aceita link: ja terminou ou foi cancelada.");
            }

            var links = _links ?? throw new InvalidOperationException("Armazenamento temporario de links nao configurado.");
            links.Definir(id, link, RegraLinkAula.FimDaAula(aula));
            var resposta = MapearEm(aula, agora);
            await PreencherCobrancaAsync(resposta, aula, cancellationToken);
            return resposta;
        }

        public async Task<AulaResponse> RemoverLinkAsync(int id, CancellationToken cancellationToken = default)
        {
            var aula = await ObterAulaAtivaAsync(id, cancellationToken);

            _links?.Remover(id);
            return await MapearComCobrancaAsync(aula, cancellationToken);
        }

        private async Task<Aula> ObterAulaAtivaAsync(int id, CancellationToken cancellationToken)
        {
            var aula = await _aulaRepository.ObterPorIdAsync(id, cancellationToken);
            return aula is { Ativo: true } ? aula : throw new NaoEncontradoException("Aula nao encontrada.");
        }

        private async Task ValidarReferenciasAsync(int materiaId, int? turmaId, int? alunoId, CancellationToken cancellationToken)
        {
            if (await _materiaRepository.ObterPorIdAsync(materiaId, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Materia nao encontrada.");
            }

            if (turmaId.HasValue && await _turmaRepository.ObterPorIdAsync(turmaId.Value, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Turma nao encontrada.");
            }

            if (alunoId.HasValue && await _alunoRepository.ObterPorIdAsync(alunoId.Value, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Aluno nao encontrado.");
            }
        }

        private async Task ValidarConflitoAsync(int professorId, AulaRequest request, int? ignorarAulaId, CancellationToken cancellationToken)
        {
            var conflito = await _aulaRepository.ExisteConflitoAsync(
                professorId, request.DataInicio, request.HoraInicio, request.HoraFim, ignorarAulaId, cancellationToken);

            if (conflito)
            {
                throw new ConflitoException("Ja existe uma aula cadastrada nesse horario.");
            }
        }

        // A resposta informa se a chamada esta liberada, o que depende do relogio do servidor
        // (specs/064): quem mapeia uma aula so le o relogio uma vez.
        private AulaResponse Mapear(Aula aula) => MapearEm(aula, _relogio.AgoraLocal());

        // specs/068: resposta de UMA aula com o selo de cobranca de cada aluno -- o vinculo que vale para esta
        // aula (turma da aula; aula individual = aulas individuais), decidido pela mesma regra que cobra a
        // presenca. A lista de aulas nao passa por aqui (continua leve, sem consulta extra).
        private async Task<AulaResponse> MapearComCobrancaAsync(Aula aula, CancellationToken cancellationToken)
        {
            var resposta = Mapear(aula);
            await PreencherCobrancaAsync(resposta, aula, cancellationToken);
            return resposta;
        }

        private async Task PreencherCobrancaAsync(AulaResponse resposta, Aula aula, CancellationToken cancellationToken)
        {
            if (_cobranca is null || aula.AulaAlunos.Count == 0)
            {
                return;
            }

            var selos = await _cobranca.ResumirParaGrupoAsync(aula.AulaAlunos.Select(aa => aa.Aluno).ToList(), aula.TurmaId, aula.DataInicio, cancellationToken);
            foreach (var aluno in resposta.Alunos)
            {
                aluno.Cobranca = selos.GetValueOrDefault(aluno.AlunoId);
            }
        }

        private AulaResponse MapearEm(Aula aula, DateTime agora)
        {
            var aceitaLink = RegraLinkAula.AceitaLink(aula, agora);

            return new()
            {
                ChamadaLiberada = RegraChamada.Liberada(aula, agora),
                SegundosParaChamada = RegraChamada.SegundosParaLiberar(aula, agora),
                // specs/067: o link so e devolvido enquanto a aula o aceita (ativa, nao cancelada, nao terminou).
                AceitaLinkAula = aceitaLink,
                LinkAula = aceitaLink ? _links?.Obter(aula.Id) : null,
                Id = aula.Id,
                Descricao = aula.Descricao,
                MateriaId = aula.MateriaId,
                MateriaNome = aula.Materia.Nome,
                ProfessorId = aula.ProfessorId,
                TurmaId = aula.TurmaId,
                TurmaNome = aula.Turma?.Nome,
                DataInicio = aula.DataInicio,
                HoraInicio = aula.HoraInicio,
                HoraFim = aula.HoraFim,
                Status = aula.Status,
                Ativo = aula.Ativo,
                Alunos = aula.AulaAlunos.Select(aa => new AlunoPresencaResponse
                {
                    AlunoId = aa.AlunoId,
                    Nome = aa.Aluno.Nome,
                    Ra = aa.Aluno.Ra,
                    Presente = aa.Presente,
                    MotivoNaoRegistro = aa.MotivoNaoRegistro,
                    Reposicao = aa.Reposicao
                }).ToList()
            };
        }
    }
}
