using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Notificacoes
{
    // specs/059: uma notificacao por aula que terminou sem status (FR-001 a
    // FR-013). "Agora" fixo em 2026-10-20 12:00 -- aula das 10h-11h de hoje ja terminou.
    public class NotificacaoServiceTests
    {
        private static readonly DateOnly Hoje = new(2026, 10, 20);
        private static readonly DateTime Agora = Hoje.ToDateTime(new TimeOnly(12, 0));

        private readonly CenarioNotificacao _c = new();

        private int Linhas(int aulaId) => _c.Notificacoes.Salvas.Count(n => n.AulaId == aulaId);

        // ---------- US1: criacao ----------

        [Fact]
        public async Task Aula_que_terminou_sem_status_gera_uma_notificacao_nao_lida()
        {
            _c.NovaAula(1, Hoje);

            var lista = await _c.Servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora);

            var n = Assert.Single(lista);
            Assert.False(n.Lida);
            Assert.Equal(1, n.Aula!.Id);
            Assert.Equal("Você ainda não atualizou o status desta aula.", n.Mensagem);
            Assert.Equal("Turma A", n.Aula.Contexto);
            Assert.Equal("Matematica", n.Aula.MateriaNome);
        }

        // FR-003 (MUST NOT duplicar), inclusive em dias seguintes.
        [Fact]
        public async Task Sincronizar_varias_vezes_e_em_dias_seguintes_mantem_uma_so()
        {
            _c.NovaAula(1, Hoje);

            await _c.Servico.SincronizarAulasPendentesAsync(Agora, null);
            await _c.Servico.SincronizarAulasPendentesAsync(Agora.AddMinutes(5), null);
            await _c.Servico.SincronizarAulasPendentesAsync(Agora.AddDays(2), null);

            Assert.Equal(1, Linhas(1));
            Assert.Null(_c.Notificacoes.Salvas.Single().ResolvidaEm);
        }

        [Fact]
        public async Task Insercao_concorrente_barrada_pelo_indice_nao_propaga_erro()
        {
            _c.NovaAula(1, Hoje);
            _c.Notificacoes.LancarConflitoNoProximoSalvar = true;

            await _c.Servico.SincronizarAulasPendentesAsync(Agora, null);
            await _c.Servico.SincronizarAulasPendentesAsync(Agora, null);

            Assert.Equal(1, Linhas(1));
        }

        [Fact]
        public async Task Aula_que_ainda_nao_terminou_nao_gera_notificacao()
        {
            _c.NovaAula(1, Hoje);

            await _c.Servico.SincronizarAulasPendentesAsync(Hoje.ToDateTime(new TimeOnly(10, 59)), null);

            Assert.Empty(_c.Notificacoes.Salvas);
        }

        // FR-004 (MUST NOT): aula que terminou antes da entrada no ar nao gera aviso.
        [Fact]
        public async Task Aula_que_terminou_antes_da_vigencia_nunca_gera_notificacao()
        {
            _c.NovaAula(1, DateOnly.FromDateTime(CenarioNotificacao.Desde).AddDays(-1));

            await _c.Servico.SincronizarAulasPendentesAsync(Agora, null);

            Assert.Empty(_c.Notificacoes.Salvas);
        }

        [Fact]
        public async Task Cada_professora_ve_so_as_proprias()
        {
            _c.NovaAula(1, Hoje);
            _c.NovaAula(2, Hoje, professorId: 99);

            var lista = await _c.Servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora);
            await _c.Servico.SincronizarAulasPendentesAsync(Agora, null);

            Assert.Equal(1, Assert.Single(lista).Aula!.Id);
            Assert.Equal(99, _c.Notificacoes.Salvas.Single(n => n.AulaId == 2).ProfessorId);
        }

        [Fact]
        public async Task Aula_individual_mostra_atendimento_individual_com_o_aluno()
        {
            _c.NovaAula(1, Hoje, turmaNome: null);

            var lista = await _c.Servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora);

            Assert.Equal("Atendimento individual · Bruno", Assert.Single(lista).Aula!.Contexto);
        }

        [Fact]
        public async Task Lista_vem_das_mais_recentes_para_as_mais_antigas()
        {
            _c.NovaAula(1, Hoje.AddDays(-1));
            await _c.Servico.SincronizarAulasPendentesAsync(Agora.AddDays(-1), null);
            _c.NovaAula(2, Hoje);

            var lista = await _c.Servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora);

            Assert.Equal(new[] { 2, 1 }, lista.Select(n => n.Aula!.Id));
        }

        // ---------- US2: lida ----------

        [Fact]
        public async Task Marcar_como_lida_nao_resolve_e_e_idempotente()
        {
            _c.NovaAula(1, Hoje);
            var id = Assert.Single(await _c.Servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora)).Id;

            await _c.Servico.MarcarComoLidaAsync(CenarioNotificacao.ProfessoraId, id, Agora);
            await _c.Servico.MarcarComoLidaAsync(CenarioNotificacao.ProfessoraId, id, Agora.AddHours(1));

            var n = _c.Notificacoes.Salvas.Single();
            Assert.Equal(Agora, n.LidaEm);
            Assert.Null(n.ResolvidaEm);
            Assert.True(Assert.Single(await _c.Servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora)).Lida);
        }

        [Fact]
        public async Task Marcar_notificacao_de_outra_professora_responde_nao_encontrada()
        {
            _c.NovaAula(1, Hoje, professorId: 99);
            await _c.Servico.SincronizarAulasPendentesAsync(Agora, null);
            var id = _c.Notificacoes.Salvas.Single().Id;

            await Assert.ThrowsAsync<NaoEncontradoException>(() => _c.Servico.MarcarComoLidaAsync(CenarioNotificacao.ProfessoraId, id, Agora));
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _c.Servico.MarcarComoLidaAsync(CenarioNotificacao.ProfessoraId, 999, Agora));
            Assert.Null(_c.Notificacoes.Salvas.Single().LidaEm);
        }

        // ---------- US3: resolucao ----------

        [Theory]
        [InlineData("Realizada", true)]
        [InlineData("Cancelada", true)]
        [InlineData("Agendada", false)]
        public async Task Aula_atualizada_ou_excluida_resolve_a_notificacao(string status, bool ativa)
        {
            var aula = _c.NovaAula(1, Hoje);
            await _c.Servico.SincronizarAulasPendentesAsync(Agora, null);

            aula.Status = status;
            aula.Ativo = ativa;
            var lista = await _c.Servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora.AddMinutes(1));

            Assert.Empty(lista);
            Assert.Equal(Agora.AddMinutes(1), _c.Notificacoes.Salvas.Single().ResolvidaEm);
        }

        [Fact]
        public async Task Aula_movida_para_o_futuro_resolve_e_ao_passar_de_novo_reativa_a_mesma()
        {
            var aula = _c.NovaAula(1, Hoje);
            await _c.Servico.SincronizarAulasPendentesAsync(Agora, null);
            var id = _c.Notificacoes.Salvas.Single().Id;
            await _c.Servico.MarcarComoLidaAsync(CenarioNotificacao.ProfessoraId, id, Agora);

            aula.DataInicio = Hoje.AddDays(1);
            await _c.Servico.SincronizarAulasPendentesAsync(Agora.AddMinutes(1), null);
            Assert.NotNull(_c.Notificacoes.Salvas.Single().ResolvidaEm);

            var depois = Agora.AddDays(1).AddHours(1);
            await _c.Servico.SincronizarAulasPendentesAsync(depois, null);

            var n = Assert.Single(_c.Notificacoes.Salvas);
            Assert.Equal(id, n.Id);
            Assert.Null(n.ResolvidaEm);
            Assert.Null(n.LidaEm);
            Assert.Equal(depois, n.CriadaEm);
        }

        // FR-012 (MUST NOT): resolvida nao volta enquanto a aula nao voltar a ficar pendente.
        [Fact]
        public async Task Notificacao_resolvida_nao_volta_sozinha()
        {
            var aula = _c.NovaAula(1, Hoje);
            await _c.Servico.SincronizarAulasPendentesAsync(Agora, null);
            aula.Status = "Realizada";
            await _c.Servico.SincronizarAulasPendentesAsync(Agora.AddMinutes(1), null);

            await _c.Servico.SincronizarAulasPendentesAsync(Agora.AddDays(3), null);

            var n = Assert.Single(_c.Notificacoes.Salvas);
            Assert.Equal(Agora.AddMinutes(1), n.ResolvidaEm);
        }
    }
}
