using SPI.Application.Dashboard.Dtos;

namespace SPI.Application.Relatorios.Dtos
{
    // Sprint 3 da evolucao do Financeiro: os mesmos indicadores do Dashboard
    // (Estoria 11/Sprint 9), so que aceitando os filtros de periodo/turma/
    // materia/aluno da "Visao de Indicadores" dentro do Relatorio Financeiro,
    // em vez de sempre mes corrente sem filtro.
    public record IndicadoresFinanceirosFiltradosResponse
    {
        public IndicadoresFinanceirosResponse Indicadores { get; set; } = new();
        public List<FluxoCaixaMensalItem> FluxoCaixaMensal { get; set; } = new();
    }
}
