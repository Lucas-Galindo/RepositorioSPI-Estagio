using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/062: um lugar so diz quem tem periodo e quantos meses dura.
    public class ModalidadeCobrancaExtensionsTests
    {
        [Theory]
        [InlineData(ModalidadeCobranca.Avulsa, false, 0)]
        [InlineData(ModalidadeCobranca.Pacote, false, 0)]
        [InlineData(ModalidadeCobranca.Mensalidade, false, 0)]
        [InlineData(ModalidadeCobranca.Semestral, true, 6)]
        [InlineData(ModalidadeCobranca.Anual, true, 12)]
        public void Periodo_e_meses_de_cada_modalidade(ModalidadeCobranca modalidade, bool comPeriodo, int meses)
        {
            Assert.Equal(comPeriodo, modalidade.ComPeriodo());
            Assert.Equal(meses, modalidade.MesesDoPeriodo());
        }

        [Fact]
        public void Os_valores_numericos_novos_nao_mudam_os_antigos()
        {
            Assert.Equal(1, (int)ModalidadeCobranca.Avulsa);
            Assert.Equal(2, (int)ModalidadeCobranca.Mensalidade);
            Assert.Equal(3, (int)ModalidadeCobranca.Pacote);
            Assert.Equal(4, (int)ModalidadeCobranca.Semestral);
            Assert.Equal(5, (int)ModalidadeCobranca.Anual);
        }
    }
}
