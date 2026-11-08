namespace SPI.Application.Aulas.Dtos
{
    // specs/077: um dia da semana da serie com o seu proprio horario.
    public record HorarioSerieRequest
    {
        /// <summary>0 = domingo ... 6 = sabado (System.DayOfWeek).</summary>
        public int DiaSemana { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFim { get; set; }
    }

    // specs/077: pedido de uma serie semanal de aulas (previa e criacao usam o mesmo corpo).
    public record RecorrenciaAulaRequest
    {
        public string? Descricao { get; set; }
        public int MateriaId { get; set; }

        /// <summary>Aula de turma: informar a turma e NAO o aluno (como na aula unica).</summary>
        public int? TurmaId { get; set; }

        /// <summary>Aula individual: informar o aluno e NAO a turma.</summary>
        public int? AlunoId { get; set; }

        /// <summary>A serie comeca na primeira data, a partir desta, que cai num dos dias escolhidos.</summary>
        public DateOnly DataInicial { get; set; }

        public List<HorarioSerieRequest> Horarios { get; set; } = new();

        /// <summary>Termino por numero de aulas (1 a 60). Exatamente um entre este campo e DataFinal.</summary>
        public int? QuantidadeAulas { get; set; }

        /// <summary>Termino por data final (inclusive). Exatamente um entre este campo e QuantidadeAulas.</summary>
        public DateOnly? DataFinal { get; set; }
    }
}
