using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/038 (Polish, FR-007): em uma mesma aula com multiplos alunos presentes,
    // o efeito e decidido de forma independente para cada um, conforme o vinculo
    // (ou ausencia dele) especifico daquele aluno para o contexto da aula.
    public class AulaServiceGerarContasAReceberMultiplosAlunosTests
    {
        private const int TurmaId = 10;

        [Fact]
        public async Task Aula_de_turma_com_tres_alunos_de_modalidades_diferentes_decide_por_aluno()
        {
            var materia = new Materia { Id = 1, Nome = "Matematica", Ativo = true };

            // A: sem vinculo (fallback). B: Avulsa com valor proprio. C: Pacote com saldo.
            var alunoA = new Aluno { Id = 1, Nome = "Ana", Ativo = true, ValorAula = 80m };
            var alunoB = new Aluno { Id = 2, Nome = "Bia", Ativo = true, ValorAula = 80m };
            var alunoC = new Aluno { Id = 3, Nome = "Caio", Ativo = true, ValorAula = 80m };

            var aula = new Aula
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
                    new() { AulaId = 100, AlunoId = alunoA.Id, Aluno = alunoA, Presente = null },
                    new() { AulaId = 100, AlunoId = alunoB.Id, Aluno = alunoB, Presente = null },
                    new() { AulaId = 100, AlunoId = alunoC.Id, Aluno = alunoC, Presente = null },
                }
            };

            var vinculoB = new VinculoCobranca { AlunoId = alunoB.Id, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Avulsa, Valor = 300m, Ativo = true };
            var vinculoC = new VinculoCobranca { AlunoId = alunoC.Id, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 3, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculoB);
            vinculos.Vinculos.Add(vinculoC);

            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = new AulaService(
                new FakeAulaRepositoryParaAula { AulaSemeada = aula },
                new FakeAlunoRepositoryVazio(),
                new FakeTurmaRepositoryVazia(),
                new FakeMateriaRepositoryVazia(),
                new FakeLembreteServiceVazio(),
                pagamentos,
                new FakeCategoriaReceitaRepositoryParaAula { AulaEmTurma = new CategoriaReceita { Id = 1, Nome = "Aula em turma", Ativo = true } },
                vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest
            {
                Presencas = { [alunoA.Id] = true, [alunoB.Id] = true, [alunoC.Id] = true }
            });

            Assert.Equal(2, pagamentos.Gerados.Count);
            Assert.Contains(pagamentos.Gerados, p => p.AlunoId == alunoA.Id && p.ValorFinal == 80m);
            Assert.Contains(pagamentos.Gerados, p => p.AlunoId == alunoB.Id && p.ValorFinal == 300m);
            Assert.DoesNotContain(pagamentos.Gerados, p => p.AlunoId == alunoC.Id);
            Assert.Equal(2, vinculoC.SaldoAulas);
        }
    }
}
