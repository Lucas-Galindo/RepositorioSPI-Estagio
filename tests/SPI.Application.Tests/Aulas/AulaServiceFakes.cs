using SPI.Application.Lembretes.Dtos;
using SPI.Application.Lembretes.Services;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;

namespace SPI.Application.Tests.Aulas
{
    // Fakes manuais (biblioteca de mock nao instalada neste projeto de testes), no
    // mesmo padrao de VinculosCobranca/VinculoCobrancaFakes.cs. Primeira suite de
    // testes de AulaService -- os metodos nao usados por RegistrarSessaoAsync/
    // GerarContasAReceberAsync lancam NotImplementedException de proposito, para
    // que qualquer chamada inesperada (efeito colateral fora do escopo de specs/038)
    // quebre o teste imediatamente.

    public class FakeAulaRepositoryParaAula : IAulaRepository
    {
        public Aula? AulaSemeada { get; set; }
        public int Salvamentos { get; private set; }

        public Task<Aula?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(AulaSemeada?.Id == id ? AulaSemeada : null);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.CompletedTask;
        }

        public Task<List<Aula>> ListarAsync(string? status, int? turmaId, int? alunoId, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> ExisteConflitoAsync(int professorId, DateOnly data, TimeOnly horaInicio, TimeOnly horaFim, int? ignorarAulaId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AdicionarAsync(Aula aula, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task DefinirAlunosAsync(int aulaId, IEnumerable<int> alunoIds, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Dictionary<int, int>> ContarPresencasPorVinculosAsync(IReadOnlyCollection<int> vinculoIds, DateOnly? de, DateOnly? ate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<AulaAluno>> ListarPresencasDoVinculoAsync(int vinculoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Aula>> ListarNaJanelaDeAvisoAsync(DateTime agora, int diasAntes, TimeOnly hora, int? professorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Aula>> ListarPendentesDeStatusAsync(DateTime agora, DateTime desde, int? professorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<int> ContarPresencasPorVinculoECompetenciaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<int>> ObterAlunosAtivosDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    public class FakeCategoriaReceitaRepositoryParaAula : ICategoriaReceitaRepository
    {
        public CategoriaReceita? AulaEmTurma { get; set; }
        public CategoriaReceita? AulaParticular { get; set; }

        public Task<CategoriaReceita?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) =>
            Task.FromResult(nome switch
            {
                "Aula em turma" => AulaEmTurma,
                "Aula particular" => AulaParticular,
                _ => null
            });

        public Task<CategoriaReceita?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<List<CategoriaReceita>> ListarAtivasAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakePagamentoRepositoryParaAula : IPagamentoRepository
    {
        private int _proximoId = 1;

        public List<Pagamento> Gerados { get; } = new();
        public List<(int PagamentoId, int AulaId)> Vinculacoes { get; } = new();
        public int Salvamentos { get; private set; }

        public Task AdicionarAsync(Pagamento pagamento, CancellationToken cancellationToken = default)
        {
            pagamento.Id = _proximoId++;
            Gerados.Add(pagamento);
            return Task.CompletedTask;
        }

        public Task VincularAulaAsync(int pagamentoId, int aulaId, CancellationToken cancellationToken = default)
        {
            Vinculacoes.Add((pagamentoId, aulaId));
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.CompletedTask;
        }

        public Task<Pagamento?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<List<Pagamento>> ListarAsync(int? alunoId, string? status, DateOnly? vencimentoInicio, DateOnly? vencimentoFim, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> ExisteMensalidadeGeradaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<Pagamento>> ListarPendentesVencidosAsync(DateOnly hoje, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<List<Pagamento>> ListarMensalidadesParaLembreteVencimentoAsync(DateOnly dataVencimentoAlvo, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AtualizarStatusViaProcedureAsync(int pagamentoId, string novoStatus, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    public class FakeVinculoCobrancaRepositoryParaAula : IVinculoCobrancaRepository
    {
        public List<VinculoCobranca> Vinculos { get; } = new();
        public int Salvamentos { get; private set; }

        // specs/056: AulaService le todos os vinculos ativos do aluno e decide a
        // cobranca com RegraCobrancaPresenca (nao ha mais busca "por contexto").
        public Task<List<VinculoCobranca>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default) =>
            Task.FromResult(Vinculos.Where(v => v.AlunoId == alunoId && (!ativo.HasValue || v.Ativo == ativo.Value)).ToList());

        public Task<List<VinculoCobranca>> ListarPacotesAsync(int? alunoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<List<VinculoCobranca>> ListarAtivosPorModalidadeAsync(SPI.Domain.Enums.ModalidadeCobranca modalidade, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.CompletedTask;
        }

        // Prova estrutural de FR-011: AulaService so pode ler os vinculos (metodo acima)
        // e persistir o decremento de SaldoAulas (SalvarAlteracoesAsync). Qualquer
        // chamada aos metodos de cadastro abaixo indica que a feature extrapolou o
        // escopo (specs/037 continua sendo o unico dono do CRUD do vinculo).
        public Task<VinculoCobranca?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task AdicionarAsync(VinculoCobranca vinculo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakeTurmaRepositoryVazia : ITurmaRepository
    {
        public Task<Turma?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Turma>> ListarAsync(string? nome, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AdicionarAsync(Turma turma, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task VincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DesvincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakeMateriaRepositoryVazia : IMateriaRepository
    {
        public Task<Materia?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Materia>> ListarAsync(string? nome, string? nivel, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExisteNomeAsync(string nome, int? ignorarId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AdicionarAsync(Materia materia, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakeAlunoRepositoryVazio : IAlunoRepository
    {
        public Task<Aluno?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Aluno?> ObterPorRaAsync(string ra, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Aluno?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Aluno?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Aluno>> ListarAsync(string? nome, string? ra, int? turmaId, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExisteCpfAsync(string cpf, int? ignorarId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<(int Id, string Ra)> CadastrarViaProcedureAsync(
            string nome, string? cpf, string? telefoneAluno, string? telefoneResponsavel, string? email,
            string? senhaHash, string? emailResponsavel, decimal valorAula, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task VincularTurmaAsync(int alunoId, int turmaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakeLembreteServiceVazio : ILembreteService
    {
        // Chamado por CadastrarAsync/AtualizarAsync/ExcluirAsync/CancelarAsync/
        // RegistrarSessaoAsync (Estoria 5) -- fica registrado em Chamadas para os
        // testes que querem afirmar sobre isso; os demais so ignoram a lista.
        public List<int> Chamadas { get; } = new();

        public Task RecalcularAsync(int turmaId, CancellationToken cancellationToken = default)
        {
            Chamadas.Add(turmaId);
            return Task.CompletedTask;
        }

        public Task<List<LembreteResponse>> ListarAsync(int? turmaId, string? status, bool? ativo, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<LembreteResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LembreteResponse> CadastrarAsync(LembreteRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LembreteResponse> AtualizarAsync(int id, LembreteRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task ExcluirAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
