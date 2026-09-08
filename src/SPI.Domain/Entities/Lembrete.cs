namespace SPI.Domain.Entities
{
    public class Lembrete
    {
        public int Id { get; set; }
        public int TurmaId { get; set; }
        public string Status { get; set; } = "Pendente";

        // Data/hora completa (09_lembrete_datetime.sql) do disparo,
        // calculada a partir da proxima aula da turma menos a antecedencia.
        public DateTime HoraProgramada { get; set; }
        public string Destinatarios { get; set; } = string.Empty;
        public string Canal { get; set; } = string.Empty;
        public int AntecedenciaHora { get; set; }
        public bool Ativo { get; set; }

        public Turma Turma { get; set; } = null!;
    }
}
