-- ============================================================
-- Arquivo: 27_turma_materia.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Turma <-> Materia (specs/075-turma-materia).
--            - A turma ganha uma materia opcional (turma.materia_id, chave para
--              materias). Uma materia por turma; vazia = turma sem materia.
--            - As turmas ja existentes recebem a materia A PARTIR DAS AULAS,
--              somente quando todas as aulas ATIVAS da turma usam a mesma
--              materia. Turma sem aulas ou com aulas de varias materias fica
--              sem materia (nada e adivinhado).
-- Seguranca: nao apaga nada (nenhum DELETE/DROP/TRUNCATE) e so toca na tabela
--            turma: uma coluna opcional, a chave estrangeira e um UPDATE que
--            preenche materia_id (so onde ainda e NULL). Aulas, vinculos de
--            cobranca, pagamentos e matriculas nao sao alterados. Pode ser
--            executado mais de uma vez: a coluna e a chave so sao criadas se
--            nao existirem e o UPDATE so atinge turmas ainda sem materia.
--            Fazer backup antes (database/backup).
-- Pre-requisito: executar 01 a 23 e 25 antes deste bloco (a 24 e da spec 062 e
--            a 26 da spec 064; nenhuma e pre-requisito).
-- ============================================================

USE spi_db;

-- 1) Coluna materia_id (so se ainda nao existir).
SET @existe_coluna := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'turma' AND COLUMN_NAME = 'materia_id'
);
SET @sql := IF(@existe_coluna = 0,
    'ALTER TABLE turma ADD COLUMN materia_id INT NULL COMMENT ''Materia da turma (opcional, specs/075); vazia = turma sem materia'' AFTER nome',
    'SELECT ''coluna turma.materia_id ja existe'' AS aviso');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2) Chave estrangeira para materias (so se ainda nao existir). Sem ON DELETE: as
--    materias tem exclusao logica, entao o vinculo nunca fica orfao.
SET @existe_chave := (
    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'turma' AND CONSTRAINT_NAME = 'fk_turma_materia'
);
SET @sql := IF(@existe_chave = 0,
    'ALTER TABLE turma ADD CONSTRAINT fk_turma_materia FOREIGN KEY (materia_id) REFERENCES materias(id)',
    'SELECT ''chave fk_turma_materia ja existe'' AS aviso');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 3) Migracao dos dados: materia da turma = a unica materia de suas aulas ativas.
UPDATE turma t
SET t.materia_id = (
    SELECT MIN(a.materia_id) FROM aula a WHERE a.turma_id = t.id AND a.ativo = TRUE
)
WHERE t.materia_id IS NULL
  AND (
    SELECT COUNT(DISTINCT a.materia_id) FROM aula a WHERE a.turma_id = t.id AND a.ativo = TRUE
  ) = 1;
