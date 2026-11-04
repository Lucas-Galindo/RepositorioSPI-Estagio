using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/062 (US2, FR-009): o plano Semestral/Anual so vale para aulas com data dentro do seu periodo. Dentro dele a
    // presenca NAO gera conta a receber nem consome pacote e fica ligada ao plano; fora, segue as demais regras.
    public class AulaServicePlanoPeriodicoTests
    {
        private const int AlunoId = 1;
        private const int Turma = 10;

        private static readonly DateOnly Inicio = new(2026, 11, 1);
        private static readonly DateOnly Fim = new(2027, 4, 30);

        private readonly FakeVinculoCobrancaRepositoryParaAula _vinculos = new();
        private readonly FakePagamentoRepositoryParaAula _pagamentos = new();

        private VinculoCobranca Plano(ModalidadeCobranca modalidade = ModalidadeCobranca.Semestral) =>
            Semear(new VinculoCobranca { Id = 1, AlunoId = AlunoId, Modalidade = modalidade, Valor = 1800m, DataInicio = Inicio, DataFim = Fim, Ativo = true });

        private VinculoCobranca Pacote(int saldo) =>
            Semear(new VinculoCobranca { Id = 2, AlunoId = AlunoId, Modalidade = ModalidadeCobranca.Pacote, TurmaId = Turma, SaldoAulas = saldo, TotalAulasPacote = 10, Valor = 300m, Ativo = true });

        private VinculoCobranca Semear(VinculoCobranca v)
        {
            _vinculos.Vinculos.Add(v);
            return v;
        }

        private static Aula NovaAula(DateOnly data, int? turmaId) => new()
        {
            Id = 50,
            MateriaId = 1,
            Materia = new Materia { Id = 1, Nome = "Matematica", Ativo = true },
            ProfessorId = 1,
            TurmaId = turmaId,
            Status = "Agendada",
            DataInicio = data,
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
            Ativo = true,
            AulaAlunos = new List<AulaAluno> { new() { AulaId = 50, AlunoId = AlunoId, Aluno = new Aluno { Id = AlunoId, Nome = "Ana", Ativo = true, ValorAula = 80m } } }
        };

        private async Task<(AulaResponse Resposta, Aula Aula)> RegistrarAsync(DateOnly data, int? turmaId, bool presente = true)
        {
            var aula = NovaAula(data, turmaId);
            var servico = new AulaService(
                new FakeAulaRepositoryParaAula { AulaSemeada = aula },
                new FakeAlunoRepositoryVazio(),
                new FakeTurmaRepositoryVazia(),
                new FakeMateriaRepositoryVazia(),
                new FakeLembreteServiceVazio(),
                _pagamentos,
                new FakeCategoriaReceitaRepositoryParaAula
                {
                    AulaEmTurma = new CategoriaReceita { Id = 1, Nome = "Aula em turma", Ativo = true },
                    AulaParticular = new CategoriaReceita { Id = 2, Nome = "Aula particular", Ativo = true },
                },
                _vinculos,
                RelogioFalso.Depois());

            var resposta = await servico.RegistrarSessaoAsync(50, new RegistrarSessaoRequest { Presencas = { [AlunoId] = presente } });
            return (resposta, aula);
        }

        // ---- dentro do periodo ----

        [Theory]
        [InlineData(ModalidadeCobranca.Semestral, true)]  // aula de turma
        [InlineData(ModalidadeCobranca.Semestral, false)] // aula individual
        [InlineData(ModalidadeCobranca.Anual, true)]
        [InlineData(ModalidadeCobranca.Anual, false)]
        public async Task Plano_vigente_nao_gera_cobranca_e_a_presenca_fica_ligada_ao_plano(ModalidadeCobranca modalidade, bool deTurma)
        {
            var plano = Plano(modalidade);

            var (resposta, aula) = await RegistrarAsync(new DateOnly(2026, 12, 10), deTurma ? Turma : null);

            Assert.Empty(_pagamentos.Gerados);
            Assert.Equal(plano.Id, aula.AulaAlunos.Single().VinculoCobrancaId);
            Assert.True(resposta.Alunos.Single().Presente);
        }

        // FR-009 (MUST NOT): nenhuma conta e criada e nenhum pacote e consumido dentro do periodo.
        [Fact]
        public async Task Plano_vigente_nao_cria_pagamento_nem_consome_pacote()
        {
            Plano();
            var pacote = Pacote(saldo: 5);

            await RegistrarAsync(new DateOnly(2026, 12, 10), Turma);

            Assert.Empty(_pagamentos.Gerados);
            Assert.Equal(5, pacote.SaldoAulas);
        }

        [Theory]
        [InlineData(2026, 11, 1)]  // primeiro dia do periodo
        [InlineData(2027, 4, 30)]  // ultimo dia do periodo
        public async Task Os_limites_do_periodo_entram(int ano, int mes, int dia)
        {
            Plano();

            await RegistrarAsync(new DateOnly(ano, mes, dia), null);

            Assert.Empty(_pagamentos.Gerados);
        }

        // ---- fora do periodo ----

        [Theory]
        [InlineData(2026, 10, 31)] // um dia antes do inicio
        [InlineData(2027, 5, 1)]   // um dia depois do fim
        public async Task Fora_do_periodo_o_plano_e_ignorado_e_cobra_o_valor_da_aula(int ano, int mes, int dia)
        {
            var plano = Plano();

            var (_, aula) = await RegistrarAsync(new DateOnly(ano, mes, dia), null);

            var cobranca = Assert.Single(_pagamentos.Gerados);
            Assert.Equal(80m, cobranca.ValorFinal);
            Assert.NotEqual(plano.Id, aula.AulaAlunos.Single().VinculoCobrancaId);
        }

        [Fact]
        public async Task Fora_do_periodo_os_outros_vinculos_ativos_do_aluno_passam_a_valer()
        {
            Plano();
            var pacote = Pacote(saldo: 5); // dado antigo coexistindo com o plano

            await RegistrarAsync(new DateOnly(2027, 5, 1), Turma);

            Assert.Equal(4, pacote.SaldoAulas);
            Assert.Empty(_pagamentos.Gerados);
        }

        [Fact]
        public async Task Falta_continua_sem_efeito_na_cobranca()
        {
            Plano();

            await RegistrarAsync(new DateOnly(2027, 5, 1), null, presente: false);

            Assert.Empty(_pagamentos.Gerados);
        }
    }
}
