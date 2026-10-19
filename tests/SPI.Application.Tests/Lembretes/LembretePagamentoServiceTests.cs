using SPI.Application.Lembretes.Dtos;
using SPI.Application.Lembretes.Services;
using SPI.Application.Lembretes.Validators;
using SPI.Application.Tests.Notificacoes;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.Lembretes
{
    // Regra e envios em memoria (os adicionados ganham id na hora).
    public class FakeLembretePagamentoRepository : ILembretePagamentoRepository
    {
        public RegraLembretePagamento? Regra { get; set; } = new() { Id = 1, Ativo = true, DiaSemana = (int)DayOfWeek.Monday, Hora = new TimeOnly(8, 0) };
        public List<EnvioLembretePagamento> Envios { get; } = new();

        public Task<RegraLembretePagamento?> ObterRegraAsync(CancellationToken cancellationToken = default) => Task.FromResult(Regra);

        public Task<List<EnvioLembretePagamento>> ListarEnviosDoDiaAsync(DateOnly dataEnvio, CancellationToken cancellationToken = default) =>
            Task.FromResult(Envios.Where(e => e.DataEnvio == dataEnvio).ToList());

        public Task<List<EnvioLembretePagamento>> ListarEnviosRecentesAsync(int limite, CancellationToken cancellationToken = default) =>
            Task.FromResult(Envios.OrderByDescending(e => e.TentadoEm).Take(limite).ToList());

        public Task AdicionarEnvioAsync(EnvioLembretePagamento envio, CancellationToken cancellationToken = default)
        {
            envio.Id = Envios.Count + 1;
            Envios.Add(envio);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    // specs/060 (US2, research.md R4): e-mail semanal de pagamento pendente.
    // 2026-10-19 e uma segunda-feira (a regra padrao).
    public class LembretePagamentoServiceTests
    {
        private static readonly DateOnly Segunda = new(2026, 10, 19);
        private static readonly DateTime SegundaAs8 = Segunda.ToDateTime(new TimeOnly(8, 0));

        private readonly FakeLembretePagamentoRepository _repo = new();
        private readonly FakePagamentoRepositoryParaNotificacao _pagamentos = new();
        private readonly FakeEmailSenderCapturaDestinatarios _email = new();
        private readonly LembretePagamentoService _servico;

        public LembretePagamentoServiceTests()
        {
            _servico = new LembretePagamentoService(_repo, _pagamentos, _email);
        }

        private Aluno NovoAluno(int id, string? email, string? emailResponsavel) =>
            new() { Id = id, Nome = $"Aluno {id}", Email = email, EmailResponsavel = emailResponsavel, Ativo = true };

        private void Pendencia(int id, Aluno aluno, DateOnly vencimento) =>
            _pagamentos.Pagamentos.Add(new Pagamento { Id = id, AlunoId = aluno.Id, Aluno = aluno, Descricao = $"Aula {id}", DataVencimento = vencimento, Status = "Pendente", ValorFinal = 80m });

        [Fact]
        public void Segunda_19_10_2026_e_segunda_feira()
        {
            Assert.Equal(DayOfWeek.Monday, Segunda.DayOfWeek);
        }

        [Fact]
        public async Task No_dia_e_hora_envia_um_email_para_aluno_e_responsavel_com_todas_as_pendencias()
        {
            var aluno = NovoAluno(1, "aluno@x.com", "resp@x.com");
            Pendencia(1, aluno, Segunda.AddDays(-10));
            Pendencia(2, aluno, Segunda.AddDays(-3));

            await _servico.ProcessarAsync(SegundaAs8);

            Assert.Equal(new[] { "aluno@x.com", "resp@x.com" }, _email.Enviados);
            var envio = Assert.Single(_repo.Envios);
            Assert.Equal(EnvioLembretePagamento.Enviado, envio.Resultado);
            Assert.Equal("aluno@x.com, resp@x.com", envio.Destinatarios);
            Assert.Equal(1, envio.Tentativas);
        }

        [Fact]
        public async Task Mesmo_endereco_para_aluno_e_responsavel_recebe_um_email_so()
        {
            Pendencia(1, NovoAluno(1, "mesmo@x.com", "MESMO@x.com"), Segunda.AddDays(-2));

            await _servico.ProcessarAsync(SegundaAs8);

            Assert.Single(_email.Enviados);
        }

        [Theory]
        [InlineData(2026, 10, 19, 7, 59)] // segunda antes da hora
        [InlineData(2026, 10, 20, 9, 0)]  // terca
        public async Task Fora_do_dia_ou_antes_da_hora_nao_envia(int ano, int mes, int dia, int h, int m)
        {
            Pendencia(1, NovoAluno(1, "aluno@x.com", null), Segunda.AddDays(-10));

            await _servico.ProcessarAsync(new DateTime(ano, mes, dia, h, m, 0));

            Assert.Empty(_email.Enviados);
            Assert.Empty(_repo.Envios);
        }

        // Vence na propria segunda: ainda nao e pendencia; o primeiro e-mail e na segunda seguinte.
        [Fact]
        public async Task Pagamento_que_vence_no_proprio_dia_nao_entra()
        {
            Pendencia(1, NovoAluno(1, "aluno@x.com", null), Segunda);

            await _servico.ProcessarAsync(SegundaAs8);

            Assert.Empty(_email.Enviados);
        }

        // FR-007 (MUST NOT): no maximo um e-mail com sucesso por aluno por dia de envio.
        [Fact]
        public async Task Segunda_execucao_no_mesmo_dia_nao_reenvia()
        {
            Pendencia(1, NovoAluno(1, "aluno@x.com", null), Segunda.AddDays(-2));

            await _servico.ProcessarAsync(SegundaAs8);
            await _servico.ProcessarAsync(SegundaAs8.AddMinutes(5));
            await _servico.ProcessarAsync(SegundaAs8.AddHours(10));

            Assert.Single(_email.Enviados);
            Assert.Single(_repo.Envios);
        }

        [Fact]
        public async Task Toda_semana_reenvia_enquanto_houver_pendencia()
        {
            Pendencia(1, NovoAluno(1, "aluno@x.com", null), Segunda.AddDays(-2));

            await _servico.ProcessarAsync(SegundaAs8);
            await _servico.ProcessarAsync(SegundaAs8.AddDays(7));

            Assert.Equal(2, _email.Enviados.Count);
            Assert.Equal(new[] { Segunda, Segunda.AddDays(7) }, _repo.Envios.Select(e => e.DataEnvio));
        }

        [Fact]
        public async Task Falha_registra_e_tenta_de_novo_ate_tres_vezes_no_mesmo_dia()
        {
            Pendencia(1, NovoAluno(1, "aluno@x.com", null), Segunda.AddDays(-2));
            _email.FalharPara = _ => true;

            for (var i = 0; i < 5; i++)
            {
                await _servico.ProcessarAsync(SegundaAs8.AddMinutes(5 * i));
            }

            var envio = Assert.Single(_repo.Envios);
            Assert.Equal(EnvioLembretePagamento.Falha, envio.Resultado);
            Assert.Equal(LembretePagamentoService.MaxTentativas, envio.Tentativas);
            Assert.Contains("Falha simulada", envio.Detalhe);
        }

        [Fact]
        public async Task Falha_seguida_de_sucesso_fica_como_enviado()
        {
            Pendencia(1, NovoAluno(1, "aluno@x.com", null), Segunda.AddDays(-2));
            _email.FalharPara = _ => true;
            await _servico.ProcessarAsync(SegundaAs8);

            _email.FalharPara = null;
            await _servico.ProcessarAsync(SegundaAs8.AddMinutes(5));

            var envio = Assert.Single(_repo.Envios);
            Assert.Equal(EnvioLembretePagamento.Enviado, envio.Resultado);
            Assert.Equal(2, envio.Tentativas);
            Assert.Null(envio.Detalhe);
        }

        [Fact]
        public async Task Aluno_sem_email_fica_sem_destinatario_e_nao_tenta_de_novo()
        {
            Pendencia(1, NovoAluno(1, null, " "), Segunda.AddDays(-2));

            await _servico.ProcessarAsync(SegundaAs8);
            await _servico.ProcessarAsync(SegundaAs8.AddMinutes(5));

            var envio = Assert.Single(_repo.Envios);
            Assert.Equal(EnvioLembretePagamento.SemDestinatario, envio.Resultado);
            Assert.Equal(1, envio.Tentativas);
            Assert.Empty(_email.Enviados);
        }

        [Fact]
        public async Task Regra_desligada_nao_envia()
        {
            _repo.Regra!.Ativo = false;
            Pendencia(1, NovoAluno(1, "aluno@x.com", null), Segunda.AddDays(-2));

            await _servico.ProcessarAsync(SegundaAs8);

            Assert.Empty(_email.Enviados);
        }

        [Fact]
        public async Task Regra_editada_para_outro_dia_e_hora_vale_na_hora()
        {
            await _servico.AtualizarRegraAsync(new RegraLembretePagamentoDto { Ativo = true, DiaSemana = (int)DayOfWeek.Wednesday, Hora = new TimeOnly(14, 30) });
            Pendencia(1, NovoAluno(1, "aluno@x.com", null), Segunda.AddDays(-2));

            await _servico.ProcessarAsync(SegundaAs8);
            await _servico.ProcessarAsync(Segunda.AddDays(2).ToDateTime(new TimeOnly(14, 29)));
            Assert.Empty(_email.Enviados);

            await _servico.ProcessarAsync(Segunda.AddDays(2).ToDateTime(new TimeOnly(14, 30)));
            Assert.Single(_email.Enviados);
        }

        [Fact]
        public async Task Historico_vem_dos_mais_recentes_para_os_mais_antigos()
        {
            var aluno = NovoAluno(1, "aluno@x.com", null);
            Pendencia(1, aluno, Segunda.AddDays(-2));
            await _servico.ProcessarAsync(SegundaAs8);
            await _servico.ProcessarAsync(SegundaAs8.AddDays(7));
            // O repositorio real inclui o Aluno na listagem; o fake o preenche aqui.
            _repo.Envios.ForEach(e => e.Aluno = aluno);

            var historico = await _servico.ListarEnviosAsync(100);

            Assert.Equal(new[] { Segunda.AddDays(7), Segunda }, historico.Select(h => h.DataEnvio));
            Assert.All(historico, h => Assert.Equal("Aluno 1", h.AlunoNome));
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, true)]
        [InlineData(6, true)]
        [InlineData(7, false)]
        public void Validator_aceita_dia_da_semana_de_0_a_6(int dia, bool valido)
        {
            var resultado = new RegraLembretePagamentoValidator().Validate(new RegraLembretePagamentoDto { Ativo = true, DiaSemana = dia, Hora = new TimeOnly(8, 0) });

            Assert.Equal(valido, resultado.IsValid);
        }
    }
}
