using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/038 (US4): quando o VinculoCobranca correspondente ao contexto da aula
    // tem Modalidade Mensalidade, nenhuma conta a receber e gerada, e nenhum campo
    // do vinculo e alterado -- FR-004, FR-009. A implementacao ja foi entregue em
    // AulaServiceGerarContasAReceberSemVinculoTests.cs (nota estrutural do
    // tasks.md); esta suite so precisa existir e ja deve passar.
    public class AulaServiceGerarContasAReceberMensalidadeTests
    {
        private const int AlunoId = 1;
        private const int TurmaId = 10;

        private static Aluno NovoAluno(decimal valorAula = 80m) =>
            new() { Id = AlunoId, Nome = "Ana", Ativo = true, ValorAula = valorAula };

        private static Materia NovaMateria() => new() { Id = 1, Nome = "Matematica", Ativo = true };

        private static Aula NovaAula(Aluno aluno, Materia materia) => new()
        {
            Id = 100,
            MateriaId = materia.Id,
            Materia = materia,
            ProfessorId = 1,
            TurmaId = TurmaId,
            Status = "Agendada",
            DataInicio = new DateOnly(2026, 9, 15),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
            Ativo = true,
            AulaAlunos = new List<AulaAluno>
            {
                new() { AulaId = 100, AlunoId = aluno.Id, Aluno = aluno, Presente = null }
            }
        };

        private static AulaService CriarServico(
            FakeAulaRepositoryParaAula aulas, FakePagamentoRepositoryParaAula pagamentos, FakeVinculoCobrancaRepositoryParaAula vinculos) =>
            new(
                aulas,
                new FakeAlunoRepositoryVazio(),
                new FakeTurmaRepositoryVazia(),
                new FakeMateriaRepositoryVazia(),
                new FakeLembreteServiceVazio(),
                pagamentos,
                new FakeCategoriaReceitaRepositoryParaAula
                {
                    AulaEmTurma = new CategoriaReceita { Id = 1, Nome = "Aula em turma", Ativo = true },
                    AulaParticular = new CategoriaReceita { Id = 2, Nome = "Aula particular", Ativo = true },
                },
                vinculos);

        [Fact]
        public async Task Mensalidade_nao_gera_conta_a_receber()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(aluno, materia);
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 350m, AulasIncluidas = 8, Ativo = true });
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(new FakeAulaRepositoryParaAula { AulaSemeada = aula }, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            Assert.Empty(pagamentos.Gerados);
        }

        [Fact]
        public async Task Mensalidade_nao_altera_nenhum_campo_do_vinculo()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(aluno, materia);
            var vinculo = new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 350m, AulasIncluidas = 8, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculo);
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(new FakeAulaRepositoryParaAula { AulaSemeada = aula }, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            Assert.Equal(350m, vinculo.Valor);
            Assert.Equal(8, vinculo.AulasIncluidas);
            Assert.Equal(0, vinculos.Salvamentos);
        }

        [Fact]
        public async Task Falta_com_mensalidade_ativa_nao_gera_conta()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(aluno, materia);
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 350m, Ativo = true });
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(new FakeAulaRepositoryParaAula { AulaSemeada = aula }, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = false } });

            Assert.Empty(pagamentos.Gerados);
        }
    }
}
