-- ============================================================
-- Arquivo: 06_dados_iniciais.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Dados iniciais (seed) necessarios para o funcionamento
--            basico do sistema. Bloco opcional.
-- Pre-requisito: executar os blocos 01 a 05 antes deste.
-- ============================================================

USE spi_db;

-- Formas de pagamento padrao, conforme levantamento com a professora
-- responsavel (Pix, Dinheiro, Cartao e Transferencia).
-- INSERT IGNORE + UNIQUE(forma) evita duplicar as linhas caso este
-- script seja executado mais de uma vez sobre o mesmo banco.
INSERT IGNORE INTO forma_pagamento (forma, descricao, ativo) VALUES
('Pix', 'Pagamento instantaneo via Pix', TRUE),
('Dinheiro', 'Pagamento em especie', TRUE),
('Cartao', 'Pagamento via cartao de credito ou debito', TRUE),
('Transferencia', 'Transferencia bancaria (TED/DOC)', TRUE);
