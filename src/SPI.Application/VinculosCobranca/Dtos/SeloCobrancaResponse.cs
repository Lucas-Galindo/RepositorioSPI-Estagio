namespace SPI.Application.VinculosCobranca.Dtos
{
    // specs/068: resumo de cobranca de um aluno para as telas da professora -- calculado no servidor
    // (ResumoCobrancaService), nunca gravado. O frontend so exibe; quem decide categoria, "o que vale"
    // e aulas restantes e o backend (Principio II).
    public record SeloCobrancaResponse
    {
        // "Mensalidade", "Pacote", "Avulsa" ou "SemVinculo" (o nome da modalidade; Semestral/Anual entram
        // sozinhas quando existirem).
        public string Categoria { get; set; } = string.Empty;

        // Valor do vinculo exibido; em "SemVinculo", o valor da aula do aluno.
        public decimal Valor { get; set; }

        // Nome da turma do vinculo (nulo = aulas individuais). Preenchido so no resumo da lista.
        public string? TurmaNome { get; set; }

        // So na categoria "Pacote": o mesmo progresso da spec 058 (ficha do aluno e Home).
        public ProgressoPacoteResponse? Progresso { get; set; }

        // true na "Avulsa" que so vale porque o pacote do grupo esgotou.
        public bool AvisoPacoteEsgotado { get; set; }

        // So no resumo da lista: quantos vinculos ativos alem do principal.
        public int OutrosVinculos { get; set; }

        // So no resumo da lista: todos os vinculos ativos (para a dica da tela).
        public List<DetalheCobrancaResponse> Detalhes { get; set; } = new();
    }

    public record DetalheCobrancaResponse
    {
        public string Categoria { get; set; } = string.Empty;
        public string? TurmaNome { get; set; }
        public decimal Valor { get; set; }
        public ProgressoPacoteResponse? Progresso { get; set; }
    }
}
