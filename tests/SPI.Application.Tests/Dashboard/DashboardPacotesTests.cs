using SPI.Application.Dashboard.Services;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Dashboard
{
    // specs/058 (US3): visao de negocio dos pacotes na Home. Substitui os testes
    // do antigo painel "Pacotes em atencao" (specs/041), que era baseado no saldo.
    // MontarResumoPacotes e estatica e pura -- testavel sem banco.
    public class DashboardPacotesTests
    {
        private static Aluno NovoAluno(int id, string nome, bool ativo = true) => new() { Id = id, Nome = nome, Ra = $"RA{id}", Ativo = ativo };

        private static VinculoCobranca Pacote(int id, Aluno aluno, int total, bool ativo = true) => new()
        {
            Id = id, AlunoId = aluno.Id, Aluno = aluno, Modalidade = ModalidadeCobranca.Pacote,
            Valor = 500m, TotalAulasPacote = total, SaldoAulas = total, Ativo = ativo,
        };

        [Fact]
        public void Indicadores_contam_cada_estado_e_somam_dadas_e_restantes_dos_ativos()
        {
            var ana = NovoAluno(1, "Ana");
            var bia = NovoAluno(2, "Bia");
            var caio = NovoAluno(3, "Caio");
            var pacotes = new[]
            {
                Pacote(1, ana, 10),             // 3 dadas -> EmDia, restam 7
                Pacote(2, bia, 4),              // 3 dadas -> Atencao, resta 1
                Pacote(3, caio, 5),             // 5 dadas -> Esgotado
                Pacote(4, ana, 8, ativo: false) // 8 dadas -> concluido antigo
            };
            var presencas = new Dictionary<int, int> { [1] = 3, [2] = 3, [3] = 5, [4] = 8 };

            var r = DashboardService.MontarResumoPacotes(pacotes, presencas, aulasDadasNoPeriodo: 4);

            Assert.Equal(2, r.Ativos);
            Assert.Equal(2, r.Concluidos);
            Assert.Equal(1, r.ProximosDoFim);
            Assert.Equal(11, r.AulasDadas);
            Assert.Equal(8, r.AulasRestantes);
            Assert.Equal(4, r.AulasDadasNoPeriodo);
        }

        [Fact]
        public void Renovacao_traz_esgotados_primeiro_depois_menos_restantes_depois_nome()
        {
            var pacotes = new[]
            {
                Pacote(1, NovoAluno(1, "Bruno"), 10),
                Pacote(2, NovoAluno(2, "Ana"), 10),
                Pacote(3, NovoAluno(3, "Carla"), 10),
                Pacote(4, NovoAluno(4, "Davi"), 10),
            };
            var presencas = new Dictionary<int, int> { [1] = 9, [2] = 9, [3] = 10, [4] = 8 };

            var r = DashboardService.MontarResumoPacotes(pacotes, presencas, 0);

            Assert.Equal(new[] { "Carla", "Ana", "Bruno", "Davi" }, r.Renovacao.Select(p => p.AlunoNome));
        }

        [Fact]
        public void Renovacao_ignora_pacote_inativo_e_aluno_inativo()
        {
            var pacotes = new[]
            {
                Pacote(1, NovoAluno(1, "Ana"), 4, ativo: false),
                Pacote(2, NovoAluno(2, "Bia", ativo: false), 4),
            };
            var presencas = new Dictionary<int, int> { [1] = 4, [2] = 4 };

            var r = DashboardService.MontarResumoPacotes(pacotes, presencas, 0);

            Assert.Empty(r.Renovacao);
            Assert.Equal(0, r.Ativos);
            Assert.Equal(2, r.Concluidos);
        }

        // SC-002: os numeros do dashboard sao os mesmos da ficha do aluno.
        [Fact]
        public void Numeros_da_renovacao_sao_os_mesmos_do_calculo_da_ficha()
        {
            var pacote = Pacote(1, NovoAluno(1, "Ana"), 6);
            var presencas = new Dictionary<int, int> { [1] = 5 };

            var item = Assert.Single(DashboardService.MontarResumoPacotes(new[] { pacote }, presencas, 0).Renovacao);

            Assert.Equal(ProgressoPacote.Calcular(pacote, 5), item.Progresso);
        }

        [Fact]
        public void Pacote_sem_presenca_conta_zero_dadas()
        {
            var r = DashboardService.MontarResumoPacotes(new[] { Pacote(1, NovoAluno(1, "Ana"), 10) }, new Dictionary<int, int>(), 0);

            Assert.Equal(1, r.Ativos);
            Assert.Equal(0, r.AulasDadas);
            Assert.Equal(10, r.AulasRestantes);
        }
    }
}
