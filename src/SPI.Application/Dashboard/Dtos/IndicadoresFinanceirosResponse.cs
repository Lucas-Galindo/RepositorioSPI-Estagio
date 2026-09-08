namespace SPI.Application.Dashboard.Dtos
{
    // Sprint 9 (Dashboard e Fluxo de Caixa): indicadores calculaveis com os
    // dados reais do SPI hoje. Indicadores que pressupunham professores
    // contratados como custo, mensalidade recorrente fixa (MRR), acordos de
    // parcelamento ou um saldo de caixa bancario cadastrado foram descartados
    // por decisao de negocio (nao ha esses conceitos no dominio atual).
    public record GargaloCaixaResponse
    {
        public int? DiaMaiorEntrada { get; set; }
        public decimal ValorMaiorEntrada { get; set; }
        public int? DiaMaiorSaida { get; set; }
        public decimal ValorMaiorSaida { get; set; }
    }

    public record IndicadoresFinanceirosResponse
    {
        /// <summary>Percentual (0-100) do valor vencido no periodo que ainda esta em aberto e atrasado.</summary>
        public decimal TaxaInadimplenciaPercentual { get; set; }

        /// <summary>Media de dias entre vencimento e pagamento, so para contas pagas com atraso no periodo. Nulo se nao houve nenhuma.</summary>
        public decimal? PrazoMedioAtrasoDias { get; set; }

        /// <summary>Percentual (0-100) de folga do caixa apos as despesas do periodo, sobre o recebido.</summary>
        public decimal MargemSegurancaPercentual { get; set; }

        /// <summary>Recebido menos pago no periodo (mesma base do Saldo Realizado da Visao Geral/Relatorio Financeiro).</summary>
        public decimal FluxoCaixaOperacional { get; set; }

        /// <summary>Recebido dividido por pago no periodo. Nulo se nao houve despesas pagas (sem base de comparacao); menor que 1 indica prejuizo operacional.</summary>
        public decimal? IndiceCoberturaCustosFixos { get; set; }

        public GargaloCaixaResponse GargaloCaixa { get; set; } = new();
    }

    public record FluxoCaixaMensalItem
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public decimal Entradas { get; set; }
        public decimal Saidas { get; set; }
        public decimal Saldo { get; set; }
    }
}
