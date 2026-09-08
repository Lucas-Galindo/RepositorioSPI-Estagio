using SPI.Application.Aulas.Dtos;
using SPI.Application.Lembretes.Services;
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

        public AulaService(
            IAulaRepository aulaRepository,
            IAlunoRepository alunoRepository,
            ITurmaRepository turmaRepository,
            IMateriaRepository materiaRepository,
            ILembreteService lembreteService,
            IPagamentoRepository pagamentoRepository,
            ICategoriaReceitaRepository categoriaReceitaRepository)
        {
            _aulaRepository = aulaRepository;
            _alunoRepository = alunoRepository;
            _turmaRepository = turmaRepository;
            _materiaRepository = materiaRepository;
            _lembreteService = lembreteService;
            _pagamentoRepository = pagamentoRepository;
            _categoriaReceitaRepository = categoriaReceitaRepository;
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

            if (request.TurmaId.HasValue)
            {
                // Estoria 5: uma nova aula agendada para a turma recalcula a
                // hora programada dos lembretes configurados para ela.
                await _lembreteService.RecalcularAsync(request.TurmaId.Value, cancellationToken);
            }

            var aulaCompleta = await _aulaRepository.ObterPorIdAsync(aula.Id, cancellationToken);
            return Mapear(aulaCompleta!);
        }

        public async Task<AulaResponse> AtualizarAsync(int id, AulaRequest request, CancellationToken cancellationToken = default)
        {
            var aula = await _aulaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aula nao encontrada.");

            var turmaIdAntiga = aula.TurmaId;

            if (aula.Status == "Realizada")
            {
                throw new ConflitoException("Uma aula ja realizada nao pode ser alterada.");
            }

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
        }

        public async Task<AulaResponse> RegistrarSessaoAsync(int id, RegistrarSessaoRequest request, CancellationToken cancellationToken = default)
        {
            var aula = await _aulaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Aula nao encontrada.");

            if (aula.Status != "Agendada")
            {
                throw new ConflitoException("Somente aulas com status 'Agendada' podem ter a sessao registrada.");
            }

            foreach (var vinculo in aula.AulaAlunos)
            {
                if (!request.Presencas.TryGetValue(vinculo.AlunoId, out var presente))
                {
                    throw new ConflitoException($"Presenca do aluno {vinculo.AlunoId} nao foi informada.");
                }

                vinculo.Presente = presente;
                if (presente)
                {
                    vinculo.Aluno.Frequencia += 1;
                }
            }

            aula.Status = "Realizada";
            await _aulaRepository.SalvarAlteracoesAsync(cancellationToken);

            // Decisao de negocio aprovada (evolucao do Financeiro, Sprint 4):
            // toda aula Realizada gera automaticamente uma Conta a Receber
            // pendente por aluno presente (auladas com falta nao geram cobranca).
            // Forma de pagamento fica em aberto ate o recebimento efetivo.
            await GerarContasAReceberAsync(aula, cancellationToken);

            return Mapear(aula);
        }

        private async Task GerarContasAReceberAsync(Aula aula, CancellationToken cancellationToken)
        {
            var categoriaNome = aula.TurmaId.HasValue ? "Aula em turma" : "Aula particular";
            var categoria = await _categoriaReceitaRepository.ObterPorNomeAsync(categoriaNome, cancellationToken);
            var competencia = new DateOnly(aula.DataInicio.Year, aula.DataInicio.Month, 1);

            foreach (var vinculo in aula.AulaAlunos.Where(v => v.Presente == true))
            {
                var pagamento = new Pagamento
                {
                    AlunoId = vinculo.AlunoId,
                    Descricao = $"{aula.Materia.Nome} - {aula.DataInicio:dd/MM/yyyy}",
                    CategoriaReceitaId = categoria?.Id,
                    DataVencimento = aula.DataInicio.AddDays(5),
                    Competencia = competencia,
                    ValorFinal = vinculo.Aluno.ValorAula,
                    Status = "Pendente"
                };

                await _pagamentoRepository.AdicionarAsync(pagamento, cancellationToken);
                await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
                await _pagamentoRepository.VincularAulaAsync(pagamento.Id, aula.Id, cancellationToken);
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
                Presente = aa.Presente
            }).ToList()
        };
    }
}
