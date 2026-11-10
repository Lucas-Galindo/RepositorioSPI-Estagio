-- ============================================================
-- Arquivo: 28_saida_pacote_acerto.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Saida do aluno de turma de pacote com acerto proporcional ao
--            consumo (specs/078-saida-pacote-acerto).
--            - vinculo_cobranca ganha saida_registrada_em: marca o pacote que
--              foi ENCERRADO pela saida do aluno (so pacote inativo). Com ela o
--              sistema recusa reativar esse pacote e distingue "saiu pelo
--              acerto" de "ficou inativo porque a turma acabou".
--            - chk_vinculocobranca_saida: so um pacote inativo pode ter a data.
--            - Categoria de receita nova "Acerto de pacote" (a conta a receber
--              da diferenca, quando o aluno pagou menos do que consumiu).
-- Seguranca: nao apaga nada (nenhum DELETE/DROP/TRUNCATE) e nao altera nenhum
--            dado existente: so acrescenta uma coluna opcional, uma regra de
--            validacao (todas as linhas existentes continuam validas, pois a
--            coluna nasce nula) e insere uma categoria. Nao mexe em pagamento,
--            aula nem matricula. Pode ser executado mais de uma vez. Fazer
--            backup antes (database/backup).
-- Nao muda: uq_pagamento_vinculo_competencia (uma conta por mes no vinculo) e
--            os demais chk_vinculocobranca_*.
-- Pre-requisito: executar 01 a 23 e 25 antes deste bloco (a 24, a 26 e a 27 sao
--            de outras specs e nao sao pre-requisito).
-- ============================================================

USE spi_db;

-- 1) Quando a saida foi registrada (so se ainda nao existir).
SET @existe_coluna := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'vinculo_cobranca' AND COLUMN_NAME = 'saida_registrada_em'
);
SET @sql := IF(@existe_coluna = 0,
    'ALTER TABLE vinculo_cobranca ADD COLUMN saida_registrada_em DATETIME NULL COMMENT ''Quando a saida do aluno foi registrada com acerto (specs/078); so pacote inativo'' AFTER ativo',
    'SELECT ''coluna vinculo_cobranca.saida_registrada_em ja existe'' AS aviso');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2) So um pacote inativo pode ter sido encerrado pela saida (so se ainda nao existir).
SET @existe_regra := (
    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'vinculo_cobranca' AND CONSTRAINT_NAME = 'chk_vinculocobranca_saida'
);
SET @sql := IF(@existe_regra = 0,
    'ALTER TABLE vinculo_cobranca ADD CONSTRAINT chk_vinculocobranca_saida CHECK (saida_registrada_em IS NULL OR (modalidade = ''Pacote'' AND ativo = FALSE))',
    'SELECT ''regra chk_vinculocobranca_saida ja existe'' AS aviso');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 3) Categoria de receita da conta de diferenca (so e inserida se ainda nao existir).
INSERT INTO categoria_receita (nome, ativo)
SELECT 'Acerto de pacote', TRUE FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM categoria_receita WHERE nome = 'Acerto de pacote');
