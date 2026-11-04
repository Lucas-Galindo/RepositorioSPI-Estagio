-- ============================================================
-- Arquivo: consultas/aulas_realizadas_antes_do_inicio.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Spec: specs/064-chamada-so-nos-detalhes (US4, FR-016)
--
-- SOMENTE LEITURA -- nao altera nem apaga nada. Esta consulta so lista; qualquer
-- correcao dos dados listados depende de confirmacao do usuario.
--
-- Lista as aulas ativas "Realizada" cuja frequencia foi lancada antes do inicio da
-- aula, em um destes dois casos:
--   (a) o inicio da aula (data + hora de inicio) ainda esta no futuro; ou
--   (b) frequencia_registrada_em existe (coluna da migracao 26) e e anterior ao inicio.
-- Aulas realizadas ANTES da migracao 26 tem frequencia_registrada_em vazia e so podem
-- aparecer pelo caso (a).
--
-- NOW() e o relogio do servidor do banco; se ele estiver fora do fuso de Sao Paulo,
-- ajuste o criterio (a) antes de interpretar o resultado.
-- ============================================================

USE spi_db;

SELECT a.id,
       a.data_inicio,
       a.hora_inicio,
       a.hora_fim,
       a.status,
       a.frequencia_registrada_em,
       m.nome AS materia,
       t.nome AS turma
FROM aula a
JOIN materias m ON m.id = a.materia_id
LEFT JOIN turma t ON t.id = a.turma_id
WHERE a.ativo = 1
  AND a.status = 'Realizada'
  AND (
        TIMESTAMP(a.data_inicio, a.hora_inicio) > NOW()
     OR a.frequencia_registrada_em < TIMESTAMP(a.data_inicio, a.hora_inicio)
  )
ORDER BY a.data_inicio, a.hora_inicio;
