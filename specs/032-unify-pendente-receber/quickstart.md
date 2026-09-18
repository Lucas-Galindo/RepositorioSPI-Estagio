# Quickstart: Validar a unificação do cálculo de "valor pendente a receber"

## Pré-requisitos

- Backend rodando localmente (`dotnet run` em `src/SPI.Api`), com o banco MySQL de dev acessível
  (nenhuma migração nova — ver [data-model.md](./data-model.md)).
- Uma credencial válida para autenticar. **Não usar/criar credenciais de teste persistentes** —
  pedir uma credencial temporária no momento do teste, conforme a regra de trabalho combinada.
- Idealmente, um conjunto de dados com pagamentos em pelo menos três situações: `"Pendente"` não
  vencido, `"Pendente"` vencido (efetivamente "Atrasado"), e `"Pago"`/`"Cancelado"` — para exercitar
  toda a tabela de comportamento de [research.md — R1](./research.md#r1--mapear-exatamente-o-comportamento-hoje-replicado-por-combinação-de-filtro).

## Cenário 1 — Dashboard sem filtro (User Story 1, Acceptance Scenario 1)

1. Autenticar e obter `accessToken`.
2. Chamar `GET /api/dashboard`.
3. Anotar o valor de `valorPendenteRecebimento`.
4. **Esperado**: igual à soma manual de todos os `Pagamento` com `Status == "Pendente"` no banco
   (visível via consulta direta, se necessário para conferência) — mesmo valor que a implementação
   anterior já retornava.

## Cenário 2 — Relatório de Pagamentos sem filtro (User Story 2, Acceptance Scenario 1)

1. Chamar `GET /api/relatorios/pagamentos` sem nenhum parâmetro de query.
2. **Esperado**: `totalPendenteConsolidado` retorna **o mesmo valor** de `valorPendenteRecebimento`
   do Cenário 1 (mesma fórmula, sem filtro).

## Cenário 3 — Filtro por aluno (Acceptance Scenario 2)

1. Chamar `GET /api/relatorios/pagamentos?alunoId=<id de um aluno com pagamentos pendentes>`.
2. **Esperado**: `totalPendenteConsolidado` soma só os pagamentos pendentes daquele aluno.

## Cenário 4 — Filtro por período de vencimento (Acceptance Scenario 3)

1. Chamar `GET /api/relatorios/pagamentos?inicio=<data>&fim=<data>` cobrindo um intervalo que
   inclua alguns vencimentos e exclua outros.
2. **Esperado**: `totalPendenteConsolidado` soma só os pagamentos pendentes com vencimento dentro
   do intervalo.

## Cenário 5 — Filtro de status incompatível (Acceptance Scenario 4 — comportamento preservado)

1. Chamar `GET /api/relatorios/pagamentos?status=Pago`.
2. **Esperado**: `totalPendenteConsolidado` retorna `0`, mesmo que existam pagamentos "Pago" no
   período — comportamento pré-existente e deliberadamente preservado (ver spec.md, Edge Cases).

## Cenário 6 — Filtro `status=Atrasado` isola só a fatia vencida

1. Chamar `GET /api/relatorios/pagamentos?status=Atrasado`.
2. **Esperado**: `totalPendenteConsolidado` é menor ou igual ao valor do Cenário 2 (só a fatia
   efetivamente vencida), e igual à soma manual dos pagamentos `"Pendente"` cuja `DataVencimento`
   já passou.

## Cenário 7 — Nenhum outro campo muda (SC-004)

1. Comparar, antes e depois da mudança, os demais campos de `GET /api/dashboard` e
   `GET /api/relatorios/pagamentos` (incluindo `Itens` do relatório) para o mesmo conjunto de
   dados e filtros.
2. **Esperado**: valores idênticos — só a fonte interna do total pendente muda, não o resultado.

## Validação automatizada

Não há teste de unidade pré-existente cobrindo este método diretamente (confirmado em
[research.md — R4](./research.md#r4--nenhum-teste-automatizado-cobre-este-cálculo-hoje)). A
validação é por: (a) `dotnet build`, (b) `dotnet test` completo (garantir ausência de regressão em
outros fluxos), e (c) os 7 cenários manuais acima.
