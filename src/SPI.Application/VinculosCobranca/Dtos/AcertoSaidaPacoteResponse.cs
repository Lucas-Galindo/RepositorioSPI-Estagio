using SPI.Application.VinculosCobranca.Services;

namespace SPI.Application.VinculosCobranca.Dtos
{
    // specs/078: o resumo do acerto (previa e resultado da gravacao). Todo numero vem do servidor; a tela so exibe.
    public record AcertoSaidaPacoteResponse
    {
        public int AlunoId { get; set; }
        public string AlunoNome { get; set; } = string.Empty;
        public int TurmaId { get; set; }
        public string TurmaNome { get; set; } = string.Empty;

        public decimal ValorPacote { get; set; }
        public int TotalAulas { get; set; }

        /// <summary>O pacote nao tinha total gravado: o total e o estimado pela ficha do aluno (spec 058).</summary>
        public bool TotalEstimado { get; set; }

        public int AulasConsumidas { get; set; }
        public decimal ValorPorAula { get; set; }
        public decimal ValorDevido { get; set; }
        public decimal ValorPago { get; set; }
        public decimal Diferenca { get; set; }
        public decimal Excedente { get; set; }
        public ConsequenciaAcerto Consequencia { get; set; }

        /// <summary>Ha diferenca a receber: a professora precisa informar o vencimento da conta.</summary>
        public bool PrecisaVencimento { get; set; }

        /// <summary>Aulas agendadas da turma que ainda nao comecaram das quais o aluno sera retirado.</summary>
        public int AulasAgendadasQueSairao { get; set; }
    }

    public record SaidaPacoteRegistradaResponse
    {
        public AcertoSaidaPacoteResponse Acerto { get; set; } = new();

        /// <summary>A conta a receber da diferenca; nulo quando nada foi gerado.</summary>
        public int? PagamentoId { get; set; }
    }
}
