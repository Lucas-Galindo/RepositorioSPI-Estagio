namespace SPI.Application.Lembretes.Dtos
{
    // specs/060: regra do e-mail semanal de pagamento pendente (aba Lembretes).
    public record RegraLembretePagamentoDto
    {
        public bool Ativo { get; set; }
        // 0 = domingo ... 6 = sabado.
        public int DiaSemana { get; set; }
        public TimeOnly Hora { get; set; }
    }

    // specs/060: um registro do historico de envios.
    public record EnvioLembretePagamentoResponse
    {
        public int Id { get; set; }
        public int AlunoId { get; set; }
        public string AlunoNome { get; set; } = string.Empty;
        public DateOnly DataEnvio { get; set; }
        public DateTime TentadoEm { get; set; }
        public string Destinatarios { get; set; } = string.Empty;
        // "Enviado", "Falha" ou "SemDestinatario".
        public string Resultado { get; set; } = string.Empty;
        public int Tentativas { get; set; }
        public string? Detalhe { get; set; }
    }
}
