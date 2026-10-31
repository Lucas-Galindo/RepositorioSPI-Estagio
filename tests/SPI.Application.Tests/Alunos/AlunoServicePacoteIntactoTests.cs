using SPI.Application.Alunos.Services;
using SPI.Application.Aulas.Dtos;
using SPI.Application.Common;
using SPI.Application.Tests.Aulas;
using SPI.Application.Tests.VinculosCobranca;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.Alunos
{
    // specs/063 (FR-020, "MUST NOT"): excluir (logicamente) e reativar o aluno nao altera o
    // pacote, o saldo nem a matricula. Se a turma ainda esta em andamento, o aluno que volta usa
    // o saldo do pacote dela na proxima presenca.
    public class AlunoServicePacoteIntactoTests
    {
        private const int AlunoId = 1;


        private sealed class SenhaFake : IPasswordHasher
        {
            public string Hash(string senha) => senha;
            public bool Verificar(string senha, string hash) => senha == hash;
        }

        [Fact]
        public async Task Excluir_e_reativar_o_aluno_mantem_pacote_saldo_e_matricula_e_a_presenca_consome_o_saldo()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(AlunoId, "Ana"));
            var pacote = new VinculoCobranca
            {
                Id = 1, AlunoId = AlunoId, TurmaId = CenarioCadastroAula.TurmaId, Modalidade = ModalidadeCobranca.Pacote,
                Valor = 450m, SaldoAulas = 5, TotalAulasPacote = 10, Ativo = true
            };
            cenario.Vinculos.Vinculos.Add(pacote);

            var alunos = new AlunoRepositoryComOMesmoAluno(cenario.AlunosRepo.Alunos[AlunoId]);
            var alunoService = new AlunoService(alunos, new FakeTurmaRepositoryParaVinculo(), new SenhaFake());

            await alunoService.ExcluirAsync(AlunoId);
            Assert.False(cenario.AlunosRepo.Alunos[AlunoId].Ativo);

            await alunoService.ReativarAsync(AlunoId);
            Assert.True(cenario.AlunosRepo.Alunos[AlunoId].Ativo);

            // Pacote, saldo e matricula (a lista de alunos ativos da turma) exatamente como antes.
            Assert.True(pacote.Ativo);
            Assert.Equal(5, pacote.SaldoAulas);
            Assert.Equal(10, pacote.TotalAulasPacote);
            Assert.Equal(CenarioCadastroAula.TurmaId, pacote.TurmaId);
            Assert.Contains(AlunoId, cenario.Aulas.AlunosAtivosPorTurma[CenarioCadastroAula.TurmaId]);

            // E o aluno que voltou usa o saldo na proxima presenca da turma.
            var aulaId = (await cenario.Servico.CadastrarAsync(1, new AulaRequest
            {
                MateriaId = 1,
                TurmaId = CenarioCadastroAula.TurmaId,
                DataInicio = DateOnly.FromDateTime(DateTime.Now).AddDays(3),
                HoraInicio = new TimeOnly(10, 0),
                HoraFim = new TimeOnly(11, 0),
            })).Id;
            await cenario.Servico.RegistrarSessaoAsync(aulaId, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            Assert.Equal(4, pacote.SaldoAulas);
            Assert.Empty(cenario.Pagamentos.Gerados);
        }

        // O AlunoService nem conhece o repositorio de vinculos: so a coluna ativo do aluno muda.
        [Fact]
        public void AlunoService_nao_depende_do_repositorio_de_vinculos()
        {
            var construtor = typeof(AlunoService).GetConstructors().Single();

            Assert.DoesNotContain(construtor.GetParameters(), p => p.ParameterType == typeof(IVinculoCobrancaRepository));
        }

        // Devolve a MESMA instancia de aluno usada pelo cenario da aula, como faria o contexto do EF
        // dentro de uma requisicao.
        private sealed class AlunoRepositoryComOMesmoAluno : IAlunoRepository
        {
            private readonly Aluno _aluno;
            public AlunoRepositoryComOMesmoAluno(Aluno aluno) => _aluno = aluno;

            public Task<Aluno?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult(id == _aluno.Id ? _aluno : null);

            public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task<Aluno?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<Aluno?> ObterPorRaAsync(string ra, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<Aluno?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Aluno>> ListarAsync(string? nome, string? ra, int? turmaId, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<bool> ExisteCpfAsync(string cpf, int? ignorarId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public Task<(int Id, string Ra)> CadastrarViaProcedureAsync(
                string nome, string? cpf, string? telefoneAluno, string? telefoneResponsavel, string? email,
                string? senhaHash, string? emailResponsavel, decimal valorAula, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();

            public Task VincularTurmaAsync(int alunoId, int turmaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }
    }
}
