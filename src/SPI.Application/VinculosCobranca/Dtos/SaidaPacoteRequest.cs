namespace SPI.Application.VinculosCobranca.Dtos
{
    // specs/078: o que a professora informa no resumo da saida. A previa e a gravacao usam o mesmo corpo base.
    public record SaidaPacoteRequest
    {
        /// <summary>Quanto o aluno ja pagou pelo pacote (o pacote nao registra pagamentos). Em branco = 0.</summary>
        public decimal ValorPago { get; set; }

        /// <summary>Vencimento da conta da diferenca. Obrigatorio (de hoje em diante) so quando ha diferenca a receber.</summary>
        public DateOnly? Vencimento { get; set; }
    }

    // specs/078: a gravacao devolve os numeros que a previa mostrou (consumo, devido e aulas de que o aluno sairia); o
    // servidor recalcula e recusa se qualquer um mudou, para a professora confirmar so o que viu.
    public record ConfirmarSaidaPacoteRequest : SaidaPacoteRequest
    {
        public int ConsumidasEsperadas { get; set; }
        public decimal ValorDevidoEsperado { get; set; }
        public int AulasQueSairaoEsperadas { get; set; }
    }
}
