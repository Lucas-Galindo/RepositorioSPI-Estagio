# Quickstart: Validar a correção do agrupamento "Por Turma"

## Pré-requisitos

- Backend rodando localmente (`dotnet run` em `src/SPI.Api`) com acesso a um banco MySQL com o
  schema de `database/*.sql` aplicado (ver [data-model.md](./data-model.md) para as entidades
  envolvidas — nenhuma migração nova é necessária para esta feature).
- Login válido de Professor/Admin (a rota de Relatório Financeiro exige autenticação — ver
  [007-autenticar-usuario](../007-autenticar-usuario/spec.md)).

## Cenário 1 — Aluno em duas turmas, pagamentos de aulas de turmas diferentes (User Story 1)

1. Cadastrar (ou reaproveitar) um Aluno vinculado a duas Turmas, "Turma A" e "Turma B".
2. Registrar uma Aula da Turma A e uma Aula da Turma B, ambas com esse aluno presente, e
   registrar a sessão de cada uma como "Realizada" (gera cobrança automática — ver
   [008-registrar-sessao-aula](../008-registrar-sessao-aula/spec.md)) — ou registrar
   manualmente dois `Pagamento`s, cada um vinculado a uma dessas aulas via `AulaIds`.
3. Marcar os dois pagamentos como "Pago" (`PUT /api/pagamentos/{id}/status`, com `DataPagamento`
   dentro do período que será consultado).
4. Chamar `GET /api/relatorios/financeiro?inicio=...&fim=...` cobrindo a data de pagamento de
   ambos.
5. **Esperado**: em `PorTurma`, existe um item com `Chave == "Turma A"` somando exatamente o
   valor do primeiro pagamento, e outro com `Chave == "Turma B"` somando exatamente o valor do
   segundo — nenhum dos dois soma o valor do outro (comportamento anterior ao bug: os dois
   apareciam somados só em uma das turmas).

## Cenário 2 — Pagamento cobrindo aulas de turmas diferentes (User Story 2)

1. Registrar manualmente um único `Pagamento` (`POST /api/pagamentos`) informando em `AulaIds`
   uma aula da Turma A e uma aula da Turma B (mesmo aluno ou não — a regra não depende disso).
2. Marcar esse pagamento como "Pago", dentro do período a ser consultado.
3. Chamar `GET /api/relatorios/financeiro?inicio=...&fim=...`.
4. **Esperado**: em `PorTurma`, existe um único item com `Chave == "Múltiplas turmas"` cujo
   `Total` é exatamente o `ValorFinal` desse pagamento — ele não aparece somado em "Turma A" nem
   em "Turma B".

## Cenário 3 — Consistência da soma (SC-003)

1. Com os dados dos Cenários 1 e 2 já cadastrados (ou qualquer conjunto de pagamentos "Pago" no
   período), chamar `GET /api/relatorios/financeiro?inicio=...&fim=...`.
2. Somar manualmente todos os `Total` da lista `PorTurma` retornada.
3. **Esperado**: a soma é exatamente igual ao campo `TotalRecebido` da mesma resposta.

## Cenário 4 — Regressão (não deve mudar)

1. Com os mesmos dados, comparar `PorFormaPagamento` e `PorAluno` da resposta antes e depois da
   correção (mesmo período/filtros).
2. **Esperado**: nenhum dos dois grupos muda — apenas `PorTurma` é afetado por esta correção
   (FR-006 da spec).

## Validação automatizada

Testes de unidade em `tests/SPI.Application.Tests/Relatorios/` (a criar, seguindo o padrão de
`RelatorioServiceLancamentosTests.cs` — fake manual de `IRelatorioRepository`, sem biblioteca de
mock) devem cobrir os quatro cenários acima antes de considerar a implementação concluída. Ver
[research.md — R4](./research.md#r4--impacto-em-testes-existentes).
