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
        private readonly IPagamentoRepository? _pagamentoRepository;
        private readonly ICategoriaReceitaRepository? _categoriaRepository;

        public VinculoCobrancaService(
            IVinculoCobrancaRepository vinculoRepository,
            IAlunoRepository alunoRepository,
            ITurmaRepository turmaRepository,
            IAulaRepository aulaRepository,
            IPagamentoRepository? pagamentoRepository = null,
            ICategoriaReceitaRepository? categoriaRepository = null)
        {
            _pagamentoRepository = pagamentoRepository;
            _categoriaRepository = categoriaRepository;
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
            // specs/062: parcelas de todos os planos da lista numa consulta so.
            var parcelasPorVinculo = await CarregarParcelasAsync(vinculos, cancellationToken);
            var respostas = new List<VinculoCobrancaResponse>();
            // A Mensalidade ainda faz 1 consulta por vinculo (aulas do mes) --
            // aceitavel porque sao os vinculos de UM aluno so.
            foreach (var vinculo in vinculos)
            {
                respostas.Add(await MapearAsync(vinculo, presencasPorPacote, parcelasPorVinculo, cancellationToken));
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

            // specs/062: Semestral/Anual tambem cobrem todas as aulas: nao convivem com Pacote/Avulsa (dado antigo);
            // dois planos so conflitam se o periodo se sobrepoe (renovacao antecipada e permitida).
            var planos = ativos.Where(r => r.Modalidade.ComPeriodo()).ToList();
            if (planos.Count > 0 && ativos.Any(r => !r.Modalidade.ComPeriodo()))
            {
                ativos.ForEach(r => r.Conflito = "Semestralidade e anuidade nao podem coexistir com outros vinculos ativos.");
                return;
            }

            foreach (var plano in planos.Where(p => planos.Any(o => o.Id != p.Id && p.DataInicio <= o.DataFim && p.DataFim >= o.DataInicio)))
            {
                plano.Conflito = "Ja existe um plano com periodo sobreposto para este aluno.";
            }

            // specs/063: ter varios pacotes e permitido (um por turma e um de aulas individuais).
            // So e conflito herdado mais de um pacote ativo do MESMO grupo (a mesma turma, ou
            // varios de aulas individuais).
            var pacotes = ativos.Where(r => r.Modalidade == ModalidadeCobranca.Pacote);
            foreach (var grupo in pacotes.GroupBy(r => r.TurmaId).Where(g => g.Count() > 1))
            {
                foreach (var r in grupo)
                {
                    r.Conflito = "Mais de um pacote ativo do mesmo grupo (mesma turma ou aulas individuais).";
                }
            }
        }

        public async Task<VinculoCobrancaResponse> ObterPorIdAsync(int alunoId, int id, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            var vinculo = await ObterDoAlunoAsync(alunoId, id, cancellationToken);
            return await MapearAsync(vinculo, presencasPorPacote: null, parcelasPorVinculo: null, cancellationToken);
        }

        public async Task<VinculoCobrancaResponse> CadastrarAsync(int alunoId, VinculoCobrancaRequest request, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            // specs/062: o plano Semestral/Anual tem um cadastro proprio (vinculo + parcelas, de uma vez).
            if (request.Modalidade.ComPeriodo())
            {
                return await CadastrarPlanoAsync(alunoId, request, cancellationToken);
            }

            Turma? turmaDePacote = null;
            if (request.TurmaId.HasValue)
            {
                turmaDePacote = await ValidarTurmaDoVinculoAsync(alunoId, request.Modalidade, request.TurmaId.Value, cancellationToken);
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

            // specs/063 (FR-003): o cadastro do pacote de turma matricula o aluno, na mesma
            // gravacao do vinculo (repositorios compartilham o contexto do banco).
            if (turmaDePacote is not null)
            {
                await GarantirMatriculaAsync(turmaDePacote, alunoId, cancellationToken);
            }

            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            // Recarrega para trazer a navegacao Turma (nome) no response.
            var salvo = await _vinculoRepository.ObterPorIdAsync(vinculo.Id, cancellationToken);
            return await MapearAsync(salvo ?? vinculo, presencasPorPacote: null, parcelasPorVinculo: null, cancellationToken);
        }

        // specs/062: cadastra o plano Semestral/Anual e TODAS as parcelas de uma vez. O vinculo e as parcelas entram no
        // mesmo contexto do banco e saem num UNICO SalvarAlteracoesAsync: ou grava tudo ou nao grava nada (FR-006).
        // Este e o unico lugar que cria Pagamento a partir de um vinculo de plano, uma vez por parcela pedida, com a
        // competencia no mes do vencimento (a trava uq_pagamento_vinculo_competencia barra duplicidade, FR-007).
        private async Task<VinculoCobrancaResponse> CadastrarPlanoAsync(int alunoId, VinculoCobrancaRequest request, CancellationToken cancellationToken)
        {
            var pagamentos = _pagamentoRepository ?? throw new InvalidOperationException("Repositorio de pagamentos nao configurado.");
            var categorias = _categoriaRepository ?? throw new InvalidOperationException("Repositorio de categorias nao configurado.");

            var inicio = request.DataInicio!.Value;
            var fim = inicio.AddMonths(request.Modalidade.MesesDoPeriodo()).AddDays(-1);

            await GarantirRegrasDeCombinacaoAsync(alunoId, request.Modalidade, null, ignorarId: null, cancellationToken, inicio, fim);

            var nomeDoPlano = NomeDoPlano(request.Modalidade);
            var categoria = await categorias.ObterPorNomeAsync(nomeDoPlano, cancellationToken)
                ?? throw new ConflitoException($"A categoria de receita \"{nomeDoPlano}\" nao existe. Aplique a migracao 24 do banco.");

            var vinculo = new VinculoCobranca
            {
                AlunoId = alunoId,
                TurmaId = null,
                Modalidade = request.Modalidade,
                Valor = request.Valor,
                DataInicio = inicio,
                DataFim = fim,
                Ativo = true
            };

            await _vinculoRepository.AdicionarAsync(vinculo, cancellationToken);

            var parcelas = request.Parcelas!.OrderBy(p => p.Vencimento).ToList();
            for (var i = 0; i < parcelas.Count; i++)
            {
                await pagamentos.AdicionarAsync(new Pagamento
                {
                    AlunoId = alunoId,
                    Descricao = $"{nomeDoPlano} - parcela {i + 1}/{parcelas.Count}",
                    CategoriaReceitaId = categoria.Id,
                    DataVencimento = parcelas[i].Vencimento,
                    Competencia = new DateOnly(parcelas[i].Vencimento.Year, parcelas[i].Vencimento.Month, 1),
                    ValorFinal = parcelas[i].Valor,
                    Status = "Pendente",
                    VinculoCobranca = vinculo,
                }, cancellationToken);
            }

            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            var salvo = await _vinculoRepository.ObterPorIdAsync(vinculo.Id, cancellationToken);
            return await MapearAsync(salvo ?? vinculo, presencasPorPacote: null, parcelasPorVinculo: null, cancellationToken);
        }

        private static string NomeDoPlano(ModalidadeCobranca modalidade) =>
            modalidade == ModalidadeCobranca.Semestral ? "Semestralidade" : "Anuidade";

        public async Task<VinculoCobrancaResponse> AtualizarAsync(int alunoId, int id, VinculoCobrancaRequest request, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            var vinculo = await ObterDoAlunoAsync(alunoId, id, cancellationToken);

            // specs/062 (FR-011): o plano salvo nao e editado (inicio, tipo, valor e parcelas ficam como foram
            // contratados); ajustes de cada conta ficam em Contas a Receber. Tambem nao se vira plano por edicao.
            if (vinculo.Modalidade.ComPeriodo() || request.Modalidade.ComPeriodo())
            {
                throw new ConflitoException("Semestralidade e anuidade nao podem ser editadas: exclua e cadastre outro vinculo.");
            }

            if (!vinculo.Ativo)
            {
                throw new ConflitoException("Reative o vinculo antes de edita-lo.");
            }

            // specs/063 (FR-005): a turma de um pacote nao muda (para trocar, inativa-se e cadastra-se
            // outro). A Avulsa continua podendo mudar de turma.
            if (vinculo.Modalidade == ModalidadeCobranca.Pacote && request.Modalidade == ModalidadeCobranca.Pacote && request.TurmaId != vinculo.TurmaId)
            {
                throw new ConflitoException("A turma de um pacote nao pode ser alterada. Inative-o e cadastre outro.");
            }

            // Turma inalterada nao e revalidada (FR-013 so vale ao definir/alterar a turma):
            // editar o Valor de um vinculo cuja turma foi desativada depois continua possivel.
            // Excecao: uma Avulsa que vira Pacote de turma e validada como pacote novo.
            var viraPacote = request.Modalidade == ModalidadeCobranca.Pacote && vinculo.Modalidade != ModalidadeCobranca.Pacote;
            Turma? turmaDePacote = null;
            if (request.TurmaId.HasValue && (request.TurmaId != vinculo.TurmaId || viraPacote))
            {
                turmaDePacote = await ValidarTurmaDoVinculoAsync(alunoId, request.Modalidade, request.TurmaId.Value, cancellationToken);
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

            if (turmaDePacote is not null)
            {
                await GarantirMatriculaAsync(turmaDePacote, alunoId, cancellationToken);
            }

            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            var salvo = await _vinculoRepository.ObterPorIdAsync(vinculo.Id, cancellationToken);
            return await MapearAsync(salvo ?? vinculo, presencasPorPacote: null, parcelasPorVinculo: null, cancellationToken);
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

            // specs/062 (FR-012): ao excluir um plano, as parcelas ainda PENDENTES com vencimento DEPOIS de hoje
            // passam a Cancelado -- no mesmo salvamento. As pagas, as ja vencidas e o que vence hoje ficam como
            // estao, e nenhum pagamento e apagado.
            if (vinculo.Modalidade.ComPeriodo() && _pagamentoRepository is not null)
            {
                var hoje = DateOnly.FromDateTime(DateTime.Now);
                foreach (var parcela in await _pagamentoRepository.ListarPendentesFuturosDoVinculoAsync(vinculo.Id, hoje, cancellationToken))
                {
                    parcela.Status = "Cancelado";
                }
            }

            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        public async Task<VinculoCobrancaResponse> ReativarAsync(int alunoId, int id, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            var vinculo = await ObterDoAlunoAsync(alunoId, id, cancellationToken);

            if (vinculo.Ativo)
            {
                return await MapearAsync(vinculo, presencasPorPacote: null, parcelasPorVinculo: null, cancellationToken);
            }

            // specs/062 (FR-013): as parcelas do plano excluido foram canceladas e nao sao recriadas.
            if (vinculo.Modalidade.ComPeriodo())
            {
                throw new ConflitoException("Este plano foi encerrado e suas parcelas canceladas. Cadastre um novo vinculo.");
            }

            // FR-014: em conflito rejeita sem desativar nem alterar o vinculo ativo existente.
            // Reaplica so as regras de combinacao -- nao FR-013 (vinculos orfaos de turma ficam para fatia futura).
            await GarantirRegrasDeCombinacaoAsync(alunoId, vinculo.Modalidade, vinculo.TurmaId, vinculo.Id, cancellationToken);

            // specs/063 (FR-021): reativar um pacote de turma exige a turma EM ANDAMENTO (ativa e
            // com data de fim vazia ou de hoje em diante) -- nao exige "nao iniciada", porque o
            // pacote ja pertencia a turma. Rematricula o aluno se ele tiver saido.
            if (vinculo.Modalidade == ModalidadeCobranca.Pacote && vinculo.TurmaId.HasValue)
            {
                var turma = await ValidarTurmaDePacoteAsync(vinculo.TurmaId.Value, exigirNaoIniciada: false, cancellationToken);
                await GarantirMatriculaAsync(turma, alunoId, cancellationToken);
            }

            vinculo.Ativo = true;
            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            return await MapearAsync(vinculo, presencasPorPacote: null, parcelasPorVinculo: null, cancellationToken);
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

        // Valida a turma informada ao cadastrar/editar um vinculo (mesma regra nos dois fluxos).
        // Pacote: turma de pacote ativa e ainda nao iniciada (specs/063); devolve a turma, para o
        // chamador matricular o aluno. Demais modalidades: o aluno ja participa da turma; devolve null.
        private async Task<Turma?> ValidarTurmaDoVinculoAsync(int alunoId, ModalidadeCobranca modalidade, int turmaId, CancellationToken cancellationToken)
        {
            if (modalidade == ModalidadeCobranca.Pacote)
            {
                return await ValidarTurmaDePacoteAsync(turmaId, exigirNaoIniciada: true, cancellationToken);
            }

            await ValidarTurmaAsync(alunoId, turmaId, cancellationToken);
            return null;
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

        // specs/063: o pacote de turma exige uma turma de pacote ATIVA e da professora. No cadastro
        // (exigirNaoIniciada) ela ainda nao pode ter comecado -- a turma e fechada depois do
        // inicio; na reativacao ela precisa estar em andamento (ainda nao terminou). A turma sem
        // datas (migrada) nunca e considerada iniciada. Devolve a turma, ja carregada.
        private async Task<Turma> ValidarTurmaDePacoteAsync(int turmaId, bool exigirNaoIniciada, CancellationToken cancellationToken)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            if (!turma.Ativo)
            {
                throw new ConflitoException("A turma informada esta inativa.");
            }

            if (turma.Tipo != TipoTurma.Pacote)
            {
                throw new ConflitoException("Somente turma do tipo Pacote recebe pacote.");
            }

            var hoje = DateOnly.FromDateTime(DateTime.Now);

            if (exigirNaoIniciada && turma.EstaIniciada(hoje))
            {
                throw new ConflitoException("A turma de pacote ja comecou e esta fechada: ninguem novo entra. Adicione o aluno as aulas como reposicao.");
            }

            if (!exigirNaoIniciada && !turma.EstaEmAndamento(hoje))
            {
                throw new ConflitoException("A turma de pacote ja terminou. Prorrogue a data de fim da turma para reativar o pacote.");
            }

            return turma;
        }

        // specs/063: matricula o aluno na turma de pacote se ele ainda nao estiver (sem duplicar).
        // Nao salva -- o chamador salva junto com o vinculo.
        private async Task GarantirMatriculaAsync(Turma turma, int alunoId, CancellationToken cancellationToken)
        {
            if (!turma.AlunosTurma.Any(at => at.AlunoId == alunoId))
            {
                await _turmaRepository.VincularAlunoAsync(turma.Id, alunoId, cancellationToken);
            }
        }

        // specs/056: quais vinculos podem ficar ativos ao mesmo tempo para o mesmo aluno.
        // Mensalidade cobre todas as aulas, entao e exclusiva; Pacote e por grupo (specs/063: um
        // por turma e um de aulas individuais); Avulsa e por contexto (uma por turma e uma
        // individual). ignorarId = o proprio vinculo, na edicao/reativacao.
        private async Task GarantirRegrasDeCombinacaoAsync(int alunoId, ModalidadeCobranca modalidade, int? turmaId, int? ignorarId, CancellationToken cancellationToken, DateOnly? inicio = null, DateOnly? fim = null)
        {
            var outrosAtivos = (await _vinculoRepository.ListarPorAlunoAsync(alunoId, ativo: true, cancellationToken))
                .Where(v => v.Id != ignorarId)
                .ToList();

            // A mensalidade cobre todas as aulas: so existe sozinha (nao pode haver nenhum outro
            // vinculo ativo, de qualquer modalidade) e, ao contrario, nenhum outro entra com ela ativa.
            if (modalidade == ModalidadeCobranca.Mensalidade)
            {
                if (outrosAtivos.Count > 0)
                {
                    throw new ConflitoException("O aluno ja tem outros vinculos ativos. Desative-os antes de cadastrar a mensalidade, que cobre todas as aulas.");
                }

                return;
            }

            // specs/062: Semestral/Anual tambem cobrem todas as aulas e sao exclusivos como a Mensalidade, com uma
            // excecao: outro plano cujo periodo NAO se sobreponha e aceito (renovacao antecipada).
            if (modalidade.ComPeriodo())
            {
                if (outrosAtivos.Any(v => !v.Modalidade.ComPeriodo()))
                {
                    throw new ConflitoException("O aluno ja tem outros vinculos ativos. Desative-os antes de cadastrar a semestralidade ou a anuidade, que cobre todas as aulas.");
                }

                if (inicio.HasValue && fim.HasValue && outrosAtivos.Any(v => inicio <= v.DataFim && fim >= v.DataInicio))
                {
                    throw new ConflitoException("Ja existe um plano com periodo sobreposto para este aluno.");
                }

                return;
            }

            if (outrosAtivos.Any(v => v.Modalidade == ModalidadeCobranca.Mensalidade))
            {
                throw new ConflitoException("O aluno tem mensalidade ativa, que ja cobre todas as aulas.");
            }

            if (outrosAtivos.Any(v => v.Modalidade.ComPeriodo()))
            {
                throw new ConflitoException("O aluno tem um plano (mensalidade, semestralidade ou anuidade) ativo, que ja cobre todas as aulas.");
            }

            // specs/063 (FR-022): o aluno pode ter 0 ou N pacotes de turma ao mesmo tempo (um por
            // turma) e no maximo um pacote de aulas individuais (sem turma).
            if (modalidade == ModalidadeCobranca.Pacote && outrosAtivos.Any(v => v.Modalidade == ModalidadeCobranca.Pacote && v.TurmaId == turmaId))
            {
                throw new ConflitoException(turmaId.HasValue
                    ? "O aluno ja tem um pacote ativo desta turma."
                    : "O aluno ja tem um pacote de aulas individuais ativo.");
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

        // specs/062: parcelas dos planos da lista, agrupadas por vinculo, numa consulta so (nada de uma consulta por plano).
        private async Task<IReadOnlyDictionary<int, List<Pagamento>>> CarregarParcelasAsync(IEnumerable<VinculoCobranca> vinculos, CancellationToken cancellationToken)
        {
            var idsDePlano = vinculos.Where(v => v.Modalidade.ComPeriodo()).Select(v => v.Id).ToList();
            if (idsDePlano.Count == 0 || _pagamentoRepository is null)
            {
                return new Dictionary<int, List<Pagamento>>();
            }

            var parcelas = await _pagamentoRepository.ListarPorVinculosAsync(idsDePlano, cancellationToken);
            return parcelas
                .Where(p => p.VinculoCobrancaId.HasValue)
                .GroupBy(p => p.VinculoCobrancaId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.DataVencimento).ToList());
        }

        // "Atrasado" nao e gravado: e uma parcela Pendente ja vencida, calculada como em Contas a Receber
        // (PagamentoService), para a ficha do aluno mostrar o mesmo status.
        private static string StatusDaParcela(Pagamento parcela) =>
            parcela.Status == "Pendente" && parcela.DataVencimento < DateOnly.FromDateTime(DateTime.UtcNow) ? "Atrasado" : parcela.Status;

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
        private async Task<VinculoCobrancaResponse> MapearAsync(
            VinculoCobranca vinculo,
            IReadOnlyDictionary<int, int>? presencasPorPacote,
            IReadOnlyDictionary<int, List<Pagamento>>? parcelasPorVinculo,
            CancellationToken cancellationToken)
        {
            ProgressoPacoteResponse? progresso = null;
            int? aulasDadasNoMes = null;
            List<ParcelaResponse>? parcelas = null;

            if (vinculo.Modalidade.ComPeriodo())
            {
                parcelasPorVinculo ??= await CarregarParcelasAsync(new[] { vinculo }, cancellationToken);
                parcelas = parcelasPorVinculo.GetValueOrDefault(vinculo.Id, new List<Pagamento>())
                    .Select(p => new ParcelaResponse
                    {
                        PagamentoId = p.Id,
                        Descricao = p.Descricao,
                        Vencimento = p.DataVencimento,
                        Valor = p.ValorFinal,
                        Status = StatusDaParcela(p),
                    })
                    .ToList();
            }

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
                DataInicio = vinculo.DataInicio,
                DataFim = vinculo.DataFim,
                Parcelas = parcelas,
                Ativo = vinculo.Ativo
            };
        }
    }
}
