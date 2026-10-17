-- ============================================================
-- Arquivo: 19_lembrete_vencimento_mensalidade.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Suporte ao lembrete por e-mail de vencimento de mensalidade,
--            enviado 5 dias antes da data_vencimento (card Trello "Lembrete
--            por e-mail de vencimento da Mensalidade (5 dias antes)").
--            O MensalidadeLembreteDispatcherService (mesmo padrao tecnico de
--            LembreteDispatcherService/MensalidadeDispatcherService) varre
--            diariamente os pagamentos de mensalidade pendentes cuja
--            data_vencimento cai dentro de N dias e ainda nao tiveram o
--            lembrete tentado.
-- Pre-requisito: executar 01 a 18 antes deste bloco.
-- ============================================================

USE spi_db;

ALTER TABLE pagamento
    ADD COLUMN lembrete_vencimento_enviado TINYINT(1) NOT NULL DEFAULT 0
        COMMENT 'Lembrete de vencimento (5 dias antes) ja foi tentado para esta cobranca de mensalidade -- fire-once, nunca reenviado';
