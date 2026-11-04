using SPI.Application.Aulas.Services;
using SPI.Application.Common;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/067: o link vive so em memoria, ate o fim da aula (hora de Sao Paulo).
    public class LinkAulaTemporarioTests
    {
        private static readonly DateTime FimDaAula = new(2026, 11, 10, 16, 0, 0);
        private const string Link = "https://meet.exemplo.com/teste";

        [Fact]
        public void Define_e_obtem_o_link()
        {
            var relogio = new RelogioFalso(new DateTime(2026, 11, 10, 10, 0, 0));
            var armazenamento = new LinkAulaTemporario(relogio);

            armazenamento.Definir(1, Link, FimDaAula);

            Assert.Equal(Link, armazenamento.Obter(1));
            Assert.Null(armazenamento.Obter(2));
        }

        [Fact]
        public void Trocar_o_link_vale_o_ultimo()
        {
            var armazenamento = new LinkAulaTemporario(new RelogioFalso(new DateTime(2026, 11, 10, 10, 0, 0)));

            armazenamento.Definir(1, Link, FimDaAula);
            armazenamento.Definir(1, "https://zoom.exemplo.com/novo", FimDaAula);

            Assert.Equal("https://zoom.exemplo.com/novo", armazenamento.Obter(1));
        }

        [Fact]
        public void Remover_apaga_e_e_idempotente()
        {
            var armazenamento = new LinkAulaTemporario(new RelogioFalso(new DateTime(2026, 11, 10, 10, 0, 0)));
            armazenamento.Definir(1, Link, FimDaAula);

            armazenamento.Remover(1);
            armazenamento.Remover(1);

            Assert.Null(armazenamento.Obter(1));
        }

        [Fact]
        public void Expira_exatamente_no_fim_da_aula()
        {
            var relogio = new RelogioFalso(new DateTime(2026, 11, 10, 15, 59, 59));
            var armazenamento = new LinkAulaTemporario(relogio);
            armazenamento.Definir(1, Link, FimDaAula);

            Assert.Equal(Link, armazenamento.Obter(1)); // um segundo antes do fim

            relogio.Definir(FimDaAula);
            Assert.Null(armazenamento.Obter(1)); // no instante do fim
        }

        [Fact]
        public void Fim_que_ja_passou_nao_guarda_e_descarta_o_link_anterior()
        {
            var relogio = new RelogioFalso(new DateTime(2026, 11, 10, 10, 0, 0));
            var armazenamento = new LinkAulaTemporario(relogio);
            armazenamento.Definir(1, Link, FimDaAula);

            armazenamento.Definir(1, "https://outro.exemplo.com/x", new DateTime(2026, 11, 10, 9, 0, 0));

            Assert.Null(armazenamento.Obter(1));
        }

        [Fact]
        public void So_o_link_vencido_some_os_demais_continuam()
        {
            var relogio = new RelogioFalso(new DateTime(2026, 11, 10, 10, 0, 0));
            var armazenamento = new LinkAulaTemporario(relogio);
            armazenamento.Definir(1, Link, new DateTime(2026, 11, 10, 11, 0, 0));
            armazenamento.Definir(2, "https://b.exemplo.com/y", new DateTime(2026, 11, 10, 18, 0, 0));

            relogio.Definir(new DateTime(2026, 11, 10, 12, 0, 0));

            Assert.Null(armazenamento.Obter(1));
            Assert.Equal("https://b.exemplo.com/y", armazenamento.Obter(2));
        }

        // O fim da aula e hora de parede de Sao Paulo (UTC-3): 16:00 em SP = 19:00Z.
        [Fact]
        public void ParaInstanteUtc_converte_a_hora_de_sao_paulo()
        {
            Assert.Equal(new DateTimeOffset(2026, 11, 10, 19, 0, 0, TimeSpan.Zero), RelogioExtensions.ParaInstanteUtc(FimDaAula));
        }
    }
}
