using SPI.Application.VinculosCobranca.Dtos;

namespace SPI.Application.Aulas.Dtos
{
    public record AlunoPresencaResponse
    {
        public int AlunoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Ra { get; set; } = string.Empty;

        /// <summary>
        /// NULL enquanto a aula nao foi registrada (Estoria 8); TRUE/FALSE apos o registro da sessao.
        /// </summary>
        public bool? Presente { get; set; }

        /// <summary>
        /// NULL = presente ou falta comum. "PacoteEsgotado" = presenca barrada
        /// por pacote de aulas esgotado (specs/041); nunca indica bloqueio quando NULL.
        /// </summary>
        public string? MotivoNaoRegistro { get; set; }

        /// <summary>
        /// TRUE = aluno de fora da turma colocado nesta aula como reposicao (specs/056).
        /// </summary>
        public bool Reposicao { get; set; }

        /// <summary>
        /// specs/068: o vinculo de cobranca que vale para ESTA aula (a turma da aula; aula individual = aulas
        /// individuais); nulo quando nao calculado (lista de aulas).
        /// </summary>
        public SeloCobrancaResponse? Cobranca { get; set; }
    }
}
