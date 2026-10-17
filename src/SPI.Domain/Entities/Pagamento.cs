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

        public byte[]? ArquivoConteudo { get; set; }
        public string? ArquivoNomeOriginal { get; set; }
        public string? ArquivoTipoMime { get; set; }
        public int? ArquivoTamanhoBytes { get; set; }
        public DateTime? ArquivoDataUpload { get; set; }

        // specs/039: vinculo de cobranca que originou esta cobranca automatica de
        // mensalidade; NULL para pagamentos manuais ou gerados por presenca (specs/038).
        public int? VinculoCobrancaId { get; set; }

        // Lembrete de vencimento (5 dias antes) desta mensalidade ja foi
        // enviado por e-mail? Fire-once: uma vez tentado (sucesso ou falha),
        // nunca mais reenviado para este pagamento.
        public bool LembreteVencimentoEnviado { get; set; }

        public Aluno Aluno { get; set; } = null!;
        public FormaPagamento? FormaPagamento { get; set; }
        public CategoriaReceita? CategoriaReceita { get; set; }
        public VinculoCobranca? VinculoCobranca { get; set; }
        public ICollection<PagamentoAula> PagamentosAula { get; set; } = new List<PagamentoAula>();
    }
}
