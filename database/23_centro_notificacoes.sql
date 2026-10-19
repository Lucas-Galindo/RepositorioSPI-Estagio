-- ============================================================
-- Arquivo: 23_centro_notificacoes.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Centro de notificacoes e lembrete semanal de pagamento
--            pendente (specs/060-centro-notificacoes).
--            1) notificacao.aluno_id: referencia dos avisos de pagamento
--               pendente (um por professora e aluno). Os tipos de aula deixam
--               aluno_id NULL -- o MySQL aceita varios NULL num indice unico,
--               entao a chave nova nao conflita com eles.
--            2) regra_lembrete_pagamento: a regra editavel na aba Lembretes
--               (uma linha so; padrao: ligada, segunda-feira, 08:00).
--            3) envio_lembrete_pagamento: historico/auditoria dos e-mails e
--               trava de duplicidade (um registro por aluno por dia de envio,
--               atualizado nas novas tentativas).
-- Pre-requisito: executar 01 a 22 antes deste bloco.
-- ============================================================

USE spi_db;

ALTER TABLE notificacao
    ADD COLUMN aluno_id INT NULL COMMENT 'Aluno a que a notificacao se refere (tipo PagamentoPendente)' AFTER aula_id,
    ADD CONSTRAINT fk_notificacao_aluno FOREIGN KEY (aluno_id) REFERENCES alunos (id),
    ADD CONSTRAINT uq_notificacao_tipo_professor_aluno UNIQUE (tipo, professor_id, aluno_id);

CREATE TABLE regra_lembrete_pagamento (
    id INT PRIMARY KEY,
    ativo TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Envio semanal ligado/desligado',
    dia_semana TINYINT NOT NULL DEFAULT 1 COMMENT 'Dia do envio: 0 = domingo ... 6 = sabado',
    hora TIME NOT NULL DEFAULT '08:00:00' COMMENT 'Horario a partir do qual o envio do dia acontece',
    CONSTRAINT chk_regralembretepagamento_dia CHECK (dia_semana BETWEEN 0 AND 6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Regra do lembrete semanal de pagamento pendente (specs/060)';

INSERT INTO regra_lembrete_pagamento (id, ativo, dia_semana, hora) VALUES (1, 1, 1, '08:00:00');

CREATE TABLE envio_lembrete_pagamento (
    id INT AUTO_INCREMENT PRIMARY KEY,
    aluno_id INT NOT NULL COMMENT 'Aluno cobrado',
    data_envio DATE NOT NULL COMMENT 'Dia de envio da regra a que esta tentativa pertence',
    tentado_em DATETIME NOT NULL COMMENT 'Ultima tentativa',
    destinatarios VARCHAR(500) NOT NULL COMMENT 'E-mails do aluno/responsavel, separados por virgula',
    resultado VARCHAR(20) NOT NULL COMMENT 'Enviado, Falha ou SemDestinatario',
    tentativas INT NOT NULL DEFAULT 1 COMMENT 'Tentativas no dia de envio',
    detalhe VARCHAR(500) NULL COMMENT 'Motivo da falha',
    CONSTRAINT fk_enviolembretepagamento_aluno FOREIGN KEY (aluno_id) REFERENCES alunos (id),
    CONSTRAINT uq_enviolembretepagamento_aluno_dia UNIQUE (aluno_id, data_envio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Historico do lembrete semanal de pagamento pendente (specs/060)';
