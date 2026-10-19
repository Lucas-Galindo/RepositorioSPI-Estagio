namespace SPI.Application.Aulas.Dtos
{
    // specs/056: aluno colocado numa aula agendada como reposicao.
    public record AdicionarParticipanteRequest
    {
        public int AlunoId { get; set; }
    }
}
