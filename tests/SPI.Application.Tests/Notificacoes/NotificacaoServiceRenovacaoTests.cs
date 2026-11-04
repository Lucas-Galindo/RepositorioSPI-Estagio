using SPI.Application.Notificacoes.Services;
using SPI.Application.Tests.VinculosCobranca;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Notificacoes
{
    // specs/062 (US4): o aluno cujo plano Semestral/Anual termina em ate 15 dias (ou ja terminou) tem UM aviso de
    // renovacao, ate existir um plano que comece depois do fim ou o plano ser excluido.
    public class NotificacaoServiceRenovacaoTests
    {
        private static readonly DateOnly Hoje = new(2026, 10, 20);
        private static readonly DateTime Agora = Hoje.ToDateTime(new TimeOnly(9, 0));

        private readonly CenarioNotificacao _c = new();
        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly NotificacaoService _servico;

        public NotificacaoServiceRenovacaoTests()
        {
            _servico = new NotificacaoService(_c.Notificacoes, _c.Aulas, _c.Pagamentos, _c.Professores, new NotificacoesOptions(), _vinculos);
        }

        private List<Notificacao> DeRenovacao => _c.Notificacoes.Salvas.Where(n => n.Tipo == TipoNotificacao.RenovacaoPlano).ToList();

        private Aluno Aluno(int id, bool ativo = true)
        {
            var aluno = new Aluno { Id = id, Nome = $"Aluno {id}", Ativo = ativo };
            _c.Notificacoes.AlunosConhecidos[id] = aluno;
            _vinculos.AlunosConhecidos.Add(aluno);
            return aluno;
        }

        private VinculoCobranca Plano(int alunoId, DateOnly fim, ModalidadeCobranca modalidade = ModalidadeCobranca.Semestral, bool ativo = true) =>
            _vinculos.Semear(new VinculoCobranca
            {
                AlunoId = alunoId, Modalidade = modalidade, Valor = 1800m, DataInicio = fim.AddMonths(-6).AddDays(1), DataFim = fim, Ativo = ativo,
            });

        [Fact]
        public async Task Aviso_aparece_a_15_dias_do_fim_e_nao_aparece_antes()
        {
            Aluno(3);
            Plano(3, Hoje.AddDays(16));
            await _servico.SincronizarAsync(Agora, null);
            Assert.Empty(DeRenovacao);

            await _servico.SincronizarAsync(Agora.AddDays(1), null); // agora faltam 15 dias

            Assert.Single(DeRenovacao);
        }

        [Fact]
        public async Task Aviso_continua_depois_do_fim_e_a_mensagem_diz_terminou()
        {
            Aluno(3);
            Plano(3, Hoje.AddDays(-5), ModalidadeCobranca.Anual);

            var lista = await _servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora);

            var n = Assert.Single(lista, x => x.Tipo == TipoNotificacao.RenovacaoPlano);
            Assert.Equal("O plano Anual de Aluno 3 terminou em 15/10/2026.", n.Mensagem);
            Assert.Equal("Anual", n.Renovacao!.Modalidade);
            Assert.Equal(Hoje.AddDays(-5), n.Renovacao.DataFim);
            Assert.Equal("Aluno 3", n.Renovacao.AlunoNome);
            Assert.Null(n.Aula);
            Assert.Null(n.Pagamento);
        }

        [Fact]
        public async Task A_mensagem_antes_do_fim_diz_termina()
        {
            Aluno(3);
            Plano(3, Hoje.AddDays(10));

            var lista = await _servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora);

            Assert.Equal("O plano Semestral de Aluno 3 termina em 30/10/2026.", Assert.Single(lista, x => x.Tipo == TipoNotificacao.RenovacaoPlano).Mensagem);
        }

        // FR-014 (MUST NOT duplicar): rotina e listagem se alternando.
        [Fact]
        public async Task Sincronizar_varias_vezes_mantem_um_unico_aviso()
        {
            Aluno(3);
            Plano(3, Hoje.AddDays(5));

            await _servico.SincronizarAsync(Agora, null);
            await _servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora.AddMinutes(5));
            await _servico.SincronizarAsync(Agora.AddDays(1), null);

            Assert.Single(DeRenovacao);
        }

        [Fact]
        public async Task Renovacao_cadastrada_com_inicio_depois_do_fim_resolve_o_aviso()
        {
            Aluno(3);
            Plano(3, Hoje.AddDays(5));
            await _servico.SincronizarAsync(Agora, null);
            Assert.Null(DeRenovacao.Single().ResolvidaEm);

            Plano(3, Hoje.AddDays(5).AddMonths(6).AddDays(1)); // o proximo plano, comecando depois do fim do atual
            await _servico.SincronizarAsync(Agora.AddMinutes(10), null);

            Assert.NotNull(DeRenovacao.Single().ResolvidaEm);
            Assert.DoesNotContain(await _servico.ListarAtivasAsync(CenarioNotificacao.ProfessoraId, Agora), x => x.Tipo == TipoNotificacao.RenovacaoPlano);
        }

        [Fact]
        public async Task Excluir_o_plano_resolve_o_aviso()
        {
            Aluno(3);
            var plano = Plano(3, Hoje.AddDays(5));
            await _servico.SincronizarAsync(Agora, null);

            plano.Ativo = false;
            await _servico.SincronizarAsync(Agora.AddMinutes(10), null);

            Assert.NotNull(DeRenovacao.Single().ResolvidaEm);
        }

        [Fact]
        public async Task Aluno_inativo_nao_gera_aviso()
        {
            Aluno(3, ativo: false);
            Plano(3, Hoje.AddDays(5));

            await _servico.SincronizarAsync(Agora, null);

            Assert.Empty(DeRenovacao);
        }

        [Fact]
        public async Task Mensalidade_nao_tem_periodo_e_nao_gera_aviso()
        {
            Aluno(3);
            _vinculos.Semear(new VinculoCobranca { AlunoId = 3, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 250m, Ativo = true });

            await _servico.SincronizarAsync(Agora, null);

            Assert.Empty(DeRenovacao);
        }

        [Fact]
        public async Task Aviso_resolvido_volta_como_o_mesmo_registro_nao_lido_quando_o_plano_volta_a_precisar()
        {
            Aluno(3);
            var plano = Plano(3, Hoje.AddDays(5));
            await _servico.SincronizarAsync(Agora, null);
            plano.Ativo = false;
            await _servico.SincronizarAsync(Agora.AddMinutes(5), null);

            plano.Ativo = true;
            await _servico.SincronizarAsync(Agora.AddMinutes(10), null);

            var aviso = Assert.Single(DeRenovacao);
            Assert.Null(aviso.ResolvidaEm);
            Assert.Null(aviso.LidaEm);
        }

        [Fact]
        public async Task Cada_aluno_tem_o_seu_aviso()
        {
            Aluno(3);
            Aluno(4);
            Plano(3, Hoje.AddDays(5));
            Plano(4, Hoje.AddDays(-2), ModalidadeCobranca.Anual);

            await _servico.SincronizarAsync(Agora, null);

            Assert.Equal(new[] { 3, 4 }, DeRenovacao.Select(n => n.AlunoId!.Value).Order());
        }
    }
}
