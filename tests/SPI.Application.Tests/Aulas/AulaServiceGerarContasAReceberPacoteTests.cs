using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/038 (US3): quando o VinculoCobranca correspondente ao contexto da aula
    // tem Modalidade Pacote, nenhuma conta a receber e gerada -- em vez disso,
    // SaldoAulas decresce em 1 por presenca, nunca abaixo de zero, e nunca a partir
    // de um saldo nunca informado (null) -- FR-005, FR-006, FR-010. A implementacao
    // ja foi entregue em AulaServiceGerarContasAReceberSemVinculoTests.cs (nota
    // estrutural do tasks.md); esta suite so precisa existir e ja deve passar.
    public class AulaServiceGerarContasAReceberPacoteTests
    {
        private const int AlunoId = 1;
        private const int TurmaId = 10;

        private static Aluno NovoAluno(decimal valorAula = 80m) =>
            new() { Id = AlunoId, Nome = "Ana", Ativo = true, ValorAula = valorAula };

        private static Materia NovaMateria() => new() { Id = 1, Nome = "Matematica", Ativo = true };

        private static Aula NovaAula(int id, Aluno aluno, Materia materia) => new()
        {
            Id = id,
            MateriaId = materia.Id,
            Materia = materia,
            ProfessorId = 1,
            TurmaId = TurmaId,
            Status = "Agendada",
            DataInicio = new DateOnly(2026, 9, 15).AddDays(id),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
            Ativo = true,
            AulaAlunos = new List<AulaAluno>
            {
                new() { AulaId = id, AlunoId = aluno.Id, Aluno = aluno, Presente = null }
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
        public async Task Pacote_com_saldo_positivo_nao_gera_conta_e_decrementa_em_1()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(1, aluno, materia);
            var vinculo = new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 5, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculo);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            Assert.Empty(pagamentos.Gerados);
            Assert.Equal(4, vinculo.SaldoAulas);
            Assert.True(vinculos.Salvamentos >= 1);
        }

        [Fact]
        public async Task Pacote_com_saldo_zero_nao_gera_conta_e_nao_fica_negativo()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(1, aluno, materia);
            var vinculo = new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 0, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculo);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            var excecao = await Record.ExceptionAsync(() =>
                servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } }));

            Assert.Null(excecao);
            Assert.Empty(pagamentos.Gerados);
            Assert.Equal(0, vinculo.SaldoAulas);
            Assert.Equal(500m, vinculo.Valor);
        }

        [Fact]
        public async Task Pacote_com_saldo_nunca_informado_nao_gera_conta_e_permanece_nulo()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(1, aluno, materia);
            var vinculo = new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = null, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculo);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            Assert.Empty(pagamentos.Gerados);
            Assert.Null(vinculo.SaldoAulas);
        }

        [Fact]
        public async Task Sequencia_de_tres_presencas_decresce_ate_zero_sem_gerar_conta()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var vinculo = new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 2, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculo);
            var pagamentos = new FakePagamentoRepositoryParaAula();

            var aula1 = NovaAula(1, aluno, materia);
            await CriarServico(new FakeAulaRepositoryParaAula { AulaSemeada = aula1 }, pagamentos, vinculos)
                .RegistrarSessaoAsync(aula1.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });
            Assert.Equal(1, vinculo.SaldoAulas);

            var aula2 = NovaAula(2, aluno, materia);
            await CriarServico(new FakeAulaRepositoryParaAula { AulaSemeada = aula2 }, pagamentos, vinculos)
                .RegistrarSessaoAsync(aula2.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });
            Assert.Equal(0, vinculo.SaldoAulas);

            var aula3 = NovaAula(3, aluno, materia);
            await CriarServico(new FakeAulaRepositoryParaAula { AulaSemeada = aula3 }, pagamentos, vinculos)
                .RegistrarSessaoAsync(aula3.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });
            Assert.Equal(0, vinculo.SaldoAulas);

            Assert.Empty(pagamentos.Gerados);
        }

        [Fact]
        public async Task Falta_com_pacote_ativo_nao_decrementa_nem_gera_conta()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(1, aluno, materia);
            var vinculo = new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 5, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculo);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = false } });

            Assert.Empty(pagamentos.Gerados);
            Assert.Equal(5, vinculo.SaldoAulas);
        }
    }
}
