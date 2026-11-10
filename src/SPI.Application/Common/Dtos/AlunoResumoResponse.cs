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

        // specs/078: calculado no servidor (turma de pacote em andamento e o aluno matriculado tem pacote ativo dela): a
        // tela so mostra o botao "Registrar saida" quando for verdadeiro.
        public bool PodeRegistrarSaida { get; set; }
    }
}
