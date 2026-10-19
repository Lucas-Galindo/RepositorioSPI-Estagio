using SPI.Application.VinculosCobranca.Dtos;

namespace SPI.Application.Dashboard.Dtos
{
    /// <summary>
    /// Pacote que precisa de renovacao (esgotado ou com ate 2 aulas restantes).
    /// specs/058: o estado vem de ProgressoPacote -- os mesmos numeros da ficha do aluno.
    /// </summary>
    public record PacoteEmAtencaoResponse
    {
        public int VinculoId { get; set; }
        public int AlunoId { get; set; }
        public string AlunoNome { get; set; } = string.Empty;
        public ProgressoPacoteResponse Progresso { get; set; } = new();
    }
}
