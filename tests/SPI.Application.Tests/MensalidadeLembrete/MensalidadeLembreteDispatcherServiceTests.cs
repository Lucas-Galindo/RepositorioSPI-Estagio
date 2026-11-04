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

        // specs/062 (FR-017): o lembrete vale tambem para as parcelas dos planos, com texto que cita o pagamento.
        [Fact]
        public async Task Parcela_de_plano_recebe_o_lembrete_com_a_descricao_e_sem_dizer_so_mensalidade()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento();
            var parcela = NovaMensalidadePendente(1);
            parcela.Descricao = "Semestralidade - parcela 2/6";
            pagamentos.Pagamentos.Add(parcela);

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Equal(2, emailSender.Mensagens.Count);
            Assert.All(emailSender.Mensagens, m =>
            {
                Assert.Equal("SPI - Lembrete: pagamento vence em breve", m.Assunto);
                Assert.Contains("o pagamento (Semestralidade - parcela 2/6) de <b>Aluno 1</b>", m.Corpo);
                Assert.DoesNotContain("mensalidade", m.Corpo.ToLowerInvariant());
            });
            Assert.True(parcela.LembreteVencimentoEnviado);
        }

        [Fact]
        public async Task A_descricao_vai_protegida_contra_html_e_sem_descricao_o_texto_continua_correto()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento();
            var comHtml = NovaMensalidadePendente(1, emailResponsavel: null);
            comHtml.Descricao = "Anuidade <b>1/12</b>";
            var semDescricao = NovaMensalidadePendente(2, emailResponsavel: null);
            pagamentos.Pagamentos.Add(comHtml);
            pagamentos.Pagamentos.Add(semDescricao);

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Contains("Anuidade &lt;b&gt;1/12&lt;/b&gt;", emailSender.Mensagens[0].Corpo);
            Assert.DoesNotContain("<b>1/12</b>", emailSender.Mensagens[0].Corpo);
            Assert.Contains("o pagamento de <b>Aluno 2</b>", emailSender.Mensagens[1].Corpo);
        }

        [Fact]
        public async Task Mensalidade_que_ainda_nao_chegou_na_janela_de_antecedencia_nao_e_considerada()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento();
            // Vence em 6 dias: ainda falta mais que a antecedencia configurada (5
            // dias) -- cedo demais pra avisar.
            pagamentos.Pagamentos.Add(NovaMensalidadePendente(1, dataVencimento: Hoje.AddDays(6)));

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Empty(emailSender.Enviados);
        }

        // A consulta usa "<=", nao "==" (ver comentario em
        // IPagamentoRepository.ListarMensalidadesParaLembreteVencimentoAsync):
        // um pagamento que ja esta DENTRO da janela (vencendo em 4 dias, menos
        // que a antecedencia de 5) tambem precisa ser avisado -- isso cobre o
        // caso de o dispatcher ter ficado fora do ar exatamente no dia em que
        // faltavam 5 dias, e so ter voltado a rodar depois.
        [Fact]
        public async Task Mensalidade_ja_dentro_da_janela_tambem_recebe_o_lembrete_atrasado()
        {
            var pagamentos = new FakePagamentoRepositoryParaLembreteVencimento();
            var emailSender = new FakeEmailSenderParaLembreteVencimento();
            pagamentos.Pagamentos.Add(NovaMensalidadePendente(1, emailResponsavel: null, dataVencimento: Hoje.AddDays(4)));

            await MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync(
                pagamentos, emailSender, AntecedenciaDias, logger: null, Hoje, CancellationToken.None);

            Assert.Contains("aluno@teste.com", emailSender.Enviados);
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
