-- ============================================================
-- Arquivo: 04_procedures.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Procedures de negocio do sistema.
-- Pre-requisito: executar 01, 02 e 03 antes deste bloco.
-- ============================================================

USE spi_db;

DELIMITER $$

-- ============================================================
-- Procedure: sp_cadastrar_aluno
-- ------------------------------------------------------------
-- Objetivo:
--   Cadastrar um novo aluno gerando automaticamente o RA no formato:
--   [ANO(4 digitos)][SEMESTRE(1 digito)][SEQUENCIAL(3 digitos)]
--   Exemplo: ano 2026, 1o semestre, 1o aluno cadastrado -> 20261001
--
-- Parametros de entrada:
--   p_nome, p_cpf, p_telefone_aluno, p_telefone_responsavel,
--   p_email, p_senha, p_email_responsavel, p_valor_aula
--
-- Parametros de saida (OUT):
--   p_novo_id -> id gerado para o aluno na tabela alunos
--   p_novo_ra -> RA gerado para o aluno
--
-- Uso (chamada pela API):
--   CALL sp_cadastrar_aluno(
--       p_nome, p_cpf, p_telefone_aluno, p_telefone_responsavel,
--       p_email, p_senha, p_email_responsavel, p_valor_aula,
--       @id_saida, @ra_saida
--   );
--   SELECT @id_saida, @ra_saida;
-- ============================================================
CREATE PROCEDURE sp_cadastrar_aluno(
    IN  p_nome VARCHAR(150),
    IN  p_cpf VARCHAR(14),
    IN  p_telefone_aluno VARCHAR(20),
    IN  p_telefone_responsavel VARCHAR(20),
    IN  p_email VARCHAR(150),
    IN  p_senha VARCHAR(255),
    IN  p_email_responsavel VARCHAR(150),
    IN  p_valor_aula DECIMAL(10,2),
    OUT p_novo_id INT,
    OUT p_novo_ra VARCHAR(20)
)
sp_cadastrar_aluno_block: BEGIN
    DECLARE v_ano INT;
    DECLARE v_semestre TINYINT;
    DECLARE v_sequencial INT;
    DECLARE v_sequencial_str VARCHAR(3);

    -- Passo 1: calcula o ano e o semestre atuais com base na data corrente.
    -- Regra: meses 01 a 06 => semestre 1 | meses 07 a 12 => semestre 2.
    SET v_ano = YEAR(CURDATE());
    SET v_semestre = IF(MONTH(CURDATE()) <= 6, 1, 2);

    -- Inicia transacao para garantir atomicidade na geracao do sequencial
    -- (evita que dois cadastros simultaneos gerem o mesmo RA).
    START TRANSACTION;

    -- Passo 2: garante que existe um registro de controle para o ano/semestre
    -- atual. Se ja existir, o INSERT vira um no-op controlado (nao altera nada).
    INSERT INTO ra_controle (ano, semestre, ultimo_sequencial)
    VALUES (v_ano, v_semestre, 0)
    ON DUPLICATE KEY UPDATE ano = ano;

    -- Passo 3: seleciona a linha de controle com FOR UPDATE, bloqueando-a
    -- ate o COMMIT. Isso evita race condition quando duas chamadas a esta
    -- procedure ocorrem ao mesmo tempo para o mesmo ano/semestre.
    SELECT ultimo_sequencial INTO v_sequencial
    FROM ra_controle
    WHERE ano = v_ano AND semestre = v_semestre
    FOR UPDATE;

    -- Passo 4: incrementa o sequencial de forma atomica e persiste.
    SET v_sequencial = v_sequencial + 1;

    UPDATE ra_controle
    SET ultimo_sequencial = v_sequencial
    WHERE ano = v_ano AND semestre = v_semestre;

    -- Passo 5: monta o RA concatenando ano (4 digitos) + semestre (1 digito)
    -- + sequencial (3 digitos, zero a esquerda via LPAD).
    SET v_sequencial_str = LPAD(v_sequencial, 3, '0');
    SET p_novo_ra = CONCAT(CAST(v_ano AS CHAR(4)), CAST(v_semestre AS CHAR(1)), v_sequencial_str);

    -- Passo 6: insere o novo aluno ja com o RA calculado.
    INSERT INTO alunos (
        ra, nome, cpf, telefone_aluno, telefone_responsavel,
        email, senha, email_responsavel, valor_aula, frequencia, ativo
    ) VALUES (
        p_novo_ra, p_nome, p_cpf, p_telefone_aluno, p_telefone_responsavel,
        p_email, p_senha, p_email_responsavel, p_valor_aula, 0, TRUE
    );

    -- Recupera o id gerado pelo AUTO_INCREMENT.
    SET p_novo_id = LAST_INSERT_ID();

    -- Confirma a transacao (libera o lock da linha de ra_controle).
    COMMIT;

    -- Passo 7: retorna id e RA tambem via SELECT, para uso conveniente pela API.
    SELECT p_novo_id AS id, p_novo_ra AS ra;
END$$

-- ============================================================
-- Procedure: sp_excluir_aluno_logico
-- Objetivo: exclusao logica de um aluno (nao remove o registro do banco).
-- ============================================================
CREATE PROCEDURE sp_excluir_aluno_logico(
    IN p_aluno_id INT
)
BEGIN
    UPDATE alunos
    SET ativo = FALSE
    WHERE id = p_aluno_id;
END$$

-- ============================================================
-- Procedure: sp_excluir_turma_logica
-- Objetivo: exclusao logica de uma turma, preservando historico de aulas.
-- ============================================================
CREATE PROCEDURE sp_excluir_turma_logica(
    IN p_turma_id INT
)
BEGIN
    UPDATE turma
    SET ativo = FALSE
    WHERE id = p_turma_id;
END$$

-- ============================================================
-- Procedure: sp_excluir_materia_logica
-- Objetivo: exclusao logica de uma materia.
-- ============================================================
CREATE PROCEDURE sp_excluir_materia_logica(
    IN p_materia_id INT
)
BEGIN
    UPDATE materias
    SET ativo = FALSE
    WHERE id = p_materia_id;
END$$

-- ============================================================
-- Procedure: sp_excluir_aula_logica
-- Objetivo: exclusao logica de uma aula.
-- ============================================================
CREATE PROCEDURE sp_excluir_aula_logica(
    IN p_aula_id INT
)
BEGIN
    UPDATE aula
    SET ativo = FALSE
    WHERE id = p_aula_id;
END$$

-- ============================================================
-- Procedure: sp_atualizar_status_pagamento
-- Objetivo:
--   Atualizar o status de um pagamento. Quando o novo status for
--   'Pago', preenche automaticamente a data_pagamento com CURDATE(),
--   caso ela ainda esteja nula (nao sobrescreve uma data ja informada).
-- ============================================================
CREATE PROCEDURE sp_atualizar_status_pagamento(
    IN p_pagamento_id INT,
    IN p_novo_status VARCHAR(20)
)
BEGIN
    UPDATE pagamento
    SET status = p_novo_status,
        data_pagamento = CASE
            WHEN p_novo_status = 'Pago' AND data_pagamento IS NULL THEN CURDATE()
            ELSE data_pagamento
        END
    WHERE id = p_pagamento_id;
END$$

DELIMITER ;
