using SPI.Application.Dashboard.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Dashboard
{
    // specs/041: DashboardService.MontarPacotesEmAtencao e' estatico e puro --
    // testavel sem fakes de repositorio nem IServiceScopeFactory (mesmo espirito
    // de MensalidadeDispatcherService.DeveDispararNesteMomento, specs/039).
    public class DashboardPacotesEmAtencaoTests
    {
        private static Aluno NovoAluno(int id, string nome, bool ativo = true) => new()
        {
            Id = id,
            Nome = nome,
            Ra = $"RA{id}",
            Ativo = ativo
        };

        private static Turma NovaTurma(int id, string nome) => new()
        {
            Id = id,
            Nome = nome,
            Ativo = true
        };

        private static VinculoCobranca NovoVinculo(int id, Aluno aluno, Turma? turma, int? saldoAulas, bool ativo = true, ModalidadeCobranca modalidade = ModalidadeCobranca.Pacote) => new()
        {
            Id = id,
            AlunoId = aluno.Id,
            Aluno = aluno,
            TurmaId = turma?.Id,
            Turma = turma,
            Modalidade = modalidade,
            Valor = 500m,
            SaldoAulas = saldoAulas,
            Ativo = ativo
        };

        [Fact]
        public void Saldo_zero_entra_como_esgotado()
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, null, saldoAulas: 0);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            var item = Assert.Single(resultado);
            Assert.Equal("Esgotado", item.Estado);
            Assert.Equal(0, item.SaldoAulas);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void Saldos_1_e_2_entram_como_atencao(int saldo)
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, null, saldoAulas: saldo);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            var item = Assert.Single(resultado);
            Assert.Equal("Atencao", item.Estado);
        }

        [Fact]
        public void Contexto_usa_nome_da_turma_quando_ha_turma()
        {
            var aluno = NovoAluno(1, "Ana");
            var turma = NovaTurma(1, "Turma A");
            var vinculo = NovoVinculo(1, aluno, turma, saldoAulas: 1);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Equal("Turma A", Assert.Single(resultado).Contexto);
        }

        [Fact]
        public void Contexto_e_atendimento_individual_quando_nao_ha_turma()
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, null, saldoAulas: 1);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Equal("Atendimento individual", Assert.Single(resultado).Contexto);
        }

        [Fact]
        public void Ordenacao_esgotados_primeiro_depois_saldo_depois_nome()
        {
            var alunoBruno = NovoAluno(1, "Bruno");
            var alunoAna = NovoAluno(2, "Ana");
            var alunoCarla = NovoAluno(3, "Carla");

            var vinculos = new[]
            {
                NovoVinculo(1, alunoBruno, null, saldoAulas: 1),
                NovoVinculo(2, alunoAna, null, saldoAulas: 0),
                NovoVinculo(3, alunoCarla, null, saldoAulas: 2)
            };

            var resultado = DashboardService.MontarPacotesEmAtencao(vinculos);

            Assert.Equal(new[] { "Ana", "Bruno", "Carla" }, resultado.Select(r => r.AlunoNome));
            Assert.Equal("Esgotado", resultado[0].Estado);
        }

        [Fact]
        public void Lista_vazia_de_entrada_gera_lista_vazia_de_saida()
        {
            var resultado = DashboardService.MontarPacotesEmAtencao(Array.Empty<VinculoCobranca>());

            Assert.Empty(resultado);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(5)]
        public void Saldo_3_ou_mais_nao_entra(int saldo)
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, null, saldoAulas: saldo);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Empty(resultado);
        }

        [Fact]
        public void Saldo_nulo_nunca_informado_nao_entra()
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, null, saldoAulas: null);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Empty(resultado);
        }

        [Fact]
        public void Vinculo_inativo_nao_entra()
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, null, saldoAulas: 1, ativo: false);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Empty(resultado);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Avulsa)]
        [InlineData(ModalidadeCobranca.Mensalidade)]
        public void Outras_modalidades_nao_entram_mesmo_com_saldo_baixo(ModalidadeCobranca modalidade)
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, null, saldoAulas: 0, modalidade: modalidade);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Empty(resultado);
        }

        [Fact]
        public void Aluno_inativo_nao_entra()
        {
            var aluno = NovoAluno(1, "Ana", ativo: false);
            var vinculo = NovoVinculo(1, aluno, null, saldoAulas: 0);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Empty(resultado);
        }

        [Fact]
        public void Mesmo_aluno_em_duas_turmas_gera_duas_linhas()
        {
            var aluno = NovoAluno(1, "Ana");
            var turmaA = NovaTurma(1, "Turma A");
            var turmaB = NovaTurma(2, "Turma B");

            var vinculos = new[]
            {
                NovoVinculo(1, aluno, turmaA, saldoAulas: 1),
                NovoVinculo(2, aluno, turmaB, saldoAulas: 2)
            };

            var resultado = DashboardService.MontarPacotesEmAtencao(vinculos);

            Assert.Equal(2, resultado.Count);
        }
    }
}
