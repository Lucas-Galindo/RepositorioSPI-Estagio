using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/038 (US1): quando nao ha VinculoCobranca correspondente ao contexto da
    // aula (nenhum cadastrado, excluido, ou de outro contexto), a conta a receber
    // continua sendo gerada exatamente como antes desta feature -- FR-001, FR-002,
    // comeco da cobertura de FR-008 (falta nunca dispara efeito).
    //
    // Nota (nao e TDD "red" classico para esta suite especifica): estes 5 cenarios
    // testam exatamente os caminhos em que o comportamento correto e identico ao
    // codigo atual, nao modificado (nenhum vinculo aplicavel => cobranca avulsa
    // incondicional com Aluno.ValorAula, do jeito que GerarContasAReceberAsync ja
    // fazia antes desta feature). Por isso, esta suite ja passa mesmo antes de
    // GerarContasAReceberAsync ser reescrita -- nao ha erro de compilacao nem falha
    // esperada aqui; ela funciona como uma suite de regressao que continua valida
    // depois da implementacao, nao como um "teste que falha primeiro". O "red" de
    // verdade desta feature esta nas suites de US2/US3/US4, que exercitam ramos que
    // o codigo atual nunca implementou e por isso falham de verdade ate la.
    public class AulaServiceGerarContasAReceberSemVinculoTests
    {
        private const int AlunoId = 1;
        private const int TurmaId = 10;

        private static Aluno NovoAluno(decimal valorAula = 80m) =>
            new() { Id = AlunoId, Nome = "Ana", Ativo = true, ValorAula = valorAula };

        private static Materia NovaMateria() => new() { Id = 1, Nome = "Matematica", Ativo = true };

        private static CategoriaReceita NovaCategoria(int id, string nome) => new() { Id = id, Nome = nome, Ativo = true };

        private static Aula NovaAula(int? turmaId, Aluno aluno, Materia materia, bool presente) => new()
        {
            Id = 100,
            MateriaId = materia.Id,
            Materia = materia,
            ProfessorId = 1,
            TurmaId = turmaId,
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

        private static (
            AulaService Servico,
            FakeAulaRepositoryParaAula Aulas,
            FakePagamentoRepositoryParaAula Pagamentos,
            FakeVinculoCobrancaRepositoryParaAula Vinculos,
            FakeCategoriaReceitaRepositoryParaAula Categorias) CriarServico(Aula aula)
        {
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            var categorias = new FakeCategoriaReceitaRepositoryParaAula
            {
                AulaEmTurma = NovaCategoria(1, "Aula em turma"),
                AulaParticular = NovaCategoria(2, "Aula particular"),
            };

            var servico = new AulaService(
                aulas,
                new FakeAlunoRepositoryVazio(),
                new FakeTurmaRepositoryVazia(),
                new FakeMateriaRepositoryVazia(),
                new FakeLembreteServiceVazio(),
                pagamentos,
                categorias,
                vinculos);

            return (servico, aulas, pagamentos, vinculos, categorias);
        }

        [Fact]
        public async Task Aluno_sem_vinculo_em_aula_de_turma_gera_conta_com_ValorAula()
        {
            var aluno = NovoAluno(80m);
            var materia = NovaMateria();
            var aula = NovaAula(TurmaId, aluno, materia, presente: true);
            var (servico, _, pagamentos, _, categorias) = CriarServico(aula);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Equal(80m, gerado.ValorFinal);
            Assert.Equal("Pendente", gerado.Status);
            Assert.Equal(categorias.AulaEmTurma!.Id, gerado.CategoriaReceitaId);
            Assert.Equal(aula.DataInicio.AddDays(5), gerado.DataVencimento);
            Assert.Equal(new DateOnly(2026, 9, 1), gerado.Competencia);
            Assert.Equal("Matematica - 15/09/2026", gerado.Descricao);
            Assert.Contains(pagamentos.Vinculacoes, v => v.PagamentoId == gerado.Id && v.AulaId == aula.Id);
        }

        [Fact]
        public async Task Aluno_sem_vinculo_em_aula_individual_usa_categoria_particular()
        {
            var aluno = NovoAluno(80m);
            var materia = NovaMateria();
            var aula = NovaAula(null, aluno, materia, presente: true);
            var (servico, _, pagamentos, _, categorias) = CriarServico(aula);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Equal(80m, gerado.ValorFinal);
            Assert.Equal(categorias.AulaParticular!.Id, gerado.CategoriaReceitaId);
        }

        [Fact]
        public async Task Vinculo_de_outro_contexto_nao_influencia_e_cai_no_fallback()
        {
            var aluno = NovoAluno(80m);
            var materia = NovaMateria();
            var aula = NovaAula(TurmaId, aluno, materia, presente: true);
            var (servico, _, pagamentos, vinculos, _) = CriarServico(aula);

            // Vinculo ativo, mas para uma turma diferente (contexto nao bate).
            vinculos.Vinculos.Add(new VinculoCobranca { AlunoId = AlunoId, TurmaId = 999, Modalidade = ModalidadeCobranca.Avulsa, Valor = 500m, Ativo = true });

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Equal(80m, gerado.ValorFinal);
        }

        [Fact]
        public async Task Vinculo_excluido_do_contexto_exato_e_tratado_como_inexistente()
        {
            var aluno = NovoAluno(80m);
            var materia = NovaMateria();
            var aula = NovaAula(TurmaId, aluno, materia, presente: true);
            var (servico, _, pagamentos, vinculos, _) = CriarServico(aula);

            vinculos.Vinculos.Add(new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Avulsa, Valor = 500m, Ativo = false });

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Equal(80m, gerado.ValorFinal);
        }

        [Fact]
        public async Task Falta_nao_gera_conta_nem_toca_no_repositorio_de_vinculo()
        {
            var aluno = NovoAluno(80m);
            var materia = NovaMateria();
            var aula = NovaAula(TurmaId, aluno, materia, presente: false);
            var (servico, _, pagamentos, vinculos, _) = CriarServico(aula);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = false } });

            Assert.Empty(pagamentos.Gerados);
            Assert.Equal(0, vinculos.Salvamentos);
        }
    }
}
