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

        private static VinculoCobranca NovoVinculo(int id, Aluno aluno, int? saldoAulas, bool ativo = true, ModalidadeCobranca modalidade = ModalidadeCobranca.Pacote) => new()
        {
            Id = id,
            AlunoId = aluno.Id,
            Aluno = aluno,
            Modalidade = modalidade,
            Valor = 500m,
            SaldoAulas = saldoAulas,
            Ativo = ativo
        };

        [Fact]
        public void Saldo_zero_entra_como_esgotado()
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, saldoAulas: 0);

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
            var vinculo = NovoVinculo(1, aluno, saldoAulas: saldo);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            var item = Assert.Single(resultado);
            Assert.Equal("Atencao", item.Estado);
        }

        // specs/056: o pacote e do aluno (vale em qualquer aula), entao o contexto
        // exibido e sempre o mesmo -- antes era o nome da turma ou "Atendimento individual".
        [Fact]
        public void Contexto_e_todas_as_aulas_do_aluno()
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, saldoAulas: 1);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Equal("Todas as aulas do aluno", Assert.Single(resultado).Contexto);
        }

        [Fact]
        public void Ordenacao_esgotados_primeiro_depois_saldo_depois_nome()
        {
            var alunoBruno = NovoAluno(1, "Bruno");
            var alunoAna = NovoAluno(2, "Ana");
            var alunoCarla = NovoAluno(3, "Carla");

            var vinculos = new[]
            {
                NovoVinculo(1, alunoBruno, saldoAulas: 1),
                NovoVinculo(2, alunoAna, saldoAulas: 0),
                NovoVinculo(3, alunoCarla, saldoAulas: 2)
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
            var vinculo = NovoVinculo(1, aluno, saldoAulas: saldo);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Empty(resultado);
        }

        [Fact]
        public void Saldo_nulo_nunca_informado_nao_entra()
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, saldoAulas: null);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Empty(resultado);
        }

        [Fact]
        public void Vinculo_inativo_nao_entra()
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, saldoAulas: 1, ativo: false);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Empty(resultado);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Avulsa)]
        [InlineData(ModalidadeCobranca.Mensalidade)]
        public void Outras_modalidades_nao_entram_mesmo_com_saldo_baixo(ModalidadeCobranca modalidade)
        {
            var aluno = NovoAluno(1, "Ana");
            var vinculo = NovoVinculo(1, aluno, saldoAulas: 0, modalidade: modalidade);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Empty(resultado);
        }

        [Fact]
        public void Aluno_inativo_nao_entra()
        {
            var aluno = NovoAluno(1, "Ana", ativo: false);
            var vinculo = NovoVinculo(1, aluno, saldoAulas: 0);

            var resultado = DashboardService.MontarPacotesEmAtencao(new[] { vinculo });

            Assert.Empty(resultado);
        }

        // specs/056: um aluno so pode ter um pacote ativo, mas um conflito herdado da
        // regra antiga (dois pacotes, um por turma) ainda aparece por inteiro no painel.
        [Fact]
        public void Aluno_com_dois_pacotes_herdados_gera_duas_linhas()
        {
            var aluno = NovoAluno(1, "Ana");

            var vinculos = new[]
            {
                NovoVinculo(1, aluno, saldoAulas: 1),
                NovoVinculo(2, aluno, saldoAulas: 2)
            };

            var resultado = DashboardService.MontarPacotesEmAtencao(vinculos);

            Assert.Equal(2, resultado.Count);
        }
    }
}
