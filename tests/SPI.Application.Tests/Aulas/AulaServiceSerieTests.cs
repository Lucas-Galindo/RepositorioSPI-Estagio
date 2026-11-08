using SPI.Application.Aulas.Dtos;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/077: serie semanal de aulas -- previa sem gravar, criacao atomica, conflito bloqueia tudo,
    // so de hoje em diante, limite de 60, aulas comuns e independentes.
    public class AulaServiceSerieTests
    {
        // Terca-feira 10/11/2026, 10h (o "agora" do relogio falso).
        private static readonly DateOnly Hoje = new(2026, 11, 10);
        private static readonly DateTime Manha = new(2026, 11, 10, 10, 0, 0);

        private static HorarioSerieRequest Horario(DayOfWeek dia, int inicio = 15, int fim = 16) =>
            new() { DiaSemana = (int)dia, HoraInicio = new TimeOnly(inicio, 0), HoraFim = new TimeOnly(fim, 0) };

        private static RecorrenciaAulaRequest SerieDeTurma(DateOnly inicio, int quantidade, params HorarioSerieRequest[] horarios) => new()
        {
            MateriaId = 1, TurmaId = CenarioCadastroAula.TurmaId, DataInicial = inicio,
            Horarios = horarios.ToList(), QuantidadeAulas = quantidade,
        };

        private static CenarioCadastroAula NovoCenario()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"), CenarioCadastroAula.NovoAluno(2, "Bia"));
            cenario.Relogio.Definir(Manha);
            return cenario;
        }

        [Fact]
        public async Task Cria_todas_as_aulas_da_serie_agendadas_com_os_alunos_da_turma()
        {
            var cenario = NovoCenario();

            var resposta = await cenario.Servico.CadastrarSerieAsync(1, SerieDeTurma(Hoje, 3, Horario(DayOfWeek.Tuesday)));

            Assert.Equal(3, resposta.Quantidade);
            Assert.Equal(new[] { Hoje, Hoje.AddDays(7), Hoje.AddDays(14) }, resposta.Aulas.Select(a => a.DataInicio));
            Assert.All(resposta.Aulas, a =>
            {
                Assert.Equal("Agendada", a.Status);
                Assert.Equal(new TimeOnly(15, 0), a.HoraInicio);
                Assert.Equal(2, a.Alunos.Count);
            });
            Assert.Equal(3, cenario.Aulas.Aulas.Count);
        }

        [Fact]
        public async Task A_criacao_e_um_unico_salvamento_e_recalcula_o_lembrete_uma_vez()
        {
            var cenario = NovoCenario();

            await cenario.Servico.CadastrarSerieAsync(1, SerieDeTurma(Hoje, 4, Horario(DayOfWeek.Tuesday)));

            Assert.Equal(1, cenario.Aulas.Salvamentos);
            Assert.Single(cenario.Lembretes.Chamadas);
            Assert.Equal(CenarioCadastroAula.TurmaId, cenario.Lembretes.Chamadas[0]);
        }

        [Fact]
        public async Task Conflito_em_qualquer_data_recusa_a_serie_inteira_e_nao_grava_nada()
        {
            var cenario = NovoCenario();
            cenario.Aulas.ConflitosEm.Add((Hoje.AddDays(14), new TimeOnly(15, 0)));

            var erro = await Assert.ThrowsAsync<ConflitoException>(() =>
                cenario.Servico.CadastrarSerieAsync(1, SerieDeTurma(Hoje, 4, Horario(DayOfWeek.Tuesday))));

            Assert.Contains("24/11/2026 15:00", erro.Message);
            Assert.Contains("Nenhuma aula foi criada", erro.Message);
            Assert.Empty(cenario.Aulas.Aulas);
            Assert.Equal(0, cenario.Aulas.Salvamentos);
            Assert.Empty(cenario.Lembretes.Chamadas);
        }

        [Fact]
        public async Task A_mensagem_de_conflito_lista_no_maximo_dez_datas_e_conta_o_resto()
        {
            var cenario = NovoCenario();
            cenario.Aulas.ConflitoSimulado = true;

            var erro = await Assert.ThrowsAsync<ConflitoException>(() =>
                cenario.Servico.CadastrarSerieAsync(1, SerieDeTurma(Hoje, 13, Horario(DayOfWeek.Tuesday))));

            Assert.Contains("e mais 3", erro.Message);
            Assert.Empty(cenario.Aulas.Aulas);
        }

        [Fact]
        public async Task A_previa_marca_as_datas_em_conflito_e_nao_grava_nada()
        {
            var cenario = NovoCenario();
            cenario.Aulas.ConflitosEm.Add((Hoje.AddDays(7), new TimeOnly(15, 0)));

            var previa = await cenario.Servico.PreVisualizarSerieAsync(1, SerieDeTurma(Hoje, 3, Horario(DayOfWeek.Tuesday)));

            Assert.Equal(3, previa.Total);
            Assert.Equal(60, previa.Limite);
            Assert.Equal(1, previa.Conflitos);
            Assert.False(previa.PodeCriar);
            Assert.Equal(new[] { false, true, false }, previa.Aulas.Select(a => a.Conflito));
            Assert.Empty(cenario.Aulas.Aulas);
            Assert.Equal(0, cenario.Aulas.Salvamentos);
        }

        [Fact]
        public async Task A_previa_sem_conflito_pode_criar()
        {
            var cenario = NovoCenario();

            var previa = await cenario.Servico.PreVisualizarSerieAsync(1, SerieDeTurma(Hoje, 2, Horario(DayOfWeek.Tuesday)));

            Assert.True(previa.PodeCriar);
            Assert.Equal(0, previa.Conflitos);
        }

        [Fact]
        public async Task Serie_que_comeca_antes_de_hoje_e_recusada_na_previa_e_na_criacao()
        {
            var cenario = NovoCenario();
            var ontem = Hoje.AddDays(-1);

            await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.PreVisualizarSerieAsync(1, SerieDeTurma(ontem, 2, Horario(DayOfWeek.Tuesday))));
            await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.CadastrarSerieAsync(1, SerieDeTurma(ontem, 2, Horario(DayOfWeek.Tuesday))));
            Assert.Empty(cenario.Aulas.Aulas);
        }

        [Fact]
        public async Task Serie_que_comeca_hoje_e_aceita_mesmo_com_o_horario_ja_passado()
        {
            var cenario = NovoCenario();
            cenario.Relogio.Definir(new DateTime(2026, 11, 10, 20, 0, 0));

            var resposta = await cenario.Servico.CadastrarSerieAsync(1, SerieDeTurma(Hoje, 1, Horario(DayOfWeek.Tuesday, 8, 9)));

            Assert.Equal(1, resposta.Quantidade);
        }

        [Fact]
        public async Task Serie_com_mais_de_sessenta_aulas_e_recusada()
        {
            var cenario = NovoCenario();
            var request = SerieDeTurma(Hoje, 0, Horario(DayOfWeek.Tuesday));
            request.QuantidadeAulas = null;
            request.DataFinal = Hoje.AddDays(7 * 61);

            var erro = await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.CadastrarSerieAsync(1, request));

            Assert.Contains("60", erro.Message);
            Assert.Empty(cenario.Aulas.Aulas);
        }

        [Fact]
        public async Task Periodo_sem_nenhuma_data_nos_dias_escolhidos_e_recusado()
        {
            var cenario = NovoCenario();
            var request = SerieDeTurma(Hoje, 0, Horario(DayOfWeek.Friday));
            request.QuantidadeAulas = null;
            request.DataFinal = Hoje.AddDays(2); // terca a quinta: nenhuma sexta

            var erro = await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.CadastrarSerieAsync(1, request));

            Assert.Contains("Nenhuma aula", erro.Message);
        }

        [Fact]
        public async Task Materia_inexistente_e_recusada_como_nao_encontrada()
        {
            var cenario = NovoCenario();
            var request = SerieDeTurma(Hoje, 2, Horario(DayOfWeek.Tuesday));
            request.MateriaId = 999;

            await Assert.ThrowsAsync<NaoEncontradoException>(() => cenario.Servico.CadastrarSerieAsync(1, request));
            Assert.Empty(cenario.Aulas.Aulas);
        }

        [Fact]
        public async Task Turma_inexistente_e_recusada_como_nao_encontrada()
        {
            var cenario = NovoCenario();
            var request = SerieDeTurma(Hoje, 2, Horario(DayOfWeek.Tuesday));
            request.TurmaId = 999;

            await Assert.ThrowsAsync<NaoEncontradoException>(() => cenario.Servico.PreVisualizarSerieAsync(1, request));
        }

        [Fact]
        public async Task Serie_individual_cria_as_aulas_so_com_o_aluno_escolhido_e_nao_recalcula_lembrete()
        {
            var cenario = NovoCenario();
            var request = new RecorrenciaAulaRequest
            {
                MateriaId = 1, AlunoId = 1, DataInicial = Hoje, QuantidadeAulas = 2,
                Horarios = { Horario(DayOfWeek.Tuesday) },
            };

            var resposta = await cenario.Servico.CadastrarSerieAsync(1, request);

            Assert.Equal(2, resposta.Quantidade);
            Assert.All(resposta.Aulas, a => Assert.Equal(1, Assert.Single(a.Alunos).AlunoId));
            Assert.Empty(cenario.Lembretes.Chamadas);
        }

        [Fact]
        public async Task A_consulta_de_conflito_usa_a_professora_logada_em_cada_data()
        {
            var cenario = NovoCenario();

            await cenario.Servico.CadastrarSerieAsync(7, SerieDeTurma(Hoje, 3, Horario(DayOfWeek.Tuesday)));

            Assert.Equal(3, cenario.Aulas.ConsultasDeConflito.Count);
            Assert.All(cenario.Aulas.ConsultasDeConflito, c => Assert.Equal(7, c.ProfessorId));
            Assert.All(cenario.Aulas.Aulas, a => Assert.Equal(7, a.ProfessorId));
        }

        [Fact]
        public async Task As_aulas_da_serie_sao_independentes_e_comuns()
        {
            var cenario = NovoCenario();

            var resposta = await cenario.Servico.CadastrarSerieAsync(1, SerieDeTurma(Hoje, 2, Horario(DayOfWeek.Tuesday)));

            // Ids distintos, sem nada que ligue uma a outra (nenhuma propriedade de serie na resposta).
            Assert.Equal(2, resposta.Aulas.Select(a => a.Id).Distinct().Count());
            Assert.DoesNotContain(typeof(AulaResponse).GetProperties(), p => p.Name.Contains("Serie", StringComparison.OrdinalIgnoreCase));
        }
    }
}
