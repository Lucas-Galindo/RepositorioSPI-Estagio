-- ============================================================
-- Arquivo: 18_lembrete_destinatarios_tentados.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Suporte a retomada parcial de envio de lembretes entre ciclos
--            (specs/049-migrar-email-gmail-smtp). O LembreteDispatcherService
--            passa a enviar no maximo Lembretes:MaxEmailsPorCiclo e-mails por
--            execucao -- precisa registrar quais destinatarios de um lembrete
--            especifico ja foram tentados antes do envio completo, para nao
--            reenviar nem perder destinatarios entre ciclos.
-- Pre-requisito: executar 01 a 17 antes deste bloco.
-- ============================================================

USE spi_db;

ALTER TABLE lembrete
    ADD COLUMN destinatarios_tentados TEXT NULL COMMENT 'E-mails ja tentados para este lembrete entre ciclos, separados por ;',
    ADD COLUMN envios_com_sucesso INT NOT NULL DEFAULT 0 COMMENT 'Contagem cumulativa de envios bem-sucedidos entre ciclos';
