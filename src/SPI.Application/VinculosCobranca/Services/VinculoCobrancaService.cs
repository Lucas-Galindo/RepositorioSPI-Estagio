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
            var respostas = new List<VinculoCobrancaResponse>();
            foreach (var vinculo in vinculos)
            {
                respostas.Add(await MapearAsync(vinculo, cancellationToken));
            }
            return respostas;
        }

        public async Task<VinculoCobrancaResponse> ObterPorIdAsync(int alunoId, int id, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            var vinculo = await ObterDoAlunoAsync(alunoId, id, cancellationToken);
            return await MapearAsync(vinculo, cancellationToken);
        }

        public async Task<VinculoCobrancaResponse> CadastrarAsync(int alunoId, VinculoCobrancaRequest request, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            if (request.TurmaId.HasValue)
            {
                await ValidarTurmaAsync(alunoId, request.TurmaId.Value, cancellationToken);
            }

            await GarantirSemVinculoAtivoAsync(alunoId, request.TurmaId, null, cancellationToken);

            var vinculo = new VinculoCobranca
            {
                AlunoId = alunoId,
                TurmaId = request.TurmaId,
                Modalidade = request.Modalidade,
                Valor = request.Valor,
                AulasIncluidas = request.AulasIncluidas,
                SaldoAulas = request.SaldoAulas,
                TotalAulasPacote = request.TotalAulasPacote,
                Ativo = true
            };

            await _vinculoRepository.AdicionarAsync(vinculo, cancellationToken);
            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            // Recarrega para trazer a navegacao Turma (nome) no response.
            var salvo = await _vinculoRepository.ObterPorIdAsync(vinculo.Id, cancellationToken);
            return await MapearAsync(salvo ?? vinculo, cancellationToken);
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
            if (request.TurmaId != vinculo.TurmaId)
            {
                if (request.TurmaId.HasValue)
                {
                    await ValidarTurmaAsync(alunoId, request.TurmaId.Value, cancellationToken);
                }

                await GarantirSemVinculoAtivoAsync(alunoId, request.TurmaId, vinculo.Id, cancellationToken);
            }

            // specs/052 (FR-007/FR-010): nunca permitir que o novo total/teto
            // fique menor que o que ja foi consumido. Nao recalcula SaldoAulas
            // automaticamente em nenhum caso (/speckit-analyze achado F1) --
            // SaldoAulas continua vindo so de request.SaldoAulas, como sempre.
            if (vinculo.Modalidade == ModalidadeCobranca.Pacote && request.TotalAulasPacote.HasValue)
            {
                var dado = (vinculo.TotalAulasPacote ?? 0) - (vinculo.SaldoAulas ?? 0);
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
            return await MapearAsync(salvo ?? vinculo, cancellationToken);
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
                return await MapearAsync(vinculo, cancellationToken);
            }

            // FR-014: em conflito rejeita sem desativar nem alterar o vinculo ativo existente.
            // Reaplica so a unicidade -- nao FR-013 (vinculos orfaos de turma ficam para fatia futura).
            await GarantirSemVinculoAtivoAsync(alunoId, vinculo.TurmaId, vinculo.Id, cancellationToken);

            vinculo.Ativo = true;
            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            return await MapearAsync(vinculo, cancellationToken);
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

        // FR-005/FR-006: no maximo um vinculo ATIVO por aluno+turma e um por aluno sem turma.
        private async Task GarantirSemVinculoAtivoAsync(int alunoId, int? turmaId, int? ignorarId, CancellationToken cancellationToken)
        {
            if (!await _vinculoRepository.ExisteAtivoAsync(alunoId, turmaId, ignorarId, cancellationToken))
            {
                return;
            }

            throw new ConflitoException(turmaId.HasValue
                ? "Ja existe um vinculo de cobranca ativo para este aluno nesta turma."
                : "Ja existe um vinculo de cobranca ativo para este aluno em atendimento individual.");
        }

        // specs/052: assincrono porque AulasDadasNoMes (Mensalidade) depende
        // de uma contagem de presencas via IAulaRepository.
        private async Task<VinculoCobrancaResponse> MapearAsync(VinculoCobranca vinculo, CancellationToken cancellationToken)
        {
            int? aulasDadas = null;
            int? aulasDadasNoMes = null;

            if (vinculo.Modalidade == ModalidadeCobranca.Pacote && vinculo.TotalAulasPacote.HasValue && vinculo.SaldoAulas.HasValue)
            {
                aulasDadas = vinculo.TotalAulasPacote - vinculo.SaldoAulas;
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
                AulasDadas = aulasDadas,
                AulasDadasNoMes = aulasDadasNoMes,
                Ativo = vinculo.Ativo
            };
        }
    }
}
