using SPI.Application.Common.Dtos;
using SPI.Domain.Enums;

namespace SPI.Application.Turmas.Dtos
{
    public record TurmaResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int ProfessorId { get; set; }
        public bool Ativo { get; set; }

        // specs/063
        public TipoTurma Tipo { get; set; }
        public DateOnly? DataInicio { get; set; }
        public DateOnly? DataFim { get; set; }

        /// <summary>
        /// Turma de pacote ja iniciada: ninguem novo entra (nem por matricula, nem por
        /// pacote). Calculado no servidor para a tela nao repetir a regra (Principio II).
        /// </summary>
        public bool Fechada { get; set; }

        public List<AlunoResumoResponse> Alunos { get; set; } = new();

        /// <summary>
        /// Turma de pacote: alunos que tem o pacote ATIVO dela mas nao estao matriculados (sairam).
        /// Sao os unicos que podem voltar pela matricula direta (specs/063, FR-004). Vazio nas
        /// turmas de mensalidade. Calculado no servidor para a tela nao repetir a regra.
        /// </summary>
        public List<AlunoResumoResponse> AlunosComPacoteForaDaTurma { get; set; } = new();
    }
}
