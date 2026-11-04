using SPI.Application.Aulas.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/064 (US1): toda aula criada nasce "Agendada", em qualquer data, sem frequencia nem
    // cobranca. Substitui o lancamento retroativo da spec 055 (aula de hoje ou passada nascia
    // "Realizada" com as presencas do formulario): as expectativas mudaram porque a regra mudou.
    public class AulaServiceCadastrarSemPresencaTests
    {
        private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Now);

        private static AulaRequest RequestDeTurma(DateOnly data) => new()
        {
            MateriaId = 1,
            TurmaId = CenarioCadastroAula.TurmaId,
            DataInicio = data,
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
        };

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public async Task Aula_nasce_agendada_sem_frequencia_nem_cobranca_em_qualquer_data(int diasAPartirDeHoje)
        {
            var ana = CenarioCadastroAula.NovoAluno(1, "Ana");
            var cenario = new CenarioCadastroAula(ana);

            var resposta = await cenario.Servico.CadastrarAsync(1, RequestDeTurma(Hoje.AddDays(diasAPartirDeHoje)));

            Assert.Equal("Agendada", resposta.Status);
            Assert.Null(resposta.Alunos.Single().Presente);
            Assert.Equal(0, ana.Frequencia);
            Assert.Empty(cenario.Pagamentos.Gerados);
            Assert.Empty(resposta.Avisos);
            Assert.Null(cenario.Aulas.Aulas.Single().FrequenciaRegistradaEm);
        }

        [Fact]
        public async Task Aula_individual_de_data_passada_tambem_nasce_agendada()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"));
            var request = new AulaRequest
            {
                MateriaId = 1, AlunoId = 1, DataInicio = Hoje.AddDays(-3),
                HoraInicio = new TimeOnly(10, 0), HoraFim = new TimeOnly(11, 0),
            };

            var resposta = await cenario.Servico.CadastrarAsync(1, request);

            Assert.Equal("Agendada", resposta.Status);
            Assert.Empty(cenario.Pagamentos.Gerados);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public async Task Lembretes_da_turma_sao_recalculados_exatamente_uma_vez_em_qualquer_data(int diasAPartirDeHoje)
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"));

            await cenario.Servico.CadastrarAsync(1, RequestDeTurma(Hoje.AddDays(diasAPartirDeHoje)));

            Assert.Equal(new[] { CenarioCadastroAula.TurmaId }, cenario.Lembretes.Chamadas);
        }

        // Edge case da spec 064: a aula antiga continua sujeita ao conflito de horario da professora.
        [Fact]
        public async Task Aula_de_data_passada_continua_sujeita_ao_conflito_de_horario()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"));
            cenario.Aulas.ConflitoSimulado = true;

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() =>
                cenario.Servico.CadastrarAsync(1, RequestDeTurma(Hoje.AddDays(-2))));

            Assert.Equal("Ja existe uma aula cadastrada nesse horario.", excecao.Message);
            Assert.Empty(cenario.Aulas.Aulas);
        }

        // O cadastro nao consulta presenca: o pacote esgotado so aparece quando a chamada e feita.
        [Fact]
        public async Task Pacote_esgotado_nao_interfere_na_criacao_da_aula()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"));
            cenario.Vinculos.Vinculos.Add(new VinculoCobranca
            {
                AlunoId = 1, TurmaId = CenarioCadastroAula.TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 0, Ativo = true,
            });

            var resposta = await cenario.Servico.CadastrarAsync(1, RequestDeTurma(Hoje.AddDays(-1)));

            Assert.Equal("Agendada", resposta.Status);
            Assert.Empty(resposta.Avisos);
        }
    }
}
