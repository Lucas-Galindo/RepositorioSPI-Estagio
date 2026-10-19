using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Notificacoes
{
    // specs/060 (US3): aviso de aula futura na vespera, a partir das 08:00
    // (padrao de NotificacoesOptions), ate a aula terminar.
    public class NotificacaoServiceAulaFuturaTests
    {
        private static readonly DateOnly Hoje = new(2026, 10, 20);
        private static readonly DateOnly Amanha = Hoje.AddDays(1);

        private readonly CenarioNotificacao _c = new();

        private List<Notificacao> Futuras => _c.Notificacoes.Salvas.Where(n => n.Tipo == TipoNotificacao.AulaFutura).ToList();

        private static DateTime Hora(DateOnly dia, int h, int m = 0) => dia.ToDateTime(new TimeOnly(h, m));

        [Fact]
        public async Task Antes_das_8h_da_vespera_nao_aparece_e_as_8h_aparece()
        {
            _c.NovaAula(1, Amanha);

            await _c.Servico.SincronizarAsync(Hora(Hoje, 7, 59), null);
            Assert.Empty(Futuras);

            var lista = await _c.Servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Hora(Hoje, 8));

            var n = Assert.Single(lista, x => x.Tipo == TipoNotificacao.AulaFutura);
            Assert.Equal(1, n.Aula!.Id);
            Assert.Equal("Você tem uma aula agendada em breve.", n.Mensagem);
        }

        [Fact]
        public async Task Aula_cancelada_resolve_o_aviso()
        {
            var aula = _c.NovaAula(1, Amanha);
            await _c.Servico.SincronizarAsync(Hora(Hoje, 9), null);

            aula.Status = "Cancelada";
            await _c.Servico.SincronizarAsync(Hora(Hoje, 10), null);

            Assert.NotNull(Assert.Single(Futuras).ResolvidaEm);
        }

        // Quando a aula termina, o aviso de aula futura sai e entra o de aula pendente (059).
        [Fact]
        public async Task Aula_que_termina_sem_status_troca_o_aviso_futuro_pelo_pendente()
        {
            _c.NovaAula(1, Amanha);
            await _c.Servico.SincronizarAsync(Hora(Hoje, 9), null);

            await _c.Servico.SincronizarAsync(Hora(Amanha, 11, 30), null);

            Assert.NotNull(Assert.Single(Futuras).ResolvidaEm);
            var pendente = Assert.Single(_c.Notificacoes.Salvas, n => n.Tipo == TipoNotificacao.AulaPendente);
            Assert.Null(pendente.ResolvidaEm);
        }

        [Fact]
        public async Task Aula_remarcada_sai_da_janela_e_volta_na_nova_vespera_com_o_mesmo_aviso()
        {
            var aula = _c.NovaAula(1, Amanha);
            await _c.Servico.SincronizarAsync(Hora(Hoje, 9), null);
            var id = Assert.Single(Futuras).Id;

            aula.DataInicio = Hoje.AddDays(3);
            await _c.Servico.SincronizarAsync(Hora(Hoje, 10), null);
            Assert.NotNull(Assert.Single(Futuras).ResolvidaEm);

            await _c.Servico.SincronizarAsync(Hora(Hoje.AddDays(2), 8), null);

            var n = Assert.Single(Futuras);
            Assert.Equal(id, n.Id);
            Assert.Null(n.ResolvidaEm);
        }

        // FR-003 (MUST NOT duplicar).
        [Fact]
        public async Task Sincronizar_varias_vezes_mantem_um_aviso_por_aula()
        {
            _c.NovaAula(1, Amanha);

            await _c.Servico.SincronizarAsync(Hora(Hoje, 8), null);
            await _c.Servico.SincronizarAsync(Hora(Hoje, 12), null);
            await _c.Servico.SincronizarAsync(Hora(Amanha, 9), null);

            Assert.Single(Futuras);
        }
    }
}
