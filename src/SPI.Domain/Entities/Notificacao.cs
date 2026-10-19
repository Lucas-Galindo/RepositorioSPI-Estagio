using SPI.Domain.Enums;

namespace SPI.Domain.Entities
{
    // specs/059/060: aviso para a professora. Uma por (Tipo, Aula) ou (Tipo, Aluno); nunca e apagada,
    // so resolvida (ResolvidaEm) -- e reativada se a referencia voltar a precisar de aviso.
    public class Notificacao
    {
        public int Id { get; set; }
        public int ProfessorId { get; set; }
        public TipoNotificacao Tipo { get; set; }
        public int? AulaId { get; set; }
        // specs/060: referencia dos avisos de pagamento pendente (tipos de aula: nulo).
        public int? AlunoId { get; set; }
        public DateTime CriadaEm { get; set; }
        // NULL = nao lida.
        public DateTime? LidaEm { get; set; }
        // NULL = ativa.
        public DateTime? ResolvidaEm { get; set; }

        public Aula? Aula { get; set; }
        public Aluno? Aluno { get; set; }
    }
}
