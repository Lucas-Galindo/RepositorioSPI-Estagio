-- ============================================================
-- Arquivo: 20_remove_trigger_frequencia.sql
-- Sistema: SPI - Sistema para Professoras Independentes
-- Descricao: Remove o trigger trg_aula_before_update_frequencia (05_triggers.sql).
--            Ele somava +1 na frequencia de TODOS os alunos ativos da turma
--            quando a aula virava "Realizada" -- inclusive de quem faltou --,
--            enquanto a API (AulaService, Estoria 8) ja soma +1 so para os
--            presentes. Resultado: presente contava 2, faltoso contava 1.
--            O proprio trigger dizia que a regra fina ficava "a cargo da API";
--            como a API ja faz isso, o trigger ficou obsoleto e errado.
--            Achado na validacao real da specs/055.
-- Observacao: NAO recalcula frequencias ja gravadas (podem estar infladas);
--            isso depende de decisao do usuario (aulas antigas podem nao ter
--            presenca registrada por aluno em aula_aluno).
-- Pre-requisito: executar 01 a 19 antes deste bloco.
-- ============================================================

USE spi_db;

DROP TRIGGER IF EXISTS trg_aula_before_update_frequencia;
