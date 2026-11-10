using SPI.Application.VinculosCobranca.Dtos;

namespace SPI.Application.VinculosCobranca.Services
{
    // specs/078: saida do aluno de uma turma de pacote com acerto proporcional ao consumo.
    public interface ISaidaPacoteService
    {
        /// <summary>Resumo do acerto (consumidas, devido, pago informado, diferenca e consequencia). Nao grava nada.</summary>
        Task<AcertoSaidaPacoteResponse> PreVisualizarAsync(int turmaId, int alunoId, SaidaPacoteRequest request, CancellationToken cancellationToken = default);

        /// <summary>Registra a saida de forma atomica: desvincula, tira das aulas que nao comecaram, encerra o pacote e gera a conta da diferenca (se houver).</summary>
        Task<SaidaPacoteRegistradaResponse> RegistrarAsync(int turmaId, int alunoId, ConfirmarSaidaPacoteRequest request, CancellationToken cancellationToken = default);
    }
}
