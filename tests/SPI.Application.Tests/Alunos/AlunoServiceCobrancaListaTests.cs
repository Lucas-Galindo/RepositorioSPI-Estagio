using SPI.Application.Alunos.Services;
using SPI.Application.Common;
using SPI.Application.Tests.VinculosCobranca;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using Xunit;

namespace SPI.Application.Tests.Alunos
{
    // specs/068 (US1, FR-013): o resumo de cobranca da lista de alunos so e calculado quando a tela pede
    // (incluirCobranca). Sem o parametro a resposta e a de sempre e nenhuma consulta extra e feita.
    public class AlunoServiceCobrancaListaTests
    {
        private sealed class SenhaFake : IPasswordHasher
        {
            public string Hash(string senha) => senha;
            public bool Verificar(string senha, string hash) => senha == hash;
        }

        // Resumo falso: so registra as chamadas e devolve um selo reconhecivel por aluno.
        private sealed class ResumoFalso : IResumoCobrancaService
        {
            public int ChamadasLista { get; private set; }
            public int TotalDeAlunosPedidos { get; private set; }

            public Task<Dictionary<int, SeloCobrancaResponse>> ResumirParaListaAsync(IReadOnlyCollection<Aluno> alunos, CancellationToken cancellationToken = default)
            {
                ChamadasLista++;
                TotalDeAlunosPedidos = alunos.Count;
                return Task.FromResult(alunos.ToDictionary(a => a.Id, a => new SeloCobrancaResponse { Categoria = "Avulsa", Valor = a.Id }));
            }

            public Task<Dictionary<int, SeloCobrancaResponse>> ResumirParaGrupoAsync(IReadOnlyCollection<Aluno> alunos, int? turmaId, DateOnly? dataDaAula = null, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
        }

        private static (AlunoService Servico, FakeAlunoRepositoryParaVinculo Alunos) Montar(IResumoCobrancaService? cobranca)
        {
            var alunos = new FakeAlunoRepositoryParaVinculo();
            for (var i = 1; i <= 3; i++)
            {
                alunos.Alunos.Add(new Aluno { Id = i, Nome = $"Aluno {i}", Ra = $"RA{i}", Ativo = true });
            }

            return (new AlunoService(alunos, new FakeTurmaRepositoryParaVinculo(), new SenhaFake(), cobranca), alunos);
        }

        [Fact]
        public async Task Com_incluirCobranca_cada_aluno_traz_o_resumo_numa_unica_chamada_em_lote()
        {
            var resumo = new ResumoFalso();
            var (servico, _) = Montar(resumo);

            var lista = await servico.ListarAsync(null, null, null, null, incluirCobranca: true);

            Assert.Equal(3, lista.Count);
            Assert.All(lista, a => Assert.Equal(a.Id, a.Cobranca!.Valor));
            Assert.Equal(1, resumo.ChamadasLista);
            Assert.Equal(3, resumo.TotalDeAlunosPedidos);
        }

        [Fact]
        public async Task Sem_incluirCobranca_o_resumo_nao_e_chamado_e_o_campo_fica_nulo()
        {
            var resumo = new ResumoFalso();
            var (servico, _) = Montar(resumo);

            var lista = await servico.ListarAsync(null, null, null, null);

            Assert.Equal(3, lista.Count);
            Assert.All(lista, a => Assert.Null(a.Cobranca));
            Assert.Equal(0, resumo.ChamadasLista);
        }

        [Fact]
        public async Task Obter_por_id_nunca_preenche_a_cobranca()
        {
            var resumo = new ResumoFalso();
            var (servico, _) = Montar(resumo);

            var aluno = await servico.ObterPorIdAsync(1);

            Assert.Null(aluno.Cobranca);
            Assert.Equal(0, resumo.ChamadasLista);
        }

        [Fact]
        public async Task Sem_o_servico_de_resumo_a_lista_funciona_e_a_cobranca_fica_nula()
        {
            var (servico, _) = Montar(null);

            var lista = await servico.ListarAsync(null, null, null, null, incluirCobranca: true);

            Assert.Equal(3, lista.Count);
            Assert.All(lista, a => Assert.Null(a.Cobranca));
        }
    }
}
