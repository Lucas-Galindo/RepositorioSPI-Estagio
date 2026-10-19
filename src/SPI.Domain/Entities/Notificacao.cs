using SPI.Domain.Enums;

namespace SPI.Domain.Entities
{
    // specs/059: aviso para a professora. Uma por (Tipo, Aula); nunca e apagada,
    // so resolvida (ResolvidaEm) -- e reativada se a mesma aula voltar a ficar pendente.
    public class Notificacao
    {
        public int Id { get; set; }
        public int ProfessorId { get; set; }
        public TipoNotificacao Tipo { get; set; }
        public int? AulaId { get; set; }
        public DateTime CriadaEm { get; set; }
        // NULL = nao lida.
        public DateTime? LidaEm { get; set; }
        // NULL = ativa.
        public DateTime? ResolvidaEm { get; set; }

        public Aula? Aula { get; set; }
    }
}
