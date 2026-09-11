-- ============================================================
-- Arquivo: 11_lembrete_canal_email_somente.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: WhatsApp e SMS foram descontinuados como canal de
--            lembrete -- a partir de agora, somente "Email" e um
--            canal valido (ver LembreteRequestValidator). Este bloco
--            migra os lembretes existentes que ainda usam WhatsApp ou
--            SMS para o canal Email, evitando registros presos
--            indefinidamente em "Pendente" com um canal que nunca
--            sera efetivamente disparado.
-- Pre-requisito: executar 01 a 10 antes deste bloco.
-- ============================================================

USE spi_db;

UPDATE lembrete
SET canal = 'Email'
WHERE canal IN ('WhatsApp', 'SMS');
