---

description: "Task list for feature 036 - remove-dead-financial-code"
---

# Tasks: Remover Código Morto do Módulo Financeiro (Dashboard e ObterValorAPagarAsync)

**Input**: Design documents from `/specs/036-remove-dead-financial-code/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Nenhum teste dedicado cobre os métodos removidos hoje (research.md — R4). Esta feature
não adiciona teste novo — apenas remove 3 stubs de teste que existiam só para satisfazer a
interface (US2), e valida via a suíte completa continuando a passar (Polish).

**Organization**: Duas user stories independentes (US1 = P1, remoção do item 1; US2 = P2,
remoção do item 2) — arquivos completamente distintos, sem dependência entre si.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 ou US2
- Caminhos de arquivo exatos incluídos em cada descrição

## Path Conventions

Projeto único (backend .NET + frontend Next.js): `src/SPI.Domain/...`,
`src/SPI.Infrastructure/...`, `src/SPI.Application/...`, `frontend/lib/api/...`,
`tests/SPI.Application.Tests/...` na raiz do repositório.

---

## Phase 1: Setup (Shared Infrastructure)

Não aplicável — nenhuma dependência nova, nenhuma configuração de projeto necessária.

---

## Phase 2: Foundational (Blocking Prerequisites)

Não aplicável — US1 e US2 tocam arquivos completamente distintos, sem nenhum pré-requisito
bloqueante compartilhado entre elas (plan.md — Scale/Scope).

---

## Phase 3: User Story 1 - Remover cálculo de indicadores/fluxo de caixa não usados no Dashboard (Priority: P1) 🎯 MVP

**Goal**: `DashboardService` para de calcular `Indicadores`/`FluxoCaixaMensal` (e `GET
/api/dashboard` para de retornar esses campos), sem afetar nenhum campo que a Home de fato usa,
e sem afetar `RelatorioService`/aba Indicadores do Relatório Financeiro (implementação separada).

**Independent Test**: Chamar `GET /api/dashboard` antes e depois da mudança e confirmar que os
campos que a tela usa (`alunosAtendidosNoPeriodo`, `valorFaturadoNoPeriodo`, etc.) continuam
idênticos, que `indicadores`/`fluxoCaixaMensal` não aparecem mais, e que a aba Indicadores do
Relatório Financeiro continua funcionando (spec.md — Independent Test da US1).

### Implementation for User Story 1

- [X] T001 [US1] Em `src/SPI.Application/Dashboard/Services/DashboardService.cs`, remover os métodos privados `ObterIndicadoresFinanceirosAsync` (linhas 62-94) e `ObterFluxoCaixaMensalAsync` (linhas 96-119), a constante `MesesFluxoCaixa` (linha 9, órfã após a remoção — research.md R2) e as chamadas a ambos em `ObterAsync` (linhas 35-36, incluindo a atribuição de `Indicadores`/`FluxoCaixaMensal` no `return new DashboardResponse { ... }`).
- [X] T002 [US1] Em `src/SPI.Application/Dashboard/Dtos/DashboardResponse.cs`, remover as propriedades `Indicadores` (tipo `IndicadoresFinanceirosResponse`) e `FluxoCaixaMensal` (tipo `List<FluxoCaixaMensalItem>`) — MUST NOT remover as classes `IndicadoresFinanceirosResponse`/`FluxoCaixaMensalItem`/`GargaloCaixaResponse` em si (FR-003; data-model.md — Explicitamente preservado), apenas as duas propriedades que as referenciam em `DashboardResponse`.
- [X] T003 [US1] Em `frontend/lib/api/dashboard.ts`, remover os campos `indicadores` e `fluxoCaixaMensal` da interface `Dashboard` (linhas 59-60) — MUST NOT remover as interfaces `IndicadoresFinanceiros`/`FluxoCaixaMensalItem`/`GargaloCaixa` (linhas 4-26), reaproveitadas por `frontend/lib/api/relatorios.ts` (FR-003).
- [X] T004 [US1] Confirmar, por `dotnet build` e por busca em `src/SPI.Application/Relatorios/Services/RelatorioService.cs`, que `RelatorioService.ObterIndicadoresFinanceirosAsync` continua compilando e chamando normalmente `ObterInadimplenciaNoPeriodoAsync`, `ObterPagamentosComAtrasoNoPeriodoAsync`, `ObterValorPagoNoPeriodoAsync`, `ObterEntradasPorDiaDoMesAsync`, `ObterSaidasPorDiaDoMesAsync` sem nenhuma alteração — confirma FR-003 (nenhum método de repositório compartilhado foi tocado por T001-T003).
- [X] T005 [US1] Chamar `GET /api/dashboard` antes e depois de T001-T003 (ou comparar com a resposta documentada em quickstart.md) e confirmar que `totalAulasAgendadasNoPeriodo`, `totalAlunosAtivos`, `alunosAtendidosNoPeriodo`, `totalTurmasAtivas`, `valorPendenteRecebimento`, `valorFaturadoNoPeriodo`, `proximasAulasHoje`, `lembretesPendentes` retornam os mesmos valores de antes, e que `indicadores`/`fluxoCaixaMensal` não aparecem mais na resposta — confirma FR-004 e SC-001.

**Checkpoint**: `GET /api/dashboard` não calcula mais `Indicadores`/`FluxoCaixaMensal`; Home e aba
Indicadores do Relatório Financeiro continuam funcionando normalmente — User Story 1 completa e
testável de forma independente.

---

## Phase 4: User Story 2 - Remover método de repositório nunca chamado (Priority: P2)

**Goal**: `ObterValorAPagarAsync` deixa de existir em `IRelatorioRepository`/`RelatorioRepository`,
e os 3 stubs de teste que só existiam para satisfazer a interface são removidos junto.

**Independent Test**: Confirmar, após a remoção, que o projeto compila e que a suíte de testes
completa passa sem nenhuma referência remanescente a `ObterValorAPagarAsync` (spec.md —
Independent Test da US2).

### Implementation for User Story 2

- [X] T006 [US2] Em `src/SPI.Domain/Repositories/IRelatorioRepository.cs`, remover a declaração de `ObterValorAPagarAsync` (linha 47).
- [X] T007 [US2] Em `src/SPI.Infrastructure/Repositories/RelatorioRepository.cs`, remover a implementação de `ObterValorAPagarAsync` (linhas 96-107) — depende de T006 (interface já sem o método).
- [X] T008 [P] [US2] Em `tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPorTurmaTests.cs`, remover o stub `ObterValorAPagarAsync` do `FakeRelatorioRepository` — depende de T006.
- [X] T009 [P] [US2] Em `tests/SPI.Application.Tests/Relatorios/RelatorioServiceLancamentosTests.cs`, remover o stub `ObterValorAPagarAsync` do `FakeRelatorioRepository` — depende de T006.
- [X] T010 [P] [US2] Em `tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPendenteTests.cs`, remover o stub `ObterValorAPagarAsync` do `FakeRelatorioRepository` — depende de T006.

**Checkpoint**: Nenhuma referência a `ObterValorAPagarAsync` permanece no código-fonte; projeto
compila — User Story 2 completa e testável de forma independente.

---

## Phase Final: Polish & Cross-Cutting Concerns

- [X] T011 Rodar `dotnet build` (solução completa) e confirmar compilação sem erros após T001-T010 (quickstart.md — passo 1).
- [X] T012 Rodar `dotnet test` (suíte completa) e confirmar 100% de aprovação, sem nenhuma falha nova — confirma FR-006/SC-004 (quickstart.md — passo 2).
- [X] T013 Buscar em todo o código-fonte (`src/`, `tests/`, `frontend/`) por `ObterIndicadoresFinanceirosAsync` (só deve aparecer em `RelatorioService.cs`/`IRelatorioService.cs`, nunca mais em `DashboardService.cs`), `ObterFluxoCaixaMensalAsync` e `ObterValorAPagarAsync` (nenhuma ocorrência deve restar) — confirma SC-005 (quickstart.md — passo 3).
- [X] T014 Validação manual complementar contra dados reais, seguindo os passos 4-7 de quickstart.md — Validação manual (requer credencial temporária de teste conforme a regra de trabalho combinada; não criar usuário de teste persistente sem perguntar antes).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup / Foundational**: Não aplicáveis (ver Phase 1/2 acima).
- **User Story 1 (Phase 3)** e **User Story 2 (Phase 4)**: Totalmente independentes entre si — podem ser feitas em qualquer ordem, ou em paralelo.
- **Polish (Phase Final)**: Depende de ambas as user stories completas (T001-T010), já que a suíte completa (T012) e a busca por referências remanescentes (T013) cobrem os dois itens juntos.

### Within User Story 1

- T001 → T002 (a propriedade de `DashboardResponse` só deve ser removida depois que nada mais a popula, embora a ordem inversa também compile — mantém a sequência lógica de "parar de calcular" antes de "parar de expor").
- T003 é independente de T001/T002 (arquivo frontend, diferente) — pode rodar em paralelo.
- T004 e T005 dependem de T001-T003 completos (validam o resultado final).

### Within User Story 2

- T006 bloqueia T007, T008, T009, T010 (todos dependem da interface já sem o método).
- T007, T008, T009, T010 são independentes entre si (arquivos diferentes) — T008/T009/T010 marcados `[P]`; T007 não é `[P]` porque é a implementação real (convém revisá-la isoladamente antes dos stubs de teste).

### Parallel Opportunities

- T003 (US1, frontend) pode rodar em paralelo com T001/T002 (US1, backend).
- T008, T009 e T010 (US2) em paralelo entre si, após T006.
- US1 e US2 inteiras podem ser feitas em paralelo, já que não compartilham nenhum arquivo.

---

## Parallel Example: User Story 2

```bash
# Apos T006 (interface sem o metodo):
Task: "Remover implementacao em RelatorioRepository.cs (T007)"
Task: "Remover stub em RelatorioServiceFinanceiroPorTurmaTests.cs (T008)"
Task: "Remover stub em RelatorioServiceLancamentosTests.cs (T009)"
Task: "Remover stub em RelatorioServiceFinanceiroPendenteTests.cs (T010)"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 3: User Story 1 (T001-T005) — já entrega o item de maior impacto (menos
   consultas ao banco por carregamento do Dashboard).
2. **STOP and VALIDATE**: `GET /api/dashboard` e a Home continuam corretos.
3. Completar Phase 4: User Story 2 (T006-T010) — item de menor impacto, mas ainda assim código
   morto a remover.
4. Phase Final: Polish (T011-T014) — validação conjunta das duas remoções.

### Incremental Delivery

1. User Story 1 → testar independentemente → entrega o item de maior impacto.
2. User Story 2 → testar independentemente → entrega o item restante.
3. Polish → validação final conjunta (build + suíte completa + busca por referências).

---

## Notes

- [P] = arquivos diferentes, sem dependência sequencial.
- Nenhuma tarefa introduz dependência nova, migração de banco ou mudança de contrato de API além
  da remoção de 2 campos já confirmados sem consumidor (ver plan.md).
- T004 e T013 existem especificamente para cobrir FR-003 (o que MUST NOT ser removido) com uma
  verificação explícita, não deixada apenas para uma revisão posterior.
- T005 existe especificamente para cobrir FR-004 (nenhum outro campo de `GET /api/dashboard`
  muda) com uma verificação explícita.
