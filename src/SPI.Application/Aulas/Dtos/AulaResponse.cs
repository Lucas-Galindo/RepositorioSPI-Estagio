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
        /// specs/064: true so se a aula esta ativa, "Agendada" e o horario atual (Sao Paulo) ja
        /// chegou ao inicio dela. Calculado pelo servidor; a tela apenas exibe.
        /// </summary>
        public bool ChamadaLiberada { get; set; }

        /// <summary>
        /// specs/064: segundos ate o inicio quando a chamada esta bloqueada so por horario; nulo nos
        /// demais casos. Permite a tela buscar a aula de novo na hora certa sem usar o relogio dela.
        /// </summary>
        public int? SegundosParaChamada { get; set; }

        /// <summary>
        /// Avisos imediatos (nao persistidos) sobre o registro desta sessao --
        /// hoje, um por aluno barrado por pacote esgotado (specs/041). Vazio
        /// em toda resposta que nao envolveu bloqueio.
        /// </summary>
        public List<string> Avisos { get; set; } = new();
    }
}
