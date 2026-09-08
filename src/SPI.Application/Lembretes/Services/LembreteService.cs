using SPI.Application.Lembretes.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Lembretes.Services
{
    public class LembreteService : ILembreteService
    {
        private readonly ILembreteRepository _lembreteRepository;
        private readonly ITurmaRepository _turmaRepository;

        public LembreteService(ILembreteRepository lembreteRepository, ITurmaRepository turmaRepository)
        {
            _lembreteRepository = lembreteRepository;
            _turmaRepository = turmaRepository;
        }

        public async Task<List<LembreteResponse>> ListarAsync(int? turmaId, string? status, bool? ativo, CancellationToken cancellationToken = default)
        {
            var lembretes = await _lembreteRepository.ListarAsync(turmaId, status, ativo, cancellationToken);
            return lembretes.Select(Mapear).ToList();
        }

        public async Task<LembreteResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var lembrete = await _lembreteRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Lembrete nao encontrado.");

            return Mapear(lembrete);
        }

        public async Task<LembreteResponse> CadastrarAsync(LembreteRequest request, CancellationToken cancellationToken = default)
        {
            if (await _turmaRepository.ObterPorIdAsync(request.TurmaId, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Turma nao encontrada.");
            }

            var lembrete = new Lembrete
            {
                TurmaId = request.TurmaId,
                Canal = request.Canal,
                AntecedenciaHora = request.AntecedenciaHora,
                Destinatarios = request.Destinatarios,
                Status = "Pendente",
                Ativo = true,
                HoraProgramada = await CalcularHoraProgramadaAsync(request.TurmaId, request.AntecedenciaHora, cancellationToken)
            };

            await _lembreteRepository.AdicionarAsync(lembrete, cancellationToken);
            await _lembreteRepository.SalvarAlteracoesAsync(cancellationToken);

            var lembreteCompleto = await _lembreteRepository.ObterPorIdAsync(lembrete.Id, cancellationToken);
            return Mapear(lembreteCompleto!);
        }

        public async Task<LembreteResponse> AtualizarAsync(int id, LembreteRequest request, CancellationToken cancellationToken = default)
        {
            var lembrete = await _lembreteRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Lembrete nao encontrado.");

            // A Turma do lembrete nao muda na edicao (Estoria 5 so lista
            // Canal/Antecedencia/Destinatarios como editaveis).
            lembrete.Canal = request.Canal;
            lembrete.AntecedenciaHora = request.AntecedenciaHora;
            lembrete.Destinatarios = request.Destinatarios;
            lembrete.HoraProgramada = await CalcularHoraProgramadaAsync(lembrete.TurmaId, request.AntecedenciaHora, cancellationToken);

            await _lembreteRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(lembrete);
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var lembrete = await _lembreteRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Lembrete nao encontrado.");

            lembrete.Ativo = false;
            await _lembreteRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        public async Task RecalcularAsync(int turmaId, CancellationToken cancellationToken = default)
        {
            var proximaAula = await _lembreteRepository.ObterProximaAulaDaTurmaAsync(turmaId, cancellationToken);
            if (proximaAula is null)
            {
                // Sem aula futura agendada: nao ha o que recalcular agora.
                // Quando uma nova aula for cadastrada, o recalculo roda de novo.
                return;
            }

            var lembretes = await _lembreteRepository.ListarAtivosPorTurmaAsync(turmaId, cancellationToken);
            if (lembretes.Count == 0)
            {
                return;
            }

            var proximaAulaDataHora = proximaAula.DataInicio.ToDateTime(proximaAula.HoraInicio);

            foreach (var lembrete in lembretes)
            {
                lembrete.HoraProgramada = proximaAulaDataHora.AddHours(-lembrete.AntecedenciaHora);
                // Recorrencia: uma nova aula reabre o ciclo do lembrete, mesmo
                // que ja tivesse sido "Enviado" para a aula anterior.
                lembrete.Status = "Pendente";
            }

            await _lembreteRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        private async Task<DateTime> CalcularHoraProgramadaAsync(int turmaId, int antecedenciaHora, CancellationToken cancellationToken)
        {
            var proximaAula = await _lembreteRepository.ObterProximaAulaDaTurmaAsync(turmaId, cancellationToken);
            if (proximaAula is null)
            {
                // Sem aula futura ainda: usa "agora" como base provisoria: o
                // primeiro RecalcularAsync (quando uma aula for agendada)
                // corrige para o valor real.
                return DateTime.Now;
            }

            return proximaAula.DataInicio.ToDateTime(proximaAula.HoraInicio).AddHours(-antecedenciaHora);
        }

        private static LembreteResponse Mapear(Lembrete lembrete) => new()
        {
            Id = lembrete.Id,
            TurmaId = lembrete.TurmaId,
            TurmaNome = lembrete.Turma.Nome,
            Status = lembrete.Status,
            HoraProgramada = lembrete.HoraProgramada,
            Destinatarios = lembrete.Destinatarios,
            Canal = lembrete.Canal,
            AntecedenciaHora = lembrete.AntecedenciaHora,
            Ativo = lembrete.Ativo
        };
    }
}
