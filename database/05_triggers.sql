-- ============================================================
-- Arquivo: 05_triggers.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Triggers de negocio e auditoria do sistema.
-- Pre-requisito: executar os blocos 01 a 04 antes deste.
-- ============================================================

USE spi_db;

DELIMITER $$

-- ============================================================
-- Trigger: trg_aula_before_update_frequencia
-- Evento: BEFORE UPDATE em aula
-- ------------------------------------------------------------
-- Objetivo:
--   Quando o status da aula muda para 'Realizada', incrementa a
--   FREQUENCIA de cada aluno vinculado a aula.
--
-- Observacoes (versao simplificada da regra de negocio):
--   - Quando a aula esta vinculada a uma turma (turma_id NOT NULL),
--     todos os alunos ATIVOS daquela turma (via alunos_turma) tem sua
--     frequencia incrementada em 1.
--   - Quando a aula e individual (turma_id NULL), o modelo atual do
--     banco nao possui uma tabela de vinculo direto aula-aluno; logo,
--     este trigger nao consegue identificar isoladamente o aluno de
--     uma aula individual para incrementar sua frequencia.
--   - Ajustes finos desta regra (por exemplo, tratar corretamente a
--     frequencia de aulas individuais, ou considerar apenas alunos
--     marcados como presentes em uma futura tela de chamada) ficam a
--     cargo da API, conforme definido no escopo do projeto.
-- ============================================================
CREATE TRIGGER trg_aula_before_update_frequencia
BEFORE UPDATE ON aula
FOR EACH ROW
BEGIN
    IF NEW.status = 'Realizada' AND OLD.status <> 'Realizada' THEN
        IF NEW.turma_id IS NOT NULL THEN
            UPDATE alunos a
            INNER JOIN alunos_turma at ON at.aluno_id = a.id
            SET a.frequencia = a.frequencia + 1
            WHERE at.turma_id = NEW.turma_id
              AND a.ativo = TRUE;
        END IF;
        -- Aula individual (NEW.turma_id IS NULL): sem vinculo direto no
        -- modelo atual; a API deve tratar a atualizacao de frequencia
        -- para este caso especifico.
    END IF;
END$$

-- ============================================================
-- Trigger (sugestao/opcional): trg_pagamento_aula_after_insert
-- Evento: AFTER INSERT em pagamento_aula
-- ------------------------------------------------------------
-- Nenhuma acao obrigatoria e executada por este trigger hoje.
-- Ele e mantido apenas como um ponto de extensao documentado, caso a
-- professora deseje futuramente auditar os vinculos entre pagamento
-- e aula (por exemplo, registrando em uma tabela de log/auditoria).
-- ============================================================
CREATE TRIGGER trg_pagamento_aula_after_insert
AFTER INSERT ON pagamento_aula
FOR EACH ROW
BEGIN
    -- Sugestao de auditoria futura (NAO implementada atualmente):
    --
    -- INSERT INTO auditoria_pagamento_aula (pagamento_id, aula_id, criado_em)
    -- VALUES (NEW.pagamento_id, NEW.aula_id, NOW());
    --
    -- Instrucao neutra apenas para manter o corpo do trigger valido,
    -- ja que nenhuma acao obrigatoria e executada no momento:
    SET @spi_pagamento_aula_no_op = 1;
END$$

-- ============================================================
-- Trigger de auditoria: trg_alunos_before_update_bloqueia_ra
-- Evento: BEFORE UPDATE em alunos
-- ------------------------------------------------------------
-- Objetivo:
--   Impedir a alteracao manual do RA de um aluno apos sua geracao,
--   garantindo que o RA so possa ser definido pela procedure
--   sp_cadastrar_aluno (bloco 04), preservando a integridade do
--   sequencial de matriculas.
-- ============================================================
CREATE TRIGGER trg_alunos_before_update_bloqueia_ra
BEFORE UPDATE ON alunos
FOR EACH ROW
BEGIN
    IF NEW.ra <> OLD.ra THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Nao e permitido alterar o RA de um aluno ja cadastrado.';
    END IF;
END$$

DELIMITER ;
