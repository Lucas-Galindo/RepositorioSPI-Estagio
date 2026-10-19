using SPI.Application.VinculosCobranca.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.VinculosCobranca.Services
{
    public class VinculoCobrancaService : IVinculoCobrancaService
    {
        private readonly IVinculoCobrancaRepository _vinculoRepository;
        private readonly IAlunoRepository _alunoRepository;
        private readonly ITurmaRepository _turmaRepository;
        private readonly IAulaRepository _aulaRepository;

        public VinculoCobrancaService(
            IVinculoCobrancaRepository vinculoRepository,
            IAlunoRepository alunoRepository,
            ITurmaRepository turmaRepository,
            IAulaRepository aulaRepository)
        {
            _vinculoRepository = vinculoRepository;
            _alunoRepository = alunoRepository;
            _turmaRepository = turmaRepository;
            _aulaRepository = aulaRepository;
        }

        public async Task<List<VinculoCobrancaResponse>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            var vinculos = await _vinculoRepository.ListarPorAlunoAsync(alunoId, ativo, cancellationToken);
            // specs/058: aulas dadas de todos os pacotes numa consulta so.
            var presencasPorPacote = await ContarPresencasDosPacotesAsync(vinculos, cancellationToken);
            var respostas = new List<VinculoCobrancaResponse>();
            // A Mensalidade ainda faz 1 consulta por vinculo (aulas do mes) --
            // aceitavel porque sao os vinculos de UM aluno so.
            foreach (var vinculo in vinculos)
            {
                respostas.Add(await MapearAsync(vinculo, presencasPorPacote, cancellationToken));
            }

            MarcarConflitosHerdados(respostas);
            return respostas;
        }

        // specs/056 (FR-020/FR-021): vinculos cadastrados na regra antiga podem formar
        // combinacoes hoje proibidas. Eles continuam valendo como estao (a cobranca
        // segue a prioridade de RegraCobrancaPresenca); aqui so sao sinalizados para a
        // professora decidir. Inativos nunca entram na conta.
        private static void MarcarConflitosHerdados(List<VinculoCobrancaResponse> respostas)
        {
            var ativos = respostas.Where(r => r.Ativo).ToList();

            if (ativos.Count > 1 && ativos.Any(r => r.Modalidade == ModalidadeCobranca.Mensalidade))
            {
                ativos.ForEach(r => r.Conflito = "Mensalidade nao pode coexistir com outros vinculos ativos.");
                return;
            }

            var pacotes = ativos.Where(r => r.Modalidade == ModalidadeCobranca.Pacote).ToList();
            if (pacotes.Count > 1)
            {
                pacotes.ForEach(r => r.Conflito = "Mais de um pacote ativo.");
            }
        }

        public async Task<VinculoCobrancaResponse> ObterPorIdAsync(int alunoId, int id, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            var vinculo = await ObterDoAlunoAsync(alunoId, id, cancellationToken);
            return await MapearAsync(vinculo, presencasPorPacote: null, cancellationToken);
        }

        public async Task<VinculoCobrancaResponse> CadastrarAsync(int alunoId, VinculoCobrancaRequest request, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            if (request.TurmaId.HasValue)
            {
                await ValidarTurmaAsync(alunoId, request.TurmaId.Value, cancellationToken);
            }

            await GarantirRegrasDeCombinacaoAsync(alunoId, request.Modalidade, request.TurmaId, ignorarId: null, cancellationToken);

            var vinculo = new VinculoCobranca
            {
                AlunoId = alunoId,
                TurmaId = request.TurmaId,
                Modalidade = request.Modalidade,
                Valor = request.Valor,
                AulasIncluidas = request.AulasIncluidas,
                // specs/058: pacote cadastrado so com o total comeca com o saldo cheio.
                SaldoAulas = request.Modalidade == ModalidadeCobranca.Pacote && request.SaldoAulas is null
                    ? request.TotalAulasPacote
                    : request.SaldoAulas,
                TotalAulasPacote = request.TotalAulasPacote,
                Ativo = true
            };

            await _vinculoRepository.AdicionarAsync(vinculo, cancellationToken);
            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            // Recarrega para trazer a navegacao Turma (nome) no response.
            var salvo = await _vinculoRepository.ObterPorIdAsync(vinculo.Id, cancellationToken);
            return await MapearAsync(salvo ?? vinculo, presencasPorPacote: null, cancellationToken);
        }

        public async Task<VinculoCobrancaResponse> AtualizarAsync(int alunoId, int id, VinculoCobrancaRequest request, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            var vinculo = await ObterDoAlunoAsync(alunoId, id, cancellationToken);

            if (!vinculo.Ativo)
            {
                throw new ConflitoException("Reative o vinculo antes de edita-lo.");
            }

            // Turma inalterada nao e revalidada (FR-013 so vale ao definir/alterar a turma):
            // editar o Valor de um vinculo cuja turma foi desativada depois continua possivel.
            if (request.TurmaId != vinculo.TurmaId && request.TurmaId.HasValue)
            {
                await ValidarTurmaAsync(alunoId, request.TurmaId.Value, cancellationToken);
            }

            // Sempre, e nao so quando a turma muda: a modalidade tambem pode mudar
            // (ex.: Avulsa virando Mensalidade com outros vinculos ativos).
            await GarantirRegrasDeCombinacaoAsync(alunoId, request.Modalidade, request.TurmaId, vinculo.Id, cancellationToken);

            // specs/052 (FR-007/FR-010): nunca permitir que o novo total/teto
            // fique menor que o que ja foi consumido. Nao recalcula SaldoAulas
            // automaticamente em nenhum caso (/speckit-analyze achado F1) --
            // SaldoAulas continua vindo so de request.SaldoAulas, como sempre.
            if (vinculo.Modalidade == ModalidadeCobranca.Pacote && request.TotalAulasPacote.HasValue)
            {
                // specs/058: aulas dadas = presencas registradas no pacote.
                var dado = (await _aulaRepository.ContarPresencasPorVinculosAsync(new[] { vinculo.Id }, null, null, cancellationToken)).GetValueOrDefault(vinculo.Id);
                if (request.TotalAulasPacote.Value < dado)
                {
                    throw new ConflitoException($"O novo total de aulas do pacote nao pode ser menor que as aulas ja dadas ({dado}).");
                }
            }
            else if (vinculo.Modalidade == ModalidadeCobranca.Mensalidade && request.AulasIncluidas.HasValue)
            {
                var competenciaAtual = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);
                var dadoNoMes = await _aulaRepository.ContarPresencasPorVinculoECompetenciaAsync(vinculo.Id, competenciaAtual, cancellationToken);
                if (request.AulasIncluidas.Value < dadoNoMes)
                {
                    throw new ConflitoException($"O novo teto de aulas da mensalidade nao pode ser menor que as aulas ja dadas neste mes ({dadoNoMes}).");
                }
            }

            vinculo.TurmaId = request.TurmaId;
            vinculo.Modalidade = request.Modalidade;
            vinculo.Valor = request.Valor;
            vinculo.AulasIncluidas = request.AulasIncluidas;
            vinculo.SaldoAulas = request.SaldoAulas;
            vinculo.TotalAulasPacote = request.TotalAulasPacote;

            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            var salvo = await _vinculoRepository.ObterPorIdAsync(vinculo.Id, cancellationToken);
            return await MapearAsync(salvo ?? vinculo, presencasPorPacote: null, cancellationToken);
        }

        // Exclusao logica (Constituicao I): nunca remove o registro fisicamente.
        public async Task ExcluirAsync(int alunoId, int id, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            var vinculo = await ObterDoAlunoAsync(alunoId, id, cancellationToken);

            if (!vinculo.Ativo)
            {
                return;
            }

            vinculo.Ativo = false;
            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        public async Task<VinculoCobrancaResponse> ReativarAsync(int alunoId, int id, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            var vinculo = await ObterDoAlunoAsync(alunoId, id, cancellationToken);

            if (vinculo.Ativo)
            {
                return await MapearAsync(vinculo, presencasPorPacote: null, cancellationToken);
            }

            // FR-014: em conflito rejeita sem desativar nem alterar o vinculo ativo existente.
            // Reaplica so as regras de combinacao -- nao FR-013 (vinculos orfaos de turma ficam para fatia futura).
            await GarantirRegrasDeCombinacaoAsync(alunoId, vinculo.Modalidade, vinculo.TurmaId, vinculo.Id, cancellationToken);

            vinculo.Ativo = true;
            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            return await MapearAsync(vinculo, presencasPorPacote: null, cancellationToken);
        }

        private async Task GarantirAlunoExisteAsync(int alunoId, CancellationToken cancellationToken)
        {
            if (await _alunoRepository.ObterPorIdAsync(alunoId, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Aluno nao encontrado.");
            }
        }

        private async Task<VinculoCobranca> ObterDoAlunoAsync(int alunoId, int id, CancellationToken cancellationToken)
        {
            var vinculo = await _vinculoRepository.ObterPorIdAsync(id, cancellationToken);

            if (vinculo is null || vinculo.AlunoId != alunoId)
            {
                throw new NaoEncontradoException("Vinculo de cobranca nao encontrado.");
            }

            return vinculo;
        }

        // FR-013: a turma do vinculo deve existir, estar ativa e ter o aluno como participante.
        // Somente le a turma -- nunca cria nem altera a participacao do aluno (AlunoTurma).
        private async Task ValidarTurmaAsync(int alunoId, int turmaId, CancellationToken cancellationToken)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            if (!turma.Ativo || !turma.AlunosTurma.Any(at => at.AlunoId == alunoId))
            {
                throw new ConflitoException("A turma informada esta inativa ou o aluno nao participa dela.");
            }
        }

        // specs/056: quais vinculos podem ficar ativos ao mesmo tempo para o mesmo aluno.
        // Mensalidade cobre todas as aulas, entao e exclusiva; Pacote e um saldo do aluno
        // (um so por vez); Avulsa e por contexto (uma por turma e uma individual).
        // ignorarId = o proprio vinculo, na edicao/reativacao.
        private async Task GarantirRegrasDeCombinacaoAsync(int alunoId, ModalidadeCobranca modalidade, int? turmaId, int? ignorarId, CancellationToken cancellationToken)
        {
            var outrosAtivos = (await _vinculoRepository.ListarPorAlunoAsync(alunoId, ativo: true, cancellationToken))
                .Where(v => v.Id != ignorarId)
                .ToList();

            if (modalidade == ModalidadeCobranca.Mensalidade)
            {
                if (outrosAtivos.Count > 0)
                {
                    throw new ConflitoException("O aluno ja tem outros vinculos ativos. Desative-os antes de cadastrar a mensalidade, que cobre todas as aulas.");
                }

                return;
            }

            if (outrosAtivos.Any(v => v.Modalidade == ModalidadeCobranca.Mensalidade))
            {
                throw new ConflitoException("O aluno tem mensalidade ativa, que ja cobre todas as aulas.");
            }

            if (modalidade == ModalidadeCobranca.Pacote && outrosAtivos.Any(v => v.Modalidade == ModalidadeCobranca.Pacote))
            {
                throw new ConflitoException("O aluno ja tem um pacote ativo.");
            }

            if (modalidade == ModalidadeCobranca.Avulsa && outrosAtivos.Any(v => v.Modalidade == ModalidadeCobranca.Avulsa && v.TurmaId == turmaId))
            {
                throw new ConflitoException(turmaId.HasValue
                    ? "Ja existe um vinculo de cobranca ativo para este aluno nesta turma."
                    : "Ja existe um vinculo de cobranca ativo para este aluno em atendimento individual.");
            }
        }

        // specs/058: aulas dadas (presencas) de cada pacote da lista, numa consulta so.
        private async Task<IReadOnlyDictionary<int, int>> ContarPresencasDosPacotesAsync(IEnumerable<VinculoCobranca> vinculos, CancellationToken cancellationToken)
        {
            var idsPacote = vinculos.Where(v => v.Modalidade == ModalidadeCobranca.Pacote).Select(v => v.Id).ToList();
            return idsPacote.Count == 0
                ? new Dictionary<int, int>()
                : await _aulaRepository.ContarPresencasPorVinculosAsync(idsPacote, null, null, cancellationToken);
        }

        // specs/058: aulas que contaram como dadas no pacote (a mesma fonte do progresso).
        public async Task<List<AulaDoPacoteResponse>> ListarAulasDoPacoteAsync(int alunoId, int id, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);
            var vinculo = await ObterDoAlunoAsync(alunoId, id, cancellationToken);

            var presencas = await _aulaRepository.ListarPresencasDoVinculoAsync(vinculo.Id, cancellationToken);
            return presencas.Select(aa => new AulaDoPacoteResponse
            {
                AulaId = aa.AulaId,
                Data = aa.Aula.DataInicio,
                HoraInicio = aa.Aula.HoraInicio,
                HoraFim = aa.Aula.HoraFim,
                MateriaNome = aa.Aula.Materia.Nome,
                Contexto = aa.Aula.Turma?.Nome ?? "Atendimento individual",
                Status = aa.Aula.Status,
            }).ToList();
        }

        // specs/052: assincrono porque AulasDadasNoMes (Mensalidade) depende
        // de uma contagem de presencas via IAulaRepository. presencasPorPacote
        // vem pronto na listagem (lote); nulo = conta so este vinculo.
        private async Task<VinculoCobrancaResponse> MapearAsync(VinculoCobranca vinculo, IReadOnlyDictionary<int, int>? presencasPorPacote, CancellationToken cancellationToken)
        {
            ProgressoPacoteResponse? progresso = null;
            int? aulasDadasNoMes = null;

            if (vinculo.Modalidade == ModalidadeCobranca.Pacote)
            {
                presencasPorPacote ??= await ContarPresencasDosPacotesAsync(new[] { vinculo }, cancellationToken);
                progresso = ProgressoPacote.Calcular(vinculo, presencasPorPacote.GetValueOrDefault(vinculo.Id));
            }
            else if (vinculo.Modalidade == ModalidadeCobranca.Mensalidade)
            {
                var competenciaAtual = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);
                aulasDadasNoMes = await _aulaRepository.ContarPresencasPorVinculoECompetenciaAsync(vinculo.Id, competenciaAtual, cancellationToken);
            }

            return new VinculoCobrancaResponse
            {
                Id = vinculo.Id,
                AlunoId = vinculo.AlunoId,
                TurmaId = vinculo.TurmaId,
                TurmaNome = vinculo.Turma?.Nome,
                Modalidade = vinculo.Modalidade,
                Valor = vinculo.Valor,
                AulasIncluidas = vinculo.AulasIncluidas,
                SaldoAulas = vinculo.SaldoAulas,
                TotalAulasPacote = vinculo.TotalAulasPacote,
                Progresso = progresso,
                AulasDadasNoMes = aulasDadasNoMes,
                Ativo = vinculo.Ativo
            };
        }
    }
}
