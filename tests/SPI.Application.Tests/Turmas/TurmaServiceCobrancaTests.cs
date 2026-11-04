using SPI.Application.Tests.VinculosCobranca;
using SPI.Application.Turmas.Services;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Turmas
{
    // specs/068 (US2): na pagina da turma cada aluno matriculado traz o selo com a cobranca que VALE NAQUELA
    // turma (a mesma regra da presenca). Quem esta so "fora da turma" nao recebe selo; a lista de turmas nao
    // calcula nada.
    public class TurmaServiceCobrancaTests
    {
        private const int TurmaId = 10;
        private const int OutraTurma = 20;

        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeAlunoRepositoryParaVinculo _alunos = new();
        private readonly FakeAulaRepositoryParaVinculo _aulas = new();
        private readonly Turma _turma;

        public TurmaServiceCobrancaTests()
        {
            _turma = new Turma { Id = TurmaId, Nome = "Turma", Ativo = true, Tipo = TipoTurma.Mensalidade };
            _turmas.Turmas.Add(_turma);
            _vinculos.TurmasConhecidas.Add(_turma);
            _vinculos.TurmasConhecidas.Add(new Turma { Id = OutraTurma, Nome = "Outra" });
        }

        private TurmaService Servico(bool comResumo = true) => new(
            _turmas, _alunos, _vinculos, new EncerramentoTurmaPacoteService(_vinculos),
            comResumo ? new ResumoCobrancaService(_vinculos, _aulas) : null);

        private Aluno Matricular(int id, string nome)
        {
            var aluno = new Aluno { Id = id, Nome = nome, Ra = $"RA{id}", Ativo = true, ValorAula = 70m };
            _alunos.Alunos.Add(aluno);
            _turma.AlunosTurma.Add(new AlunoTurma { AlunoId = id, TurmaId = TurmaId, Aluno = aluno, Turma = _turma });
            return aluno;
        }

        private VinculoCobranca Vinculo(int alunoId, ModalidadeCobranca modalidade, int? turmaId, int? saldo = null, int? total = null) =>
            _vinculos.Semear(new VinculoCobranca
            {
                AlunoId = alunoId, Modalidade = modalidade, TurmaId = turmaId, Valor = 300m, SaldoAulas = saldo, TotalAulasPacote = total, Ativo = true,
            });

        [Fact]
        public async Task Obter_preenche_o_selo_de_cada_aluno_matriculado_com_o_que_vale_na_turma()
        {
            Matricular(1, "Ana");
            Matricular(2, "Bia");
            Matricular(3, "Caio");
            Matricular(4, "Duda");
            Vinculo(1, ModalidadeCobranca.Pacote, TurmaId, saldo: 4, total: 10);
            Vinculo(2, ModalidadeCobranca.Mensalidade, null);
            Vinculo(3, ModalidadeCobranca.Pacote, OutraTurma, saldo: 4, total: 10); // pacote de OUTRA turma: nao vale aqui
            // aluno 4 sem vinculo

            var turma = await Servico().ObterPorIdAsync(TurmaId);

            var porId = turma.Alunos.ToDictionary(a => a.Id, a => a.Cobranca!);
            Assert.Equal("Pacote", porId[1].Categoria);
            Assert.Equal(10, porId[1].Progresso!.Total);
            Assert.Equal("Mensalidade", porId[2].Categoria);
            Assert.Equal("SemVinculo", porId[3].Categoria);
            Assert.Equal("SemVinculo", porId[4].Categoria);
            Assert.Equal(70m, porId[4].Valor);
            Assert.All(porId.Values, s => { Assert.Null(s.TurmaNome); Assert.Equal(0, s.OutrosVinculos); Assert.Empty(s.Detalhes); });
        }

        [Fact]
        public async Task Alunos_com_pacote_fora_da_turma_nao_recebem_selo()
        {
            _turma.Tipo = TipoTurma.Pacote;
            _turma.DataInicio = DateOnly.FromDateTime(DateTime.Now).AddDays(-5);
            _turma.DataFim = DateOnly.FromDateTime(DateTime.Now).AddDays(30);
            Matricular(1, "Ana");
            var fora = new Aluno { Id = 9, Nome = "Fora", Ra = "RA9", Ativo = true };
            _alunos.Alunos.Add(fora);
            Vinculo(1, ModalidadeCobranca.Pacote, TurmaId, saldo: 4, total: 10);
            Vinculo(9, ModalidadeCobranca.Pacote, TurmaId, saldo: 4, total: 10).Aluno = fora;

            var turma = await Servico().ObterPorIdAsync(TurmaId);

            Assert.NotNull(turma.Alunos.Single().Cobranca);
            var foraDaTurma = Assert.Single(turma.AlunosComPacoteForaDaTurma);
            Assert.Null(foraDaTurma.Cobranca);
        }

        [Fact]
        public async Task A_lista_de_turmas_nao_calcula_o_selo()
        {
            Matricular(1, "Ana");
            Vinculo(1, ModalidadeCobranca.Mensalidade, null);

            var lista = await Servico().ListarAsync(null, null);

            Assert.All(lista.SelectMany(t => t.Alunos), a => Assert.Null(a.Cobranca));
        }

        [Fact]
        public async Task Uma_unica_consulta_de_vinculos_para_a_turma_inteira()
        {
            for (var i = 1; i <= 12; i++)
            {
                Matricular(i, $"Aluno {i}");
                Vinculo(i, ModalidadeCobranca.Avulsa, TurmaId);
            }

            await Servico().ObterPorIdAsync(TurmaId);

            Assert.Equal(1, _vinculos.ChamadasListarAtivosDosAlunos);
        }

        [Fact]
        public async Task Sem_o_servico_de_resumo_a_turma_funciona_e_o_selo_fica_nulo()
        {
            Matricular(1, "Ana");
            Vinculo(1, ModalidadeCobranca.Mensalidade, null);

            var turma = await Servico(comResumo: false).ObterPorIdAsync(TurmaId);

            Assert.Null(turma.Alunos.Single().Cobranca);
        }
    }
}
