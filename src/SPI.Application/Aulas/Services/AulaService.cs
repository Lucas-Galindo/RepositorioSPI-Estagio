using SPI.Application.Aulas.Dtos;
using SPI.Application.Lembretes.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
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

        public AulaService(
            IAulaRepository aulaRepository,
            IAlunoRepository alunoRepository,
            ITurmaRepository turmaRepository,
            IMateriaRepository materiaRepository,
            ILembreteService lembreteService,
            IPagamentoRepository pagamentoRepository,
            ICategoriaReceitaRepository categoriaReceitaRepository,
            IVinculoCobrancaRepository vinculoCobrancaRepository)
        {
            _aulaRepository = aulaRepository;
            _alunoRepository = alunoRepository;
            _turmaRepository = turmaRepository;
            _materiaRepository = materiaRepository;
            _lembreteService = lembreteService;
            _pagamentoRepository = pagamentoRepository;
            _categoriaReceitaRepository = categoriaReceitaRepository;
            _vinculoCobrancaRepository = vinculoCobrancaRepository;
        }

        public async Task<List<AulaResponse>> ListarAsync(
            string? status,
            int? turmaId,
            int? alunoId,
            DateOnly? dataInicio,
            DateOnly? dataFim,
            CancellationToken cancellationToken = default)
        {
            var aulas = await _aulaRepository.ListarAsync(status, turmaId, alunoId, dataInicio, dataFim, cancellationToken);
            return aulas.Select(Mapear).ToList();
        }

        public async Task<AulaResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var aula = await _aulaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aula nao encontrada.");

            return Mapear(aula);
        }

        public async Task<AulaResponse> CadastrarAsync(int professorId, AulaRequest request, CancellationToken cancellationToken = default)
        {
            await ValidarReferenciasAsync(request, cancellationToken);
            await ValidarConflitoAsync(professorId, request, ignorarAulaId: null, cancellationToken);

            // specs/055: aula com data de hoje ou do passado e um lancamento
            // retroativo -- ela ja nasce "Realizada", com as presencas informadas
            // no mesmo formulario. Compara so a data (sem a hora), como decidido
            // na spec: uma aula marcada pra mais tarde hoje ja conta como retroativa.
            var retroativa = request.DataInicio <= DateOnly.FromDateTime(DateTime.Now);
            if (retroativa && request.Presencas is null)
            {
                throw new ConflitoException("Informe a presenca de cada aluno para criar uma aula retroativa.");
            }

            var aula = new Aula
            {
                Descricao = request.Descricao,
                MateriaId = request.MateriaId,
                ProfessorId = professorId,
                TurmaId = request.TurmaId,
                DataInicio = request.DataInicio,
                HoraInicio = request.HoraInicio,
                HoraFim = request.HoraFim,
                Status = "Agendada",
                Ativo = true
            };

            await _aulaRepository.AdicionarAsync(aula, cancellationToken);
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            var alunoIds = request.TurmaId.HasValue
                ? await _aulaRepository.ObterAlunosAtivosDaTurmaAsync(request.TurmaId.Value, cancellationToken)
                : new List<int> { request.AlunoId!.Value };

            await _aulaRepository.DefinirAlunosAsync(aula.Id, alunoIds, cancellationToken);
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            // Estoria 5: a aula nova muda a "proxima aula" da turma, entao os
            // lembretes precisam recalcular. Na aula retroativa isso ja acontece
            // dentro de AplicarPresencasAsync -- por isso aqui so no caso futuro,
            // para nunca recalcular duas vezes.
            if (!retroativa && request.TurmaId.HasValue)
            {
                await _lembreteService.RecalcularAsync(request.TurmaId.Value, cancellationToken);
            }

            // Recarrega com Materia e AulaAlunos.Aluno: AplicarPresencasAsync usa
            // esses dados (nome do aluno no aviso, frequencia, descricao da cobranca).
            var aulaCompleta = (await _aulaRepository.ObterPorIdAsync(aula.Id, cancellationToken))!;

            var avisos = retroativa
                ? await AplicarPresencasAsync(aulaCompleta, request.Presencas!, cancellationToken)
                : new List<string>();

            var resposta = Mapear(aulaCompleta);
            resposta.Avisos = avisos;
            return resposta;
        }

        public async Task<AulaResponse> AtualizarAsync(int id, AulaRequest request, CancellationToken cancellationToken = default)
        {
            var aula = await _aulaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aula nao encontrada.");

            if (aula.Status == "Realizada")
            {
                return await AtualizarDescricaoDeAulaRealizadaAsync(aula, request, cancellationToken);
            }

            var turmaIdAntiga = aula.TurmaId;

            await ValidarReferenciasAsync(request, cancellationToken);
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
            return Mapear(aulaCompleta!);
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
            return Mapear(aulaCompleta!);
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
            return Mapear(aulaCompleta!);
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
            var aula = await _aulaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aula nao encontrada.");

            if (aula.Status != "Agendada")
            {
                throw new ConflitoException("Somente aulas com status 'Agendada' podem ter a sessao registrada.");
            }

            var avisos = await AplicarPresencasAsync(aula, request.Presencas, cancellationToken);

            var resposta = Mapear(aula);
            resposta.Avisos = avisos;
            return resposta;
        }

        // Unico ponto que transforma as presencas informadas em uma aula
        // "Realizada" (specs/055, research.md R1). Usado por RegistrarSessaoAsync
        // (aula que ja existia) e por CadastrarAsync (aula retroativa criada ja
        // realizada), para a regra de presenca e cobranca nunca divergir entre os
        // dois caminhos. Devolve os avisos a mostrar para a professora.
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
                var decisao = RegraCobrancaPresenca.Decidir(vinculosAtivos, aula.TurmaId);

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
            return Mapear(aula);
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

                    case TipoCobrancaPresenca.Mensalidade:
                        // A mensalidade ja cobre a aula; a cobranca mensal e do job (specs/039).
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

            if (aula.TurmaId.HasValue)
            {
                // Se a aula cancelada era a "proxima aula" da turma, a hora
                // programada dos lembretes precisa apontar para a seguinte.
                await _lembreteService.RecalcularAsync(aula.TurmaId.Value, cancellationToken);
            }

            return Mapear(aula);
        }

        private async Task ValidarReferenciasAsync(AulaRequest request, CancellationToken cancellationToken)
        {
            if (await _materiaRepository.ObterPorIdAsync(request.MateriaId, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Materia nao encontrada.");
            }

            if (request.TurmaId.HasValue && await _turmaRepository.ObterPorIdAsync(request.TurmaId.Value, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Turma nao encontrada.");
            }

            if (request.AlunoId.HasValue && await _alunoRepository.ObterPorIdAsync(request.AlunoId.Value, cancellationToken) is null)
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

        private static AulaResponse Mapear(Aula aula) => new()
        {
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
