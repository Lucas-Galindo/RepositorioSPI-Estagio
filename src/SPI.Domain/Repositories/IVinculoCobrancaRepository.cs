using SPI.Domain.Entities;
using SPI.Domain.Enums;

namespace SPI.Domain.Repositories
{
    public interface IVinculoCobrancaRepository
    {
        Task<VinculoCobranca?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

        // Ativos primeiro; depois por nome da turma, com os vinculos sem turma por ultimo.
        // Com ativo = true e a base das regras de combinacao (VinculoCobrancaService)
        // e da decisao de cobranca de cada presenca (RegraCobrancaPresenca, specs/056).
        Task<List<VinculoCobranca>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default);

        // specs/039: todo vinculo Ativo com a modalidade informada (usado pelo job
        // de cobranca de mensalidade, para listar os candidatos a gerar cobranca).
        Task<List<VinculoCobranca>> ListarAtivosPorModalidadeAsync(ModalidadeCobranca modalidade, CancellationToken cancellationToken = default);

        Task AdicionarAsync(VinculoCobranca vinculo, CancellationToken cancellationToken = default);

        Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
    }
}
