-- ============================================================
-- Arquivo: 13_endereco_aluno_responsavel.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Endereco do aluno (CEP, Rua, Numero, Complemento, Bairro,
--            Cidade, Estado) e endereco do responsavel (aplicavel quando o
--            aluno e menor de idade), com uma flag booleana indicando se o
--            endereco do responsavel espelha o do aluno. Todas as colunas
--            sao nullable -- a obrigatoriedade de CEP/Rua/Numero e imposta
--            somente na camada de validacao (FluentValidation), nunca via
--            NOT NULL, para nao quebrar alunos ja cadastrados sem endereco.
-- Pre-requisito: executar 01 a 12 antes deste bloco.
-- ============================================================

USE spi_db;

ALTER TABLE alunos
    ADD COLUMN cep VARCHAR(8) NULL COMMENT 'CEP do endereco do aluno (8 digitos, sem mascara)',
    ADD COLUMN rua VARCHAR(150) NULL COMMENT 'Logradouro do endereco do aluno',
    ADD COLUMN numero VARCHAR(10) NULL COMMENT 'Numero do endereco do aluno',
    ADD COLUMN complemento VARCHAR(100) NULL COMMENT 'Complemento do endereco do aluno (opcional)',
    ADD COLUMN bairro VARCHAR(100) NULL COMMENT 'Bairro do endereco do aluno (opcional)',
    ADD COLUMN cidade VARCHAR(100) NULL COMMENT 'Cidade do endereco do aluno (opcional)',
    ADD COLUMN estado VARCHAR(2) NULL COMMENT 'UF do endereco do aluno (opcional)',
    ADD COLUMN responsavel_mesmo_endereco BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Quando true, o endereco do responsavel espelha o do aluno (colunas responsavel_* abaixo ficam sem uso)',
    ADD COLUMN responsavel_cep VARCHAR(8) NULL COMMENT 'CEP proprio do responsavel, usado só quando responsavel_mesmo_endereco = false',
    ADD COLUMN responsavel_rua VARCHAR(150) NULL COMMENT 'Logradouro proprio do responsavel',
    ADD COLUMN responsavel_numero VARCHAR(10) NULL COMMENT 'Numero proprio do responsavel',
    ADD COLUMN responsavel_complemento VARCHAR(100) NULL COMMENT 'Complemento proprio do responsavel (opcional)',
    ADD COLUMN responsavel_bairro VARCHAR(100) NULL COMMENT 'Bairro proprio do responsavel (opcional)',
    ADD COLUMN responsavel_cidade VARCHAR(100) NULL COMMENT 'Cidade propria do responsavel (opcional)',
    ADD COLUMN responsavel_estado VARCHAR(2) NULL COMMENT 'UF propria do responsavel (opcional)';
