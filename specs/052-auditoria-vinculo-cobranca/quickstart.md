# Quickstart: validar a auditoria de aulas dadas por Vínculo de Cobrança

Guia de validação ponta a ponta. Contrato em [contracts/vinculos-cobranca-api-delta.md](./contracts/vinculos-cobranca-api-delta.md); modelo de dados em [data-model.md](./data-model.md); decisões técnicas em [research.md](./research.md).

## Pré-requisitos

- Backend rodando, migração `database/18_auditoria_vinculo_cobranca.sql` aplicada.
- Login como Professor, acesso à API de Vínculos de Cobrança (`api/alunos/{alunoId}/vinculos-cobranca`) e de Aulas (`api/aulas`).

## 1. Total original do Pacote é persistido e nunca decrementado (US1, FR-001 a FR-003)

| # | Passo | Esperado |
|---|---|---|
| 1 | `POST` um Vínculo de Cobrança Pacote com `totalAulasPacote: 10`, `saldoAulas: 10` | 201; resposta tem `totalAulasPacote: 10`, `saldoAulas: 10`, `aulasDadas: 0` |
| 2 | Registrar presença confirmada em 3 aulas desse aluno, nesse contexto (turma) | Cada `RegistrarSessaoAsync` decrementa `saldoAulas` em 1 |
| 3 | `GET` o vínculo | `totalAulasPacote` continua `10` (inalterado); `saldoAulas: 7`; `aulasDadas: 3` |

## 2. Cada presença fica associada ao vínculo que afetou (US2, FR-004 a FR-006)

| # | Passo | Esperado |
|---|---|---|
| 1 | Aluno com um Vínculo de Cobrança ativo (qualquer modalidade) para o contexto de uma aula | — |
| 2 | Registrar a presença confirmada dessa aula | No banco, `aula_aluno.vinculo_cobranca_id` da linha correspondente é o Id desse vínculo |
| 3 | Aluno sem vínculo correspondente ao contexto, presença confirmada | `aula_aluno.vinculo_cobranca_id` permanece `NULL` |
| 4 | Mesmo aluno, presença não confirmada (falta) | `aula_aluno.vinculo_cobranca_id` permanece `NULL`, independente de haver vínculo |
| 5 | Esgotar um Vínculo de Cobrança Pacote (saldo chega a 0), cadastrar um novo Vínculo de Cobrança Pacote para o mesmo aluno/turma, registrar novas presenças | Presenças anteriores à troca continuam referenciando o vínculo antigo; as novas referenciam o vínculo novo — nunca misturadas |

## 3. Mensalidade: teto opcional e contagem por mês (US3, FR-008 a FR-010)

| # | Passo | Esperado |
|---|---|---|
| 1 | `POST` um Vínculo de Cobrança Mensalidade com `aulasIncluidas: 10` | 201; `aulasDadasNoMes: 0` |
| 2 | Registrar 4 presenças confirmadas nesse vínculo, no mês corrente | `GET` o vínculo → `aulasDadasNoMes: 4` |
| 3 | Avançar para o mês seguinte (ou simular a competência), registrar 1 nova presença | `aulasDadasNoMes` do novo mês é `1` (não `5` — reinicia sozinho) |
| 4 | `POST` um Vínculo de Cobrança Mensalidade sem `aulasIncluidas` (null), registrar presenças | `aulasDadasNoMes` segue contando normalmente; nenhum bloqueio em nenhum momento |

## 4. Bloqueio ao reduzir total/teto abaixo do já consumido (US4, FR-007/FR-010)

| # | Passo | Esperado |
|---|---|---|
| 1 | Vínculo Pacote com `totalAulasPacote: 10`, 6 aulas já dadas (`saldoAulas: 4`) | — |
| 2 | `PUT` editando `totalAulasPacote` para `5` | 409, mensagem citando que não pode ser menor que as 6 já dadas |
| 3 | `PUT` editando `totalAulasPacote` para `6`, sem alterar `saldoAulas` no mesmo request | 200, aceito; `saldoAulas` permanece `4` (campo independente — o sistema não recalcula automaticamente, ver spec.md Assumptions) |
| 4 | Vínculo Mensalidade com `aulasIncluidas: 10`, 7 aulas já dadas no mês corrente | — |
| 5 | `PUT` editando `aulasIncluidas` para `5` | 409, mensagem equivalente citando o mês corrente |
| 6 | Vínculo Mensalidade **sem** `aulasIncluidas` definido, 7 aulas já dadas no mês corrente | — |
| 7 | `PUT` definindo `aulasIncluidas` pela primeira vez como `5` | 409, mesma mensagem — a regra vale tanto para reduzir um teto existente quanto para definir um teto novo abaixo do já dado |

## 5. Não-regressão (FR-011/FR-012, SC-005)

- O job `MensalidadeDispatcherService` (specs/039) continua gerando a cobrança mensal pelo `Valor` cheio do vínculo, em valor e data idênticos a antes desta feature — `aulasIncluidas`/`aulasDadasNoMes` não alteram o valor cobrado.
- A cobrança por presença (specs/038) continua idêntica — mesmo valor, mesma categoria, mesmo comportamento de decremento de `saldoAulas` — só o campo novo de auditoria é acrescentado.
- `git diff --stat -- frontend` vazio (FR-013 — nenhuma tela tocada).
- `dotnet test "tests/SPI.Application.Tests"` 100% verde, incluindo as suítes já existentes de specs/037/038/039.
