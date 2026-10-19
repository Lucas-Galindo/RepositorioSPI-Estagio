namespace SPI.Application.Dashboard.Dtos
{
    /// <summary>
    /// specs/058: visao de negocio dos pacotes na Home (research.md R5).
    /// </summary>
    public record PacotesResumoResponse
    {
        // Ativos (aluno ativo) que ainda tem aulas restantes (ou sem total).
        public int Ativos { get; set; }
        // Pacotes, ativos ou nao, com 0 aulas restantes.
        public int Concluidos { get; set; }
        // Ativos com 1 ou 2 aulas restantes.
        public int ProximosDoFim { get; set; }
        // Somas dos pacotes ativos de alunos ativos.
        public int AulasDadas { get; set; }
        public int AulasRestantes { get; set; }
        // Presencas (em qualquer pacote filtrado) dentro do periodo do dashboard.
        public int AulasDadasNoPeriodo { get; set; }
        // Ativos esgotados ou proximos do fim: esgotados primeiro, depois menos restantes, depois nome.
        public List<PacoteEmAtencaoResponse> Renovacao { get; set; } = new();
    }
}
