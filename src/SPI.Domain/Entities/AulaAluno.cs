namespace SPI.Domain.Entities
{
    public class AulaAluno
    {
        public int AulaId { get; set; }
        public int AlunoId { get; set; }

        // NULL = aula ainda nao realizada; TRUE/FALSE = presenca marcada
        // ao registrar a sessao (Estoria 8).
        public bool? Presente { get; set; }

        // NULL = comportamento normal (presente) ou falta comum -- NUNCA
        // indica bloqueio. So preenchido por RegistrarSessaoAsync quando a
        // presenca e barrada por pacote esgotado (specs/041).
        public string? MotivoNaoRegistro { get; set; }

        public const string MotivoPacoteEsgotado = "PacoteEsgotado";

        // specs/052: vinculo de cobranca ativo no momento desta presenca
        // confirmada, para auditoria historica -- NULL quando nao havia
        // vinculo correspondente ao contexto, ou presenca nao confirmada.
        public int? VinculoCobrancaId { get; set; }
        public VinculoCobranca? VinculoCobranca { get; set; }

        // specs/056: TRUE = aluno colocado pela professora numa aula de turma sem
        // ser matriculado nela (reposicao). So esses podem ser removidos
        // individualmente da aula, e a edicao da aula os preserva.
        public bool Reposicao { get; set; }

        public Aula Aula { get; set; } = null!;
        public Aluno Aluno { get; set; } = null!;
    }
}
