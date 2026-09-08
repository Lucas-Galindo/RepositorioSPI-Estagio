namespace SPI.Application.Financeiro.Dtos
{
    public record ReceitasResumo
    {
        public decimal Recebido { get; set; }
        public decimal AReceber { get; set; }
        public decimal Atrasado { get; set; }
    }

    public record DespesasResumo
    {
        public decimal Pago { get; set; }
        public decimal APagar { get; set; }
        public decimal Atrasado { get; set; }
    }

    public record ResultadoResumo
    {
        /// <summary>Recebido no periodo menos Pago no periodo (o que ja aconteceu).</summary>
        public decimal SaldoRealizado { get; set; }

        /// <summary>
        /// (Recebido + a receber) menos (Pago + a pagar): saldo se tudo o que
        /// esta em aberto hoje for liquidado, incluindo o que ja esta atrasado.
        /// </summary>
        public decimal SaldoPrevisto { get; set; }
    }

    public record ProximoVencimentoItem
    {
        public string Tipo { get; set; } = string.Empty; // "Receber" ou "Pagar"
        public string Descricao { get; set; } = string.Empty;
        public DateOnly DataVencimento { get; set; }
        public decimal Valor { get; set; }
    }

    public record VisaoGeralFinanceiraResponse
    {
        public ReceitasResumo Receitas { get; set; } = new();
        public DespesasResumo Despesas { get; set; } = new();
        public ResultadoResumo Resultado { get; set; } = new();
        public List<ProximoVencimentoItem> ProximosVencimentos { get; set; } = new();
    }
}
