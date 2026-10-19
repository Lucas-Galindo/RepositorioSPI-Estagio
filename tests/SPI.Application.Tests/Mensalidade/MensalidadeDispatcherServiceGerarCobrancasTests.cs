using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Infrastructure.BackgroundServices;
using Xunit;

namespace SPI.Application.Tests.Mensalidade
{
    // specs/039 (US1): geracao das cobrancas do mes -- um Pagamento por vinculo
    // Mensalidade ativo, pelo valor cheio, independente de presenca (FR-002,
    // FR-003, FR-006, FR-007). Chama GerarCobrancasDoMesAsync diretamente,
    // sem IServiceScopeFactory/BackgroundService (research.md R7).
    public class MensalidadeDispatcherServiceGerarCobrancasTests
    {
        private static readonly DateOnly Competencia = new(2026, 9, 1);

        // specs/056: mensalidade e do aluno, nunca tem turma.
        private static VinculoCobranca NovoVinculo(int id, int alunoId, decimal valor, bool ativo = true) => new()
        {
            Id = id,
            AlunoId = alunoId,
            Modalidade = ModalidadeCobranca.Mensalidade,
            Valor = valor,
            Ativo = ativo,
        };

        private static (
            FakeVinculoCobrancaRepositoryParaMensalidade Vinculos,
            FakePagamentoRepositoryParaMensalidade Pagamentos,
            FakeCategoriaReceitaRepositoryParaMensalidade Categorias) CriarFakes()
        {
            var vinculos = new FakeVinculoCobrancaRepositoryParaMensalidade();
            var pagamentos = new FakePagamentoRepositoryParaMensalidade();
            var categorias = new FakeCategoriaReceitaRepositoryParaMensalidade { Mensalidade = new CategoriaReceita { Id = 9, Nome = "Mensalidade", Ativo = true } };
            return (vinculos, pagamentos, categorias);
        }

        [Fact]
        public async Task Gera_uma_cobranca_com_valor_cheio_e_descricao_da_competencia()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            vinculos.Vinculos.Add(NovoVinculo(1, alunoId: 1, valor: 300m));

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Equal(1, gerado.AlunoId);
            Assert.Equal(1, gerado.VinculoCobrancaId);
            Assert.Equal(300m, gerado.ValorFinal);
            Assert.Equal("Pendente", gerado.Status);
            Assert.Equal(Competencia, gerado.Competencia);
            Assert.Equal(9, gerado.CategoriaReceitaId);
            Assert.Equal("Mensalidade - 09/2026", gerado.Descricao);
        }

        [Fact]
        public async Task DataVencimento_e_o_dia_10_do_mes_seguinte_a_competencia()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            vinculos.Vinculos.Add(NovoVinculo(3, alunoId: 1, valor: 100m));

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Equal(new DateOnly(2026, 10, 10), gerado.DataVencimento);
        }

        // specs/056 (edge case): duas mensalidades ativas so existem como conflito herdado
        // da regra antiga; ate a professora resolver, cada uma continua gerando a sua
        // cobranca (o sistema nao soma nem descarta valores sozinho).
        [Fact]
        public async Task Duas_mensalidades_herdadas_do_mesmo_aluno_geram_uma_cobranca_cada()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            vinculos.Vinculos.Add(NovoVinculo(1, alunoId: 1, valor: 300m));
            vinculos.Vinculos.Add(NovoVinculo(2, alunoId: 1, valor: 250m));

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            Assert.Equal(2, pagamentos.Gerados.Count);
            Assert.Contains(pagamentos.Gerados, p => p.VinculoCobrancaId == 1 && p.ValorFinal == 300m);
            Assert.Contains(pagamentos.Gerados, p => p.VinculoCobrancaId == 2 && p.ValorFinal == 250m);
        }

        [Fact]
        public async Task Sem_vinculos_mensalidade_ativos_nao_gera_nada_e_nao_lanca_excecao()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            // Vinculo de outra modalidade nao deve ser considerado.
            vinculos.Vinculos.Add(new VinculoCobranca { Id = 5, AlunoId = 1, Modalidade = ModalidadeCobranca.Avulsa, Valor = 80m, Ativo = true });

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            Assert.Empty(pagamentos.Gerados);
        }
    }
}
