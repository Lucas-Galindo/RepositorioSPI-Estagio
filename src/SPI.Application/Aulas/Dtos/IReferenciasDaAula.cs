namespace SPI.Application.Aulas.Dtos
{
    // Os campos que a aula unica (AulaRequest) e a serie (RecorrenciaAulaRequest) usam para dizer "que aula e esta":
    // a materia e, ou a turma, ou o aluno (atendimento individual).
    public interface IReferenciasDaAula
    {
        int MateriaId { get; }
        int? TurmaId { get; }
        int? AlunoId { get; }
    }
}
