using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/064 (US2): a chamada so e aceita a partir do inicio da aula (hora de Sao Paulo, decidida
    // pelo servidor); antes disso o servidor recusa com 409 e nada e gravado.
    public class AulaServiceChamadaTests
    {
        private static readonly DateOnly Dia = new(2026, 11, 10);
        private static readonly DateTime Inicio = new(2026, 11, 10, 15, 0, 0);

        private static AulaRequest RequestDeTurma() => new()
        {
            MateriaId = 1, TurmaId = CenarioCadastroAula.TurmaId, DataInicio = Dia,
            HoraInicio = new TimeOnly(15, 0), HoraFim = new TimeOnly(16, 0),
        };

        private static RegistrarSessaoRequest Chamada(int alunoId) => new() { Presencas = { [alunoId] = true } };

        // Cenario com relogio controlavel, ja com uma aula de turma agendada para 10/11/2026 as 15:00.
        private static async Task<(CenarioCadastroAula Cenario, int AulaId, SPI.Domain.Entities.Aluno Ana)> AulaAgendadaAsync(DateTime agora)
        {
            var ana = CenarioCadastroAula.NovoAluno(1, "Ana");
            var cenario = new CenarioCadastroAula(ana);
            cenario.Relogio.Definir(agora);
            var aula = await cenario.Servico.CadastrarAsync(1, RequestDeTurma());
            return (cenario, aula.Id, ana);
        }

        [Fact]
        public async Task Antes_do_inicio_a_chamada_e_recusada_e_nada_e_gravado()
        {
            var (cenario, aulaId, ana) = await AulaAgendadaAsync(Inicio.AddHours(-5));

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.RegistrarSessaoAsync(aulaId, Chamada(1)));

            Assert.Equal("A chamada ficara disponivel a partir das 15:00 de 10/11/2026.", excecao.Message);
            var aula = cenario.Aulas.Aulas.Single();
            Assert.Equal("Agendada", aula.Status);
            Assert.Null(aula.FrequenciaRegistradaEm);
            Assert.Equal(0, ana.Frequencia);
            Assert.Empty(cenario.Pagamentos.Gerados);
        }

        [Fact]
        public async Task No_minuto_exato_do_inicio_a_chamada_e_aceita()
        {
            var (cenario, aulaId, _) = await AulaAgendadaAsync(Inicio);

            var resposta = await cenario.Servico.RegistrarSessaoAsync(aulaId, Chamada(1));

            Assert.Equal("Realizada", resposta.Status);
        }

        [Fact]
        public async Task Um_segundo_antes_do_inicio_ainda_e_recusada()
        {
            var (cenario, aulaId, _) = await AulaAgendadaAsync(Inicio.AddSeconds(-1));

            await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.RegistrarSessaoAsync(aulaId, Chamada(1)));
        }

        [Fact]
        public async Task Aula_antiga_aceita_a_chamada_com_frequencia_e_cobranca_de_sempre()
        {
            var (cenario, aulaId, ana) = await AulaAgendadaAsync(Inicio.AddDays(3));

            var resposta = await cenario.Servico.RegistrarSessaoAsync(aulaId, Chamada(1));

            Assert.Equal("Realizada", resposta.Status);
            Assert.Equal(1, ana.Frequencia);
            Assert.Single(cenario.Pagamentos.Gerados);
        }

        [Fact]
        public async Task A_chamada_grava_quando_foi_registrada_na_hora_de_sao_paulo()
        {
            var agora = Inicio.AddMinutes(20);
            var (cenario, aulaId, _) = await AulaAgendadaAsync(agora);

            await cenario.Servico.RegistrarSessaoAsync(aulaId, Chamada(1));

            Assert.Equal(agora, cenario.Aulas.Aulas.Single().FrequenciaRegistradaEm);
        }

        [Fact]
        public async Task Aula_ja_realizada_nao_aceita_nova_chamada()
        {
            var (cenario, aulaId, ana) = await AulaAgendadaAsync(Inicio.AddMinutes(5));
            await cenario.Servico.RegistrarSessaoAsync(aulaId, Chamada(1));

            await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.RegistrarSessaoAsync(aulaId, Chamada(1)));

            Assert.Equal(1, ana.Frequencia);
            Assert.Single(cenario.Pagamentos.Gerados);
        }

        [Fact]
        public async Task Aula_excluida_nao_aceita_chamada_e_responde_como_inexistente()
        {
            var (cenario, aulaId, _) = await AulaAgendadaAsync(Inicio.AddMinutes(5));
            await cenario.Servico.ExcluirAsync(aulaId);

            await Assert.ThrowsAsync<NaoEncontradoException>(() => cenario.Servico.RegistrarSessaoAsync(aulaId, Chamada(1)));
        }

        [Fact]
        public async Task Aula_individual_segue_a_mesma_regra_de_horario()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"));
            cenario.Relogio.Definir(Inicio.AddHours(-2));
            var individual = new AulaRequest
            {
                MateriaId = 1, AlunoId = 1, DataInicio = Dia, HoraInicio = new TimeOnly(15, 0), HoraFim = new TimeOnly(16, 0),
            };
            var aula = await cenario.Servico.CadastrarAsync(1, individual);

            await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.RegistrarSessaoAsync(aula.Id, Chamada(1)));

            cenario.Relogio.Definir(Inicio);
            var resposta = await cenario.Servico.RegistrarSessaoAsync(aula.Id, Chamada(1));
            Assert.Equal("Realizada", resposta.Status);
        }

        // --- FR-009: editar, cancelar e excluir continuam disponiveis com a chamada bloqueada ---

        [Fact]
        public async Task Com_a_chamada_bloqueada_editar_cancelar_e_excluir_continuam_funcionando()
        {
            var (cenario, aulaId, _) = await AulaAgendadaAsync(Inicio.AddDays(-4));
            var edicao = RequestDeTurma() with { Descricao = "Nova descricao" };

            var editada = await cenario.Servico.AtualizarAsync(aulaId, edicao);
            Assert.Equal("Nova descricao", editada.Descricao);
            Assert.False(editada.ChamadaLiberada);

            var cancelada = await cenario.Servico.CancelarAsync(aulaId);
            Assert.Equal("Cancelada", cancelada.Status);

            await cenario.Servico.ExcluirAsync(aulaId);
            Assert.False(cenario.Aulas.Aulas.Single().Ativo);
        }

        // --- resposta da API: ChamadaLiberada / SegundosParaChamada ---

        [Theory]
        [InlineData(10, 0, false, 18000)]   // hoje antes do inicio
        [InlineData(15, 0, true, null)]     // minuto exato
        [InlineData(15, 30, true, null)]    // em andamento
        [InlineData(20, 0, true, null)]     // ja terminou
        public async Task Resposta_informa_se_a_chamada_esta_liberada(int hora, int minuto, bool liberada, int? segundos)
        {
            var (cenario, aulaId, _) = await AulaAgendadaAsync(new DateTime(2026, 11, 10, hora, minuto, 0));

            var resposta = await cenario.Servico.ObterPorIdAsync(aulaId);

            Assert.Equal(liberada, resposta.ChamadaLiberada);
            Assert.Equal(segundos, resposta.SegundosParaChamada);
        }

        [Fact]
        public async Task Resposta_de_data_futura_e_de_data_passada()
        {
            var (cenario, aulaId, _) = await AulaAgendadaAsync(Inicio.AddDays(-1));
            Assert.False((await cenario.Servico.ObterPorIdAsync(aulaId)).ChamadaLiberada);
            Assert.True((await cenario.Servico.ObterPorIdAsync(aulaId)).SegundosParaChamada > 0);

            cenario.Relogio.Definir(Inicio.AddDays(5));
            var depois = await cenario.Servico.ObterPorIdAsync(aulaId);
            Assert.True(depois.ChamadaLiberada);
            Assert.Null(depois.SegundosParaChamada);
        }

        [Fact]
        public async Task Aula_realizada_e_cancelada_nunca_ficam_liberadas_na_resposta()
        {
            var (cenario, aulaId, _) = await AulaAgendadaAsync(Inicio.AddMinutes(5));
            await cenario.Servico.RegistrarSessaoAsync(aulaId, Chamada(1));

            var resposta = await cenario.Servico.ObterPorIdAsync(aulaId);

            Assert.False(resposta.ChamadaLiberada);
            Assert.Null(resposta.SegundosParaChamada);
        }
    }
}
