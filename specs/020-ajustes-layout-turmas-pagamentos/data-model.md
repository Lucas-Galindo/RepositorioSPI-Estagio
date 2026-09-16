# Phase 1 Data Model: Ajustes de Layout — Turmas e Pagamentos

**Feature**: [spec.md](./spec.md) | **Date**: 2026-09-11

## Não aplicável

Esta feature não introduz, altera nem remove nenhuma entidade de dados, campo, validação ou transição de estado. É um conjunto de ajustes puramente visuais (posicionamento e espaçamento) sobre telas que já consomem dados existentes sem modificação:

- A tela de Turmas continua consumindo a mesma lista de turmas via a API já existente (ver [Gerenciar Turma](../006-gerenciar-turma/spec.md)).
- A tela de detalhe de Turma continua usando o mesmo fluxo de vínculo/desvínculo de aluno (mesmo endpoint, mesma validação).
- A aba Pagamentos continua exibindo os mesmos dados de Pagamento e de FormaPagamento (ver [Registrar Pagamento](../009-registrar-pagamento/spec.md)), apenas reordenados visualmente na página.

Nenhum `data-model.md` funcional é necessário para esta feature além deste registro explícito de não aplicabilidade.
