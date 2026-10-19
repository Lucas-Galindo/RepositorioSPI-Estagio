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

            // Recalcula os alunos vinculados: remove os antigos e define os novos
            // (turma inteira ou o aluno individual informado).
            foreach (var vinculo in aula.AulaAlunos.ToList())
            {
                aula.AulaAlunos.Remove(vinculo);
            }

            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            var alunoIds = request.TurmaId.HasValue
                ? await _aulaRepository.ObterAlunosAtivosDaTurmaAsync(request.TurmaId.Value, cancellationToken)
                : new List<int> { request.AlunoId!.Value };

            await _aulaRepository.DefinirAlunosAsync(aula.Id, alunoIds, cancellationToken);
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

            foreach (var aulaAluno in aula.AulaAlunos)
            {
                if (!presencas.TryGetValue(aulaAluno.AlunoId, out var presente))
                {
                    throw new ConflitoException($"Presenca do aluno {aulaAluno.AlunoId} nao foi informada.");
                }

                // specs/041 (US2/FR-005/FR-006/FR-012): pacote esgotado (saldo 0) no
                // contexto desta aula barra so este aluno -- a aula continua Realizada
                // normalmente para os demais (clarificacao 2026-09-23, opcao A). O
                // motivo e' persistido (MotivoNaoRegistro) para distinguir de falta
                // comum no historico; nunca lanca excecao (FR-005 nao recusa a aula).
                if (presente)
                {
                    var vinculoCobranca = await _vinculoCobrancaRepository.ObterAtivoPorAlunoEContextoAsync(aulaAluno.AlunoId, aula.TurmaId, cancellationToken);
                    if (vinculoCobranca is { Modalidade: ModalidadeCobranca.Pacote, SaldoAulas: 0 })
                    {
                        aulaAluno.Presente = false;
                        aulaAluno.MotivoNaoRegistro = AulaAluno.MotivoPacoteEsgotado;
                        avisos.Add($"Pacote esgotado: {aulaAluno.Aluno.Nome} não teve a presença registrada. Renove o pacote (edite o Vínculo de Cobrança) para liberar novos registros.");
                        continue;
                    }
                }

                aulaAluno.Presente = presente;
                if (presente)
                {
                    aulaAluno.Aluno.Frequencia += 1;
                }
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

            // Decisao de negocio aprovada (evolucao do Financeiro, Sprint 4):
            // toda aula Realizada gera automaticamente uma Conta a Receber
            // pendente por aluno presente (auladas com falta nao geram cobranca).
            // Forma de pagamento fica em aberto ate o recebimento efetivo.
            await GerarContasAReceberAsync(aula, cancellationToken);

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

        private async Task GerarContasAReceberAsync(Aula aula, CancellationToken cancellationToken)
        {
            var categoriaNome = aula.TurmaId.HasValue ? "Aula em turma" : "Aula particular";
            var categoria = await _categoriaReceitaRepository.ObterPorNomeAsync(categoriaNome, cancellationToken);
            var competencia = new DateOnly(aula.DataInicio.Year, aula.DataInicio.Month, 1);

            foreach (var aulaAluno in aula.AulaAlunos.Where(v => v.Presente == true))
            {
                // specs/038: a modalidade do VinculoCobranca correspondente ao
                // contexto da aula (mesma Turma, ou nenhuma para atendimento
                // individual) decide o efeito financeiro desta presenca.
                var vinculoCobranca = await _vinculoCobrancaRepository.ObterAtivoPorAlunoEContextoAsync(
                    aulaAluno.AlunoId, aula.TurmaId, cancellationToken);

                // specs/052: registra qual vinculo (se algum) estava ativo no
                // momento desta presenca, para auditoria historica -- vale
                // para todas as modalidades, nao so Pacote. Nunca altera o
                // efeito financeiro abaixo (FR-012).
                aulaAluno.VinculoCobrancaId = vinculoCobranca?.Id;

                switch (vinculoCobranca?.Modalidade)
                {
                    case null:
                    case ModalidadeCobranca.Avulsa:
                        // FR-002 (sem vinculo) e FR-003 (Avulsa) geram a cobranca do
                        // mesmo jeito -- so a fonte do valor muda.
                        var pagamento = new Pagamento
                        {
                            AlunoId = aulaAluno.AlunoId,
                            Descricao = $"{aula.Materia.Nome} - {aula.DataInicio:dd/MM/yyyy}",
                            CategoriaReceitaId = categoria?.Id,
                            DataVencimento = aula.DataInicio.AddDays(5),
                            Competencia = competencia,
                            ValorFinal = vinculoCobranca?.Valor ?? aulaAluno.Aluno.ValorAula,
                            Status = "Pendente"
                        };

                        await _pagamentoRepository.AdicionarAsync(pagamento, cancellationToken);
                        await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
                        await _pagamentoRepository.VincularAulaAsync(pagamento.Id, aula.Id, cancellationToken);
                        break;

                    case ModalidadeCobranca.Mensalidade:
                        // FR-004/FR-009: nenhuma cobranca avulsa; a mensalidade em si
                        // fica para um job futuro, fora do escopo desta fatia.
                        break;

                    case ModalidadeCobranca.Pacote:
                        // FR-005/FR-006/FR-010: consome uma aula do saldo em vez de
                        // gerar cobranca; nunca decrementa abaixo de zero nem altera
                        // um saldo nunca informado (null).
                        if (vinculoCobranca.SaldoAulas is > 0)
                        {
                            vinculoCobranca.SaldoAulas -= 1;
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
                MotivoNaoRegistro = aa.MotivoNaoRegistro
            }).ToList()
        };
    }
}
