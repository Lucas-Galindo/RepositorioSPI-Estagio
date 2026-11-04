using SPI.Application.VinculosCobranca.Dtos;

namespace SPI.Application.Common.Dtos
{
    // Representacao reduzida de Aluno, usada dentro de outros DTOs (Turma, Aula, etc).
    public record AlunoResumoResponse
    {
        public int Id { get; set; }
        public string Ra { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;

        // specs/068: o vinculo de cobranca que vale no contexto da tela (a turma); nulo quando nao calculado.
        public SeloCobrancaResponse? Cobranca { get; set; }
    }
}
