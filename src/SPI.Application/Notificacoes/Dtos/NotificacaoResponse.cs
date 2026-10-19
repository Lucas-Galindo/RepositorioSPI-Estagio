using SPI.Domain.Enums;

namespace SPI.Application.Notificacoes.Dtos
{
    // specs/059 (contracts/notificacoes-api.md).
    public record NotificacaoResponse
    {
        public int Id { get; set; }
        public TipoNotificacao Tipo { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public DateTime CriadaEm { get; set; }
        public bool Lida { get; set; }
        public NotificacaoAulaResponse? Aula { get; set; }
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
}
