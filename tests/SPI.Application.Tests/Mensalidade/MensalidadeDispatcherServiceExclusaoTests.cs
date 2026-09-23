using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Infrastructure.BackgroundServices;
using Xunit;

namespace SPI.Application.Tests.Mensalidade
{
    // specs/039 (US3): a implementacao ja existe desde
    // MensalidadeDispatcherServiceGerarCobrancasTests (T014) -- esta suite so
    // valida, de forma isolada, que um vinculo excluido no momento do disparo
    // nao e cobrado (FR-005).
    public class MensalidadeDispatcherServiceExclusaoTests
    {
        private static readonly DateOnly Competencia = new(2026, 9, 1);

        private static (FakeVinculoCobrancaRepositoryParaMensalidade Vinculos, FakePagamentoRepositoryParaMensalidade Pagamentos, FakeCategoriaReceitaRepositoryParaMensalidade Categorias) CriarFakes()
        {
            var vinculos = new FakeVinculoCobrancaRepositoryParaMensalidade();
            var pagamentos = new FakePagamentoRepositoryParaMensalidade();
            var categorias = new FakeCategoriaReceitaRepositoryParaMensalidade { Mensalidade = new CategoriaReceita { Id = 9, Nome = "Mensalidade", Ativo = true } };
            return (vinculos, pagamentos, categorias);
        }

        [Fact]
        public async Task Vinculo_excluido_nao_gera_cobranca()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            vinculos.Vinculos.Add(new VinculoCobranca { Id = 1, AlunoId = 1, TurmaId = null, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 300m, Ativo = false });

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            Assert.Empty(pagamentos.Gerados);
        }

        [Fact]
        public async Task Entre_dois_vinculos_do_mesmo_aluno_so_o_ativo_gera_cobranca()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            vinculos.Vinculos.Add(new VinculoCobranca { Id = 1, AlunoId = 1, TurmaId = null, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 300m, Ativo = false });
            vinculos.Vinculos.Add(new VinculoCobranca { Id = 2, AlunoId = 1, TurmaId = 4, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 250m, Ativo = true, Turma = new Turma { Id = 4, Nome = "Turma A", Ativo = true } });

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Equal(2, gerado.VinculoCobrancaId);
        }

        [Fact]
        public async Task Vinculo_reativado_antes_do_disparo_gera_cobranca_normalmente()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            // Ativo=true no momento da chamada e o que importa, independente de ter
            // sido excluido e reativado antes (US3-2) -- o fake so reflete o estado atual.
            vinculos.Vinculos.Add(new VinculoCobranca { Id = 1, AlunoId = 1, TurmaId = null, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 300m, Ativo = true });

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            Assert.Single(pagamentos.Gerados);
        }
    }
}
