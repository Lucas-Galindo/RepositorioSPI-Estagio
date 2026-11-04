namespace SPI.Domain.Enums
{
    // specs/062: um lugar so diz quem tem periodo e quantos meses dura (Semestral e Anual), sem repetir listas
    // de modalidades pelo sistema.
    public static class ModalidadeCobrancaExtensions
    {
        // So Semestral e Anual tem inicio, fim e parcelas (a Mensalidade nao tem periodo: e cobrada todo mes
        // pelo job). Como a Mensalidade, cobrem todas as aulas do aluno e nao tem turma.
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
