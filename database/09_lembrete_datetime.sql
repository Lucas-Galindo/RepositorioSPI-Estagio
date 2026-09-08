-- ============================================================
-- Arquivo: 09_lembrete_datetime.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: A Estoria 5 do ERS exige calcular a Hora Programada do
--            lembrete a partir da DATA/HORA da proxima aula da turma, e
--            recalcula-la sempre que uma nova aula e agendada. A coluna
--            original (TIME, sem data) nao consegue representar isso: um
--            lembrete diario as 14h nao tem como dizer "para o dia X".
--            Este bloco converte a coluna para DATETIME (data + hora).
-- Pre-requisito: executar 01 a 08 antes deste bloco.
-- ============================================================

USE spi_db;

ALTER TABLE lembrete
    MODIFY COLUMN hora_programada DATETIME NOT NULL COMMENT 'Data/hora calculada para disparo do lembrete (proxima aula da turma menos a antecedencia)';
