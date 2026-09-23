-- ============================================================
-- Arquivo: 15_job_cobranca_mensalidade.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Suporte ao job de cobranca automatica de mensalidade
--            (specs/039). Duas partes:
--            1) Nova categoria de receita "Mensalidade", distinta de
--               "Aula em turma"/"Aula particular" (decisao de
--               /speckit-clarify, 2026-09-22), para que a professora
--               consiga diferenciar essa receita nos relatorios
--               financeiros.
--            2) Coluna pagamento.vinculo_cobranca_id, que liga uma conta
--               a receber ao VinculoCobranca que a originou -- so
--               preenchida pelas cobrancas geradas por este job (NULL
--               para pagamentos manuais ou gerados por presenca de aula,
--               specs/038). O indice unico (vinculo_cobranca_id,
--               competencia) sustenta a idempotencia do job no proprio
--               banco (FR-004): o job nunca pode gerar duas cobrancas do
--               mesmo vinculo na mesma competencia. NULL nunca colide em
--               indice UNIQUE do MySQL, entao pagamentos sem relacao com
--               um vinculo (todos os ja existentes, e todos os gerados
--               por specs/038) nunca sao afetados por essa restricao --
--               mesmo raciocinio do indice uq_vinculocobranca_chave_ativa
--               em 14_vinculo_cobranca.sql.
-- Pre-requisito: executar 01 a 14 antes deste bloco.
-- ============================================================

USE spi_db;

INSERT INTO categoria_receita (nome, ativo) VALUES ('Mensalidade', TRUE);

ALTER TABLE pagamento
    ADD COLUMN vinculo_cobranca_id INT NULL COMMENT 'Vinculo de cobranca que originou esta cobranca automatica de mensalidade (specs/039); NULL para pagamentos manuais ou gerados por presenca (specs/038)',
    ADD CONSTRAINT fk_pagamento_vinculo_cobranca FOREIGN KEY (vinculo_cobranca_id) REFERENCES vinculo_cobranca(id),
    ADD CONSTRAINT uq_pagamento_vinculo_competencia UNIQUE (vinculo_cobranca_id, competencia);
