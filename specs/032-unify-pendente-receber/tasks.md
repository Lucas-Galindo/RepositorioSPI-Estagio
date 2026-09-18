---

description: "Task list for 032-unify-pendente-receber"
---

# Tasks: Unificar Cálculo de "Valor Pendente a Receber"

**Input**: Design documents from `/specs/032-unify-pendente-receber/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required for user stories), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Não incluídos — a spec não pede TDD, e não há teste de unidade existente cobrindo `ObterValorPendenteAsync` (confirmado em [research.md — R4](./research.md#r4--nenhum-teste-automatizado-cobre-este-cálculo-hoje)). A validação é por compilação, pela suíte de testes completa (regressão) e por verificação manual guiada por `quickstart.md` (comparação de valores antes/depois, já que ambos os endpoints exigem dados reais e autenticação).

**Organization**: Tarefas agrupadas por user story (US1 = P1, US2 = P2), conforme [spec.md](./spec.md). A implementação central (método compartilhado) é pré-requisito comum às duas stories — fica em Foundational porque nenhuma delas pode ser verificada sem ela.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1 ou US2, conforme spec.md
- Caminhos de arquivo são relativos à raiz do repositório

## Path Conventions

Backend .NET já existente: `src/SPI.Domain`, `src/SPI.Infrastructure`, `src/SPI.Application` (ver [plan.md](./plan.md), Structure Decision). Nenhum arquivo em `SPI.Api` ou `frontend/` é tocado.

---

## Phase 1: Setup

**Purpose**: Nenhuma inicialização de projeto é necessária — nenhuma dependência nova, nenhuma ferramenta nova (ver [plan.md](./plan.md), Technical Context).

Nenhuma tarefa nesta fase. Prosseguir direto para Phase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Criar a implementação compartilhada e filtrável de "valor pendente a receber" — pré-requisito de ambas as user stories, já que nenhuma pode ser verificada (Dashboard sem mudar de valor; Relatório de Pagamentos com filtros reais) antes de o método existir com a nova assinatura.

**⚠️ CRITICAL**: Nenhuma user story pode ser verificada corretamente antes desta fase.

- [X] T001 Em `src/SPI.Domain/Repositories/IRelatorioRepository.cs`, estender a assinatura de `ObterValorPendenteAsync` (linha ~19) de `Task<decimal> ObterValorPendenteAsync(CancellationToken cancellationToken = default);` para aceitar quatro parâmetros opcionais adicionais, todos com valor padrão `null`: `int? alunoId = null, string? status = null, DateOnly? vencimentoInicio = null, DateOnly? vencimentoFim = null`. Atualizar o comentário acima do método para documentar os novos parâmetros e referenciar a tabela de comportamento por `status` (ver [research.md — R1](./research.md#r1--mapear-exatamente-o-comportamento-hoje-replicado-por-combinação-de-filtro)).
- [X] T002 Em `src/SPI.Infrastructure/Repositories/RelatorioRepository.cs`, reimplementar `ObterValorPendenteAsync` (linhas ~27-37) para aplicar, numa única consulta EF Core (sem materializar entidades completas antes de somar): (a) filtro por `AlunoId` quando `alunoId` informado; (b) filtro por `DataVencimento >= vencimentoInicio`/`<= vencimentoFim` quando informados; (c) lógica de `status` reproduzindo exatamente a tabela de [research.md — R1](./research.md#r1--mapear-exatamente-o-comportamento-hoje-replicado-por-combinação-de-filtro): `status == null` ou `status == "Pendente"` → todos os `Pagamento` com `Status == "Pendente"`; `status == "Atrasado"` → apenas os `Status == "Pendente"` com `DataVencimento < DateOnly.FromDateTime(DateTime.UtcNow)` (mesmo corte usado em `PagamentoService.Mapear`, [PagamentoService.cs:200](../../src/SPI.Application/Pagamentos/Services/PagamentoService.cs#L200)); qualquer outro valor de `status` → resultado `0`. Depende de T001.
- [X] T003 Compilar o backend (`dotnet build`) e confirmar que a chamada existente em `DashboardService.cs:30` (`ObterValorPendenteAsync(cancellationToken)`, sem argumentos) continua compilando sem nenhuma alteração no arquivo — depende de T001, T002.

**Checkpoint**: A implementação compartilhada existe e compila; nenhum chamador foi migrado para os novos filtros ainda (User Story 2), mas o comportamento sem filtro (User Story 1) já está pronto para validação.

---

## Phase 3: User Story 1 - Dashboard continua correto após a unificação (Priority: P1) 🎯 MVP

**Goal**: `GET /api/dashboard` continua retornando exatamente o mesmo `valorPendenteRecebimento` de antes da unificação, sem nenhuma mudança de código no `DashboardService`.

**Independent Test**: Chamar `GET /api/dashboard` antes e depois da mudança, para o mesmo conjunto de dados, e confirmar que `valorPendenteRecebimento` é idêntico — Cenário 1 do [quickstart.md](./quickstart.md).

### Implementation for User Story 1

- [X] T004 [US1] Não há alteração de código nesta story — `DashboardService.cs` já chama `ObterValorPendenteAsync(cancellationToken)` sem argumentos, que continua válido e com o mesmo comportamento após T001-T002 (todos os parâmetros novos são opcionais e, sem eles, a query se reduz exatamente ao `WHERE Status == "Pendente"` de hoje). Esta tarefa é a verificação: rodar o Cenário 1 do [quickstart.md](./quickstart.md) contra o backend com T001-T003 aplicados, e confirmar que `valorPendenteRecebimento` bate com o valor obtido antes da mudança (ou com a soma manual de conferência descrita no cenário). Depende de T003.

**Checkpoint**: Dashboard verificado como inalterado — MVP entregue (a duplicação já não afeta o único consumidor ativo, mesmo antes de o Relatório de Pagamentos ser migrado).

---

## Phase 4: User Story 2 - Relatório de Pagamentos usa a implementação compartilhada (Priority: P2)

**Goal**: `GET /api/relatorios/pagamentos` calcula `totalPendenteConsolidado` chamando a mesma implementação central, preservando exatamente os valores que já retorna hoje para cada combinação de filtros (`alunoId`, `status`, período de vencimento).

**Independent Test**: Chamar `GET /api/relatorios/pagamentos` com cada combinação de filtros já aceita hoje, antes e depois da mudança, e confirmar que `totalPendenteConsolidado` é idêntico em cada caso — Cenários 2-6 do [quickstart.md](./quickstart.md).

### Implementation for User Story 2

- [X] T005 [US2] Em `src/SPI.Application/Relatorios/Services/RelatorioService.cs`, no método `ObterPagamentosAsync` (linhas ~73-96), substituir a linha `var totalPendente = itens.Where(i => i.Status is "Pendente" or "Atrasado").Sum(i => i.Valor);` (linha ~89) por uma chamada a `_relatorioRepository.ObterValorPendenteAsync(cancellationToken, alunoId, status, inicio, fim)`, usando os mesmos parâmetros `alunoId`, `status`, `inicio`, `fim` já recebidos pela assinatura do método. Não alterar a construção de `itens` (continua vindo de `_pagamentoService.ListarAsync(...)`, usada para a lista individual da resposta, propósito diferente do total agregado). Depende de T001, T002.
- [X] T006 [US2] Compilar o backend (`dotnet build`) e confirmar que T005 não introduz nenhum erro de compilação — depende de T005.
- [X] T007 [US2] Executar manualmente os Cenários 2-6 do [quickstart.md](./quickstart.md) (sem filtro, `alunoId`, período de vencimento, `status` incompatível zerando o total, `status=Atrasado` isolando a fatia vencida) contra o backend rodando localmente, comparando com os valores que a implementação anterior retornava para os mesmos dados — depende de T006.

**Checkpoint**: As duas user stories completas — existe uma única implementação de "valor pendente a receber", usada pelos dois endpoints, com paridade de comportamento comprovada em todas as combinações de filtro já suportadas.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Confirmar ausência de regressão em qualquer outro fluxo do sistema e fechar a validação end-to-end.

- [X] T008 Rodar a suíte de testes completa (`dotnet test` a partir da raiz do repositório) e confirmar que todos os testes existentes continuam passando sem alteração — cobre SC-004 da spec (nenhum outro campo/fluxo afetado) — depende de T003, T006.
- [X] T009 Executar manualmente o Cenário 7 do [quickstart.md](./quickstart.md) (comparar todos os demais campos de ambos os endpoints antes/depois, incluindo `Itens` do Relatório de Pagamentos), solicitando uma credencial temporária no momento do teste (não reutilizar nem persistir credenciais — regra de trabalho combinada), antes de considerar a feature concluída — depende de T004, T007.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Vazia.
- **Foundational (Phase 2)**: Vazia como pré-requisito próprio, mas T001-T003 BLOQUEIAM as duas user stories — é a implementação central que ambas verificam.
- **User Story 1 (Phase 3)**: Depende de Foundational (T003). Não depende de US2. Sem código próprio a alterar — só verificação.
- **User Story 2 (Phase 4)**: Depende de Foundational (T001, T002). Não depende de US1 (pode ser feita em paralelo com a verificação de US1, já que ambas partem do mesmo Foundational).
- **Polish (Phase 5)**: T008 depende de Foundational + US2 aplicada (T003, T006). T009 depende de US1 e US2 verificadas (T004, T007).

### User Story Dependencies

- **User Story 1 (P1)**: Entrega o valor principal sozinha — o único consumidor ativo hoje (Dashboard) já está usando a implementação unificada e comprovadamente sem regressão, mesmo que o Relatório de Pagamentos ainda não tenha sido migrado (User Story 2).
- **User Story 2 (P2)**: Completa a unificação eliminando a segunda implementação (o objetivo central da spec, SC-003) — sem ela, a duplicação de código ainda existiria do lado do Relatório de Pagamentos, mesmo com o Dashboard já usando a versão compartilhada.

### Within Each User Story

- T001 antes de T002 (assinatura antes da implementação).
- T002 antes de T003 (implementação antes da verificação de build).
- T005 depende de T001-T002 (a chamada só existe depois que o método compartilhado aceita os filtros).

### Parallel Opportunities

- T004 (verificação de US1) e T005-T007 (implementação de US2) podem ser feitas em paralelo depois que Foundational (T001-T003) estiver pronta — arquivos diferentes, sem dependência entre si.

---

## Parallel Example: Após Foundational

```bash
# Podem ser feitas em paralelo (nenhuma depende da outra, ambas dependem só de T001-T003):
Task: "Verificar Dashboard inalterado (Cenario 1 do quickstart.md) -- User Story 1"
Task: "Migrar RelatorioService.ObterPagamentosAsync para a implementacao compartilhada -- User Story 2 (T005)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 2 (Foundational — T001-T003): a implementação compartilhada existe e o Dashboard já a usa sem saber (mesma chamada, mesmo resultado).
2. Completar Phase 3 (User Story 1 — T004): confirmar que nada mudou para o único consumidor ativo.
3. **PARAR e VALIDAR**: Cenário 1 do quickstart.md.
4. Diferente de features anteriores desta sessão (029/031), aqui não há nenhum estado intermediário arriscado — parar em US1 deixa o sistema num estado estritamente melhor (uma implementação a menos duplicada já em uso pelo consumidor real), sem nenhuma pendência de contrato externo.

### Incremental Delivery

1. Foundational (T001-T003) → implementação compartilhada pronta, Dashboard já a usa.
2. User Story 1 (T004) → confirma zero regressão no único consumidor ativo → MVP pronto.
3. User Story 2 (T005-T007) → migra o Relatório de Pagamentos, completando a eliminação da duplicação (SC-003) → pode ser feito em paralelo com o passo 2.
4. Polish (T008-T009) → suíte completa + validação manual final de todos os campos.

## Notes

- [P] = arquivos diferentes ou tarefas sem dependência entre si.
- [US1]/[US2] mapeiam cada tarefa à user story correspondente da spec, para rastreabilidade.
- T004 é uma tarefa de verificação pura (sem código) — reflete que User Story 1 não exige nenhuma
  mudança no `DashboardService`, apenas a confirmação de que a implementação compartilhada
  preserva o comportamento que ele já depende.
- Nenhuma tarefa desta lista requer migração de banco, nova dependência, ou teste automatizado
  novo (ver [research.md](./research.md) e [data-model.md](./data-model.md)).
- T009 segue a regra de trabalho combinada sobre credenciais de teste: pedir uma credencial
  temporária no momento do teste, nunca criar/persistir uma nova, nunca gravá-la em arquivo.
