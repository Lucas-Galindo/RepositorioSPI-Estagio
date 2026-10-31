using SPI.Domain.Enums;

namespace SPI.Application.Turmas.Dtos
{
    public record TurmaRequest
    {
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// specs/063: Mensalidade (padrao quando ausente no cadastro) ou Pacote. Nao pode
        /// mudar depois de criada (no PUT, ausente = mantem o tipo atual).
        /// </summary>
        public TipoTurma? Tipo { get; set; }

        /// <summary>
        /// Somente turma de Pacote. Obrigatoria no cadastro; depois de preenchida nao muda
        /// (mudar reabriria uma turma fechada). Turma migrada sem data pode preencher.
        /// </summary>
        public DateOnly? DataInicio { get; set; }

        /// <summary>
        /// Somente turma de Pacote. Obrigatoria no cadastro; pode ser ajustada depois
        /// (prorrogar ou antecipar), com fim >= inicio.
        /// </summary>
        public DateOnly? DataFim { get; set; }
    }
}
