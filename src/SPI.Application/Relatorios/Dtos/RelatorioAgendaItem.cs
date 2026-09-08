namespace SPI.Application.Relatorios.Dtos
{
    // Estoria 12: Relatorio de Agenda.
    public record RelatorioAgendaItem
    {
        public int AulaId { get; set; }
        public DateOnly Data { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFim { get; set; }
        public string Materia { get; set; } = string.Empty;

        /// <summary>Nome da turma, ou "Individual - {nome do aluno}" quando a aula nao e de turma.</summary>
        public string TurmaOuAluno { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
