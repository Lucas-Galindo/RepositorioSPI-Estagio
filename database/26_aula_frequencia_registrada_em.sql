-- ============================================================
-- Arquivo: 26_aula_frequencia_registrada_em.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Chamada so nos detalhes da aula (specs/064-chamada-so-nos-detalhes).
--            A aula passa a guardar QUANDO a frequencia foi registrada
--            (hora de Sao Paulo), para auditoria e para detectar frequencia
--            lancada antes do inicio da aula.
-- Seguranca: nao apaga nada e nao altera nenhum dado existente: so acrescenta
--            uma coluna opcional. NAO preenche retroativamente (FR-015): as
--            aulas ja realizadas ficam com a coluna vazia, porque nao ha como
--            saber quando a chamada foi feita. Fazer backup antes
--            (database/backup).
-- Pre-requisito: executar 01 a 23 e 25 antes deste bloco (a 24 e da spec 062 e
--            nao e pre-requisito).
-- ============================================================

USE spi_db;

ALTER TABLE aula
    ADD COLUMN frequencia_registrada_em DATETIME NULL
        COMMENT 'Quando a chamada foi registrada (hora de Sao Paulo); vazia nas aulas anteriores a specs/064'
        AFTER ativo;
