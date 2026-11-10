namespace SPI.Application.VinculosCobranca.Services
{
    // specs/078: o que o acerto da saida faz com o valor pago. Serializado como texto (ver o contrato).
    public enum ConsequenciaAcerto
    {
        // Pagou menos do que consumiu: gera uma conta a receber da diferenca.
        GerarConta,

        // Pagou exatamente o que consumiu: nada a gerar.
        Fechado,

        // Pagou mais do que consumiu: o excedente NAO e devolvido nem vira credito (decisao do usuario).
        ExcedenteNaoDevolvido,
    }

    public record ResultadoAcerto(decimal ValorPorAula, decimal ValorDevido, decimal Diferenca, decimal Excedente, ConsequenciaAcerto Consequencia);

    // specs/078: unico lugar da regra do acerto proporcional ao consumo. Estatica e pura (sem banco, sem relogio):
    // a previa e a gravacao da saida usam este mesmo calculo, e a tela nunca refaz a conta (Principio II).
    public static class AcertoSaidaPacote
    {
        public static ResultadoAcerto Calcular(decimal valorPacote, int totalAulas, int consumidas, decimal valorPago)
        {
            if (totalAulas <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalAulas), "O total de aulas do pacote deve ser maior que zero.");
            }

            // Mais presencas do que o total (total editado para baixo depois): nunca cobra alem do valor do pacote.
            var cobradas = Math.Clamp(consumidas, 0, totalAulas);

            // Um unico arredondamento, sobre o total: com todas as aulas consumidas o devido e exatamente o valor do
            // pacote (arredondar o valor por aula antes de multiplicar daria 99,99 para R$ 100 em 3 aulas).
            var devido = Arredondar(valorPacote * cobradas / totalAulas);
            var porAula = Arredondar(valorPacote / totalAulas);

            var diferenca = Math.Max(0m, devido - valorPago);
            var excedente = Math.Max(0m, valorPago - devido);

            var consequencia = diferenca > 0
                ? ConsequenciaAcerto.GerarConta
                : excedente > 0 ? ConsequenciaAcerto.ExcedenteNaoDevolvido : ConsequenciaAcerto.Fechado;

            return new ResultadoAcerto(porAula, devido, diferenca, excedente, consequencia);
        }

        private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }
}
