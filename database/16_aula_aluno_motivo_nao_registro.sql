-- ============================================================
-- Arquivo: 16_aula_aluno_motivo_nao_registro.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Registra o MOTIVO quando a presenca de um aluno nao e
--            gravada ao registrar a sessao de uma aula (specs/041).
--            Hoje o unico motivo e o bloqueio por pacote de aulas
--            esgotado (VinculoCobranca Pacote com saldo_aulas = 0):
--            nesse caso presente = FALSE e motivo_nao_registro =
--            'PacoteEsgotado', o que permite distinguir, no historico
--            da aula, "faltou" de "foi barrado porque o pacote acabou".
--            NULL significa comportamento normal (presente) ou falta
--            comum -- NUNCA indica bloqueio. Linhas ja existentes
--            permanecem NULL (nenhum backfill). O CHECK restringe os
--            valores permitidos; ampliar a lista de motivos no futuro
--            exige um script novo, de proposito.
-- Pre-requisito: executar 01 a 15 antes deste bloco.
-- ============================================================

USE spi_db;

ALTER TABLE aula_aluno
    ADD COLUMN motivo_nao_registro VARCHAR(30) NULL COMMENT 'Motivo pelo qual a presenca nao foi gravada (specs/041): PacoteEsgotado. NULL = normal ou falta comum',
    ADD CONSTRAINT ck_aulaaluno_motivo_nao_registro CHECK (motivo_nao_registro IS NULL OR motivo_nao_registro IN ('PacoteEsgotado'));
