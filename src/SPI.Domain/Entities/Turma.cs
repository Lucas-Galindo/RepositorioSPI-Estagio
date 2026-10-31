using SPI.Domain.Enums;

namespace SPI.Domain.Entities
{
    public class Turma
    {
        public int Id { get; set; }
        public int ProfessorId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; }

        // specs/063: turma de Mensalidade (comum) ou de Pacote (fechada). O tipo nao
        // muda depois de criada.
        public TipoTurma Tipo { get; set; } = TipoTurma.Mensalidade;

        // Somente Tipo = Pacote. A data de inicio nao muda depois de preenchida (mudar
        // reabriria uma turma fechada); a de fim pode ser ajustada. Turma migrada pode
        // ficar sem datas ate a professora preencher: sem data nunca e "iniciada".
        public DateOnly? DataInicio { get; set; }
        public DateOnly? DataFim { get; set; }

        // specs/063: turma de pacote ja iniciada (data de inicio anterior a `hoje`)
        // esta fechada: ninguem novo entra.
        public bool EstaIniciada(DateOnly hoje) => DataInicio.HasValue && DataInicio.Value < hoje;

        // specs/063: ativa e dentro do periodo (sem data de fim, ou de hoje em diante).
        // Reativar pacote e rematricular quem saiu exigem a turma em andamento.
        public bool EstaEmAndamento(DateOnly hoje) => Ativo && (!DataFim.HasValue || DataFim.Value >= hoje);

        public Professor Professor { get; set; } = null!;
        public ICollection<AlunoTurma> AlunosTurma { get; set; } = new List<AlunoTurma>();
        public ICollection<Aula> Aulas { get; set; } = new List<Aula>();
        public ICollection<Lembrete> Lembretes { get; set; } = new List<Lembrete>();
        public ICollection<VinculoCobranca> VinculosCobranca { get; set; } = new List<VinculoCobranca>();
    }
}
