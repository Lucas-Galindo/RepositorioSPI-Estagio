namespace SPI.Application.Aulas.Dtos
{
    // specs/077: uma data da serie na previa.
    public record OcorrenciaSerieResponse
    {
        public DateOnly Data { get; set; }
        public int DiaSemana { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFim { get; set; }

        /// <summary>Ja existe outra aula ativa da professora nesse horario.</summary>
        public bool Conflito { get; set; }
    }

    public record PreviaSerieResponse
    {
        public int Total { get; set; }
        public int Limite { get; set; }
        public int Conflitos { get; set; }

        /// <summary>Nenhuma data em conflito: a serie pode ser criada.</summary>
        public bool PodeCriar { get; set; }

        public List<OcorrenciaSerieResponse> Aulas { get; set; } = new();
    }

    public record SerieCriadaResponse
    {
        public int Quantidade { get; set; }
        public List<AulaResponse> Aulas { get; set; } = new();
    }
}
