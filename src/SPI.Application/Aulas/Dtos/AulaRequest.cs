namespace SPI.Application.Aulas.Dtos
{
    public record AulaRequest
    {
        public string? Descricao { get; set; }
        public int MateriaId { get; set; }

        /// <summary>
        /// Quando informado, a aula e de turma (todos os alunos ativos da turma sao vinculados).
        /// Quando nulo, a aula e individual e AlunoId passa a ser obrigatorio.
        /// </summary>
        public int? TurmaId { get; set; }

        /// <summary>
        /// Obrigatorio apenas quando a aula e individual (TurmaId nulo).
        /// </summary>
        public int? AlunoId { get; set; }

        public DateOnly DataInicio { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFim { get; set; }
    }
}
