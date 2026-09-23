using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Infrastructure.BackgroundServices;
using Xunit;

namespace SPI.Application.Tests.Mensalidade
{
    // specs/039 (US2): a implementacao ja existe desde
    // MensalidadeDispatcherServiceGerarCobrancasTests (T014) -- esta suite so
    // valida, de forma isolada, que a mesma competencia nunca duplica (FR-004).
    public class MensalidadeDispatcherServiceIdempotenciaTests
    {
        private static readonly DateOnly Competencia = new(2026, 9, 1);

        private static VinculoCobranca NovoVinculo(int id, decimal valor = 300m) => new()
        {
            Id = id,
            AlunoId = 1,
            TurmaId = null,
            Modalidade = ModalidadeCobranca.Mensalidade,
            Valor = valor,
            Ativo = true,
        };

        private static (FakeVinculoCobrancaRepositoryParaMensalidade Vinculos, FakePagamentoRepositoryParaMensalidade Pagamentos, FakeCategoriaReceitaRepositoryParaMensalidade Categorias) CriarFakes()
        {
            var vinculos = new FakeVinculoCobrancaRepositoryParaMensalidade();
            var pagamentos = new FakePagamentoRepositoryParaMensalidade();
            var categorias = new FakeCategoriaReceitaRepositoryParaMensalidade { Mensalidade = new CategoriaReceita { Id = 9, Nome = "Mensalidade", Ativo = true } };
            return (vinculos, pagamentos, categorias);
        }

        [Fact]
        public async Task Chamar_duas_vezes_seguidas_para_o_mesmo_vinculo_e_competencia_gera_so_uma_conta()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            vinculos.Vinculos.Add(NovoVinculo(1));

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);
            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            Assert.Single(pagamentos.Gerados);
        }

        [Fact]
        public async Task Cobranca_ja_gerada_antes_da_chamada_simulando_reinicio_nao_e_duplicada()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            var vinculo = NovoVinculo(1);
            vinculos.Vinculos.Add(vinculo);
            pagamentos.JaGerados.Add((vinculo.Id, Competencia));

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            Assert.Empty(pagamentos.Gerados);
        }

        [Fact]
        public async Task Mesmo_vinculo_em_competencias_diferentes_gera_uma_conta_para_cada()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            vinculos.Vinculos.Add(NovoVinculo(1));
            var competenciaSeguinte = Competencia.AddMonths(1);

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);
            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, competenciaSeguinte, logger: null, CancellationToken.None);

            Assert.Equal(2, pagamentos.Gerados.Count);
            Assert.Contains(pagamentos.Gerados, p => p.Competencia == Competencia);
            Assert.Contains(pagamentos.Gerados, p => p.Competencia == competenciaSeguinte);
        }
    }
}
