namespace SPI.Domain.Enums
{
    // specs/062: os pontos do sistema que hoje perguntam "e Mensalidade?" passam a perguntar "e um plano
    // periodico?", sem repetir listas de modalidades (um lugar so decide quem e quem).
    public static class ModalidadeCobrancaExtensions
    {
        // Mensalidade, Semestral e Anual cobrem TODAS as aulas do aluno e nao geram cobranca por presenca.
        public static bool EhPlanoPeriodico(this ModalidadeCobranca modalidade) =>
            modalidade is ModalidadeCobranca.Mensalidade or ModalidadeCobranca.Semestral or ModalidadeCobranca.Anual;

        // So Semestral e Anual tem inicio, fim e parcelas (a Mensalidade nao tem periodo: e cobrada todo mes
        // pelo job). Sem turma, como a Mensalidade.
        public static bool ComPeriodo(this ModalidadeCobranca modalidade) =>
            modalidade is ModalidadeCobranca.Semestral or ModalidadeCobranca.Anual;

        // Duracao do plano: 6 meses (Semestral) ou 12 (Anual); 0 nas modalidades sem periodo.
        public static int MesesDoPeriodo(this ModalidadeCobranca modalidade) => modalidade switch
        {
            ModalidadeCobranca.Semestral => 6,
            ModalidadeCobranca.Anual => 12,
            _ => 0,
        };
    }
}
