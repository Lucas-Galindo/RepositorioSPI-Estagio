using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Infrastructure.BackgroundServices;
using Xunit;

namespace SPI.Application.Tests.Mensalidade
{
    // specs/039 (Polish, FR-011): uma falha ao gerar a cobranca de um vinculo
    // especifico nao impede os demais de serem processados, nem propaga a
    // excecao para quem chamou GerarCobrancasDoMesAsync.
    public class MensalidadeDispatcherServiceIsolamentoFalhaTests
    {
        private static readonly DateOnly Competencia = new(2026, 9, 1);

        [Fact]
        public async Task Falha_em_um_vinculo_nao_impede_os_demais_nem_lanca_excecao()
        {
            var vinculoA = new VinculoCobranca { Id = 1, AlunoId = 1, TurmaId = null, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 100m, Ativo = true };
            var vinculoB = new VinculoCobranca { Id = 2, AlunoId = 2, TurmaId = null, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 200m, Ativo = true };
            var vinculoC = new VinculoCobranca { Id = 3, AlunoId = 3, TurmaId = null, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 300m, Ativo = true };

            var vinculos = new FakeVinculoCobrancaRepositoryParaMensalidade();
            vinculos.Vinculos.Add(vinculoA);
            vinculos.Vinculos.Add(vinculoB);
            vinculos.Vinculos.Add(vinculoC);

            var pagamentos = new FakePagamentoRepositoryParaMensalidade
            {
                // Simula uma falha (ex.: dado inconsistente) so ao gerar a cobranca do vinculo B.
                FalharAoAdicionarSe = p => p.VinculoCobrancaId == vinculoB.Id
            };
            var categorias = new FakeCategoriaReceitaRepositoryParaMensalidade { Mensalidade = new CategoriaReceita { Id = 9, Nome = "Mensalidade", Ativo = true } };

            var excecao = await Record.ExceptionAsync(() =>
                MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculos, pagamentos, categorias, Competencia, logger: null, CancellationToken.None));

            Assert.Null(excecao);
            Assert.Equal(2, pagamentos.Gerados.Count);
            Assert.Contains(pagamentos.Gerados, p => p.VinculoCobrancaId == vinculoA.Id);
            Assert.Contains(pagamentos.Gerados, p => p.VinculoCobrancaId == vinculoC.Id);
            Assert.DoesNotContain(pagamentos.Gerados, p => p.VinculoCobrancaId == vinculoB.Id);
        }
    }
}
