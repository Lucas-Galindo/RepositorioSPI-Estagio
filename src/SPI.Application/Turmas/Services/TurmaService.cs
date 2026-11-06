using SPI.Application.Common.Dtos;
using SPI.Application.Turmas.Dtos;
using SPI.Application.VinculosCobranca.Services;
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
        private readonly IResumoCobrancaService? _cobranca;
        private readonly IMateriaRepository? _materias;

        public TurmaService(
            ITurmaRepository turmaRepository,
            IAlunoRepository alunoRepository,
            IVinculoCobrancaRepository vinculoRepository,
            IEncerramentoTurmaPacoteService encerramentoPacotes,
            IResumoCobrancaService? cobranca = null,
            IMateriaRepository? materias = null)
        {
            _cobranca = cobranca;
            _materias = materias;
            _turmaRepository = turmaRepository;
            _alunoRepository = alunoRepository;
            _vinculoRepository = vinculoRepository;
            _encerramentoPacotes = encerramentoPacotes;
        }

        private static DateOnly Hoje() => DateOnly.FromDateTime(DateTime.Now);

        // specs/075: materia opcional. Nula = sem materia. Inexistente = 404; inativa = 409 (so se escolhe ativa).
        private async Task<Materia?> ValidarMateriaAsync(int? materiaId, CancellationToken cancellationToken)
        {
            if (!materiaId.HasValue)
            {
                return null;
            }

            if (_materias is null)
            {
                throw new InvalidOperationException("O repositorio de materias nao foi configurado no TurmaService.");
            }

            var materia = await _materias.ObterPorIdAsync(materiaId.Value, cancellationToken)
                ?? throw new NaoEncontradoException("Materia nao encontrada.");

            if (!materia.Ativo)
            {
                throw new ConflitoException("A materia esta inativa e nao pode ser vinculada a turma.");
            }

            return materia;
        }

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

            var materia = await ValidarMateriaAsync(request.MateriaId, cancellationToken);

            var turma = new Turma
            {
                Nome = request.Nome,
                MateriaId = materia?.Id,
                Materia = materia,
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

            // specs/075: so valida quando o vinculo muda (a mesma materia, mesmo inativada depois, pode ser mantida);
            // nulo remove o vinculo.
            if (request.MateriaId != turma.MateriaId)
            {
                var materia = await ValidarMateriaAsync(request.MateriaId, cancellationToken);
                turma.MateriaId = materia?.Id;
                turma.Materia = materia;
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

            return await RecarregarEMapearAsync(turmaId, cancellationToken);
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

            return await RecarregarEMapearAsync(turmaId, cancellationToken);
        }

        // Depois de gravar uma mudanca de matricula, le a turma de novo (com os alunos) e a mapeia.
        private async Task<TurmaResponse> RecarregarEMapearAsync(int turmaId, CancellationToken cancellationToken)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken);
            return await MapearComPacotesAsync(turma!, cancellationToken);
        }

        // specs/063: em turma de pacote acrescenta quem saiu mas ainda tem o pacote ativo dela (so esses
        // podem voltar pela matricula direta).
        private async Task<TurmaResponse> MapearComPacotesAsync(Turma turma, CancellationToken cancellationToken)
        {
            var resposta = Mapear(turma);

            // specs/068: o selo de cada aluno matriculado mostra a cobranca que VALE nesta turma (a mesma regra
            // que cobra a presenca). Uma chamada em lote; so os matriculados -- quem esta so "fora da turma"
            // (AlunosComPacoteForaDaTurma) nao recebe.
            if (_cobranca is not null && turma.AlunosTurma.Count > 0)
            {
                var selos = await _cobranca.ResumirParaGrupoAsync(turma.AlunosTurma.Select(at => at.Aluno).ToList(), turma.Id, cancellationToken: cancellationToken);
                foreach (var aluno in resposta.Alunos)
                {
                    aluno.Cobranca = selos.GetValueOrDefault(aluno.Id);
                }
            }

            if (turma.Tipo == TipoTurma.Pacote)
            {
                var pacotes = await _vinculoRepository.ListarPacotesAtivosDaTurmaAsync(turma.Id, cancellationToken);
                var matriculados = turma.AlunosTurma.Select(at => at.AlunoId).ToHashSet();
                resposta.AlunosComPacoteForaDaTurma = pacotes
                    .Where(p => p.Aluno is { Ativo: true } && !matriculados.Contains(p.AlunoId))
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
            MateriaId = turma.MateriaId,
            MateriaNome = turma.Materia?.Nome,
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
