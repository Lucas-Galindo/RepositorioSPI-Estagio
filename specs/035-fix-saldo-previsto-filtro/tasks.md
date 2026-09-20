---

description: "Task list for feature 035 - fix-saldo-previsto-filtro"
---

# Tasks: Corrigir Filtro de Receita/Despesa Pendente no Relatório Financeiro

**Input**: Design documents from `/specs/035-fix-saldo-previsto-filtro/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: `ObterFinanceiroAsync` já tem cobertura existente (specs/029); esta feature adiciona
cobertura nova para o comportamento corrigido, seguindo o padrão de fakes manuais já usado no
projeto (research.md — R4).

**Organization**: Feature de escopo único (1 user story P1).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 para todas as tarefas de história de usuário
- Caminhos de arquivo exatos incluídos em cada descrição

## Path Conventions

Projeto único (backend .NET): `src/SPI.Domain/...`, `src/SPI.Infrastructure/...`,
`src/SPI.Application/...`, `tests/SPI.Application.Tests/...` na raiz do repositório.

---

## Phase 1: Setup (Shared Infrastructure)

Não aplicável — nenhuma dependência nova, nenhuma configuração de projeto necessária.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Estender a assinatura de `IRelatorioRepository.ObterReceitasPendentesSegregadasAsync`
e sua implementação — pré-requisito bloqueante, pois qualquer teste ou chamador precisa que a
interface já exista com os novos parâmetros, e os dois fakes de teste existentes precisam ser
atualizados para o projeto continuar compilando (research.md — R4).

**⚠️ CRITICAL**: Nenhuma tarefa de US1 pode começar antes desta fase estar completa.

- [X] T001 Em `src/SPI.Domain/Repositories/IRelatorioRepository.cs`, estender a assinatura de `ObterReceitasPendentesSegregadasAsync` com os parâmetros opcionais `int? turmaId = null, int? materiaId = null, int? alunoId = null`, posicionados antes dos parâmetros de nome já existentes (`turmaNome`, `materiaNome`, `alunoBusca`), mesmo estilo de `ObterValorFaturadoNoPeriodoAsync` (data-model.md — Assinatura alterada).
- [X] T002 Em `src/SPI.Infrastructure/Repositories/RelatorioRepository.cs`, implementar os novos parâmetros de `ObterReceitasPendentesSegregadasAsync` (linha ~119-132) aplicando `AplicarFiltroReceita(query, turmaId, materiaId, alunoId)` (helper já existente, linha ~310) antes de `AplicarFiltroReceitaPorNome`, na mesma ordem usada por `ObterValorFaturadoNoPeriodoAsync` (linhas 84-88) — depende de T001.
- [X] T003 [P] Em `tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPorTurmaTests.cs`, atualizar a assinatura do método `ObterReceitasPendentesSegregadasAsync` do `FakeRelatorioRepository` (linha ~271) para incluir os 3 novos parâmetros opcionais, mantendo o retorno atual (o teste não exercita este método) — depende de T001.
- [X] T004 [P] Em `tests/SPI.Application.Tests/Relatorios/RelatorioServiceLancamentosTests.cs`, atualizar a assinatura do método `ObterReceitasPendentesSegregadasAsync` do `FakeRelatorioRepository` (linha ~125) para incluir os 3 novos parâmetros opcionais, mantendo o retorno atual (o teste não exercita este método) — depende de T001.

**Checkpoint**: Projeto compila com a interface estendida; `ObterReceitasPendentesSegregadasAsync`
já aceita e aplica os filtros de id, mas `RelatorioService.ObterFinanceiroAsync` ainda não os
repassa.

---

## Phase 3: User Story 1 - Receita Pendente e Saldo Previsto refletem o filtro aplicado (Priority: P1) 🎯 MVP

**Goal**: `GET /api/relatorios/financeiro` passa a calcular `ReceitaPendente` (e, por consequência,
`SaldoPrevisto`) respeitando os filtros `alunoId`/`turmaId`/`materiaId` já recebidos, do mesmo
modo que `TotalRecebido`/`SaldoRealizado` já fazem. `DespesaPendente` permanece sempre global.

**Independent Test**: Filtrar o Relatório Financeiro (aba Visão Financeiro) por um aluno com
pagamentos pendentes, e confirmar que "Receita Pendente" corresponde à soma dos valores
pendentes apenas desse aluno, não ao total do negócio (spec.md — Acceptance Scenario 1).

### Tests for User Story 1 ⚠️

> **NOTE: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação (o
> comportamento atual ignora os filtros para Receita/Despesa Pendente).**

- [X] T005 [US1] Criar `tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPendenteTests.cs` com um `FakeRelatorioRepository` mínimo (seguindo o padrão manual já usado em `RelatorioServiceFinanceiroPorTurmaTests.cs`, sem biblioteca de mock) cujo `ObterReceitasPendentesSegregadasAsync` fake simula pendências de mais de um aluno/turma, e o teste do Acceptance Scenario 1: filtro `alunoId` aplicado → `ReceitaPendente` reflete apenas os pendentes desse aluno, não o total (depende de T001-T004). Neste mesmo teste, também assertar FR-005 (nenhum outro campo da resposta muda): `TotalRecebido`, `TotalPago`, `SaldoRealizado`, `PorFormaPagamento`, `PorAluno` e `PorTurma` devem corresponder exatamente ao que o fake retornaria sem a correção — confirmando que a mudança não afeta nenhum campo além de `ReceitaPendente`/`SaldoPrevisto`.
- [X] T006 [US1] Em `RelatorioServiceFinanceiroPendenteTests.cs`, adicionar o teste do Acceptance Scenario 2: filtro `turmaId` e/ou `materiaId` aplicado (sem `alunoId`) → `ReceitaPendente` reflete apenas os pendentes associados a essa turma/matéria (depende do fixture criado em T005).
- [X] T007 [US1] Em `RelatorioServiceFinanceiroPendenteTests.cs`, adicionar o teste do Acceptance Scenario 3: qualquer um dos filtros acima aplicado → `DespesaPendente` continua idêntico ao valor sem filtro (sempre global), confirmando FR-002 (depende do fixture criado em T005).
- [X] T008 [US1] Em `RelatorioServiceFinanceiroPendenteTests.cs`, adicionar o teste do Acceptance Scenario 4: `SaldoPrevisto` corresponde a `SaldoRealizado + (ReceitaPendente filtrada - DespesaPendente global)`, confirmando FR-003 (depende do fixture criado em T005).
- [X] T009 [US1] Em `RelatorioServiceFinanceiroPendenteTests.cs`, adicionar o teste do Acceptance Scenario 5: sem nenhum filtro de aluno/turma/matéria, `ReceitaPendente`/`DespesaPendente`/`SaldoPrevisto` permanecem idênticos ao comportamento anterior à correção, confirmando FR-004 (depende do fixture criado em T005).

### Implementation for User Story 1

- [X] T010 [US1] Em `src/SPI.Application/Relatorios/Services/RelatorioService.cs`, na chamada a `ObterReceitasPendentesSegregadasAsync` (linha ~124), repassar `turmaId, materiaId, alunoId` — os mesmos três parâmetros que `ObterFinanceiroAsync` já recebe e já usa na chamada a `ListarPagosNoPeriodoAsync` (linha 114) — sem alterar a chamada a `ObterDespesasPendentesSegregadasAsync` (linha ~125), que permanece sem argumentos (FR-001, FR-002).
- [X] T011 [US1] Rodar `dotnet test --filter RelatorioServiceFinanceiroPendenteTests` e confirmar que todos os testes de T005-T009 passam com a implementação de T001-T002 e T010 (quickstart.md — Validação automatizada).

**Checkpoint**: `GET /api/relatorios/financeiro` retorna `ReceitaPendente`/`SaldoPrevisto`
corretamente filtrados quando `alunoId`/`turmaId`/`materiaId` são informados, e inalterados sem
filtro — User Story 1 completa e testável de forma independente.

---

## Phase Final: Polish & Cross-Cutting Concerns

- [X] T012 Rodar a suíte de testes completa (`dotnet test`) para confirmar que nenhum outro consumidor de `ObterReceitasPendentesSegregadasAsync` foi afetado — em especial `FinanceiroService.ObterVisaoGeralAsync` (usa os filtros por nome já existentes, não os novos por id) e os testes já existentes que implementam `FakeRelatorioRepository` (quickstart.md — Regressão).
- [X] T013 Validação manual complementar contra dados reais, seguindo os 5 passos de quickstart.md — Validação manual (requer credencial temporária de teste conforme a regra de trabalho combinada; não criar usuário de teste persistente sem perguntar antes).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup**: Não aplicável.
- **Foundational (Phase 2)**: Sem dependências externas — pode começar imediatamente. BLOQUEIA toda a Phase 3.
- **User Story 1 (Phase 3)**: Depende de Phase 2 completa (T001-T004).
- **Polish (Phase Final)**: Depende de Phase 3 completa (T005-T011).

### Within Phase 2

- T001 bloqueia T002, T003 e T004 (todos dependem da interface já estendida).
- T002, T003, T004 são independentes entre si (arquivos diferentes) — `[P]` em T003/T004; T002 não é `[P]` porque é o passo mais crítico (implementação real do filtro) e convém revisá-lo isoladamente antes de mexer nos fakes.

### Within Phase 3 (US1)

- T005 cria o fixture compartilhado; T006-T009 dependem dele (mesmo arquivo, adições sequenciais).
- T010 pode ser escrito em paralelo com T005-T009 (arquivos diferentes), mas a ordem recomendada é TDD: testes antes da implementação.
- T011 depende de T005-T010 completos.

### Parallel Opportunities

- T003 e T004 em paralelo (Foundational, arquivos de teste diferentes).
- T005 (novo arquivo de teste) pode ser escrito em paralelo com T010 (arquivo de serviço), mas ambos dependem de T001-T002 completos primeiro.

---

## Parallel Example: Foundational

```bash
# Apos T001 (interface estendida):
Task: "Atualizar fake em RelatorioServiceFinanceiroPorTurmaTests.cs (T003)"
Task: "Atualizar fake em RelatorioServiceLancamentosTests.cs (T004)"
```

---

## Implementation Strategy

### MVP First (única história)

1. Completar Phase 2: Foundational (T001-T004) — interface estendida, projeto compilando.
2. T005-T009: escrever os testes (devem falhar contra o comportamento atual).
3. T010: implementar a correção em `RelatorioService.cs`.
4. T011: confirmar que os testes passam.
5. **STOP and VALIDATE**: User Story 1 completa e testável de forma independente.
6. T012-T013: Polish (regressão completa, validação manual opcional).

### Incremental Delivery

Feature de escopo único — não há incremento por história, apenas o fluxo Foundational → T005-T011
(US1) → Polish (T012-T013).

---

## Notes

- [P] = arquivos diferentes, sem dependência sequencial.
- Nenhuma tarefa introduz mudança de contrato de API, migração de banco ou nova dependência (ver plan.md).
- Verificar que os testes falham antes da implementação (T005-T009 antes de T010).
- Rodar `dotnet test` completo (T012) para confirmar ausência de regressão em `FinanceiroService`/Visão Geral.
