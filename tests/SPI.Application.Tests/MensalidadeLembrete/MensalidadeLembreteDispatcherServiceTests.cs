using SPI.Domain.Entities;
using SPI.Infrastructure.BackgroundServices;
using Xunit;

namespace SPI.Application.Tests.MensalidadeLembrete
{
    // Card Trello "Lembrete por e-mail de vencimento da Mensalidade (5 dias
    // antes)": varre pagamentos de mensalidade (VinculoCobrancaId != null)
    // Pendentes cuja DataVencimento cai dentro da antecedencia configurada e
    // ainda nao tiveram o lembrete tentado, avisando aluno e/ou responsavel.
    // Chama ProcessarLembretesVencimentoAsync diretamente, sem
    // IServiceScopeFactory/BackgroundService (mesmo padrao de
    // MensalidadeDispatcherServiceGerarCobrancasTests).
    public class MensalidadeLembreteDispatcherServiceTests
    {
        private static readonly DateOnly Hoje = new(2026, 10, 17);
        private const int AntecedenciaDias = 5;
        private static readonly DateOnly DataVencimentoAlvo = Hoje.AddDays(AntecedenciaDias);

        private static Pagamento NovaMensalidadePendente(
            int id, string? emailAluno = "aluno@teste.com", string? emailResponsavel = "responsavel@teste.com", DateOnly? dataVencimento = null, bool jaEnviado = false, string status = "Pendente") => new()
        {
            Id = id,
            AlunoId = id,
            VinculoCobrancaId = id,
            Status = status,
            DataVencimento = dataVencimento ?? DataVencimentoAlvo,
            ValorFinal = 300m,
            LembreteVencimentoEnviado = jaEnviado,
            Aluno = new Aluno { Id = id, Nome = $"Aluno {id}", Ativo = true, Email = emailAluno, EmailResponsavel = emailResponsavel },
        };

        [Fact]
        public async Task Mensalidade_pendente_vencendo_na_antecedencia_envia_para_aluno_e_responsavel()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento();
            var pagamento = NovaMensalidadePendente(1);
            pagamentos.Pagamentos.Add(pagamento);

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Contains("aluno@teste.com", emailSender.Enviados);
            Assert.Contains("responsavel@teste.com", emailSender.Enviados);
            Assert.True(pagamento.LembreteVencimentoEnviado);
            Assert.Equal(1, pagamentos.Salvamentos);
        }

        [Fact]
        public async Task Mensalidade_com_vencimento_fora_da_antecedencia_nao_e_considerada()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento();
            pagamentos.Pagamentos.Add(NovaMensalidadePendente(1, dataVencimento: Hoje.AddDays(6)));
            pagamentos.Pagamentos.Add(NovaMensalidadePendente(2, dataVencimento: Hoje.AddDays(4)));

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Empty(emailSender.Enviados);
        }

        [Fact]
        public async Task Mensalidade_ja_paga_nao_recebe_lembrete()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento();
            pagamentos.Pagamentos.Add(NovaMensalidadePendente(1, status: "Pago"));

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Empty(emailSender.Enviados);
        }

        [Fact]
        public async Task Pagamento_manual_sem_vinculo_de_cobranca_nunca_e_considerado()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento();
            var manual = NovaMensalidadePendente(1);
            manual.VinculoCobrancaId = null;
            pagamentos.Pagamentos.Add(manual);

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Empty(emailSender.Enviados);
        }

        [Fact]
        public async Task Lembrete_ja_enviado_antes_nunca_e_reenviado_fire_once()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento();
            pagamentos.Pagamentos.Add(NovaMensalidadePendente(1, jaEnviado: true));

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Empty(emailSender.Enviados);
        }

        [Fact]
        public async Task Chamar_duas_vezes_seguidas_no_mesmo_dia_envia_so_uma_vez()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento();
            pagamentos.Pagamentos.Add(NovaMensalidadePendente(1, emailResponsavel: null));

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);
            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Single(emailSender.Enviados);
        }

        [Fact]
        public async Task Sem_email_de_aluno_nem_responsavel_marca_como_tentado_sem_lancar_excecao()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento();
            var pagamento = NovaMensalidadePendente(1, emailAluno: null, emailResponsavel: null);
            pagamentos.Pagamentos.Add(pagamento);

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Empty(emailSender.Enviados);
            Assert.True(pagamento.LembreteVencimentoEnviado);
        }

        [Fact]
        public async Task Falha_ao_enviar_para_um_pagamento_nao_impede_os_demais_e_marca_como_tentado()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento { FalharPara = email => email == "aluno@teste.com" };
            var falho = NovaMensalidadePendente(1, emailResponsavel: null);
            var ok = NovaMensalidadePendente(2, emailAluno: "outro@teste.com", emailResponsavel: null);
            pagamentos.Pagamentos.Add(falho);
            pagamentos.Pagamentos.Add(ok);

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Contains("outro@teste.com", emailSender.Enviados);
            Assert.True(falho.LembreteVencimentoEnviado);
            Assert.True(ok.LembreteVencimentoEnviado);
        }
    }
}
