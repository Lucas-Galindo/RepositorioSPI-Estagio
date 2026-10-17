using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // Diagnostico do modulo de lembretes de aula (nao de Mensalidade): Estoria 5
    // exige que os lembretes de uma turma recalculem a "proxima aula" sempre que
    // uma aula sai do conjunto Ativo+Agendada (cancelada, excluida ou realizada),
    // mesmo motivo ja documentado em LembreteService.RecalcularAsync ("uma nova
    // aula reabre o ciclo do lembrete"). Antes desta correcao, so
    // CadastrarAsync/AtualizarAsync/CancelarAsync chamavam RecalcularAsync --
    // ExcluirAsync e RegistrarSessaoAsync (marcar Realizada) nao chamavam, deixando
    // o lembrete apontando para uma aula que ja nao e mais a "proxima" quando a
    // turma tinha mais de uma aula futura agendada.
    public class AulaServiceRecalculoLembreteTests
    {
        private const int TurmaId = 10;

        private static Aluno NovoAluno(int id, string nome) =>
            new() { Id = id, Nome = nome, Ativo = true, ValorAula = 80m };

        private static Materia NovaMateria() => new() { Id = 1, Nome = "Matematica", Ativo = true };

        private static Aula NovaAula(int id, string status = "Agendada", int? turmaId = TurmaId) => new()
        {
            Id = id,
            MateriaId = 1,
            Materia = NovaMateria(),
            ProfessorId = 1,
            TurmaId = turmaId,
            Status = status,
            DataInicio = new DateOnly(2026, 9, 15).AddDays(id),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
            Ativo = true,
            AulaAlunos = new List<AulaAluno>(),
        };

        private static AulaService CriarServico(FakeAulaRepositoryParaAula aulas, FakeLembreteServiceVazio lembretes) =>
            new(
                aulas,
                new FakeAlunoRepositoryVazio(),
                new FakeTurmaRepositoryVazia(),
                new FakeMateriaRepositoryVazia(),
                lembretes,
                new FakePagamentoRepositoryParaAula(),
                new FakeCategoriaReceitaRepositoryParaAula
                {
                    AulaEmTurma = new CategoriaReceita { Id = 1, Nome = "Aula em turma", Ativo = true },
                    AulaParticular = new CategoriaReceita { Id = 2, Nome = "Aula particular", Ativo = true },
                },
                new FakeVinculoCobrancaRepositoryParaAula());

        [Fact]
        public async Task Excluir_uma_aula_de_turma_recalcula_os_lembretes_da_turma()
        {
            var aula = NovaAula(1);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var lembretes = new FakeLembreteServiceVazio();
            var servico = CriarServico(aulas, lembretes);

            await servico.ExcluirAsync(aula.Id);

            Assert.Contains(TurmaId, lembretes.Chamadas);
        }

        [Fact]
        public async Task Excluir_uma_aula_individual_sem_turma_nao_chama_recalculo()
        {
            var aula = NovaAula(1, turmaId: null);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var lembretes = new FakeLembreteServiceVazio();
            var servico = CriarServico(aulas, lembretes);

            await servico.ExcluirAsync(aula.Id);

            Assert.Empty(lembretes.Chamadas);
        }

        [Fact]
        public async Task Registrar_sessao_marcando_realizada_recalcula_os_lembretes_da_turma()
        {
            var aula = NovaAula(1);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var lembretes = new FakeLembreteServiceVazio();
            var servico = CriarServico(aulas, lembretes);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest());

            Assert.Contains(TurmaId, lembretes.Chamadas);
        }

        [Fact]
        public async Task Registrar_sessao_de_aula_individual_sem_turma_nao_chama_recalculo()
        {
            var aula = NovaAula(1, turmaId: null);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var lembretes = new FakeLembreteServiceVazio();
            var servico = CriarServico(aulas, lembretes);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest());

            Assert.Empty(lembretes.Chamadas);
        }
    }
}
