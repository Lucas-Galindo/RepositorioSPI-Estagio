-- ============================================================
-- Arquivo: 03_tabela_controle_ra.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Tabela auxiliar que controla o sequencial usado na
--            geracao automatica do RA dos alunos (bloco 04).
--            O sequencial reinicia em 001 a cada novo ano+semestre.
-- Pre-requisito: executar 02_criacao_tabelas.sql antes deste bloco.
-- ============================================================

USE spi_db;

CREATE TABLE ra_controle (
    ano INT NOT NULL COMMENT 'Ano de referencia do cadastro, ex: 2026',
    semestre TINYINT NOT NULL COMMENT 'Semestre de referencia do cadastro: 1 ou 2',
    ultimo_sequencial INT NOT NULL DEFAULT 0 COMMENT 'Ultimo sequencial utilizado para este ano/semestre',
    PRIMARY KEY (ano, semestre),
    CONSTRAINT chk_ra_controle_semestre CHECK (semestre IN (1, 2))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Controle do sequencial de geracao do RA, reiniciado a cada ano+semestre';
