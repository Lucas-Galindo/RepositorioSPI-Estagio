using SPI.Application.Tests.VinculosCobranca;
using SPI.Application.Turmas.Dtos;
using SPI.Application.Turmas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.Turmas
{
    // specs/075: materia opcional da turma. Criar ou trocar exige materia existente e ATIVA; manter a mesma
    // (mesmo inativada depois) e permitido; nulo no PUT remove; turma sem materia segue como antes.
    public class TurmaServiceMateriaTests
    {
        private const int ProfessorId = 1;
        private const int TurmaId = 10;

        private sealed class FakeMateriaRepositoryParaTurma : IMateriaRepository
        {
            public Dictionary<int, Materia> Materias { get; } = new();

            public Task<Materia?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult(Materias.GetValueOrDefault(id));

            public Task<List<Materia>> ListarAsync(string? nome, string? nivel, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<bool> ExisteNomeAsync(string nome, int? ignorarId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task AdicionarAsync(Materia materia, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeAlunoRepositoryParaVinculo _alunos = new();
        private readonly FakeMateriaRepositoryParaTurma _materias = new();
        private readonly TurmaService _servico;

        public TurmaServiceMateriaTests()
        {
            _materias.Materias[1] = new Materia { Id = 1, Nome = "Matematica", Ativo = true };
            _materias.Materias[2] = new Materia { Id = 2, Nome = "Fisica", Ativo = true };
            _materias.Materias[3] = new Materia { Id = 3, Nome = "Antiga", Ativo = false };
            _servico = new TurmaService(_turmas, _alunos, _vinculos, new EncerramentoTurmaPacoteService(_vinculos), materias: _materias);
        }

        private Turma SemearTurma(int? materiaId = null)
        {
            var turma = new Turma
            {
                Id = TurmaId, Nome = "Turma", Ativo = true, Tipo = TipoTurma.Mensalidade,
                MateriaId = materiaId, Materia = materiaId.HasValue ? _materias.Materias[materiaId.Value] : null,
            };
            _turmas.Turmas.Add(turma);
            _vinculos.TurmasConhecidas.Add(turma);
            return turma;
        }

        private static TurmaRequest Pedido(int? materiaId) => new() { Nome = "Turma", MateriaId = materiaId };

        [Fact]
        public async Task Cadastrar_com_materia_ativa_devolve_id_e_nome()
        {
            var resposta = await _servico.CadastrarAsync(ProfessorId, Pedido(1));

            Assert.Equal(1, resposta.MateriaId);
            Assert.Equal("Matematica", resposta.MateriaNome);
        }

        [Fact]
        public async Task Cadastrar_sem_materia_continua_como_antes()
        {
            var resposta = await _servico.CadastrarAsync(ProfessorId, Pedido(null));

            Assert.Null(resposta.MateriaId);
            Assert.Null(resposta.MateriaNome);
        }

        [Fact]
        public async Task Cadastrar_com_materia_inexistente_e_recusado()
        {
            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.CadastrarAsync(ProfessorId, Pedido(99)));
        }

        [Fact]
        public async Task Cadastrar_com_materia_inativa_e_recusado()
        {
            await Assert.ThrowsAsync<ConflitoException>(() => _servico.CadastrarAsync(ProfessorId, Pedido(3)));
        }

        [Fact]
        public async Task Editar_trocando_por_materia_ativa_atualiza_o_vinculo()
        {
            SemearTurma(materiaId: 1);

            var resposta = await _servico.AtualizarAsync(TurmaId, Pedido(2));

            Assert.Equal(2, resposta.MateriaId);
            Assert.Equal("Fisica", resposta.MateriaNome);
        }

        [Fact]
        public async Task Editar_trocando_por_materia_inativa_e_recusado_e_nada_muda()
        {
            var turma = SemearTurma(materiaId: 1);

            await Assert.ThrowsAsync<ConflitoException>(() => _servico.AtualizarAsync(TurmaId, Pedido(3)));

            Assert.Equal(1, turma.MateriaId);
        }

        [Fact]
        public async Task Editar_mantendo_a_mesma_materia_inativada_depois_e_permitido()
        {
            SemearTurma(materiaId: 3);

            var resposta = await _servico.AtualizarAsync(TurmaId, Pedido(3));

            Assert.Equal(3, resposta.MateriaId);
            Assert.Equal("Antiga", resposta.MateriaNome);
        }

        [Fact]
        public async Task Editar_com_materia_nula_remove_o_vinculo()
        {
            SemearTurma(materiaId: 1);

            var resposta = await _servico.AtualizarAsync(TurmaId, Pedido(null));

            Assert.Null(resposta.MateriaId);
            Assert.Null(resposta.MateriaNome);
        }

        [Fact]
        public async Task Editar_com_materia_inexistente_e_recusado()
        {
            SemearTurma();

            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.AtualizarAsync(TurmaId, Pedido(99)));
        }

        [Fact]
        public async Task Turma_sem_materia_editada_sem_materia_segue_sem_materia()
        {
            SemearTurma();

            var resposta = await _servico.AtualizarAsync(TurmaId, Pedido(null));

            Assert.Null(resposta.MateriaId);
        }

        [Fact]
        public async Task Listar_e_obter_devolvem_a_materia_da_turma()
        {
            SemearTurma(materiaId: 2);

            var lista = await _servico.ListarAsync(null, null);
            var detalhe = await _servico.ObterPorIdAsync(TurmaId);

            Assert.Equal("Fisica", lista.Single().MateriaNome);
            Assert.Equal(2, detalhe.MateriaId);
        }
    }
}
