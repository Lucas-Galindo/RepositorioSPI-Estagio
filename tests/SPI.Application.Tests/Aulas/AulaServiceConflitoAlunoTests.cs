using SPI.Application.Aulas.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/063 (FR-016, "MUST NOT"): o conflito de horario do aluno ja e impedido pela regra
    // existente da PROFESSORA (ExisteConflitoAsync: qualquer aula ativa dela que se sobreponha),
    // porque todo aluno so participa das aulas da propria professora. A spec proibe uma segunda
    // regra; aqui fica provado que (a) a unica consulta e a da professora e (b) ela recusa a aula
    // sobreposta mesmo quando os participantes sao alunos de pacote.
    public class AulaServiceConflitoAlunoTests
    {
        private const int ProfessorId = 1;
        private const int AlunoId = 1;

        private static AulaRequest Pedido() => new()
        {
            MateriaId = 1,
            TurmaId = CenarioCadastroAula.TurmaId,
            DataInicio = DateOnly.FromDateTime(DateTime.Now).AddDays(7),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
        };

        [Fact]
        public async Task Aula_sobreposta_e_recusada_mesmo_com_aluno_de_pacote_como_participante()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(AlunoId, "Ana"));
            cenario.Vinculos.Vinculos.Add(new VinculoCobranca
            {
                Id = 1, AlunoId = AlunoId, TurmaId = CenarioCadastroAula.TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 100m, SaldoAulas = 5, Ativo = true
            });
            cenario.Aulas.ConflitoSimulado = true;

            await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.CadastrarAsync(ProfessorId, Pedido()));

            Assert.Empty(cenario.Aulas.Aulas);
        }

        [Fact]
        public async Task A_unica_consulta_de_conflito_e_a_da_professora_e_nao_do_aluno()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(AlunoId, "Ana"));
            var pedido = Pedido();

            await cenario.Servico.CadastrarAsync(ProfessorId, pedido);

            var consulta = Assert.Single(cenario.Aulas.ConsultasDeConflito);
            Assert.Equal(ProfessorId, consulta.ProfessorId);
            Assert.Equal(pedido.DataInicio, consulta.Data);
            Assert.Equal(pedido.HoraInicio, consulta.Inicio);
            Assert.Equal(pedido.HoraFim, consulta.Fim);
        }

        [Fact]
        public async Task Aula_nova_de_turma_de_pacote_inclui_os_alunos_matriculados_como_em_qualquer_turma()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(AlunoId, "Ana"), CenarioCadastroAula.NovoAluno(2, "Bia"));
            cenario.Aulas.Turma!.Tipo = TipoTurma.Pacote;
            cenario.Aulas.Turma.DataInicio = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
            cenario.Aulas.Turma.DataFim = DateOnly.FromDateTime(DateTime.Now).AddDays(60);

            var resposta = await cenario.Servico.CadastrarAsync(ProfessorId, Pedido());

            Assert.Equal(new[] { AlunoId, 2 }, resposta.Alunos.Select(a => a.AlunoId).OrderBy(i => i));
            Assert.All(resposta.Alunos, a => Assert.False(a.Reposicao));
        }
    }
}
