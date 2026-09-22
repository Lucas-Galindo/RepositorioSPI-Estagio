using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/038 (US2): quando o VinculoCobranca correspondente ao contexto da aula
    // tem Modalidade Avulsa, a conta a receber usa o Valor do vinculo em vez de
    // Aluno.ValorAula -- FR-003. A implementacao ja foi entregue em
    // AulaServiceGerarContasAReceberSemVinculoTests.cs (nota estrutural do
    // tasks.md); esta suite so precisa existir e ja deve passar.
    public class AulaServiceGerarContasAReceberAvulsaTests
    {
        private const int AlunoId = 1;
        private const int TurmaId = 10;

        private static Aluno NovoAluno(decimal valorAula = 80m) =>
            new() { Id = AlunoId, Nome = "Ana", Ativo = true, ValorAula = valorAula };

        private static Materia NovaMateria() => new() { Id = 1, Nome = "Matematica", Ativo = true };

        private static Aula NovaAula(int? turmaId, Aluno aluno, Materia materia) => new()
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

        private static (AulaService Servico, FakePagamentoRepositoryParaAula Pagamentos, FakeVinculoCobrancaRepositoryParaAula Vinculos) CriarServico(Aula aula)
        {
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            var categorias = new FakeCategoriaReceitaRepositoryParaAula
            {
                AulaEmTurma = new CategoriaReceita { Id = 1, Nome = "Aula em turma", Ativo = true },
                AulaParticular = new CategoriaReceita { Id = 2, Nome = "Aula particular", Ativo = true },
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

            return (servico, pagamentos, vinculos);
        }

        [Fact]
        public async Task Avulsa_para_turma_usa_o_Valor_do_vinculo_nao_o_ValorAula()
        {
            var aluno = NovoAluno(80m);
            var materia = NovaMateria();
            var aula = NovaAula(TurmaId, aluno, materia);
            var (servico, pagamentos, vinculos) = CriarServico(aula);
            vinculos.Vinculos.Add(new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Avulsa, Valor = 350m, Ativo = true });

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Equal(350m, gerado.ValorFinal);
            Assert.NotEqual(aluno.ValorAula, gerado.ValorFinal);
            Assert.Equal("Matematica - 15/09/2026", gerado.Descricao);
            Assert.Equal(aula.DataInicio.AddDays(5), gerado.DataVencimento);
        }

        [Fact]
        public async Task Avulsa_de_atendimento_individual_usa_o_Valor_do_vinculo()
        {
            var aluno = NovoAluno(80m);
            var materia = NovaMateria();
            var aula = NovaAula(null, aluno, materia);
            var (servico, pagamentos, vinculos) = CriarServico(aula);
            vinculos.Vinculos.Add(new VinculoCobranca { AlunoId = AlunoId, TurmaId = null, Modalidade = ModalidadeCobranca.Avulsa, Valor = 90m, Ativo = true });

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Equal(90m, gerado.ValorFinal);
        }

        [Fact]
        public async Task Avulsa_nunca_escreve_no_vinculo_de_cobranca()
        {
            var aluno = NovoAluno(80m);
            var materia = NovaMateria();
            var aulaTurma = NovaAula(TurmaId, aluno, materia);
            var (servicoTurma, _, vinculosTurma) = CriarServico(aulaTurma);
            vinculosTurma.Vinculos.Add(new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Avulsa, Valor = 350m, Ativo = true });

            await servicoTurma.RegistrarSessaoAsync(aulaTurma.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            Assert.Equal(0, vinculosTurma.Salvamentos);
        }
    }
}
