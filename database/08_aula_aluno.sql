-- ============================================================
-- Arquivo: 08_aula_aluno.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: O ERS (Estorias 2, 8 e 13) referencia uma entidade
--            AULA_ALUNOS que nao existia no schema 01-06: sem ela,
--            uma aula individual (turma_id NULL) nao tem como
--            identificar o aluno dono, e nao ha como marcar presenca
--            por aluno (nem em aula individual nem em aula de turma).
--            Este bloco cria essa tabela.
--
-- Sobre o trigger trg_aula_before_update_frequencia (bloco 05):
--            O proprio comentario do trigger ja documentava que ele e
--            uma "versao simplificada" (incrementa frequencia de TODOS
--            os alunos ativos da turma, sem opcao de marcar falta
--            individual) e que "ajustes finos... ficam a cargo da API".
--            Com aula_aluno, a API agora controla frequencia de forma
--            precisa (individual e turma, com falta por aluno), entao
--            o trigger e removido para nao contar frequencia em
--            duplicidade com o que a API grava.
-- Pre-requisito: executar 01 a 07 antes deste bloco.
-- ============================================================

USE spi_db;

CREATE TABLE aula_aluno (
    aula_id INT NOT NULL COMMENT 'Aula em que o aluno esta vinculado',
    aluno_id INT NOT NULL COMMENT 'Aluno vinculado a aula (individual ou de turma)',
    presente BOOLEAN NULL COMMENT 'NULL = aula ainda nao realizada; TRUE/FALSE = presenca marcada ao registrar a sessao',
    PRIMARY KEY (aula_id, aluno_id),
    CONSTRAINT fk_aulaaluno_aula FOREIGN KEY (aula_id) REFERENCES aula(id),
    CONSTRAINT fk_aulaaluno_aluno FOREIGN KEY (aluno_id) REFERENCES alunos(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Vinculo aula-aluno com presenca, cobrindo aulas individuais e de turma';

CREATE INDEX ix_aulaaluno_aluno ON aula_aluno (aluno_id);

DROP TRIGGER IF EXISTS trg_aula_before_update_frequencia;
