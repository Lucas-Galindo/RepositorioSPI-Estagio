using SPI.Application.Common.Dtos;
using SPI.Application.Turmas.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Turmas.Services
{
    public class TurmaService : ITurmaService
    {
        private readonly ITurmaRepository _turmaRepository;
        private readonly IAlunoRepository _alunoRepository;
        private readonly IVinculoCobrancaRepository _vinculoRepository;
        private readonly IEncerramentoTurmaPacoteService _encerramentoPacotes;

        public TurmaService(
            ITurmaRepository turmaRepository,
            IAlunoRepository alunoRepository,
            IVinculoCobrancaRepository vinculoRepository,
            IEncerramentoTurmaPacoteService encerramentoPacotes)
        {
            _turmaRepository = turmaRepository;
            _alunoRepository = alunoRepository;
            _vinculoRepository = vinculoRepository;
            _encerramentoPacotes = encerramentoPacotes;
        }

        private static DateOnly Hoje() => DateOnly.FromDateTime(DateTime.Now);

        public async Task<List<TurmaResponse>> ListarAsync(string? nome, bool? ativo, CancellationToken cancellationToken = default)
        {
            var turmas = await _turmaRepository.ListarAsync(nome, ativo, cancellationToken);
            return turmas.Select(Mapear).ToList();
        }

        public async Task<TurmaResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            return await MapearComPacotesAsync(turma, cancellationToken);
        }

        public async Task<TurmaResponse> CadastrarAsync(int professorId, TurmaRequest request, CancellationToken cancellationToken = default)
        {
            // specs/063: ausente = Mensalidade. A turma nova de pacote exige as duas datas (o
            // validador ja barra com 400; aqui fica a defesa para quem chama o servico direto).
            var tipo = request.Tipo ?? TipoTurma.Mensalidade;

            if (tipo == TipoTurma.Mensalidade && (request.DataInicio.HasValue || request.DataFim.HasValue))
            {
                throw new ConflitoException("Turma de mensalidade nao tem datas de inicio e de fim.");
            }

            if (tipo == TipoTurma.Pacote)
            {
                if (!request.DataInicio.HasValue || !request.DataFim.HasValue)
                {
                    throw new ConflitoException("A turma de pacote precisa de data de inicio e de fim.");
                }

                if (request.DataFim.Value < request.DataInicio.Value)
                {
                    throw new ConflitoException("A data de fim nao pode ser anterior a data de inicio.");
                }
            }

            var turma = new Turma
            {
                Nome = request.Nome,
                ProfessorId = professorId,
                Ativo = true,
                Tipo = tipo,
                DataInicio = tipo == TipoTurma.Pacote ? request.DataInicio : null,
                DataFim = tipo == TipoTurma.Pacote ? request.DataFim : null
            };

            await _turmaRepository.AdicionarAsync(turma, cancellationToken);
            await _turmaRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(turma);
        }

        public async Task<TurmaResponse> AtualizarAsync(int id, TurmaRequest request, CancellationToken cancellationToken = default)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            // specs/063 (FR-005): a turma "nao muda". Ausente = mantem o tipo atual.
            if (request.Tipo.HasValue && request.Tipo.Value != turma.Tipo)
            {
                throw new ConflitoException("O tipo da turma nao pode ser alterado depois de criada.");
            }

            if (turma.Tipo == TipoTurma.Mensalidade)
            {
                if (request.DataInicio.HasValue || request.DataFim.HasValue)
                {
                    throw new ConflitoException("Turma de mensalidade nao tem datas de inicio e de fim.");
                }
            }
            else
            {
                // Data de inicio: preenche se ainda estiver vazia (turma migrada); depois de
                // preenchida nao muda, senao reabriria uma turma fechada. Ausente = mantem.
                if (request.DataInicio.HasValue)
                {
                    if (turma.DataInicio.HasValue && request.DataInicio.Value != turma.DataInicio.Value)
                    {
                        throw new ConflitoException("A data de inicio da turma de pacote nao pode ser alterada depois de preenchida.");
                    }

                    turma.DataInicio = request.DataInicio;
                }

                // Data de fim pode ser ajustada (prorrogar ou antecipar). Ausente = mantem.
                // Prorrogar nao reativa pacotes ja inativados (FR-021).
                if (request.DataFim.HasValue)
                {
                    turma.DataFim = request.DataFim;
                }

                // Confere a ordem com o resultado final (as datas ja atribuidas acima, ou as que a
                // turma tinha). Se recusar, nada foi salvo: o salvamento so acontece abaixo.
                if (turma.DataInicio.HasValue && turma.DataFim.HasValue && turma.DataFim.Value < turma.DataInicio.Value)
                {
                    throw new ConflitoException("A data de fim nao pode ser anterior a data de inicio.");
                }
            }

            turma.Nome = request.Nome;
            await _turmaRepository.SalvarAlteracoesAsync(cancellationToken);

            return await MapearComPacotesAsync(turma, cancellationToken);
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            turma.Ativo = false;

            // specs/063 (FR-019): a turma acabou, entao os pacotes dela ficam inativos -- no mesmo
            // salvamento, com o saldo preservado. ReativarAsync nao faz o caminho inverso.
            await _encerramentoPacotes.InativarPacotesDaTurmaAsync(turma.Id, cancellationToken);

            await _turmaRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        public async Task<TurmaResponse> ReativarAsync(int id, CancellationToken cancellationToken = default)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            turma.Ativo = true;
            await _turmaRepository.SalvarAlteracoesAsync(cancellationToken);

            return await MapearComPacotesAsync(turma, cancellationToken);
        }

        public async Task<TurmaResponse> VincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            if (await _alunoRepository.ObterPorIdAsync(alunoId, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Aluno nao encontrado.");
            }

            if (turma.AlunosTurma.Any(at => at.AlunoId == alunoId))
            {
                throw new ConflitoException("Aluno ja esta vinculado a esta turma.");
            }

            // specs/063 (FR-004): a turma de pacote so recebe quem tem pacote ativo dela (a
            // volta de quem saiu) e so enquanto esta em andamento. Qualquer outro aluno entra
            // pelo cadastro do pacote (que matricula) ou como "outro aluno" nas aulas.
            if (turma.Tipo == TipoTurma.Pacote)
            {
                var pacotes = await _vinculoRepository.ListarPacotesAtivosDaTurmaAsync(turmaId, cancellationToken);

                if (!pacotes.Any(p => p.AlunoId == alunoId))
                {
                    throw new ConflitoException("Turma de pacote: o aluno entra pelo cadastro do pacote dele, nao por matricula direta.");
                }

                if (!turma.EstaEmAndamento(Hoje()))
                {
                    throw new ConflitoException("A turma de pacote esta encerrada.");
                }
            }

            await _turmaRepository.VincularAlunoAsync(turmaId, alunoId, cancellationToken);
            await _turmaRepository.SalvarAlteracoesAsync(cancellationToken);

            var turmaAtualizada = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken);
            return await MapearComPacotesAsync(turmaAtualizada!, cancellationToken);
        }

        public async Task<TurmaResponse> DesvincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            if (!turma.AlunosTurma.Any(at => at.AlunoId == alunoId))
            {
                throw new NaoEncontradoException("Aluno nao esta vinculado a esta turma.");
            }

            await _turmaRepository.DesvincularAlunoAsync(turmaId, alunoId, cancellationToken);
            await _turmaRepository.SalvarAlteracoesAsync(cancellationToken);

            var turmaAtualizada = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken);
            return await MapearComPacotesAsync(turmaAtualizada!, cancellationToken);
        }

        // specs/063: em turma de pacote acrescenta quem saiu mas ainda tem o pacote ativo dela (so esses
        // podem voltar pela matricula direta).
        private async Task<TurmaResponse> MapearComPacotesAsync(Turma turma, CancellationToken cancellationToken)
        {
            var resposta = Mapear(turma);

            if (turma.Tipo == TipoTurma.Pacote)
            {
                var pacotes = await _vinculoRepository.ListarPacotesAtivosDaTurmaAsync(turma.Id, cancellationToken);
                resposta.AlunosComPacoteForaDaTurma = pacotes
                    .Where(p => p.Aluno is { Ativo: true } && !turma.AlunosTurma.Any(at => at.AlunoId == p.AlunoId))
                    .Select(p => new AlunoResumoResponse { Id = p.Aluno.Id, Ra = p.Aluno.Ra, Nome = p.Aluno.Nome })
                    .ToList();
            }

            return resposta;
        }

        private static TurmaResponse Mapear(Turma turma) => new()
        {
            Id = turma.Id,
            Nome = turma.Nome,
            ProfessorId = turma.ProfessorId,
            Ativo = turma.Ativo,
            Tipo = turma.Tipo,
            DataInicio = turma.DataInicio,
            DataFim = turma.DataFim,
            Fechada = turma.Tipo == TipoTurma.Pacote && turma.EstaIniciada(Hoje()),
            Alunos = turma.AlunosTurma.Select(at => new AlunoResumoResponse
            {
                Id = at.Aluno.Id,
                Ra = at.Aluno.Ra,
                Nome = at.Aluno.Nome
            }).ToList()
        };
    }
}
