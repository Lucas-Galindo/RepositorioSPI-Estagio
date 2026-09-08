namespace SPI.Domain.Entities
{
    // Sem coluna "ativo": o estado deste registro e controlado exclusivamente
    // pela coluna "status", conforme convencao definida no schema (02_criacao_tabelas.sql).
    public class Pagamento
    {
        public int Id { get; set; }
        public int AlunoId { get; set; }
        public string? Descricao { get; set; }
        public int? FormaPagamentoId { get; set; }
        public int? CategoriaReceitaId { get; set; }
        public DateOnly DataVencimento { get; set; }
        public DateOnly? DataPagamento { get; set; }
        public DateOnly? Competencia { get; set; }
        public decimal ValorFinal { get; set; }
        public string Status { get; set; } = "Pendente";
        public string? Observacoes { get; set; }

        public Aluno Aluno { get; set; } = null!;
        public FormaPagamento? FormaPagamento { get; set; }
        public CategoriaReceita? CategoriaReceita { get; set; }
        public ICollection<PagamentoAula> PagamentosAula { get; set; } = new List<PagamentoAula>();
    }
}
