-- ============================================================
-- Arquivo: 25_turmas_de_pacote.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Turmas de pacote e pacote por turma (specs/063-pacote-fora-da-turma).
--            - A turma ganha um tipo (Mensalidade ou Pacote) e, quando de
--              Pacote, data de inicio e de fim. A turma de pacote e fechada:
--              depois que comeca, ninguem novo entra; quando acaba, os pacotes
--              dela ficam inativos (saldo preservado) pela rotina do servidor.
--            - O pacote volta a poder apontar para uma turma (de pacote); sem
--              turma ele e o pacote de aulas individuais. A Mensalidade
--              continua sem turma.
--            - "Um pacote por turma e um individual por aluno" e regra do
--              VinculoCobrancaService, nao do banco: um indice unico falharia se
--              existirem duplicados antigos, e a spec proibe resolver
--              combinacoes herdadas sozinho (mesmo criterio da spec 056).
--            - Migracao dos dados: ver o bloco 3.
-- Seguranca: nao apaga nada (nenhum DELETE) e nao altera valor, saldo, total de
--            aulas nem "ativo" de nenhum vinculo. O unico UPDATE em
--            vinculo_cobranca preenche turma_id dos pacotes convertidos; o unico
--            UPDATE em turma preenche tipo. Matriculas (alunos_turma), aulas e
--            pagamentos nao sao tocados. Fazer backup antes (database/backup).
-- Pre-requisito: executar 01 a 23 antes deste bloco (a 24 e da spec 062 e nao
--            e pre-requisito).
-- ============================================================

USE spi_db;

-- 1) Turma: tipo e periodo da turma de pacote.
ALTER TABLE turma
    ADD COLUMN tipo VARCHAR(20) NOT NULL DEFAULT 'Mensalidade' COMMENT 'Mensalidade ou Pacote (specs/063)' AFTER nome,
    ADD COLUMN data_inicio DATE NULL COMMENT 'Inicio da turma de pacote; depois de preenchida nao muda (a turma fica fechada)' AFTER tipo,
    ADD COLUMN data_fim DATE NULL COMMENT 'Fim da turma de pacote; vazia ate a professora preencher (turmas migradas)' AFTER data_inicio;

ALTER TABLE turma ADD CONSTRAINT chk_turma_tipo CHECK (tipo IN ('Mensalidade', 'Pacote'));
ALTER TABLE turma ADD CONSTRAINT chk_turma_datas_so_pacote CHECK (tipo = 'Pacote' OR (data_inicio IS NULL AND data_fim IS NULL));
ALTER TABLE turma ADD CONSTRAINT chk_turma_periodo CHECK (data_inicio IS NULL OR data_fim IS NULL OR data_fim >= data_inicio);

-- 2) Vinculo: turma passa a ser permitida tambem para Pacote.
ALTER TABLE vinculo_cobranca DROP CHECK chk_vinculocobranca_turma_so_avulsa;
ALTER TABLE vinculo_cobranca
    ADD CONSTRAINT chk_vinculocobranca_turma_avulsa_ou_pacote CHECK (turma_id IS NULL OR modalidade IN ('Avulsa', 'Pacote'));

-- 3) Migracao dos pacotes existentes (hoje todo pacote e "sem turma").
--    Converte so o pacote ATIVO de aluno que:
--      - tem exatamente um pacote ativo;
--      - esta matriculado em exatamente UMA turma (qualquer estado), e ela esta ATIVA
--        (turma inativa nao converte: a rotina de fim de turma inativaria o
--        pacote logo em seguida);
--    e somente se TODOS os alunos ativos dessa turma tambem forem candidatos
--    (a turma de pacote so tem quem tem pacote dela). Os demais casos NAO sao
--    alterados e aparecem nas consultas do bloco 4.
--    Pacote de aluno sem nenhuma matricula nao muda: passa a ser o pacote de
--    aulas individuais (turma_id nulo).
CREATE TEMPORARY TABLE tmp_pacote_candidato AS
SELECT p.aluno_id, p.id AS vinculo_id, m.turma_id
FROM vinculo_cobranca p
JOIN (SELECT aluno_id, MIN(turma_id) AS turma_id
      FROM alunos_turma
      GROUP BY aluno_id
      HAVING COUNT(*) = 1) m ON m.aluno_id = p.aluno_id
JOIN turma t ON t.id = m.turma_id AND t.ativo = 1
WHERE p.modalidade = 'Pacote' AND p.ativo = 1 AND p.turma_id IS NULL
  AND (SELECT COUNT(*) FROM vinculo_cobranca x
       WHERE x.aluno_id = p.aluno_id AND x.modalidade = 'Pacote' AND x.ativo = 1) = 1;

CREATE TEMPORARY TABLE tmp_turma_elegivel AS
SELECT t.id AS turma_id
FROM turma t
WHERE t.ativo = 1
  AND EXISTS (SELECT 1 FROM alunos_turma at
              JOIN alunos a ON a.id = at.aluno_id AND a.ativo = 1
              WHERE at.turma_id = t.id)
  AND NOT EXISTS (SELECT 1 FROM alunos_turma at
                  JOIN alunos a ON a.id = at.aluno_id AND a.ativo = 1
                  LEFT JOIN tmp_pacote_candidato c ON c.aluno_id = at.aluno_id AND c.turma_id = at.turma_id
                  WHERE at.turma_id = t.id AND c.aluno_id IS NULL);

UPDATE turma t
JOIN tmp_turma_elegivel e ON e.turma_id = t.id
SET t.tipo = 'Pacote';

UPDATE vinculo_cobranca v
JOIN tmp_pacote_candidato c ON c.vinculo_id = v.id
JOIN tmp_turma_elegivel e ON e.turma_id = c.turma_id
SET v.turma_id = c.turma_id;

DROP TEMPORARY TABLE tmp_turma_elegivel;
DROP TEMPORARY TABLE tmp_pacote_candidato;

-- 4) Casos para a professora decidir (somente leitura). Nada disto foi alterado.
--    (a) pacote ativo, sem turma, de aluno matriculado em varias turmas
SELECT 'a) pacote de aluno em varias turmas' AS caso, p.id AS vinculo_id, p.aluno_id, GROUP_CONCAT(at.turma_id ORDER BY at.turma_id) AS turmas
FROM vinculo_cobranca p
JOIN alunos_turma at ON at.aluno_id = p.aluno_id
WHERE p.modalidade = 'Pacote' AND p.ativo = 1 AND p.turma_id IS NULL
GROUP BY p.id, p.aluno_id
HAVING COUNT(*) > 1;

--    (b) turma ativa com pacote sem turma e alunos de outras modalidades juntos
SELECT 'b) turma mista' AS caso, at.turma_id, GROUP_CONCAT(DISTINCT at.aluno_id ORDER BY at.aluno_id) AS alunos_com_pacote_sem_turma
FROM alunos_turma at
JOIN turma t ON t.id = at.turma_id AND t.ativo = 1 AND t.tipo = 'Mensalidade'
JOIN vinculo_cobranca p ON p.aluno_id = at.aluno_id AND p.modalidade = 'Pacote' AND p.ativo = 1 AND p.turma_id IS NULL
GROUP BY at.turma_id;

--    (c) aluno com mais de um pacote ativo do mesmo grupo (dado herdado da spec 056)
SELECT 'c) mais de um pacote ativo do mesmo grupo' AS caso, aluno_id, turma_id, COUNT(*) AS pacotes_ativos
FROM vinculo_cobranca
WHERE modalidade = 'Pacote' AND ativo = 1
GROUP BY aluno_id, turma_id
HAVING COUNT(*) > 1;

--    (d) pacote ativo sem turma de aluno matriculado em turma inativa (nao convertido)
SELECT 'd) pacote de aluno em turma inativa' AS caso, p.id AS vinculo_id, p.aluno_id, at.turma_id
FROM vinculo_cobranca p
JOIN alunos_turma at ON at.aluno_id = p.aluno_id
JOIN turma t ON t.id = at.turma_id AND t.ativo = 0
WHERE p.modalidade = 'Pacote' AND p.ativo = 1 AND p.turma_id IS NULL;
