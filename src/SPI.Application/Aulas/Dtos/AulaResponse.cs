namespace SPI.Application.Aulas.Dtos
{
    public record AulaResponse
    {
        public int Id { get; set; }
        public string? Descricao { get; set; }
        public int MateriaId { get; set; }
        public string MateriaNome { get; set; } = string.Empty;
        public int ProfessorId { get; set; }
        public int? TurmaId { get; set; }
        public string? TurmaNome { get; set; }
        public DateOnly DataInicio { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFim { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public List<AlunoPresencaResponse> Alunos { get; set; } = new();

        /// <summary>
        /// Avisos imediatos (nao persistidos) sobre o registro desta sessao --
        /// hoje, um por aluno barrado por pacote esgotado (specs/041). Vazio
        /// em toda resposta que nao envolveu bloqueio.
        /// </summary>
        public List<string> Avisos { get; set; } = new();
    }
}
