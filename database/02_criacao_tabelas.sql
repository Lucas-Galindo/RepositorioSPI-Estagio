-- ============================================================
-- Arquivo: 02_criacao_tabelas.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Criacao de todas as tabelas principais do sistema,
--            com PKs, FKs, CHECK CONSTRAINTS e comentarios de negocio.
-- Pre-requisito: executar 01_criacao_schema.sql antes deste bloco.
-- ============================================================

USE spi_db;

-- ------------------------------------------------------------
-- Tabela: professor
-- Cadastro da(s) professora(s) responsavel(is) pelo sistema.
-- ------------------------------------------------------------
CREATE TABLE professor (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(150) NOT NULL COMMENT 'Nome completo da professora',
    cpf VARCHAR(14) NOT NULL COMMENT 'CPF da professora (unico)',
    email VARCHAR(150) NOT NULL COMMENT 'E-mail de acesso ao sistema (unico)',
    senha VARCHAR(255) NOT NULL COMMENT 'Hash da senha (compativel com bcrypt)',
    telefone VARCHAR(20) NULL COMMENT 'Telefone de contato da professora',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Exclusao logica: TRUE = ativo, FALSE = inativo',
    CONSTRAINT uq_professor_cpf UNIQUE (cpf),
    CONSTRAINT uq_professor_email UNIQUE (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Cadastro da(s) professora(s) responsavel(is) pelo sistema';

-- ------------------------------------------------------------
-- Tabela: materias
-- Materias/disciplinas cadastradas livremente pela professora,
-- sem se limitar a um unico tipo de conteudo pedagogico.
-- ------------------------------------------------------------
CREATE TABLE materias (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL COMMENT 'Nome da materia, ex: Matematica (unico)',
    descricao VARCHAR(255) NULL COMMENT 'Descricao opcional da materia',
    nivel VARCHAR(100) NULL COMMENT 'Nivel de ensino em texto livre, ex: Fundamental II',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Exclusao logica: TRUE = ativa, FALSE = inativa',
    CONSTRAINT uq_materias_nome UNIQUE (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Materias/disciplinas cadastradas pela professora';

-- ------------------------------------------------------------
-- Tabela: alunos
-- Cadastro dos alunos atendidos pela professora. O RA e gerado
-- automaticamente pela procedure sp_cadastrar_aluno (ver bloco 04).
-- ------------------------------------------------------------
CREATE TABLE alunos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ra VARCHAR(20) NOT NULL COMMENT 'Registro academico gerado automaticamente (formato AAAA+S+SSS)',
    nome VARCHAR(150) NOT NULL COMMENT 'Nome completo do aluno',
    cpf VARCHAR(14) NULL COMMENT 'CPF do aluno; opcional, unico quando informado',
    telefone_aluno VARCHAR(20) NULL COMMENT 'Telefone de contato do proprio aluno',
    telefone_responsavel VARCHAR(20) NULL COMMENT 'Telefone do responsavel (obrigatorio via API para menores de idade)',
    email VARCHAR(150) NULL COMMENT 'E-mail de acesso/login proprio do aluno',
    senha VARCHAR(255) NULL COMMENT 'Hash da senha do aluno (compativel com bcrypt)',
    email_responsavel VARCHAR(150) NULL COMMENT 'E-mail do responsavel (obrigatorio via API para menores de idade)',
    valor_aula DECIMAL(10,2) NOT NULL DEFAULT 0.00 COMMENT 'Valor cobrado por aula/hora-aula deste aluno',
    frequencia INT NOT NULL DEFAULT 0 COMMENT 'Contador de presencas em aulas realizadas',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Exclusao logica (equivale ao STATUS_ATIVIDADE do documento conceitual)',
    CONSTRAINT uq_alunos_ra UNIQUE (ra),
    CONSTRAINT uq_alunos_cpf UNIQUE (cpf)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Cadastro de alunos atendidos pela professora';

-- ------------------------------------------------------------
-- Tabela: turma
-- Grupos de atendimento coletivo criados pela professora.
-- ------------------------------------------------------------
CREATE TABLE turma (
    id INT AUTO_INCREMENT PRIMARY KEY,
    professor_id INT NOT NULL COMMENT 'Professora responsavel pela turma',
    nome VARCHAR(100) NOT NULL COMMENT 'Nome da turma, ex: 8 Ano A',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Exclusao logica: TRUE = ativa, FALSE = inativa',
    CONSTRAINT fk_turma_professor FOREIGN KEY (professor_id) REFERENCES professor(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Turmas/grupos de atendimento coletivo';

-- ------------------------------------------------------------
-- Tabela: alunos_turma (associativa N:N entre alunos e turma)
-- ------------------------------------------------------------
CREATE TABLE alunos_turma (
    aluno_id INT NOT NULL COMMENT 'Aluno vinculado a turma',
    turma_id INT NOT NULL COMMENT 'Turma a qual o aluno pertence',
    PRIMARY KEY (aluno_id, turma_id),
    CONSTRAINT fk_alunosturma_aluno FOREIGN KEY (aluno_id) REFERENCES alunos(id),
    CONSTRAINT fk_alunosturma_turma FOREIGN KEY (turma_id) REFERENCES turma(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tabela associativa N:N entre alunos e turma';

-- ------------------------------------------------------------
-- Tabela: aula
-- Sessoes de aula, individuais (turma_id NULL) ou vinculadas a turma.
-- ------------------------------------------------------------
CREATE TABLE aula (
    id INT AUTO_INCREMENT PRIMARY KEY,
    descricao VARCHAR(255) NULL COMMENT 'Descricao livre da aula',
    materia_id INT NOT NULL COMMENT 'Materia lecionada nesta aula',
    professor_id INT NOT NULL COMMENT 'Professora responsavel pela aula',
    turma_id INT NULL COMMENT 'Turma vinculada; NULL indica aula individual',
    data_inicio DATE NOT NULL COMMENT 'Data em que a aula ocorre/ocorreu',
    hora_inicio TIME NOT NULL COMMENT 'Horario de inicio da aula',
    hora_fim TIME NOT NULL COMMENT 'Horario de termino da aula',
    status VARCHAR(20) NOT NULL DEFAULT 'Agendada' COMMENT 'Situacao da aula',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Exclusao logica: TRUE = ativa, FALSE = inativa',
    CONSTRAINT fk_aula_materia FOREIGN KEY (materia_id) REFERENCES materias(id),
    CONSTRAINT fk_aula_professor FOREIGN KEY (professor_id) REFERENCES professor(id),
    CONSTRAINT fk_aula_turma FOREIGN KEY (turma_id) REFERENCES turma(id),
    CONSTRAINT chk_aula_status CHECK (status IN ('Agendada','Realizada','Cancelada')),
    -- Garante que o horario de termino seja sempre posterior ao de inicio
    CONSTRAINT chk_aula_horario CHECK (hora_fim > hora_inicio)
    -- IMPORTANTE: a validacao de CONFLITO de horario entre aulas (sobreposicao
    -- de agenda de uma mesma professora) NAO e implementada no banco (nem via
    -- trigger, nem via CHECK CONSTRAINT). Essa regra de negocio fica
    -- exclusivamente sob responsabilidade da camada de API, por decisao de
    -- projeto (evita bloqueios/erros dificeis de tratar no nivel de SGBD).
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Sessoes de aula, individuais ou vinculadas a uma turma';

-- ------------------------------------------------------------
-- Tabela: lembrete
-- Configuracao de lembretes automaticos enviados aos alunos das turmas.
-- ------------------------------------------------------------
CREATE TABLE lembrete (
    id INT AUTO_INCREMENT PRIMARY KEY,
    turma_id INT NOT NULL COMMENT 'Turma para a qual o lembrete se aplica',
    status VARCHAR(20) NOT NULL DEFAULT 'Pendente' COMMENT 'Situacao do envio do lembrete',
    hora_programada TIME NOT NULL COMMENT 'Horario calculado para disparo do lembrete',
    destinatarios VARCHAR(255) NOT NULL COMMENT 'Destinatarios configurados: alunos e/ou responsaveis',
    canal VARCHAR(20) NOT NULL COMMENT 'Canal de envio: WhatsApp, E-mail ou SMS',
    antecedencia_hora INT NOT NULL COMMENT 'Antecedencia, em horas, para disparo do lembrete',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Exclusao logica: TRUE = ativo, FALSE = inativo',
    CONSTRAINT fk_lembrete_turma FOREIGN KEY (turma_id) REFERENCES turma(id),
    CONSTRAINT chk_lembrete_status CHECK (status IN ('Pendente','Enviado','Falha','Cancelado'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Configuracao de lembretes automaticos enviados aos alunos das turmas';

-- ------------------------------------------------------------
-- Tabela: forma_pagamento
-- Formas de pagamento aceitas pela professora.
-- ------------------------------------------------------------
CREATE TABLE forma_pagamento (
    id INT AUTO_INCREMENT PRIMARY KEY,
    forma VARCHAR(50) NOT NULL COMMENT 'Nome da forma de pagamento, ex: Pix, Dinheiro',
    descricao VARCHAR(255) NULL COMMENT 'Descricao opcional da forma de pagamento',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Exclusao logica: TRUE = ativa, FALSE = inativa',
    CONSTRAINT uq_forma_pagamento_forma UNIQUE (forma)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Formas de pagamento aceitas pela professora';

-- ------------------------------------------------------------
-- Tabela: pagamento
-- Registros financeiros de pagamentos dos alunos.
-- OBS: esta tabela NAO possui coluna "ativo" — o estado e controlado
-- exclusivamente pela coluna "status", conforme convencoes do projeto.
-- ------------------------------------------------------------
CREATE TABLE pagamento (
    id INT AUTO_INCREMENT PRIMARY KEY,
    aluno_id INT NOT NULL COMMENT 'Aluno relacionado ao pagamento',
    forma_pagamento_id INT NOT NULL COMMENT 'Forma de pagamento utilizada',
    data_vencimento DATE NOT NULL COMMENT 'Data de vencimento do pagamento',
    data_pagamento DATE NULL COMMENT 'Data em que o pagamento foi efetivamente realizado',
    valor_final DECIMAL(10,2) NOT NULL COMMENT 'Valor final cobrado/recebido',
    status VARCHAR(20) NOT NULL DEFAULT 'Pendente' COMMENT 'Situacao do pagamento',
    CONSTRAINT fk_pagamento_aluno FOREIGN KEY (aluno_id) REFERENCES alunos(id),
    CONSTRAINT fk_pagamento_forma FOREIGN KEY (forma_pagamento_id) REFERENCES forma_pagamento(id),
    -- 'Atrasado' normalmente e calculado pela API (comparando data_vencimento
    -- com a data atual quando status ainda esta 'Pendente'), mas o valor
    -- precisa ser permitido no banco para persistir o resultado desse calculo.
    CONSTRAINT chk_pagamento_status CHECK (status IN ('Pendente','Pago','Atrasado'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Registros financeiros de pagamentos dos alunos';

-- ------------------------------------------------------------
-- Tabela: pagamento_aula (associativa N:N entre pagamento e aula)
-- ------------------------------------------------------------
CREATE TABLE pagamento_aula (
    pagamento_id INT NOT NULL COMMENT 'Pagamento vinculado',
    aula_id INT NOT NULL COMMENT 'Aula coberta pelo pagamento',
    PRIMARY KEY (pagamento_id, aula_id),
    CONSTRAINT fk_pagamentoaula_pagamento FOREIGN KEY (pagamento_id) REFERENCES pagamento(id),
    CONSTRAINT fk_pagamentoaula_aula FOREIGN KEY (aula_id) REFERENCES aula(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tabela associativa N:N entre pagamento e aula';
