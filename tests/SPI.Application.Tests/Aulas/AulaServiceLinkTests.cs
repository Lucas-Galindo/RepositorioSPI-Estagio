using SPI.Application.Aulas.Dtos;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/067: link da reuniao so em memoria; aceito ate o fim da aula; some ao cancelar/excluir;
    // acompanha a edicao de horario; nunca aparece em mensagem de erro.
    public class AulaServiceLinkTests
    {
        private static readonly DateOnly Dia = new(2026, 11, 10);
        private static readonly DateTime Manha = new(2026, 11, 10, 10, 0, 0);
        private const string Link = "https://meet.exemplo.com/teste-067";

        private static AulaRequest RequestDeTurma(int horaInicio = 15, int horaFim = 16) => new()
        {
            MateriaId = 1, TurmaId = CenarioCadastroAula.TurmaId, DataInicio = Dia,
            HoraInicio = new TimeOnly(horaInicio, 0), HoraFim = new TimeOnly(horaFim, 0),
        };

        // Aula de turma de 10/11/2026 das 15h as 16h, criada com o relogio as 10h.
        private static async Task<(CenarioCadastroAula Cenario, int AulaId)> AulaAsync()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"));
            cenario.Relogio.Definir(Manha);
            var aula = await cenario.Servico.CadastrarAsync(1, RequestDeTurma());
            return (cenario, aula.Id);
        }

        [Fact]
        public async Task Definir_o_link_aparece_nos_detalhes_e_na_lista()
        {
            var (cenario, aulaId) = await AulaAsync();

            var resposta = await cenario.Servico.DefinirLinkAsync(aulaId, Link);

            Assert.Equal(Link, resposta.LinkAula);
            Assert.True(resposta.AceitaLinkAula);
            Assert.Equal(Link, (await cenario.Servico.ObterPorIdAsync(aulaId)).LinkAula);
            Assert.Equal(Link, (await cenario.Servico.ListarAsync(null, null, null, null, null)).Single().LinkAula);
        }

        [Fact]
        public async Task Aula_sem_link_continua_como_hoje()
        {
            var (cenario, aulaId) = await AulaAsync();

            var resposta = await cenario.Servico.ObterPorIdAsync(aulaId);

            Assert.Null(resposta.LinkAula);
            Assert.True(resposta.AceitaLinkAula);
        }

        [Fact]
        public async Task Trocar_o_link_vale_o_ultimo_e_remover_apaga_de_forma_idempotente()
        {
            var (cenario, aulaId) = await AulaAsync();
            await cenario.Servico.DefinirLinkAsync(aulaId, Link);

            var trocado = await cenario.Servico.DefinirLinkAsync(aulaId, "https://zoom.exemplo.com/novo");
            Assert.Equal("https://zoom.exemplo.com/novo", trocado.LinkAula);

            Assert.Null((await cenario.Servico.RemoverLinkAsync(aulaId)).LinkAula);
            Assert.Null((await cenario.Servico.RemoverLinkAsync(aulaId)).LinkAula);
            Assert.Null((await cenario.Servico.ObterPorIdAsync(aulaId)).LinkAula);
        }

        // --- quem aceita link ---

        [Fact]
        public async Task Aula_cancelada_recusa_o_link_sem_vazar_o_link_na_mensagem()
        {
            var (cenario, aulaId) = await AulaAsync();
            await cenario.Servico.CancelarAsync(aulaId);

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.DefinirLinkAsync(aulaId, Link));

            Assert.Equal("Esta aula nao aceita link: ja terminou ou foi cancelada.", excecao.Message);
            Assert.DoesNotContain(Link, excecao.Message);
            Assert.Null(cenario.Links.Obter(aulaId));
        }

        [Fact]
        public async Task Aula_que_ja_terminou_recusa_o_link()
        {
            var (cenario, aulaId) = await AulaAsync();
            cenario.Relogio.Definir(new DateTime(2026, 11, 10, 16, 0, 0)); // instante do fim

            await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.DefinirLinkAsync(aulaId, Link));

            Assert.False((await cenario.Servico.ObterPorIdAsync(aulaId)).AceitaLinkAula);
        }

        [Fact]
        public async Task Aula_excluida_ou_inexistente_responde_nao_encontrada_em_definir_e_remover()
        {
            var (cenario, aulaId) = await AulaAsync();
            await cenario.Servico.ExcluirAsync(aulaId);

            await Assert.ThrowsAsync<NaoEncontradoException>(() => cenario.Servico.DefinirLinkAsync(aulaId, Link));
            await Assert.ThrowsAsync<NaoEncontradoException>(() => cenario.Servico.RemoverLinkAsync(aulaId));
            await Assert.ThrowsAsync<NaoEncontradoException>(() => cenario.Servico.DefinirLinkAsync(999, Link));
        }

        // --- o link vai embora ---

        [Fact]
        public async Task Cancelar_a_aula_descarta_o_link()
        {
            var (cenario, aulaId) = await AulaAsync();
            await cenario.Servico.DefinirLinkAsync(aulaId, Link);

            var cancelada = await cenario.Servico.CancelarAsync(aulaId);

            Assert.Null(cancelada.LinkAula);
            Assert.False(cancelada.AceitaLinkAula);
            Assert.Null(cenario.Links.Obter(aulaId));
        }

        [Fact]
        public async Task Excluir_a_aula_descarta_o_link()
        {
            var (cenario, aulaId) = await AulaAsync();
            await cenario.Servico.DefinirLinkAsync(aulaId, Link);

            await cenario.Servico.ExcluirAsync(aulaId);

            Assert.Null(cenario.Links.Obter(aulaId));
        }

        [Fact]
        public async Task Depois_do_fim_da_aula_o_link_some_da_resposta()
        {
            var (cenario, aulaId) = await AulaAsync();
            await cenario.Servico.DefinirLinkAsync(aulaId, Link);

            cenario.Relogio.Definir(new DateTime(2026, 11, 10, 15, 59, 59));
            Assert.Equal(Link, (await cenario.Servico.ObterPorIdAsync(aulaId)).LinkAula);

            cenario.Relogio.Definir(new DateTime(2026, 11, 10, 16, 0, 0));
            var depois = await cenario.Servico.ObterPorIdAsync(aulaId);
            Assert.Null(depois.LinkAula);
            Assert.False(depois.AceitaLinkAula);
        }

        [Fact]
        public async Task Editar_o_horario_renova_a_validade_do_link()
        {
            var (cenario, aulaId) = await AulaAsync();
            await cenario.Servico.DefinirLinkAsync(aulaId, Link);

            await cenario.Servico.AtualizarAsync(aulaId, RequestDeTurma(horaInicio: 18, horaFim: 19));

            cenario.Relogio.Definir(new DateTime(2026, 11, 10, 17, 0, 0)); // passou do fim antigo (16h)
            Assert.Equal(Link, (await cenario.Servico.ObterPorIdAsync(aulaId)).LinkAula);
        }

        [Fact]
        public async Task Mover_a_aula_para_um_horario_que_ja_passou_descarta_o_link()
        {
            var (cenario, aulaId) = await AulaAsync();
            await cenario.Servico.DefinirLinkAsync(aulaId, Link);

            await cenario.Servico.AtualizarAsync(aulaId, RequestDeTurma(horaInicio: 8, horaFim: 9)); // relogio: 10h

            Assert.Null(cenario.Links.Obter(aulaId));
        }

        [Fact]
        public async Task Editar_sem_mudar_o_horario_mantem_o_link()
        {
            var (cenario, aulaId) = await AulaAsync();
            await cenario.Servico.DefinirLinkAsync(aulaId, Link);

            var editada = await cenario.Servico.AtualizarAsync(aulaId, RequestDeTurma() with { Descricao = "Nova descricao" });

            Assert.Equal(Link, editada.LinkAula);
        }

        [Fact]
        public async Task Registrar_a_chamada_mantem_o_link_ate_o_fim_da_aula()
        {
            var (cenario, aulaId) = await AulaAsync();
            await cenario.Servico.DefinirLinkAsync(aulaId, Link);
            cenario.Relogio.Definir(new DateTime(2026, 11, 10, 15, 30, 0)); // aula em andamento

            var realizada = await cenario.Servico.RegistrarSessaoAsync(aulaId, new RegistrarSessaoRequest { Presencas = { [1] = true } });

            Assert.Equal("Realizada", realizada.Status);
            Assert.Equal(Link, realizada.LinkAula);

            cenario.Relogio.Definir(new DateTime(2026, 11, 10, 16, 30, 0));
            Assert.Null((await cenario.Servico.ObterPorIdAsync(aulaId)).LinkAula);
        }
    }
}
