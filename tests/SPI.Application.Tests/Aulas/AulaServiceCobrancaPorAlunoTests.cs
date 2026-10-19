using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/056 (US1/US2): mensalidade e pacote sao do aluno e valem em qualquer
    // aula (turma ou individual); avulsa continua por contexto. Exercita o fluxo
    // completo de RegistrarSessaoAsync -- a ordem de decisao em si e coberta por
    // RegraCobrancaPresencaTests.
    public class AulaServiceCobrancaPorAlunoTests
    {
        private const int AlunoId = 1;
        private const int TurmaA = 10;
        private const int TurmaB = 20;

        private readonly FakeVinculoCobrancaRepositoryParaAula _vinculos = new();
        private readonly FakePagamentoRepositoryParaAula _pagamentos = new();

        private static Aluno NovoAluno() => new() { Id = AlunoId, Nome = "Bruno", Ativo = true, ValorAula = 80m };

        // Cada aula recebe uma instancia nova do aluno, como faria o EF numa nova consulta.
        private static Aula NovaAula(int id, int? turmaId) => new()
        {
            Id = id,
            MateriaId = 1,
            Materia = new Materia { Id = 1, Nome = "Matematica", Ativo = true },
            ProfessorId = 1,
            TurmaId = turmaId,
            Status = "Agendada",
            DataInicio = new DateOnly(2026, 9, 15).AddDays(id),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
            Ativo = true,
            AulaAlunos = new List<AulaAluno> { new() { AulaId = id, AlunoId = AlunoId, Aluno = NovoAluno() } }
        };

        private VinculoCobranca Vinculo(int id, ModalidadeCobranca modalidade, decimal valor, int? turmaId = null, int? saldo = null)
        {
            var vinculo = new VinculoCobranca { Id = id, AlunoId = AlunoId, Modalidade = modalidade, Valor = valor, TurmaId = turmaId, SaldoAulas = saldo, Ativo = true };
            _vinculos.Vinculos.Add(vinculo);
            return vinculo;
        }

        // Registra a sessao de uma aula nova (turma ou individual) com o aluno presente ou faltando.
        private async Task<(AulaResponse Resposta, Aula Aula)> RegistrarAsync(int aulaId, int? turmaId, bool presente = true)
        {
            var aula = NovaAula(aulaId, turmaId);
            var resposta = await NovoServico(aula).RegistrarSessaoAsync(aulaId, new RegistrarSessaoRequest { Presencas = { [AlunoId] = presente } });
            return (resposta, aula);
        }

        private AulaService NovoServico(Aula aula) =>
            new AulaService(
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
                _vinculos);

        // ---------- Mensalidade (US1) ----------

        // FR-004 (MUST NOT): presenca de aluno com mensalidade nunca gera cobranca por aula,
        // em turma ou individual; a presenca fica atribuida a mensalidade (conta no teto).
        [Theory]
        [InlineData(TurmaA)]
        [InlineData(null)]
        public async Task Mensalidade_cobre_aula_de_turma_e_individual_sem_conta_a_receber(int? turmaId)
        {
            var mensalidade = Vinculo(5, ModalidadeCobranca.Mensalidade, 350m);

            var (resposta, aula) = await RegistrarAsync(1, turmaId);

            Assert.True(Assert.Single(resposta.Alunos).Presente);
            Assert.Empty(_pagamentos.Gerados);
            Assert.Equal(mensalidade.Id, aula.AulaAlunos.Single().VinculoCobrancaId);
        }

        // FR-006: falta nao entra no teto -- a presenca nao e atribuida a mensalidade.
        [Fact]
        public async Task Falta_de_aluno_com_mensalidade_nao_e_atribuida_a_mensalidade()
        {
            Vinculo(5, ModalidadeCobranca.Mensalidade, 350m);

            var (resposta, aula) = await RegistrarAsync(1, TurmaA, presente: false);

            Assert.False(Assert.Single(resposta.Alunos).Presente);
            Assert.Null(aula.AulaAlunos.Single().VinculoCobrancaId);
            Assert.Empty(_pagamentos.Gerados);
        }

        // FR-022: conflito herdado (mensalidade + pacote ativos) -- mensalidade prevalece
        // e o saldo do pacote nao e tocado.
        [Fact]
        public async Task Mensalidade_prevalece_sobre_pacote_herdado_sem_consumir_saldo()
        {
            var pacote = Vinculo(3, ModalidadeCobranca.Pacote, 500m, saldo: 4);
            var mensalidade = Vinculo(5, ModalidadeCobranca.Mensalidade, 350m);

            var (_, aula) = await RegistrarAsync(1, TurmaA);

            Assert.Equal(4, pacote.SaldoAulas);
            Assert.Equal(mensalidade.Id, aula.AulaAlunos.Single().VinculoCobrancaId);
            Assert.Empty(_pagamentos.Gerados);
        }

        // ---------- Pacote (US2) ----------

        [Fact]
        public async Task Mesmo_pacote_e_consumido_em_turma_A_turma_B_e_individual()
        {
            var pacote = Vinculo(3, ModalidadeCobranca.Pacote, 500m, saldo: 10);

            await RegistrarAsync(1, TurmaA);
            await RegistrarAsync(2, TurmaB);
            var (_, individual) = await RegistrarAsync(3, null);

            Assert.Equal(7, pacote.SaldoAulas);
            Assert.Empty(_pagamentos.Gerados);
            Assert.Equal(pacote.Id, individual.AulaAlunos.Single().VinculoCobrancaId);
        }

        // FR-010 (MUST NOT): falta nao consome o pacote.
        [Fact]
        public async Task Falta_nao_consome_o_pacote()
        {
            var pacote = Vinculo(3, ModalidadeCobranca.Pacote, 500m, saldo: 10);

            await RegistrarAsync(1, TurmaA, presente: false);

            Assert.Equal(10, pacote.SaldoAulas);
            Assert.Empty(_pagamentos.Gerados);
        }

        // specs/058 FR-001 (MUST NOT): falta e aula cancelada nao contam como aula
        // dada. A contagem le AulaAluno.VinculoCobrancaId, que so a presenca recebe.
        [Fact]
        public async Task So_a_presenca_fica_atribuida_ao_pacote_falta_e_cancelada_nao()
        {
            var pacote = Vinculo(3, ModalidadeCobranca.Pacote, 500m, saldo: 10);

            var (_, presenca) = await RegistrarAsync(1, TurmaA, presente: true);
            var (_, falta) = await RegistrarAsync(2, TurmaA, presente: false);
            var cancelada = NovaAula(3, TurmaA);
            await NovoServico(cancelada).CancelarAsync(cancelada.Id);

            Assert.Equal(pacote.Id, presenca.AulaAlunos.Single().VinculoCobrancaId);
            Assert.Null(falta.AulaAlunos.Single().VinculoCobrancaId);
            Assert.Null(cancelada.AulaAlunos.Single().VinculoCobrancaId);
            Assert.Equal(9, pacote.SaldoAulas);
        }

        [Fact]
        public async Task Pacote_com_saldo_tem_prioridade_sobre_avulsa_do_contexto()
        {
            var pacote = Vinculo(3, ModalidadeCobranca.Pacote, 500m, saldo: 2);
            Vinculo(4, ModalidadeCobranca.Avulsa, 90m, turmaId: TurmaA);

            await RegistrarAsync(1, TurmaA);

            Assert.Equal(1, pacote.SaldoAulas);
            Assert.Empty(_pagamentos.Gerados);
        }

        [Fact]
        public async Task Pacote_esgotado_com_avulsa_cobra_avulsa_e_avisa()
        {
            Vinculo(3, ModalidadeCobranca.Pacote, 500m, saldo: 0);
            var avulsa = Vinculo(4, ModalidadeCobranca.Avulsa, 90m, turmaId: TurmaA);

            var (resposta, aula) = await RegistrarAsync(1, TurmaA);

            Assert.True(Assert.Single(resposta.Alunos).Presente);
            Assert.Equal(90m, Assert.Single(_pagamentos.Gerados).ValorFinal);
            Assert.Equal(avulsa.Id, aula.AulaAlunos.Single().VinculoCobrancaId);
            Assert.Contains(resposta.Avisos, a => a.StartsWith("Pacote esgotado: Bruno foi cobrado como aula avulsa."));
        }

        // A avulsa vale so no proprio contexto: a de outra turma nao socorre o pacote esgotado.
        [Fact]
        public async Task Pacote_esgotado_com_avulsa_de_outro_contexto_bloqueia()
        {
            Vinculo(3, ModalidadeCobranca.Pacote, 500m, saldo: 0);
            Vinculo(4, ModalidadeCobranca.Avulsa, 90m, turmaId: TurmaB);

            var (resposta, _) = await RegistrarAsync(1, TurmaA);

            var aluno = Assert.Single(resposta.Alunos);
            Assert.False(aluno.Presente);
            Assert.Equal("PacoteEsgotado", aluno.MotivoNaoRegistro);
            Assert.Empty(_pagamentos.Gerados);
        }

        [Fact]
        public async Task Sem_vinculo_cobra_valor_da_aula_do_aluno()
        {
            var (_, aula) = await RegistrarAsync(1, TurmaA);

            Assert.Equal(80m, Assert.Single(_pagamentos.Gerados).ValorFinal);
            Assert.Null(aula.AulaAlunos.Single().VinculoCobrancaId);
        }
    }
}
