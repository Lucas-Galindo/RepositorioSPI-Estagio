using SPI.Application.Tests.VinculosCobranca;
using SPI.Application.Turmas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Turmas
{
    // specs/063 (US5, FR-019, "MUST NOT"): quando a turma de pacote acaba -- data de fim anterior
    // a hoje (rotina) ou turma inativada pela professora --, os pacotes dela ficam inativos. SO a
    // coluna Ativo muda: saldo, valor e total de aulas ficam exatamente como estavam.
    public class EncerramentoTurmaPacoteServiceTests
    {
        private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Now);

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly EncerramentoTurmaPacoteService _servico;

        public EncerramentoTurmaPacoteServiceTests()
        {
            _servico = new EncerramentoTurmaPacoteService(_vinculos);
        }

        private Turma SemearTurma(int id, DateOnly? fim, bool ativa = true, TipoTurma tipo = TipoTurma.Pacote)
        {
            var turma = new Turma { Id = id, Nome = $"Turma {id}", Ativo = ativa, Tipo = tipo, DataInicio = fim?.AddDays(-30), DataFim = fim };
            _turmas.Turmas.Add(turma);
            _vinculos.TurmasConhecidas.Add(turma);
            return turma;
        }

        private VinculoCobranca Pacote(int alunoId, int? turmaId, int saldo = 3, bool ativo = true) =>
            _vinculos.Semear(new VinculoCobranca
            {
                AlunoId = alunoId, TurmaId = turmaId, Modalidade = ModalidadeCobranca.Pacote,
                Valor = 450m, SaldoAulas = saldo, TotalAulasPacote = 10, Ativo = ativo
            });

        // ---------- Rotina: data de fim anterior a hoje ----------

        [Fact]
        public async Task Data_de_fim_anterior_a_hoje_inativa_os_pacotes_com_o_saldo_intacto()
        {
            SemearTurma(10, fim: Hoje.AddDays(-1));
            var a = Pacote(1, 10, saldo: 3);
            var b = Pacote(2, 10, saldo: 0);

            var inativados = await _servico.EncerrarPacotesDeTurmasEncerradasAsync(Hoje);

            Assert.Equal(2, inativados);
            Assert.False(a.Ativo);
            Assert.False(b.Ativo);
            Assert.Equal(3, a.SaldoAulas);
            Assert.Equal(0, b.SaldoAulas);
            Assert.Equal(450m, a.Valor);
            Assert.Equal(10, a.TotalAulasPacote);
            Assert.Equal(10, a.TurmaId); // continua apontando para a turma: nada e apagado
        }

        [Fact]
        public async Task Data_de_fim_de_hoje_ainda_e_turma_em_andamento()
        {
            SemearTurma(10, fim: Hoje);
            var pacote = Pacote(1, 10);

            Assert.Equal(0, await _servico.EncerrarPacotesDeTurmasEncerradasAsync(Hoje));
            Assert.True(pacote.Ativo);
        }

        [Fact]
        public async Task Turma_sem_data_de_fim_so_acaba_quando_a_professora_inativa()
        {
            SemearTurma(10, fim: null);
            var pacote = Pacote(1, 10);

            Assert.Equal(0, await _servico.EncerrarPacotesDeTurmasEncerradasAsync(Hoje));
            Assert.True(pacote.Ativo);
        }

        [Fact]
        public async Task Turma_inativa_tem_os_pacotes_inativados_pela_rotina_mesmo_sem_data()
        {
            SemearTurma(10, fim: null, ativa: false);
            var pacote = Pacote(1, 10);

            Assert.Equal(1, await _servico.EncerrarPacotesDeTurmasEncerradasAsync(Hoje));
            Assert.False(pacote.Ativo);
            Assert.Equal(3, pacote.SaldoAulas);
        }

        [Fact]
        public async Task Nao_toca_em_pacote_de_aulas_individuais_nem_em_outras_modalidades_nem_em_turma_em_andamento()
        {
            SemearTurma(10, fim: Hoje.AddDays(-1));
            SemearTurma(20, fim: Hoje.AddDays(15));
            var individual = Pacote(1, turmaId: null);
            var emAndamento = Pacote(2, 20);
            var mensalidade = _vinculos.Semear(new VinculoCobranca { AlunoId = 3, Modalidade = ModalidadeCobranca.Mensalidade, Valor = 300m, Ativo = true });
            var avulsa = _vinculos.Semear(new VinculoCobranca { AlunoId = 4, TurmaId = 10, Modalidade = ModalidadeCobranca.Avulsa, Valor = 90m, Ativo = true });

            Assert.Equal(0, await _servico.EncerrarPacotesDeTurmasEncerradasAsync(Hoje));

            Assert.True(individual.Ativo);
            Assert.True(emAndamento.Ativo);
            Assert.True(mensalidade.Ativo);
            Assert.True(avulsa.Ativo);
        }

        [Fact]
        public async Task Pacote_que_ja_estava_inativo_continua_assim_e_rodar_duas_vezes_nao_muda_nada()
        {
            SemearTurma(10, fim: Hoje.AddDays(-1));
            var jaInativo = Pacote(1, 10, ativo: false);
            var ativo = Pacote(2, 10);

            var primeira = await _servico.EncerrarPacotesDeTurmasEncerradasAsync(Hoje);
            var segunda = await _servico.EncerrarPacotesDeTurmasEncerradasAsync(Hoje);

            Assert.Equal(1, primeira);
            Assert.Equal(0, segunda);
            Assert.False(jaInativo.Ativo);
            Assert.False(ativo.Ativo);
        }

        [Fact]
        public async Task Sem_nada_a_encerrar_nao_salva()
        {
            SemearTurma(10, fim: Hoje.AddDays(5));
            Pacote(1, 10);

            await _servico.EncerrarPacotesDeTurmasEncerradasAsync(Hoje);

            Assert.Equal(0, _vinculos.SalvamentosRealizados);
        }

        // ---------- Inativar a turma (professora) ----------

        [Fact]
        public async Task Inativar_a_turma_inativa_so_os_pacotes_dela_com_o_saldo_preservado()
        {
            var alunos = new FakeAlunoRepositoryParaVinculo();
            var turma = SemearTurma(10, fim: Hoje.AddDays(30));
            SemearTurma(20, fim: Hoje.AddDays(30));
            turma.AlunosTurma.Add(new AlunoTurma { AlunoId = 1, TurmaId = 10, Turma = turma, Aluno = new Aluno { Id = 1, Nome = "Ana", Ativo = true } });
            var daTurma = Pacote(1, 10, saldo: 6);
            var outraTurma = Pacote(1, 20, saldo: 6);
            var individual = Pacote(2, turmaId: null, saldo: 6);
            var turmaService = new TurmaService(_turmas, alunos, _vinculos, _servico);

            await turmaService.ExcluirAsync(10);

            Assert.False(turma.Ativo);
            Assert.False(daTurma.Ativo);
            Assert.Equal(6, daTurma.SaldoAulas);
            Assert.True(outraTurma.Ativo);
            Assert.True(individual.Ativo);
            Assert.Single(turma.AlunosTurma); // a matricula nao e removida
        }

        [Fact]
        public async Task Reativar_a_turma_nao_reativa_os_pacotes()
        {
            var alunos = new FakeAlunoRepositoryParaVinculo();
            var turma = SemearTurma(10, fim: Hoje.AddDays(30));
            var pacote = Pacote(1, 10, saldo: 6);
            var turmaService = new TurmaService(_turmas, alunos, _vinculos, _servico);

            await turmaService.ExcluirAsync(10);
            await turmaService.ReativarAsync(10);

            Assert.True(turma.Ativo);
            Assert.False(pacote.Ativo);
            Assert.Equal(6, pacote.SaldoAulas);
        }
    }
}
