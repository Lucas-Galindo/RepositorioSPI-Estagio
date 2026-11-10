using SPI.Application.Tests.VinculosCobranca;
using SPI.Application.Turmas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Turmas
{
    // specs/078 (FR-001): o servidor diz quem pode "Registrar saida" -- pacote ativo da turma, turma de pacote em
    // andamento. A tela so le o campo. O desvincular nao muda (FR-011).
    public class TurmaServicePodeRegistrarSaidaTests
    {
        private const int AlunoId = 1;
        private const int OutroAlunoId = 2;
        private const int TurmaId = 10;

        private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Now);

        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeAlunoRepositoryParaVinculo _alunos = new();
        private readonly TurmaService _servico;
        private readonly Aluno _ana = new() { Id = AlunoId, Nome = "Ana", Ra = "RA1", Ativo = true };
        private readonly Aluno _bia = new() { Id = OutroAlunoId, Nome = "Bia", Ra = "RA2", Ativo = true };

        public TurmaServicePodeRegistrarSaidaTests()
        {
            _alunos.Alunos.AddRange(new[] { _ana, _bia });
            _turmas.AlunosConhecidos[AlunoId] = _ana;
            _turmas.AlunosConhecidos[OutroAlunoId] = _bia;
            _servico = new TurmaService(_turmas, _alunos, _vinculos, new EncerramentoTurmaPacoteService(_vinculos));
        }

        private Turma SemearTurma(TipoTurma tipo, DateOnly? inicio = null, DateOnly? fim = null, bool ativa = true)
        {
            var turma = new Turma { Id = TurmaId, Nome = "Turma", Ativo = ativa, Tipo = tipo, DataInicio = inicio, DataFim = fim };
            _turmas.Turmas.Add(turma);
            _vinculos.TurmasConhecidas.Add(turma);
            _turmas.VincularAlunoAsync(TurmaId, AlunoId).Wait();
            _turmas.VincularAlunoAsync(TurmaId, OutroAlunoId).Wait();
            return turma;
        }

        private VinculoCobranca SemearPacote(Aluno aluno, bool ativo = true) =>
            _vinculos.Semear(new VinculoCobranca { AlunoId = aluno.Id, Aluno = aluno, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 100m, TotalAulasPacote = 10, SaldoAulas = 5, Ativo = ativo });

        [Fact]
        public async Task Turma_de_pacote_em_andamento_libera_a_saida_so_para_quem_tem_pacote_ativo()
        {
            SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-5), Hoje.AddDays(30));
            SemearPacote(_ana);
            SemearPacote(_bia, ativo: false);

            var turma = await _servico.ObterPorIdAsync(TurmaId);

            Assert.True(turma.Alunos.Single(a => a.Id == AlunoId).PodeRegistrarSaida);
            Assert.False(turma.Alunos.Single(a => a.Id == OutroAlunoId).PodeRegistrarSaida);
        }

        [Fact]
        public async Task Aluno_sem_nenhum_pacote_nao_libera_a_saida()
        {
            SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-5), Hoje.AddDays(30));

            var turma = await _servico.ObterPorIdAsync(TurmaId);

            Assert.All(turma.Alunos, a => Assert.False(a.PodeRegistrarSaida));
        }

        [Fact]
        public async Task Turma_de_pacote_encerrada_nao_libera_a_saida()
        {
            SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-30), Hoje.AddDays(-1));
            SemearPacote(_ana);

            var turma = await _servico.ObterPorIdAsync(TurmaId);

            Assert.All(turma.Alunos, a => Assert.False(a.PodeRegistrarSaida));
        }

        [Fact]
        public async Task Turma_de_mensalidade_nunca_oferece_a_saida()
        {
            SemearTurma(TipoTurma.Mensalidade);
            _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, Aluno = _ana, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Avulsa, Valor = 50m, Ativo = true });

            var turma = await _servico.ObterPorIdAsync(TurmaId);

            Assert.All(turma.Alunos, a => Assert.False(a.PodeRegistrarSaida));
        }

        [Fact]
        public async Task Desvincular_continua_como_antes_o_pacote_e_o_saldo_nao_mudam()
        {
            SemearTurma(TipoTurma.Pacote, Hoje.AddDays(-5), Hoje.AddDays(30));
            var pacote = SemearPacote(_ana);

            var depois = await _servico.DesvincularAlunoAsync(TurmaId, AlunoId);

            Assert.True(pacote.Ativo);
            Assert.Equal(5, pacote.SaldoAulas);
            Assert.Null(pacote.SaidaRegistradaEm);
            Assert.DoesNotContain(depois.Alunos, a => a.Id == AlunoId);
            // Quem saiu pelo desvincular continua com o pacote ativo e pode voltar (spec 063).
            Assert.Contains(depois.AlunosComPacoteForaDaTurma, a => a.Id == AlunoId);
        }
    }
}
