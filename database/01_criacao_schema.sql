-- ============================================================
-- Arquivo: 01_criacao_schema.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Criacao do schema (banco de dados) do sistema.
--            Este bloco deve ser executado primeiro, antes dos demais.
-- ============================================================

-- Remove o banco de dados caso ja exista (usar apenas em ambiente de
-- desenvolvimento/teste; NUNCA executar em producao sem certeza).
-- DROP DATABASE IF EXISTS spi_db;

-- Cria o banco de dados utilizando utf8mb4/utf8mb4_unicode_ci, garantindo
-- suporte completo a acentuacao, caracteres especiais e emojis (ex.: em
-- mensagens de lembrete enviadas aos alunos).
CREATE DATABASE IF NOT EXISTS spi_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

-- Seleciona o banco de dados para as proximas operacoes (blocos seguintes)
USE spi_db;
