using SPI.Infrastructure.BackgroundServices;
using Xunit;

namespace SPI.Application.Tests.Lembretes
{
    // specs/049-migrar-email-gmail-smtp: cobertura do orcamento de e-mails por
    // ciclo e da retomada parcial de LembreteDispatcherService.ProcessarLembretesVencidosAsync.
    public class LembreteDispatcherOrcamentoTests
    {
        private static readonly DateTime Agora = new(2026, 10, 13, 12, 0, 0);

        // T010: um lembrete com destinatarios acima do limite -- a primeira
        // chamada envia so ate o limite, Status permanece "Pendente",
        // DestinatariosTentados contem exatamente os enderecos tentados.
        [Fact]
        public async Task PrimeiraChamada_EnviaSoAteOLimite_StatusContinuaPendente()
        {
            var lembreteRepository = new FakeLembreteRepositoryParaOrcamento();
            var emailSender = new FakeEmailSenderCapturaDestinatarios();
            var lembrete = LembreteTestHelper.CriarLembretePendente(id: 1, horaProgramada: Agora.AddMinutes(-5), quantidadeAlunos: 15);
            lembreteRepository.Lembretes.Add(lembrete);

            await LembreteDispatcherService.ProcessarLembretesVencidosAsync(
                lembreteRepository, emailSender, maxEmailsPorCiclo: 10, logger: null, agora: Agora, cancellationToken: default);

            Assert.Equal(10, emailSender.Enviados.Count);
            Assert.Equal("Pendente", lembrete.Status);
            Assert.Equal(10, lembrete.DestinatariosTentados!.Split(';', StringSplitOptions.RemoveEmptyEntries).Length);
            Assert.Equal(10, lembrete.EnviosComSucesso);
            Assert.All(emailSender.Enviados, email => Assert.Contains(email, lembrete.DestinatariosTentados));
        }

        // T011: uma segunda chamada (simulando o proximo ciclo) sobre o mesmo
        // lembrete envia so os destinatarios que faltavam (nunca repete os ja
        // tentados); quando o ultimo e tentado, Status vira "Enviado".
        [Fact]
        public async Task SegundaChamada_EnviaSoOsRestantes_NuncaRepeteJaTentados_StatusViraEnviado()
        {
            var lembreteRepository = new FakeLembreteRepositoryParaOrcamento();
            var emailSender = new FakeEmailSenderCapturaDestinatarios();
            var lembrete = LembreteTestHelper.CriarLembretePendente(id: 1, horaProgramada: Agora.AddMinutes(-5), quantidadeAlunos: 15);
            lembreteRepository.Lembretes.Add(lembrete);

            await LembreteDispatcherService.ProcessarLembretesVencidosAsync(
                lembreteRepository, emailSender, maxEmailsPorCiclo: 10, logger: null, agora: Agora, cancellationToken: default);
            var enviadosNoCicloUm = emailSender.Enviados.ToList();

            // Ciclo 2.
            await LembreteDispatcherService.ProcessarLembretesVencidosAsync(
                lembreteRepository, emailSender, maxEmailsPorCiclo: 10, logger: null, agora: Agora.AddMinutes(1), cancellationToken: default);

            Assert.Equal(15, emailSender.Enviados.Count);
            Assert.Equal(15, emailSender.Enviados.Distinct().Count());
            Assert.Empty(enviadosNoCicloUm.Intersect(emailSender.Enviados.Skip(10)));
            Assert.Equal("Enviado", lembrete.Status);
            Assert.Equal(15, lembrete.EnviosComSucesso);
        }

        // T011 (complemento): quando nenhum envio tem sucesso, Status vira "Falha".
        [Fact]
        public async Task QuandoNenhumEnvioTemSucesso_StatusViraFalha()
        {
            var lembreteRepository = new FakeLembreteRepositoryParaOrcamento();
            var emailSender = new FakeEmailSenderCapturaDestinatarios { FalharPara = _ => true };
            var lembrete = LembreteTestHelper.CriarLembretePendente(id: 1, horaProgramada: Agora.AddMinutes(-5), quantidadeAlunos: 3);
            lembreteRepository.Lembretes.Add(lembrete);

            await LembreteDispatcherService.ProcessarLembretesVencidosAsync(
                lembreteRepository, emailSender, maxEmailsPorCiclo: 10, logger: null, agora: Agora, cancellationToken: default);

            Assert.Equal("Falha", lembrete.Status);
            Assert.Equal(0, lembrete.EnviosComSucesso);
            Assert.Equal(3, lembrete.DestinatariosTentados!.Split(';', StringSplitOptions.RemoveEmptyEntries).Length);
        }

        // T012 (FR-005): nenhum Task.Delay bloqueante -- duas chamadas em
        // sequencia, sem nenhuma espera entre elas, completam imediatamente.
        [Fact]
        public async Task DuasChamadasEmSequencia_SemEspera_CompletamImediatamente()
        {
            var lembreteRepository = new FakeLembreteRepositoryParaOrcamento();
            var emailSender = new FakeEmailSenderCapturaDestinatarios();
            lembreteRepository.Lembretes.Add(LembreteTestHelper.CriarLembretePendente(id: 1, horaProgramada: Agora.AddMinutes(-5), quantidadeAlunos: 5));

            var cronometro = System.Diagnostics.Stopwatch.StartNew();
            await LembreteDispatcherService.ProcessarLembretesVencidosAsync(
                lembreteRepository, emailSender, maxEmailsPorCiclo: 10, logger: null, agora: Agora, cancellationToken: default);
            await LembreteDispatcherService.ProcessarLembretesVencidosAsync(
                lembreteRepository, emailSender, maxEmailsPorCiclo: 10, logger: null, agora: Agora, cancellationToken: default);
            cronometro.Stop();

            Assert.True(cronometro.ElapsedMilliseconds < 2000, "As duas chamadas nao deveriam levar segundos -- sugere um Task.Delay bloqueante introduzido por engano.");
        }

        // T013 (FR-004): dois lembretes vencidos com HoraProgramada diferentes,
        // destinatarios somados acima do limite -- o mais antigo consome o
        // orcamento primeiro.
        [Fact]
        public async Task LembreteMaisAntigo_ConsomeOrcamentoPrimeiro()
        {
            var lembreteRepository = new FakeLembreteRepositoryParaOrcamento();
            var emailSender = new FakeEmailSenderCapturaDestinatarios();
            var maisAntigo = LembreteTestHelper.CriarLembretePendente(id: 1, horaProgramada: Agora.AddHours(-2), quantidadeAlunos: 8, turmaId: 1);
            var maisRecente = LembreteTestHelper.CriarLembretePendente(id: 2, horaProgramada: Agora.AddHours(-1), quantidadeAlunos: 8, turmaId: 2);
            lembreteRepository.Lembretes.Add(maisRecente);
            lembreteRepository.Lembretes.Add(maisAntigo);

            await LembreteDispatcherService.ProcessarLembretesVencidosAsync(
                lembreteRepository, emailSender, maxEmailsPorCiclo: 10, logger: null, agora: Agora, cancellationToken: default);

            Assert.Equal(8, maisAntigo.EnviosComSucesso);
            Assert.Equal("Enviado", maisAntigo.Status);
            Assert.Equal(2, maisRecente.EnviosComSucesso);
            Assert.Equal("Pendente", maisRecente.Status);
        }

        // T014 (FR-006): regra de elegibilidade (Status == "Pendente",
        // HoraProgramada <= agora, Ativo) nao foi alterada.
        [Fact]
        public async Task LembreteComHoraProgramadaFutura_NaoEhProcessado()
        {
            var lembreteRepository = new FakeLembreteRepositoryParaOrcamento();
            var emailSender = new FakeEmailSenderCapturaDestinatarios();
            lembreteRepository.Lembretes.Add(LembreteTestHelper.CriarLembretePendente(id: 1, horaProgramada: Agora.AddMinutes(5), quantidadeAlunos: 3));

            await LembreteDispatcherService.ProcessarLembretesVencidosAsync(
                lembreteRepository, emailSender, maxEmailsPorCiclo: 10, logger: null, agora: Agora, cancellationToken: default);

            Assert.Empty(emailSender.Enviados);
        }

        [Fact]
        public async Task LembreteInativoOuJaEnviado_NaoEhProcessado()
        {
            var lembreteRepository = new FakeLembreteRepositoryParaOrcamento();
            var emailSender = new FakeEmailSenderCapturaDestinatarios();
            var inativo = LembreteTestHelper.CriarLembretePendente(id: 1, horaProgramada: Agora.AddMinutes(-5), quantidadeAlunos: 3);
            inativo.Ativo = false;
            var jaEnviado = LembreteTestHelper.CriarLembretePendente(id: 2, horaProgramada: Agora.AddMinutes(-5), quantidadeAlunos: 3, turmaId: 2);
            jaEnviado.Status = "Enviado";
            lembreteRepository.Lembretes.Add(inativo);
            lembreteRepository.Lembretes.Add(jaEnviado);

            await LembreteDispatcherService.ProcessarLembretesVencidosAsync(
                lembreteRepository, emailSender, maxEmailsPorCiclo: 10, logger: null, agora: Agora, cancellationToken: default);

            Assert.Empty(emailSender.Enviados);
        }

        // T014 (complemento): Destinatarios == "AlunosEResponsaveis" continua
        // incluindo e-mail do aluno e do responsavel; aluno inativo continua
        // excluido -- mesmo resultado de antes desta feature (ObterEmailsDestinatarios inalterado).
        [Fact]
        public async Task DestinatariosAlunosEResponsaveis_IncluiOsDoisEExcluiAlunoInativo()
        {
            var lembreteRepository = new FakeLembreteRepositoryParaOrcamento();
            var emailSender = new FakeEmailSenderCapturaDestinatarios();
            var lembrete = LembreteTestHelper.CriarLembretePendente(id: 1, horaProgramada: Agora.AddMinutes(-5), quantidadeAlunos: 0);
            lembrete.Destinatarios = "AlunosEResponsaveis";

            var alunoAtivo = new SPI.Domain.Entities.Aluno { Id = 1, Nome = "Ativo", Email = "ativo@teste.com", EmailResponsavel = "resp@teste.com", Ativo = true };
            var alunoInativo = new SPI.Domain.Entities.Aluno { Id = 2, Nome = "Inativo", Email = "inativo@teste.com", Ativo = false };
            lembrete.Turma.AlunosTurma.Add(new SPI.Domain.Entities.AlunoTurma { AlunoId = alunoAtivo.Id, Aluno = alunoAtivo, TurmaId = lembrete.Turma.Id, Turma = lembrete.Turma });
            lembrete.Turma.AlunosTurma.Add(new SPI.Domain.Entities.AlunoTurma { AlunoId = alunoInativo.Id, Aluno = alunoInativo, TurmaId = lembrete.Turma.Id, Turma = lembrete.Turma });
            lembreteRepository.Lembretes.Add(lembrete);

            await LembreteDispatcherService.ProcessarLembretesVencidosAsync(
                lembreteRepository, emailSender, maxEmailsPorCiclo: 10, logger: null, agora: Agora, cancellationToken: default);

            Assert.Equal(2, emailSender.Enviados.Count);
            Assert.Contains("ativo@teste.com", emailSender.Enviados);
            Assert.Contains("resp@teste.com", emailSender.Enviados);
            Assert.DoesNotContain("inativo@teste.com", emailSender.Enviados);
        }
    }
}
