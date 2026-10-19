using SPI.Application.Aulas.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/055 (US1): aula criada com data de hoje ou passada ja nasce "Realizada",
    // com as presencas do formulario e os mesmos efeitos de "Registrar Sessao".
    public class AulaServiceCadastrarRetroativoTests
    {
        private static readonly DateOnly Ontem = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
        private static readonly DateOnly Amanha = DateOnly.FromDateTime(DateTime.Now).AddDays(1);

        private static AulaRequest RequestDeTurma(DateOnly data, Dictionary<int, bool>? presencas) => new()
        {
            MateriaId = 1,
            TurmaId = CenarioCadastroAula.TurmaId,
            DataInicio = data,
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
            Presencas = presencas,
        };

        [Fact]
        public async Task Aula_retroativa_sem_presencas_e_recusada()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"));

            await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.CadastrarAsync(1, RequestDeTurma(Ontem, presencas: null)));

            Assert.Empty(cenario.Aulas.Aulas);
        }

        [Fact]
        public async Task Aula_retroativa_nasce_realizada_com_as_presencas_e_a_cobranca_de_sempre()
        {
            var ana = CenarioCadastroAula.NovoAluno(1, "Ana");
            var bruno = CenarioCadastroAula.NovoAluno(2, "Bruno");
            var cenario = new CenarioCadastroAula(ana, bruno);

            var resposta = await cenario.Servico.CadastrarAsync(1, RequestDeTurma(Ontem, new() { [1] = true, [2] = false }));

            Assert.Equal("Realizada", resposta.Status);
            Assert.True(resposta.Alunos.Single(a => a.AlunoId == 1).Presente);
            Assert.False(resposta.Alunos.Single(a => a.AlunoId == 2).Presente);
            Assert.Equal(1, ana.Frequencia);
            Assert.Equal(0, bruno.Frequencia);
            // Sem vinculo: presenca cobrada pelo Valor da aula (mesma regra do Registrar Sessao).
            var cobranca = Assert.Single(cenario.Pagamentos.Gerados);
            Assert.Equal(1, cobranca.AlunoId);
            Assert.Equal(80m, cobranca.ValorFinal);
        }

        [Fact]
        public async Task Aula_retroativa_com_presenca_faltando_e_recusada()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"), CenarioCadastroAula.NovoAluno(2, "Bruno"));

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() =>
                cenario.Servico.CadastrarAsync(1, RequestDeTurma(Ontem, new() { [1] = true })));

            Assert.Equal("Presenca do aluno 2 nao foi informada.", excecao.Message);
            Assert.Empty(cenario.Pagamentos.Gerados);
        }

        [Fact]
        public async Task Aula_futura_ignora_presencas_e_continua_agendada()
        {
            var ana = CenarioCadastroAula.NovoAluno(1, "Ana");
            var cenario = new CenarioCadastroAula(ana);

            var resposta = await cenario.Servico.CadastrarAsync(1, RequestDeTurma(Amanha, new() { [1] = true }));

            Assert.Equal("Agendada", resposta.Status);
            Assert.Null(resposta.Alunos.Single().Presente);
            Assert.Equal(0, ana.Frequencia);
            Assert.Empty(cenario.Pagamentos.Gerados);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public async Task Lembretes_da_turma_sao_recalculados_exatamente_uma_vez(int diasAPartirDeHoje)
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"));
            var data = DateOnly.FromDateTime(DateTime.Now).AddDays(diasAPartirDeHoje);

            await cenario.Servico.CadastrarAsync(1, RequestDeTurma(data, new() { [1] = true }));

            Assert.Equal(new[] { CenarioCadastroAula.TurmaId }, cenario.Lembretes.Chamadas);
        }

        [Fact]
        public async Task Pacote_esgotado_bloqueia_so_o_aluno_na_aula_retroativa()
        {
            var ana = CenarioCadastroAula.NovoAluno(1, "Ana");
            var bruno = CenarioCadastroAula.NovoAluno(2, "Bruno");
            var cenario = new CenarioCadastroAula(ana, bruno);
            cenario.Vinculos.Vinculos.Add(new VinculoCobranca
            {
                AlunoId = 1, TurmaId = CenarioCadastroAula.TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 0, Ativo = true,
            });

            var resposta = await cenario.Servico.CadastrarAsync(1, RequestDeTurma(Ontem, new() { [1] = true, [2] = true }));

            Assert.Equal("Realizada", resposta.Status);
            var respostaAna = resposta.Alunos.Single(a => a.AlunoId == 1);
            Assert.False(respostaAna.Presente);
            Assert.Equal(AulaAluno.MotivoPacoteEsgotado, respostaAna.MotivoNaoRegistro);
            Assert.Contains(resposta.Avisos, a => a.Contains("Ana"));
            Assert.True(resposta.Alunos.Single(a => a.AlunoId == 2).Presente);
        }
    }
}
