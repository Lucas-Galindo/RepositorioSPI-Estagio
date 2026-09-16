# Phase 1 Data Model: Área de Clique Expandida no Menu do Financeiro e Correção de Hitbox na Tabela de Contas a Pagar

**Feature**: [spec.md](./spec.md) | **Date**: 2026-09-14

## Não aplicável

Esta feature não introduz, altera nem remove nenhuma entidade de dados, campo, validação ou
transição de estado. É uma melhoria/correção de comportamento de interação (área de clique)
sobre telas que já consomem dados existentes sem modificação:

- O menu de abas do módulo Financeiro continua navegando entre as mesmas três rotas
  (`/financeiro`, `/financeiro/contas-a-receber`, `/financeiro/contas-a-pagar`), sem nenhuma
  mudança de destino — apenas a área que responde ao clique aumenta.
- A tabela de Contas a Pagar continua exibindo os mesmos registros de `ContaPagar` (ver
  [Relatório Financeiro](../015-relatorio-financeiro/spec.md) para o módulo Financeiro em
  geral), apenas com a área de clique do cabeçalho corrigida para não vazar para a área de
  dados.

Nenhum `data-model.md` funcional é necessário para esta feature além deste registro explícito
de não aplicabilidade.
