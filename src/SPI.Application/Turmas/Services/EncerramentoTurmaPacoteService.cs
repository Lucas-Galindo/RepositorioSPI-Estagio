using SPI.Domain.Repositories;

namespace SPI.Application.Turmas.Services
{
    // specs/063 (FR-019, research.md R7). Nas duas operacoes SO a coluna Ativo do pacote muda:
    // saldo, valor, total de aulas, pagamentos e presencas ficam como estao (o saldo nunca e
    // zerado). Nada e apagado (Principio I).
    public class EncerramentoTurmaPacoteService : IEncerramentoTurmaPacoteService
    {
        private readonly IVinculoCobrancaRepository _vinculoRepository;

        public EncerramentoTurmaPacoteService(IVinculoCobrancaRepository vinculoRepository)
        {
            _vinculoRepository = vinculoRepository;
        }

        public async Task<int> InativarPacotesDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default)
        {
            var pacotes = await _vinculoRepository.ListarPacotesAtivosDaTurmaAsync(turmaId, cancellationToken);

            foreach (var pacote in pacotes)
            {
                pacote.Ativo = false;
            }

            return pacotes.Count;
        }

        public async Task<int> EncerrarPacotesDeTurmasEncerradasAsync(DateOnly hoje, CancellationToken cancellationToken = default)
        {
            var pacotes = await _vinculoRepository.ListarPacotesAtivosDeTurmasEncerradasAsync(hoje, cancellationToken);

            if (pacotes.Count == 0)
            {
                return 0;
            }

            foreach (var pacote in pacotes)
            {
                pacote.Ativo = false;
            }

            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);
            return pacotes.Count;
        }
    }
}
