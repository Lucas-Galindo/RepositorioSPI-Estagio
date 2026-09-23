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

        private static VinculoCobranca NovoVinculo(int id, int alunoId, int? turmaId, decimal valor, Turma? turma = null, bool ativo = true) => new()
        {
            Id = id,
            AlunoId = alunoId,
            TurmaId = turmaId,
            Modalidade = ModalidadeCobranca.Mensalidade,
            Valor = valor,
            Ativo = ativo,
            Turma = turma,
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
        public async Task Vinculo_com_turma_gera_cobranca_com_valor_cheio_e_contexto_da_turma()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            var turma = new Turma { Id = 4, Nome = "Turma A", Ativo = true };
            vinculos.Vinculos.Add(NovoVinculo(1, alunoId: 1, turmaId: 4, valor: 300m, turma: turma));

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Equal(1, gerado.AlunoId);
            Assert.Equal(1, gerado.VinculoCobrancaId);
            Assert.Equal(300m, gerado.ValorFinal);
            Assert.Equal("Pendente", gerado.Status);
            Assert.Equal(Competencia, gerado.Competencia);
            Assert.Equal(9, gerado.CategoriaReceitaId);
            Assert.Contains("Turma A", gerado.Descricao);
            Assert.Contains("Mensalidade", gerado.Descricao);
            Assert.Contains("09/2026", gerado.Descricao);
        }

        [Fact]
        public async Task Vinculo_de_atendimento_individual_usa_esse_termo_no_contexto()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            vinculos.Vinculos.Add(NovoVinculo(2, alunoId: 1, turmaId: null, valor: 250m));

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Contains("Atendimento individual", gerado.Descricao);
        }

        [Fact]
        public async Task DataVencimento_e_o_dia_1_do_mes_seguinte_a_competencia()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            vinculos.Vinculos.Add(NovoVinculo(3, alunoId: 1, turmaId: null, valor: 100m));

            await MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None);

            var gerado = Assert.Single(pagamentos.Gerados);
            Assert.Equal(new DateOnly(2026, 10, 1), gerado.DataVencimento);
        }

        [Fact]
        public async Task Dois_vinculos_mensalidade_do_mesmo_aluno_geram_uma_cobranca_cada()
        {
            var (vinculos, pagamentos, categorias) = CriarFakes();
            var turma = new Turma { Id = 4, Nome = "Turma A", Ativo = true };
            vinculos.Vinculos.Add(NovoVinculo(1, alunoId: 1, turmaId: 4, valor: 300m, turma: turma));
            vinculos.Vinculos.Add(NovoVinculo(2, alunoId: 1, turmaId: null, valor: 250m));

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
