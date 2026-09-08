-- ============================================================
-- Arquivo: 07_auth.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Tabelas de suporte a autenticacao que nao existiam no
--            schema original (professor, alunos e materias ja tinham
--            tudo que precisavam). Este bloco so ADICIONA, nunca
--            altera nada definido em 01-06.
-- Pre-requisito: executar 01_criacao_schema.sql a 06_dados_iniciais.sql
--                antes deste bloco.
-- ============================================================

USE spi_db;

-- ------------------------------------------------------------
-- Tabela: admin
-- Papel de sistema separado da Professora (suporte/manutencao).
-- Nao ha endpoint publico de cadastro; o registro inicial e criado
-- via seed da aplicacao, lendo a senha de User Secrets/variavel de
-- ambiente (nunca fixa em texto no repositorio).
-- ------------------------------------------------------------
CREATE TABLE admin (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(150) NOT NULL COMMENT 'Nome do administrador',
    email VARCHAR(150) NOT NULL COMMENT 'E-mail de acesso ao sistema (unico)',
    senha VARCHAR(255) NOT NULL COMMENT 'Hash da senha (compativel com bcrypt)',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Exclusao logica: TRUE = ativo, FALSE = inativo',
    CONSTRAINT uq_admin_email UNIQUE (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Usuarios administradores do sistema (suporte/manutencao)';

-- ------------------------------------------------------------
-- Tabela: refresh_token
-- Suporte a rotacao/revogacao de refresh tokens de JWT. Cobre os tres
-- perfis de login (Professor/Aluno/Admin) atraves de usuario_id + perfil,
-- ja que o schema nao tem uma tabela unica de "usuario". Guarda apenas
-- o hash do token, nunca o valor em claro.
-- ------------------------------------------------------------
CREATE TABLE refresh_token (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL COMMENT 'Id do professor/aluno/admin dono do token',
    perfil VARCHAR(20) NOT NULL COMMENT 'Perfil do dono do token: Professor, Aluno ou Admin',
    token_hash VARCHAR(255) NOT NULL COMMENT 'Hash do refresh token (nunca o valor em claro)',
    criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Data/hora de emissao do token',
    expira_em DATETIME NOT NULL COMMENT 'Data/hora de expiracao do token',
    revogado_em DATETIME NULL COMMENT 'Data/hora de revogacao; NULL enquanto valido',
    substituido_por_token_hash VARCHAR(255) NULL COMMENT 'Hash do token que substituiu este na rotacao',
    ip_criacao VARCHAR(45) NULL COMMENT 'IP de origem no momento da emissao',
    user_agent VARCHAR(255) NULL COMMENT 'User-Agent do dispositivo/sessao que originou o token',
    CONSTRAINT uq_refresh_token_hash UNIQUE (token_hash),
    CONSTRAINT chk_refresh_token_perfil CHECK (perfil IN ('Professor','Aluno','Admin'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Refresh tokens de JWT, com suporte a rotacao e revogacao por sessao/dispositivo';

CREATE INDEX ix_refresh_token_usuario ON refresh_token (usuario_id, perfil);

-- ------------------------------------------------------------
-- Tabela: senha_reset_token
-- Recuperacao de senha exclusiva da Professora (o Aluno nao tem esse
-- fluxo nesta fase; quem reseta a senha do aluno e a propria professora,
-- via modulo "Gerenciar Alunos", fora do escopo deste script). Token de
-- uso unico, validade curta controlada por expira_em, hash armazenado.
-- ------------------------------------------------------------
CREATE TABLE senha_reset_token (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    professor_id INT NOT NULL COMMENT 'Professora dona do token de recuperacao',
    token_hash VARCHAR(255) NOT NULL COMMENT 'Hash do token de recuperacao (nunca o valor em claro)',
    criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Data/hora de emissao do token',
    expira_em DATETIME NOT NULL COMMENT 'Data/hora de expiracao do token (janela de 15 minutos)',
    usado BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'TRUE apos o token ser consumido (uso unico)',
    CONSTRAINT uq_senha_reset_token_hash UNIQUE (token_hash),
    CONSTRAINT fk_senha_reset_token_professor FOREIGN KEY (professor_id) REFERENCES professor(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tokens de recuperacao de senha da Professora, uso unico e validade curta';

CREATE INDEX ix_senha_reset_token_professor ON senha_reset_token (professor_id);
