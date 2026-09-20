using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/037 (US1): listagem por aluno -- FR-009, e a base do filtro de
    // FR-015 ("Mostrar excluidos").
    public class VinculoCobrancaServiceListarTests
    {
        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly FakeAlunoRepositoryParaVinculo _alunos = new();
        private readonly VinculoCobrancaService _servico;

        public VinculoCobrancaServiceListarTests()
        {
            _alunos.Alunos.Add(new Aluno { Id = 1, Nome = "Ana", Ativo = true });
            _alunos.Alunos.Add(new Aluno { Id = 2, Nome = "Bia", Ativo = true });
            _vinculos.TurmasConhecidas.Add(new Turma { Id = 10, Nome = "Turma A", Ativo = true });
            _servico = new VinculoCobrancaService(_vinculos, _alunos, _turmas);
        }

        private VinculoCobranca Semear(int alunoId, int? turmaId, bool ativo, ModalidadeCobranca modalidade = ModalidadeCobranca.Avulsa) =>
            _vinculos.Semear(new VinculoCobranca { AlunoId = alunoId, TurmaId = turmaId, Modalidade = modalidade, Valor = 80m, Ativo = ativo });

        [Fact]
        public async Task Lista_apenas_os_vinculos_do_aluno_pedido()
        {
            Semear(1, 10, ativo: true);
            Semear(1, null, ativo: true);
            Semear(2, 10, ativo: true);

            var lista = await _servico.ListarPorAlunoAsync(1, ativo: null);

            Assert.Equal(2, lista.Count);
            Assert.All(lista, v => Assert.Equal(1, v.AlunoId));
        }

        [Fact]
        public async Task Vinculo_individual_sai_sem_nome_de_turma_e_o_de_turma_com_o_nome()
        {
            Semear(1, 10, ativo: true);
            Semear(1, null, ativo: true);

            var lista = await _servico.ListarPorAlunoAsync(1, ativo: true);

            Assert.Equal("Turma A", lista.Single(v => v.TurmaId == 10).TurmaNome);
            Assert.Null(lista.Single(v => v.TurmaId == null).TurmaNome);
        }

        [Fact]
        public async Task Filtro_ativo_true_omite_excluidos_e_sem_filtro_devolve_ativos_e_excluidos()
        {
            Semear(1, 10, ativo: true);
            Semear(1, null, ativo: false);

            var somenteAtivos = await _servico.ListarPorAlunoAsync(1, ativo: true);
            var todos = await _servico.ListarPorAlunoAsync(1, ativo: null);

            Assert.Single(somenteAtivos);
            Assert.True(somenteAtivos[0].Ativo);
            Assert.Equal(2, todos.Count);
            Assert.Contains(todos, v => !v.Ativo);
        }

        [Fact]
        public async Task Aluno_sem_vinculos_recebe_lista_vazia()
        {
            Semear(2, 10, ativo: true);

            var lista = await _servico.ListarPorAlunoAsync(1, ativo: true);

            Assert.Empty(lista);
        }

        [Fact]
        public async Task Aluno_inexistente_lanca_NaoEncontrado()
        {
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ListarPorAlunoAsync(999, ativo: true));
        }

        [Fact]
        public async Task ObterPorId_de_vinculo_de_outro_aluno_lanca_NaoEncontrado()
        {
            var vinculoDoAluno2 = Semear(2, 10, ativo: true);

            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ObterPorIdAsync(1, vinculoDoAluno2.Id));
        }

        [Fact]
        public async Task ObterPorId_devolve_o_vinculo_do_aluno()
        {
            var vinculo = Semear(1, 10, ativo: true, ModalidadeCobranca.Mensalidade);

            var resposta = await _servico.ObterPorIdAsync(1, vinculo.Id);

            Assert.Equal(vinculo.Id, resposta.Id);
            Assert.Equal(ModalidadeCobranca.Mensalidade, resposta.Modalidade);
            Assert.Equal("Turma A", resposta.TurmaNome);
        }
    }
}
