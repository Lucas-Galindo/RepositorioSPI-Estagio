-- ============================================================
-- Arquivo: 10_financeiro_contas.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Evolucao do modulo Financeiro (Estoria de evolucao do
--            Financeiro - Sprint 1). "pagamento" ja representava, na
--            pratica, uma Conta a Receber (ver comentarios no
--            PagamentosController); este bloco estende essa tabela em
--            vez de criar uma entidade paralela (evita duplicar a
--            mesma obrigacao financeira em dois lugares), e cria a
--            contraparte que ainda nao existia: Contas a Pagar.
-- Pre-requisito: executar 01 a 09 antes deste bloco.
-- ============================================================

USE spi_db;

-- ------------------------------------------------------------
-- Tabela: categoria_receita
-- Categorias fixas de receita (Contas a Receber), mesmo padrao de
-- forma_pagamento: lista de apoio simples, sem CRUD dedicado por ora.
-- ------------------------------------------------------------
CREATE TABLE categoria_receita (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(80) NOT NULL COMMENT 'Nome da categoria de receita, ex: Aula particular',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Exclusao logica: TRUE = ativa, FALSE = inativa',
    CONSTRAINT uq_categoria_receita_nome UNIQUE (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Categorias fixas de receita usadas em Contas a Receber';

-- ------------------------------------------------------------
-- Tabela: categoria_despesa
-- Categorias fixas de despesa (Contas a Pagar).
-- ------------------------------------------------------------
CREATE TABLE categoria_despesa (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(80) NOT NULL COMMENT 'Nome da categoria de despesa, ex: Internet',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Exclusao logica: TRUE = ativa, FALSE = inativa',
    CONSTRAINT uq_categoria_despesa_nome UNIQUE (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Categorias fixas de despesa usadas em Contas a Pagar';

-- ------------------------------------------------------------
-- Evolucao da tabela: pagamento (passa a suportar o modelo de
-- Conta a Receber completo, sem quebrar os registros existentes:
-- todas as colunas novas sao NULL/opcionais).
-- ------------------------------------------------------------
ALTER TABLE pagamento
    ADD COLUMN descricao VARCHAR(255) NULL COMMENT 'Descricao livre da conta a receber' AFTER aluno_id,
    ADD COLUMN categoria_receita_id INT NULL COMMENT 'Categoria de receita (ex: aula particular, reposicao)' AFTER forma_pagamento_id,
    ADD COLUMN competencia DATE NULL COMMENT 'Mes/ano de referencia da receita (dia sempre 01)' AFTER data_pagamento,
    ADD COLUMN observacoes TEXT NULL COMMENT 'Observacoes livres sobre a conta a receber';

ALTER TABLE pagamento
    ADD CONSTRAINT fk_pagamento_categoria_receita FOREIGN KEY (categoria_receita_id) REFERENCES categoria_receita(id);

-- forma_pagamento_id passa a ser opcional: a geracao automatica de
-- contas a receber (aula realizada -> conta pendente, Sprint 4) ainda
-- nao sabe qual sera a forma de pagamento -- so no recebimento.
ALTER TABLE pagamento
    MODIFY COLUMN forma_pagamento_id INT NULL COMMENT 'Forma de pagamento utilizada (definida no recebimento)';

-- Adiciona o status 'Cancelado' (necessario para a operacao de
-- cancelamento de Contas a Receber, ainda nao suportada).
ALTER TABLE pagamento
    DROP CHECK chk_pagamento_status;
ALTER TABLE pagamento
    ADD CONSTRAINT chk_pagamento_status CHECK (status IN ('Pendente','Pago','Atrasado','Cancelado'));

-- ------------------------------------------------------------
-- Tabela: conta_pagar
-- Despesas relacionadas a atividade profissional da professora.
-- Segue o mesmo padrao estrutural de "pagamento" (Contas a Receber).
-- ------------------------------------------------------------
CREATE TABLE conta_pagar (
    id INT AUTO_INCREMENT PRIMARY KEY,
    descricao VARCHAR(255) NOT NULL COMMENT 'Descricao da despesa, ex: Internet - Setembro/2026',
    categoria_despesa_id INT NOT NULL COMMENT 'Categoria da despesa',
    favorecido VARCHAR(150) NULL COMMENT 'Fornecedor/favorecido, texto livre',
    valor DECIMAL(10,2) NOT NULL COMMENT 'Valor da despesa',
    competencia DATE NULL COMMENT 'Mes/ano de referencia da despesa (dia sempre 01)',
    data_vencimento DATE NOT NULL COMMENT 'Data de vencimento da despesa',
    data_pagamento DATE NULL COMMENT 'Data em que a despesa foi efetivamente paga',
    forma_pagamento_id INT NULL COMMENT 'Forma de pagamento utilizada (definida no pagamento)',
    status VARCHAR(20) NOT NULL DEFAULT 'Pendente' COMMENT 'Situacao da despesa',
    observacoes TEXT NULL COMMENT 'Observacoes livres sobre a despesa',
    CONSTRAINT fk_contapagar_categoria FOREIGN KEY (categoria_despesa_id) REFERENCES categoria_despesa(id),
    CONSTRAINT fk_contapagar_forma FOREIGN KEY (forma_pagamento_id) REFERENCES forma_pagamento(id),
    -- 'Atrasado' e calculado pela API (mesma convencao de "pagamento"),
    -- mas o valor precisa ser permitido no banco para persistir o resultado.
    CONSTRAINT chk_contapagar_status CHECK (status IN ('Pendente','Pago','Atrasado','Cancelado'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Despesas relacionadas a atividade profissional da professora (Contas a Pagar)';

-- ------------------------------------------------------------
-- Procedure: sp_atualizar_status_conta_pagar
-- Espelha sp_atualizar_status_pagamento (bloco 04): ao marcar 'Pago',
-- preenche data_pagamento com CURDATE() caso ainda esteja nula.
-- ------------------------------------------------------------
DELIMITER $$

CREATE PROCEDURE sp_atualizar_status_conta_pagar(
    IN p_conta_pagar_id INT,
    IN p_novo_status VARCHAR(20)
)
BEGIN
    UPDATE conta_pagar
    SET status = p_novo_status,
        data_pagamento = CASE
            WHEN p_novo_status = 'Pago' AND data_pagamento IS NULL THEN CURDATE()
            ELSE data_pagamento
        END
    WHERE id = p_conta_pagar_id;
END$$

DELIMITER ;

-- ------------------------------------------------------------
-- Seeds: categorias fixas de receita e despesa.
-- ------------------------------------------------------------
INSERT INTO categoria_receita (nome, ativo) VALUES
('Aula particular', TRUE),
('Aula em turma', TRUE),
('Reposicao', TRUE),
('Outro', TRUE);

INSERT INTO categoria_despesa (nome, ativo) VALUES
('Material didatico', TRUE),
('Material de escritorio', TRUE),
('Internet', TRUE),
('Energia', TRUE),
('Aluguel', TRUE),
('Softwares', TRUE),
('Servicos', TRUE),
('Impostos e taxas', TRUE),
('Outros', TRUE);
