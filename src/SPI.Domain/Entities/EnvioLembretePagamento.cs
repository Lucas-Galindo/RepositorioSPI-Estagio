namespace SPI.Domain.Entities
{
    // specs/060: tentativa(s) de e-mail de pagamento pendente para um aluno num
    // dia de envio. Um registro por (aluno, dia), atualizado nas novas
    // tentativas -- serve de historico na aba Lembretes e de trava de duplicidade.
    public class EnvioLembretePagamento
    {
        public const string Enviado = "Enviado";
        public const string Falha = "Falha";
        public const string SemDestinatario = "SemDestinatario";

        public int Id { get; set; }
        public int AlunoId { get; set; }
        public DateOnly DataEnvio { get; set; }
        public DateTime TentadoEm { get; set; }
        public string Destinatarios { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public int Tentativas { get; set; }
        public string? Detalhe { get; set; }

        public Aluno Aluno { get; set; } = null!;
    }
}
