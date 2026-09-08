namespace SPI.Application.Relatorios.Dtos
{
    // Estoria 15: Relatorio Financeiro.
    public record RelatorioFinanceiroItem
    {
        public string Chave { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    // Evoluido na Sprint 8 (evolucao do Financeiro) para virar o Relatorio
    // Financeiro Consolidado: alem do recebido/por forma/por aluno originais
    // da Estoria 15, agora inclui despesas do periodo e a visao previsto x
    // realizado (reaproveitando a mesma segregacao Pendente/Atrasado usada na
    // Visao Geral, Sprint 7 -- sem duplicar essa logica).
    public record RelatorioFinanceiroResponse
    {
        public decimal TotalRecebido { get; set; }
        public decimal TotalPago { get; set; }
        public decimal SaldoRealizado { get; set; }
        public decimal ReceitaPendente { get; set; }
        public decimal DespesaPendente { get; set; }
        public decimal SaldoPrevisto { get; set; }
        public List<RelatorioFinanceiroItem> PorFormaPagamento { get; set; } = new();
        public List<RelatorioFinanceiroItem> PorAluno { get; set; } = new();
    }
}
