-- ============================================================
-- Arquivo: 22_notificacao.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Notificacoes da professora (specs/059-notificacoes-aula-pendente).
--            Comeca com o tipo "AulaPendente" (aula que terminou sem status
--            atualizado); a spec 060 acrescenta outros tipos na mesma tabela.
--            Nunca se apaga linha: a notificacao e resolvida (resolvida_em).
--            O indice unico (tipo, aula_id) garante uma notificacao por aula,
--            mesmo com duas sincronizacoes rodando ao mesmo tempo.
-- Pre-requisito: executar 01 a 21 antes deste bloco.
-- ============================================================

USE spi_db;

CREATE TABLE notificacao (
    id INT AUTO_INCREMENT PRIMARY KEY,
    professor_id INT NOT NULL COMMENT 'Professora destinataria',
    tipo VARCHAR(40) NOT NULL COMMENT 'Tipo da notificacao (ex.: AulaPendente)',
    aula_id INT NULL COMMENT 'Aula a que a notificacao se refere (tipos de aula)',
    criada_em DATETIME NOT NULL COMMENT 'Quando foi criada (ou reativada)',
    lida_em DATETIME NULL COMMENT 'Quando a professora abriu; NULL = nao lida',
    resolvida_em DATETIME NULL COMMENT 'Quando deixou de valer; NULL = ativa',
    CONSTRAINT fk_notificacao_professor FOREIGN KEY (professor_id) REFERENCES professor (id),
    CONSTRAINT fk_notificacao_aula FOREIGN KEY (aula_id) REFERENCES aula (id),
    CONSTRAINT uq_notificacao_tipo_aula UNIQUE (tipo, aula_id),
    INDEX idx_notificacao_professor_ativa (professor_id, resolvida_em)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Notificacoes da professora no sistema (specs/059)';
