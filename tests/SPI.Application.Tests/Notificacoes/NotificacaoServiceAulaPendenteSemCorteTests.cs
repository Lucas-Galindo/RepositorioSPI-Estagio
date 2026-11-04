using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Notificacoes
{
    // specs/064 (US3): toda aula ativa, Agendada, que ja terminou sem frequencia tem um (e so um) aviso,
    // sem corte de data; aula em andamento nao gera; o aviso some quando a frequencia e registrada.
    public class NotificacaoServiceAulaPendenteSemCorteTests
    {
        private static readonly DateOnly Hoje = new(2026, 11, 10);
        private static readonly DateTime Agora = Hoje.ToDateTime(new TimeOnly(12, 0));

        private readonly CenarioNotificacao _c = new();

        private List<Notificacao> Pendentes => _c.Notificacoes.Salvas.Where(n => n.Tipo == TipoNotificacao.AulaPendente).ToList();

        [Fact]
        public async Task Aulas_antigas_de_qualquer_data_geram_exatamente_um_aviso_cada()
        {
            _c.NovaAula(1, new DateOnly(2026, 10, 1));  // bem antes do antigo corte de 19/10/2026
            _c.NovaAula(2, new DateOnly(2026, 10, 18)); // a vespera do corte
            _c.NovaAula(3, Hoje.AddDays(-1));

            await _c.Servico.SincronizarAsync(Agora, null);
            await _c.Servico.SincronizarAsync(Agora.AddMinutes(5), null);

            Assert.Equal(new[] { 1, 2, 3 }, Pendentes.Select(n => n.AulaId!.Value).OrderBy(i => i));
            Assert.All(Pendentes, n => Assert.Null(n.ResolvidaEm));
        }

        [Fact]
        public async Task Aula_em_andamento_nao_gera_aviso()
        {
            _c.NovaAula(1, Hoje); // das 10h as 11h

            await _c.Servico.SincronizarAsync(Hoje.ToDateTime(new TimeOnly(10, 30)), null);

            Assert.Empty(Pendentes);
        }

        [Fact]
        public async Task Ao_terminar_a_aula_o_aviso_nasce_no_primeiro_minuto_depois_do_fim()
        {
            _c.NovaAula(1, Hoje);

            await _c.Servico.SincronizarAsync(Hoje.ToDateTime(new TimeOnly(10, 59)), null);
            Assert.Empty(Pendentes);

            await _c.Servico.SincronizarAsync(Hoje.ToDateTime(new TimeOnly(11, 0)), null);
            Assert.Single(Pendentes);
        }

        [Fact]
        public async Task Registrar_a_frequencia_resolve_o_aviso_da_aula_antiga()
        {
            var aula = _c.NovaAula(1, new DateOnly(2026, 10, 1));
            await _c.Servico.SincronizarAsync(Agora, null);
            Assert.Null(Pendentes.Single().ResolvidaEm);

            aula.Status = "Realizada"; // a chamada foi registrada nos detalhes
            await _c.Servico.SincronizarAsync(Agora.AddMinutes(1), null);

            Assert.Equal(Agora.AddMinutes(1), Pendentes.Single().ResolvidaEm);
        }

        [Fact]
        public async Task Aula_cancelada_ou_excluida_nao_gera_aviso()
        {
            _c.NovaAula(1, new DateOnly(2026, 10, 1)).Status = "Cancelada";
            _c.NovaAula(2, new DateOnly(2026, 10, 2)).Ativo = false;

            await _c.Servico.SincronizarAsync(Agora, null);

            Assert.Empty(Pendentes);
        }
    }
}
