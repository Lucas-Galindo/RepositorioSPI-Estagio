-- ============================================================
-- Arquivo: 21_cobranca_por_aluno.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Cobranca por aluno e reposicao de aulas (specs/056).
--            - Mensalidade e Pacote deixam de ter turma: passam a valer para
--              todas as aulas do aluno. So a Avulsa continua por contexto
--              (uma turma, ou atendimento individual quando sem turma).
--            - A unicidade "um vinculo ativo por aluno+turma" passa a valer so
--              para Avulsa. "Mensalidade exclusiva" e "um pacote por aluno" sao
--              regras do VinculoCobrancaService, nao do banco: um indice unico
--              para pacote falharia se ja existirem dois pacotes ativos do
--              mesmo aluno, e a spec proibe resolver esses conflitos sozinho
--              (FR-021) -- eles sao mostrados para a professora decidir.
--            - aula_aluno.reposicao marca o aluno colocado numa aula de turma
--              sem ser matriculado nela.
-- Seguranca: so altera turma_id de Mensalidade/Pacote. Nenhum vinculo e
--            desativado, somado ou tem valor/saldo alterado.
-- Pre-requisito: executar 01 a 20 antes deste bloco.
-- ============================================================

USE spi_db;

-- 1) Remove a unicidade antiga ANTES de mexer em turma_id: com ela, um aluno
--    com Avulsa individual (chave aluno:0) e Pacote numa turma colidiria ao
--    pacote virar "sem turma" (tambem aluno:0).
ALTER TABLE vinculo_cobranca DROP INDEX uq_vinculocobranca_chave_ativa;
ALTER TABLE vinculo_cobranca DROP COLUMN chave_ativa;

-- 2) Mensalidade e Pacote passam a ser do aluno.
UPDATE vinculo_cobranca SET turma_id = NULL WHERE modalidade IN ('Mensalidade', 'Pacote') AND turma_id IS NOT NULL;

-- 3) Nova unicidade, so para Avulsa ativa (0 no lugar de turma NULL, ids de
--    turma comecam em 1; NULL quando nao se aplica, e NULL nunca colide).
ALTER TABLE vinculo_cobranca
    ADD COLUMN chave_avulsa_ativa VARCHAR(30) GENERATED ALWAYS AS (
        IF(ativo AND modalidade = 'Avulsa', CONCAT(aluno_id, ':', IFNULL(turma_id, 0)), NULL)
    ) VIRTUAL COMMENT 'Chave de unicidade das Avulsas ativas (aluno:turma, 0 = individual)';
ALTER TABLE vinculo_cobranca ADD CONSTRAINT uq_vinculocobranca_chave_avulsa_ativa UNIQUE (chave_avulsa_ativa);
ALTER TABLE vinculo_cobranca ADD CONSTRAINT chk_vinculocobranca_turma_so_avulsa CHECK (turma_id IS NULL OR modalidade = 'Avulsa');

-- 4) Participante de reposicao.
ALTER TABLE aula_aluno
    ADD COLUMN reposicao TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Aluno colocado na aula de turma sem ser matriculado nela (specs/056)';
