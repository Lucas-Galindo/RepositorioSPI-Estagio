using SPI.Application.Lembretes.Dtos;
using SPI.Application.Lembretes.Services;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.Lembretes
{
    // Diagnostico do modulo de lembretes de aula: AtualizarAsync nao verificava
    // se o lembrete ja tinha sido excluido (Ativo == false). Como um lembrete
    // excluido nunca e recalculado de novo (RecalcularAsync so olha ativos),
    // editar um lembrete excluido so mudava a HoraProgramada guardada sem
    // nenhum efeito real -- o usuario via "sucesso" numa edicao inutil. Mesmo
    // cuidado que AulaService.AtualizarAsync ja tem com uma Aula "Realizada".
    public class LembreteServiceAtualizarTests
    {
        private class FakeLembreteRepositoryParaAtualizar : ILembreteRepository
        {
            public Lembrete? LembreteSemeado { get; set; }
            public Aula? ProximaAula { get; set; }
            public int Salvamentos { get; private set; }

            public Task<Lembrete?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
                Task.FromResult(LembreteSemeado?.Id == id ? LembreteSemeado : null);

            public Task<Aula?> ObterProximaAulaDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default) =>
                Task.FromResult(ProximaAula);

            public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
            {
                Salvamentos++;
                return Task.CompletedTask;
            }

            public Task<List<Lembrete>> ListarAsync(int? turmaId, string? status, bool? ativo, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
            public Task<List<Lembrete>> ListarAtivosPorTurmaAsync(int turmaId, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
            public Task<List<Lembrete>> ListarPendentesVencidosAsync(DateTime agora, CancellationToken cancellationToken = default) =>
                throw new NotImplementedException();
            public Task AdicionarAsync(Lembrete lembrete, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private class FakeTurmaRepositoryVazia : ITurmaRepository
        {
            public Task<Turma?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Turma>> ListarAsync(string? nome, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task AdicionarAsync(Turma turma, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task VincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task DesvincularAlunoAsync(int turmaId, int alunoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private static LembreteRequest NovaRequest() => new()
        {
            TurmaId = 1,
            Canal = "Email",
            AntecedenciaHora = 2,
            Destinatarios = "Alunos",
        };

        [Fact]
        public async Task Editar_lembrete_excluido_lanca_conflito_e_nao_salva()
        {
            var lembrete = LembreteTestHelper.CriarLembretePendente(1, DateTime.Now.AddHours(5), quantidadeAlunos: 1);
            lembrete.Ativo = false;
            lembrete.Status = "Cancelado";
            var repositorio = new FakeLembreteRepositoryParaAtualizar { LembreteSemeado = lembrete };
            var servico = new LembreteService(repositorio, new FakeTurmaRepositoryVazia());

            await Assert.ThrowsAsync<ConflitoException>(() => servico.AtualizarAsync(lembrete.Id, NovaRequest()));

            Assert.Equal(0, repositorio.Salvamentos);
        }

        [Fact]
        public async Task Editar_lembrete_ativo_funciona_normalmente()
        {
            var lembrete = LembreteTestHelper.CriarLembretePendente(1, DateTime.Now.AddHours(5), quantidadeAlunos: 1);
            var proximaAula = new Aula { Id = 1, TurmaId = lembrete.TurmaId, DataInicio = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), HoraInicio = new TimeOnly(10, 0) };
            var repositorio = new FakeLembreteRepositoryParaAtualizar { LembreteSemeado = lembrete, ProximaAula = proximaAula };
            var servico = new LembreteService(repositorio, new FakeTurmaRepositoryVazia());

            var resposta = await servico.AtualizarAsync(lembrete.Id, NovaRequest() with { AntecedenciaHora = 3 });

            Assert.Equal(3, resposta.AntecedenciaHora);
            Assert.Equal(1, repositorio.Salvamentos);
        }
    }
}
