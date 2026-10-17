using SPI.Application.Common;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;

namespace SPI.Application.Tests.Lembretes
{
    // Fakes manuais (biblioteca de mock nao instalada neste projeto de testes),
    // mesmo padrao de Mensalidade/MensalidadeFakes.cs e VinculosCobranca/VinculoCobrancaFakes.cs.
    // Primeira suite de testes do orcamento por ciclo (specs/049) -- os metodos
    // nao usados por ProcessarLembretesVencidosAsync lancam NotImplementedException
    // de proposito, para que qualquer chamada inesperada quebre o teste imediatamente.

    public class FakeLembreteRepositoryParaOrcamento : ILembreteRepository
    {
        public List<Lembrete> Lembretes { get; } = new();
        public int Salvamentos { get; private set; }

        public Task<List<Lembrete>> ListarPendentesVencidosAsync(DateTime agora, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Lembretes
                    .Where(l => l.Ativo && l.Status == "Pendente" && l.HoraProgramada <= agora)
                    .OrderBy(l => l.HoraProgramada)
                    .ToList());

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.CompletedTask;
        }

        public Task<Lembrete?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Lembrete>> ListarAsync(int? turmaId, string? status, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Lembrete>> ListarAtivosPorTurmaAsync(int turmaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Aula?> ObterProximaAulaDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AdicionarAsync(Lembrete lembrete, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakeEmailSenderCapturaDestinatarios : IEmailSender
    {
        public List<string> Enviados { get; } = new();

        // Hook opcional para simular falha de envio pra um destinatario especifico.
        public Func<string, bool>? FalharPara { get; set; }

        public Task EnviarAsync(string destinatario, string assunto, string corpoHtml, CancellationToken cancellationToken = default)
        {
            if (FalharPara?.Invoke(destinatario) == true)
            {
                throw new InvalidOperationException($"Falha simulada ao enviar para {destinatario}.");
            }

            Enviados.Add(destinatario);
            return Task.CompletedTask;
        }
    }

    public static class LembreteTestHelper
    {
        // Monta um Lembrete "Pendente" com uma turma de N alunos ativos com e-mail
        // (email{i}@teste.com), destinatarios = "Alunos", canal = "Email".
        public static Lembrete CriarLembretePendente(int id, DateTime horaProgramada, int quantidadeAlunos, int turmaId = 1)
        {
            var turma = new Turma { Id = turmaId, Nome = $"Turma {turmaId}", Ativo = true };

            for (var i = 1; i <= quantidadeAlunos; i++)
            {
                var aluno = new Aluno { Id = i, Nome = $"Aluno {i}", Email = $"aluno{i}@teste.com", Ativo = true };
                turma.AlunosTurma.Add(new AlunoTurma { AlunoId = aluno.Id, Aluno = aluno, TurmaId = turma.Id, Turma = turma });
            }

            return new Lembrete
            {
                Id = id,
                TurmaId = turma.Id,
                Turma = turma,
                Status = "Pendente",
                HoraProgramada = horaProgramada,
                Destinatarios = "Alunos",
                Canal = "Email",
                AntecedenciaHora = 1,
                Ativo = true,
            };
        }
    }
}
