using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/058 (research.md R2): calculo unico de progresso de pacote.
    public class ProgressoPacoteTests
    {
        private static VinculoCobranca Pacote(int? total, int? saldo = null) =>
            new() { Id = 1, AlunoId = 1, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, TotalAulasPacote = total, SaldoAulas = saldo, Ativo = true };

        [Theory]
        [InlineData(4, 3, 1, 75)]
        [InlineData(20, 3, 17, 15)]
        [InlineData(10, 6, 4, 60)]
        public void Tamanhos_diferentes_usam_o_proprio_total(int total, int dadas, int restantes, int percentual)
        {
            var p = ProgressoPacote.Calcular(Pacote(total), dadas);

            Assert.Equal(total, p.Total);
            Assert.False(p.TotalEstimado);
            Assert.Equal(dadas, p.AulasDadas);
            Assert.Equal(restantes, p.AulasRestantes);
            Assert.Equal(percentual, p.AulasDadas * 100 / p.Total!.Value);
        }

        // O saldo nao entra no calculo quando ha total (decisao do usuario: contar presencas).
        [Fact]
        public void Com_total_informado_o_saldo_e_ignorado()
        {
            var p = ProgressoPacote.Calcular(Pacote(total: 10, saldo: 9), aulasDadas: 6);

            Assert.Equal(4, p.AulasRestantes);
        }

        [Fact]
        public void Sem_total_estima_pelas_dadas_mais_o_saldo()
        {
            var p = ProgressoPacote.Calcular(Pacote(total: null, saldo: 5), aulasDadas: 3);

            Assert.Equal(8, p.Total);
            Assert.True(p.TotalEstimado);
            Assert.Equal(5, p.AulasRestantes);
            Assert.Equal(ProgressoPacote.EmDia, p.Estado);
        }

        [Fact]
        public void Sem_total_nem_saldo_fica_sem_total()
        {
            var p = ProgressoPacote.Calcular(Pacote(total: null), aulasDadas: 3);

            Assert.Null(p.Total);
            Assert.Null(p.AulasRestantes);
            Assert.Equal(3, p.AulasDadas);
            Assert.Equal(ProgressoPacote.SemTotal, p.Estado);
        }

        [Theory]
        [InlineData(10, ProgressoPacote.Esgotado)]
        [InlineData(9, ProgressoPacote.Atencao)]
        [InlineData(8, ProgressoPacote.Atencao)]
        [InlineData(7, ProgressoPacote.EmDia)]
        [InlineData(0, ProgressoPacote.EmDia)]
        public void Estado_pelo_numero_de_restantes(int dadas, string estado)
        {
            Assert.Equal(estado, ProgressoPacote.Calcular(Pacote(10), dadas).Estado);
        }

        [Fact]
        public void Mais_dadas_que_o_total_nunca_fica_negativo()
        {
            var p = ProgressoPacote.Calcular(Pacote(5), aulasDadas: 7);

            Assert.Equal(0, p.AulasRestantes);
            Assert.Equal(ProgressoPacote.Esgotado, p.Estado);
        }
    }
}
