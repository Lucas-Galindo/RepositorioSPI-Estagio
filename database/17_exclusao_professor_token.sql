-- ------------------------------------------------------------
-- Tabela: exclusao_professor_token
-- 2FA por e-mail para o Admin confirmar a exclusao logica da
-- Professora (spec 044). Dono do token e o Admin que solicitou a
-- exclusao (admin_id); alvo e a Professora a ser desativada
-- (professor_id) - donos diferentes do fluxo de senha_reset_token,
-- onde dono e alvo sao sempre a mesma professora. Token de uso
-- unico, validade de 15 minutos, hash armazenado (nunca o valor em
-- claro).
-- ------------------------------------------------------------
CREATE TABLE exclusao_professor_token (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    admin_id INT NOT NULL COMMENT 'Admin autenticado que solicitou a exclusao e dono do codigo',
    professor_id INT NOT NULL COMMENT 'Professora alvo da exclusao',
    token_hash VARCHAR(255) NOT NULL COMMENT 'Hash do codigo de verificacao (nunca o valor em claro)',
    criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Data/hora de emissao do codigo',
    expira_em DATETIME NOT NULL COMMENT 'Data/hora de expiracao do codigo (janela de 15 minutos)',
    usado BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'TRUE apos o codigo ser consumido (confirmacao) ou invalidado por um pedido mais novo do mesmo admin',
    CONSTRAINT uq_exclusao_professor_token_hash UNIQUE (token_hash),
    CONSTRAINT fk_exclusao_professor_token_admin FOREIGN KEY (admin_id) REFERENCES admin(id),
    CONSTRAINT fk_exclusao_professor_token_professor FOREIGN KEY (professor_id) REFERENCES professor(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Codigos de verificacao (2FA por e-mail) para o Admin confirmar a exclusao logica da Professora';

CREATE INDEX ix_exclusao_professor_token_admin ON exclusao_professor_token (admin_id);
CREATE INDEX ix_exclusao_professor_token_professor ON exclusao_professor_token (professor_id);
