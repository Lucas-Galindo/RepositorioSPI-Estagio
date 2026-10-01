using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Application.Dashboard.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/041 (US3): renovar o pacote (editar o SaldoAulas via specs/037) libera
    // o aluno e atualiza o painel -- sem nenhum codigo de producao novo (nota
    // estrutural do tasks.md). Os testes cruzam RegistrarSessaoAsync (bloqueio,
    // US2) com DashboardService.MontarPacotesEmAtencao (painel, US1) sobre o
    // MESMO VinculoCobranca, simulando a edicao do saldo entre duas aulas.
    public class AulaServiceRegistrarSessaoPacoteRenovacaoTests
    {
        private const int AlunoId = 1;
        private const int TurmaId = 10;

        private static Aluno NovoAluno() => new() { Id = AlunoId, Nome = "Bruno", Ativo = true, ValorAula = 80m };

        private static Materia NovaMateria() => new() { Id = 1, Nome = "Matematica", Ativo = true };

        private static Aula NovaAula(int id, Aluno aluno) => new()
        {
            Id = id,
            MateriaId = 1,
            Materia = NovaMateria(),
            ProfessorId = 1,
            TurmaId = TurmaId,
            Status = "Agendada",
            DataInicio = new DateOnly(2026, 9, 15).AddDays(id),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
            Ativo = true,
            AulaAlunos = new List<AulaAluno> { new() { AulaId = id, AlunoId = aluno.Id, Aluno = aluno, Presente = null } }
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
        public async Task Renovar_saldo_para_10_libera_a_presenca_na_proxima_aula()
        {
            var aluno = NovoAluno();
            var vinculo = new VinculoCobranca { AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 0, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculo);
            var pagamentos = new FakePagamentoRepositoryParaAula();

            var aula1 = NovaAula(1, aluno);
            var resposta1 = await CriarServico(new FakeAulaRepositoryParaAula { AulaSemeada = aula1 }, pagamentos, vinculos)
                .RegistrarSessaoAsync(aula1.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });
            Assert.False(Assert.Single(resposta1.Alunos).Presente);

            // Renovacao (specs/037): a professora edita o vinculo e define um saldo novo.
            vinculo.SaldoAulas = 10;

            var aula2 = NovaAula(2, aluno);
            var resposta2 = await CriarServico(new FakeAulaRepositoryParaAula { AulaSemeada = aula2 }, pagamentos, vinculos)
                .RegistrarSessaoAsync(aula2.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            var alunoResp2 = Assert.Single(resposta2.Alunos);
            Assert.True(alunoResp2.Presente);
            Assert.Null(alunoResp2.MotivoNaoRegistro);
            Assert.Empty(resposta2.Avisos);
            Assert.Equal(9, vinculo.SaldoAulas);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void Saldo_renovado_para_1_ou_2_libera_presenca_e_continua_no_painel_como_atencao(int saldoRenovado)
        {
            var aluno = NovoAluno();
            // TurmaId nulo (atendimento individual) -- este teste cobre o saldo, nao o contexto.
            var vinculo = new VinculoCobranca { AlunoId = AlunoId, Aluno = aluno, TurmaId = null, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 0, Ativo = true };

            vinculo.SaldoAulas = saldoRenovado;

            var painel = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            var item = Assert.Single(painel);
            Assert.Equal("Atencao", item.Estado);
            Assert.Equal(saldoRenovado, item.SaldoAulas);
        }

        [Fact]
        public void Saldo_renovado_para_3_ou_mais_some_do_painel()
        {
            var aluno = NovoAluno();
            var vinculo = new VinculoCobranca { AlunoId = AlunoId, Aluno = aluno, TurmaId = null, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 0, Ativo = true };

            vinculo.SaldoAulas = 10;

            var painel = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Empty(painel);
        }
    }
}
