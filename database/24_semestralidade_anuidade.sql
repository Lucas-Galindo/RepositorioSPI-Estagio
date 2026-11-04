-- ============================================================
-- Arquivo: 24_semestralidade_anuidade.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Semestralidade e Anuidade como formas de cobranca do aluno
--            (specs/062-semestralidade-anuidade).
--            - vinculo_cobranca ganha data_inicio e data_fim (so para
--              Semestral/Anual): o plano vale para as aulas desse periodo.
--            - A modalidade passa a aceitar 'Semestral' e 'Anual'.
--            - chk_vinculocobranca_periodo: Semestral/Anual exigem as duas
--              datas (fim depois do inicio); as demais modalidades exigem as
--              duas nulas.
--            - Duas categorias de receita novas: Semestralidade e Anuidade
--              (as parcelas do plano sao contas a receber nessas categorias).
-- Seguranca: nao apaga nada e nao altera nenhum dado existente: so acrescenta
--            colunas opcionais, troca regras de validacao (todas as linhas
--            existentes continuam validas) e insere duas categorias. Nao mexe
--            em pagamento, aula nem matricula. Fazer backup antes
--            (database/backup).
-- Nao muda: uq_pagamento_vinculo_competencia (uma parcela por mes no vinculo)
--            e chk_vinculocobranca_turma_avulsa_ou_pacote (turma so para
--            Avulsa/Pacote: Semestral/Anual ficam sem turma).
-- Pre-requisito: executar 01 a 23 antes deste bloco (a 25 e a 26 sao
--            independentes e podem vir antes ou depois).
-- ============================================================

USE spi_db;

-- 1) Periodo do plano.
ALTER TABLE vinculo_cobranca
    ADD COLUMN data_inicio DATE NULL COMMENT 'Inicio do plano; so Semestral/Anual (specs/062)' AFTER total_aulas_pacote,
    ADD COLUMN data_fim DATE NULL COMMENT 'Fim do plano = inicio + 6 ou 12 meses - 1 dia; so Semestral/Anual (specs/062)' AFTER data_inicio;

-- 2) Modalidades aceitas.
ALTER TABLE vinculo_cobranca DROP CHECK chk_vinculocobranca_modalidade;
ALTER TABLE vinculo_cobranca
    ADD CONSTRAINT chk_vinculocobranca_modalidade CHECK (modalidade IN ('Avulsa', 'Mensalidade', 'Pacote', 'Semestral', 'Anual'));

-- 3) O periodo existe so nos planos Semestral/Anual.
ALTER TABLE vinculo_cobranca
    ADD CONSTRAINT chk_vinculocobranca_periodo CHECK (
        (modalidade IN ('Semestral', 'Anual') AND data_inicio IS NOT NULL AND data_fim IS NOT NULL AND data_fim > data_inicio)
        OR (modalidade NOT IN ('Semestral', 'Anual') AND data_inicio IS NULL AND data_fim IS NULL)
    );

-- 4) Categorias de receita das parcelas (cada uma so e inserida se ainda nao existir).
INSERT INTO categoria_receita (nome, ativo)
SELECT 'Semestralidade', TRUE FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM categoria_receita WHERE nome = 'Semestralidade');
INSERT INTO categoria_receita (nome, ativo)
SELECT 'Anuidade', TRUE FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM categoria_receita WHERE nome = 'Anuidade');
