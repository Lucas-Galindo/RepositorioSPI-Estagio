-- ============================================================
-- Arquivo: 14_vinculo_cobranca.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Tabela vinculo_cobranca -- configuracao de cobranca de um
--            aluno em um contexto especifico (uma turma, ou atendimento
--            individual quando turma_id e NULL). Modalidade Avulsa,
--            Mensalidade (com aulas_incluidas opcional) ou Pacote (com
--            saldo_aulas opcional). Exclusao logica via ativo.
--            Nesta fatia (specs/037) a tabela e apenas cadastro: nenhum
--            processo automatico a le (a geracao de contas a receber
--            continua usando alunos.valor_aula).
-- Pre-requisito: executar 01 a 13 antes deste bloco.
-- ============================================================

USE spi_db;

CREATE TABLE vinculo_cobranca (
    id INT AUTO_INCREMENT PRIMARY KEY,
    aluno_id INT NOT NULL COMMENT 'Aluno cobrado por este vinculo',
    turma_id INT NULL COMMENT 'Turma do vinculo; NULL indica atendimento individual',
    modalidade VARCHAR(20) NOT NULL COMMENT 'Modalidade de cobranca: Avulsa, Mensalidade ou Pacote',
    valor DECIMAL(10,2) NOT NULL COMMENT 'Valor da cobranca (sempre maior que zero)',
    aulas_incluidas INT NULL COMMENT 'Aulas incluidas na mensalidade; so se aplica a modalidade Mensalidade',
    saldo_aulas INT NULL COMMENT 'Aulas restantes do pacote; so se aplica a modalidade Pacote',
    ativo BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Exclusao logica: TRUE = ativo, FALSE = excluido',
    -- Coluna gerada que sustenta a unicidade "no maximo um vinculo ATIVO por
    -- aluno+turma e um por aluno sem turma". Um UNIQUE (aluno_id, turma_id)
    -- simples nao serve: NULL nunca colide em indice unico do MySQL (permitiria
    -- varios vinculos individuais) e ele contaria vinculos inativos. Aqui a
    -- chave so existe para linhas ativas (NULL quando inativo, entao nao
    -- colide) e usa 0 no lugar de turma_id NULL (ids de turma comecam em 1).
    -- E gerida somente pelo banco: o EF Core nao a mapeia.
    chave_ativa VARCHAR(30) GENERATED ALWAYS AS (
        IF(ativo, CONCAT(aluno_id, ':', IFNULL(turma_id, 0)), NULL)
    ) VIRTUAL COMMENT 'Chave de unicidade dos vinculos ativos (aluno:turma, 0 = individual)',
    CONSTRAINT fk_vinculocobranca_aluno FOREIGN KEY (aluno_id) REFERENCES alunos(id),
    CONSTRAINT fk_vinculocobranca_turma FOREIGN KEY (turma_id) REFERENCES turma(id),
    CONSTRAINT chk_vinculocobranca_modalidade CHECK (modalidade IN ('Avulsa','Mensalidade','Pacote')),
    CONSTRAINT chk_vinculocobranca_valor CHECK (valor > 0),
    CONSTRAINT chk_vinculocobranca_aulas_incluidas CHECK (aulas_incluidas IS NULL OR (modalidade = 'Mensalidade' AND aulas_incluidas >= 1)),
    CONSTRAINT chk_vinculocobranca_saldo_aulas CHECK (saldo_aulas IS NULL OR (modalidade = 'Pacote' AND saldo_aulas >= 0)),
    CONSTRAINT uq_vinculocobranca_chave_ativa UNIQUE (chave_ativa)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Configuracao de cobranca do aluno por turma ou atendimento individual';
