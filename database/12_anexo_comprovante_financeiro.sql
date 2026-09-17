-- ============================================================
-- Arquivo: 12_anexo_comprovante_financeiro.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Anexo de arquivo (nota fiscal/cupom fiscal) em Contas a Pagar
--            (conta_pagar) e Contas a Receber (pagamento). Cada registro
--            pode ter no maximo um anexo -- por isso o conteudo binario e
--            os metadados moram como colunas diretas na propria linha (nao
--            uma tabela separada), a mesma decisao de design ja usada para
--            os demais campos financeiros (ver 10_financeiro_contas.sql):
--            um unico backup cobrindo dados e anexos, sem storage externo.
--            Substituir o anexo e um simples UPDATE dessas colunas; nao ha
--            "lixo" a limpar, pois o conteudo antigo mora na mesma linha.
-- Pre-requisito: executar 01 a 11 antes deste bloco.
-- ============================================================

USE spi_db;

ALTER TABLE conta_pagar
    ADD COLUMN arquivo_conteudo MEDIUMBLOB NULL COMMENT 'Conteudo binario do comprovante (nota fiscal/cupom); NULL = sem anexo',
    ADD COLUMN arquivo_nome_original VARCHAR(255) NULL COMMENT 'Nome original do arquivo enviado, para exibicao/download',
    ADD COLUMN arquivo_tipo_mime VARCHAR(100) NULL COMMENT 'Tipo MIME do arquivo (image/jpeg, image/png ou application/pdf)',
    ADD COLUMN arquivo_tamanho_bytes INT UNSIGNED NULL COMMENT 'Tamanho do arquivo em bytes',
    ADD COLUMN arquivo_data_upload DATETIME NULL COMMENT 'Data/hora (UTC) em que o anexo atual foi enviado';

ALTER TABLE pagamento
    ADD COLUMN arquivo_conteudo MEDIUMBLOB NULL COMMENT 'Conteudo binario do comprovante (nota fiscal/cupom); NULL = sem anexo',
    ADD COLUMN arquivo_nome_original VARCHAR(255) NULL COMMENT 'Nome original do arquivo enviado, para exibicao/download',
    ADD COLUMN arquivo_tipo_mime VARCHAR(100) NULL COMMENT 'Tipo MIME do arquivo (image/jpeg, image/png ou application/pdf)',
    ADD COLUMN arquivo_tamanho_bytes INT UNSIGNED NULL COMMENT 'Tamanho do arquivo em bytes',
    ADD COLUMN arquivo_data_upload DATETIME NULL COMMENT 'Data/hora (UTC) em que o anexo atual foi enviado';
