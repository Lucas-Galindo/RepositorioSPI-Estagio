-- ============================================================
-- Arquivo: 18_auditoria_vinculo_cobranca.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Suporte a auditoria historica de "aulas dadas" por Vinculo de
--            Cobranca (specs/052-auditoria-vinculo-cobranca). Duas partes:
--            1) vinculo_cobranca.total_aulas_pacote: tamanho original do
--               Pacote, imutavel por consumo (so saldo_aulas decresce).
--            2) aula_aluno.vinculo_cobranca_id: vinculo ativo no momento de
--               cada presenca, para reconstruir o historico mesmo que o
--               aluno troque de pacote/mensalidade ao longo do tempo.
--            Backfill: vinculos Pacote ja existentes recebem
--            total_aulas_pacote = saldo_aulas atual (assume consumo zero
--            previo - aproximacao documentada em spec.md Assumptions).
-- Pre-requisito: executar 01 a 17 antes deste bloco.
-- Nota de correcao (execucao real): o backfill original tentava
-- total_aulas_pacote = saldo_aulas mesmo quando saldo_aulas = 0 (pacote ja
-- esgotado), violando a propria CHECK (total_aulas_pacote >= 1 quando
-- informado). Pacotes ja esgotados ficam com total_aulas_pacote NULL no
-- backfill (mesmo tratamento do Edge Case "vinculo nunca migrado" de
-- spec.md) -- nao ha como inferir o total original so a partir de saldo 0.
-- ============================================================

USE spi_db;

ALTER TABLE vinculo_cobranca
    ADD COLUMN total_aulas_pacote INT NULL COMMENT 'Total original de aulas do pacote, imutavel por consumo (specs/052); so se aplica a modalidade Pacote',
    ADD CONSTRAINT chk_vinculocobranca_total_aulas_pacote CHECK (total_aulas_pacote IS NULL OR (modalidade = 'Pacote' AND total_aulas_pacote >= 1));

ALTER TABLE aula_aluno
    ADD COLUMN vinculo_cobranca_id INT NULL COMMENT 'Vinculo de cobranca ativo no momento desta presenca, para auditoria historica (specs/052); NULL quando nao havia vinculo correspondente',
    ADD CONSTRAINT fk_aulaaluno_vinculo_cobranca FOREIGN KEY (vinculo_cobranca_id) REFERENCES vinculo_cobranca(id);

UPDATE vinculo_cobranca SET total_aulas_pacote = saldo_aulas WHERE modalidade = 'Pacote' AND saldo_aulas >= 1;
