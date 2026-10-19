using SPI.Domain.Enums;

namespace SPI.Application.Notificacoes.Dtos
{
    // specs/059/060 (contracts/api.md). Tipos de aula preenchem Aula; o de
    // pagamento pendente preenche Pagamento.
    public record NotificacaoResponse
    {
        public int Id { get; set; }
        public TipoNotificacao Tipo { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public DateTime CriadaEm { get; set; }
        public bool Lida { get; set; }
        public NotificacaoAulaResponse? Aula { get; set; }
        public NotificacaoPagamentoResponse? Pagamento { get; set; }
    }

    public record NotificacaoAulaResponse
    {
        public int Id { get; set; }
        public DateOnly Data { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFim { get; set; }
        public string MateriaNome { get; set; } = string.Empty;
        // Nome da turma, ou "Atendimento individual · {aluno}".
        public string Contexto { get; set; } = string.Empty;
    }

    // specs/060: pendencias atuais do aluno (calculadas na hora da listagem).
    public record NotificacaoPagamentoResponse
    {
        public int AlunoId { get; set; }
        public string AlunoNome { get; set; } = string.Empty;
        public List<PendenciaPagamentoResponse> Pendencias { get; set; } = new();
    }

    public record PendenciaPagamentoResponse
    {
        public int PagamentoId { get; set; }
        // Descricao do pagamento, ou a modalidade do vinculo quando nao ha descricao.
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateOnly Vencimento { get; set; }
        public int DiasAtraso { get; set; }
    }
}
