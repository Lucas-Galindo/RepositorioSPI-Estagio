using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Notificacoes
{
    // specs/060 (US1): uma notificacao por aluno com pagamento vencido sem baixa.
    public class NotificacaoServicePagamentoTests
    {
        private static readonly DateOnly Hoje = new(2026, 10, 20);
        private static readonly DateTime Agora = Hoje.ToDateTime(new TimeOnly(9, 0));

        private readonly CenarioNotificacao _c = new();

        private List<Notificacao> DePagamento => _c.Notificacoes.Salvas.Where(n => n.Tipo == TipoNotificacao.PagamentoPendente).ToList();

        [Fact]
        public async Task Pendencias_vencidas_geram_uma_notificacao_por_aluno_com_o_detalhe()
        {
            _c.NovoPagamento(1, alunoId: 3, Hoje.AddDays(-14), valor: 80m);
            _c.NovoPagamento(2, alunoId: 3, Hoje.AddDays(-7), valor: 90m);
            _c.NovoPagamento(3, alunoId: 3, Hoje); // vence hoje: nao e pendencia (FR-001)

            var lista = await _c.Servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora);

            var n = Assert.Single(lista, x => x.Tipo == TipoNotificacao.PagamentoPendente);
            Assert.Null(n.Aula);
            Assert.Equal("Aluno 3", n.Pagamento!.AlunoNome);
            Assert.Equal(new[] { 1, 2 }, n.Pagamento.Pendencias.Select(p => p.PagamentoId));
            Assert.Equal(new[] { 14, 7 }, n.Pagamento.Pendencias.Select(p => p.DiasAtraso));
            Assert.Equal(new[] { 80m, 90m }, n.Pagamento.Pendencias.Select(p => p.Valor));
            Assert.Contains("Aluno 3", n.Mensagem);
        }

        [Fact]
        public async Task Pagamento_que_vence_hoje_ou_ja_pago_nao_gera_notificacao()
        {
            _c.NovoPagamento(1, alunoId: 3, Hoje);
            _c.NovoPagamento(2, alunoId: 4, Hoje.AddDays(-3), status: "Pago");

            await _c.Servico.SincronizarAsync(Agora, null);

            Assert.Empty(DePagamento);
        }

        // FR-003 (MUST NOT duplicar), inclusive com a listagem e a rotina se alternando.
        [Fact]
        public async Task Sincronizar_varias_vezes_mantem_uma_por_aluno()
        {
            _c.NovoPagamento(1, alunoId: 3, Hoje.AddDays(-2));

            await _c.Servico.SincronizarAsync(Agora, null);
            await _c.Servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora.AddMinutes(5));
            await _c.Servico.SincronizarAsync(Agora.AddDays(1), null);

            Assert.Single(DePagamento);
        }

        [Fact]
        public async Task Dois_alunos_geram_duas_notificacoes()
        {
            _c.NovoPagamento(1, alunoId: 3, Hoje.AddDays(-2));
            _c.NovoPagamento(2, alunoId: 4, Hoje.AddDays(-2));

            await _c.Servico.SincronizarAsync(Agora, null);

            Assert.Equal(new[] { 3, 4 }, DePagamento.Select(n => n.AlunoId!.Value).Order());
            Assert.All(DePagamento, n => Assert.Equal(CenarioNotificacao.ProfessoraId, n.ProfessorId));
        }

        [Fact]
        public async Task Dar_baixa_em_tudo_resolve_e_nova_pendencia_reativa_a_mesma()
        {
            var p1 = _c.NovoPagamento(1, alunoId: 3, Hoje.AddDays(-2));
            await _c.Servico.SincronizarAsync(Agora, null);
            var id = Assert.Single(DePagamento).Id;

            p1.Status = "Pago";
            await _c.Servico.SincronizarAsync(Agora.AddMinutes(1), null);
            Assert.NotNull(Assert.Single(DePagamento).ResolvidaEm);

            _c.NovoPagamento(2, alunoId: 3, Hoje.AddDays(-1));
            await _c.Servico.SincronizarAsync(Agora.AddMinutes(2), null);

            var n = Assert.Single(DePagamento);
            Assert.Equal(id, n.Id);
            Assert.Null(n.ResolvidaEm);
            Assert.Null(n.LidaEm);
        }

        [Fact]
        public async Task Sem_professora_cadastrada_a_rotina_nao_cria_aviso_de_pagamento()
        {
            _c.Professores.Unica = null;
            _c.NovoPagamento(1, alunoId: 3, Hoje.AddDays(-2));

            await _c.Servico.SincronizarAsync(Agora, null);

            Assert.Empty(DePagamento);
        }
    }
}
