using SPI.Application.VinculosCobranca.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.VinculosCobranca.Services
{
    public class VinculoCobrancaService : IVinculoCobrancaService
    {
        private readonly IVinculoCobrancaRepository _vinculoRepository;
        private readonly IAlunoRepository _alunoRepository;
        private readonly ITurmaRepository _turmaRepository;

        public VinculoCobrancaService(
            IVinculoCobrancaRepository vinculoRepository,
            IAlunoRepository alunoRepository,
            ITurmaRepository turmaRepository)
        {
            _vinculoRepository = vinculoRepository;
            _alunoRepository = alunoRepository;
            _turmaRepository = turmaRepository;
        }

        public async Task<List<VinculoCobrancaResponse>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            var vinculos = await _vinculoRepository.ListarPorAlunoAsync(alunoId, ativo, cancellationToken);
            return vinculos.Select(Mapear).ToList();
        }

        public async Task<VinculoCobrancaResponse> ObterPorIdAsync(int alunoId, int id, CancellationToken cancellationToken = default)
        {
            await GarantirAlunoExisteAsync(alunoId, cancellationToken);

            var vinculo = await ObterDoAlunoAsync(alunoId, id, cancellationToken);
            return Mapear(vinculo);
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
                Ativo = true
            };

            await _vinculoRepository.AdicionarAsync(vinculo, cancellationToken);
            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            // Recarrega para trazer a navegacao Turma (nome) no response.
            var salvo = await _vinculoRepository.ObterPorIdAsync(vinculo.Id, cancellationToken);
            return Mapear(salvo ?? vinculo);
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

            vinculo.TurmaId = request.TurmaId;
            vinculo.Modalidade = request.Modalidade;
            vinculo.Valor = request.Valor;
            vinculo.AulasIncluidas = request.AulasIncluidas;
            vinculo.SaldoAulas = request.SaldoAulas;

            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            var salvo = await _vinculoRepository.ObterPorIdAsync(vinculo.Id, cancellationToken);
            return Mapear(salvo ?? vinculo);
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
                return Mapear(vinculo);
            }

            // FR-014: em conflito rejeita sem desativar nem alterar o vinculo ativo existente.
            // Reaplica so a unicidade -- nao FR-013 (vinculos orfaos de turma ficam para fatia futura).
            await GarantirSemVinculoAtivoAsync(alunoId, vinculo.TurmaId, vinculo.Id, cancellationToken);

            vinculo.Ativo = true;
            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(vinculo);
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

        private static VinculoCobrancaResponse Mapear(VinculoCobranca vinculo) => new()
        {
            Id = vinculo.Id,
            AlunoId = vinculo.AlunoId,
            TurmaId = vinculo.TurmaId,
            TurmaNome = vinculo.Turma?.Nome,
            Modalidade = vinculo.Modalidade,
            Valor = vinculo.Valor,
            AulasIncluidas = vinculo.AulasIncluidas,
            SaldoAulas = vinculo.SaldoAulas,
            Ativo = vinculo.Ativo
        };
    }
}
