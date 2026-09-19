---

description: "Task list for feature 033 - fix-percentual-frequencia"
---

# Tasks: Corrigir Cálculo de "% Frequência do Aluno"

**Input**: Design documents from `/specs/033-fix-percentual-frequencia/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: research.md (R4) confirma que não há cobertura de teste existente para `ObterHistoricoAlunoAsync` e planeja adicionar a primeira, num arquivo novo (`RelatorioServiceHistoricoAlunoTests.cs`), seguindo o padrão de fakes manuais já usado no projeto. Tarefas de teste estão incluídas abaixo.

**Organization**: Feature de escopo único (1 método, 1 user story P1). Não há user story P2/P3.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 para todas as tarefas de história de usuário
- Caminhos de arquivo exatos incluídos em cada descrição

## Path Conventions

Projeto único (backend .NET): `src/SPI.Application/...`, `tests/SPI.Application.Tests/...` na raiz do repositório (ver plan.md — Project Structure).

---

## Phase 1: Setup (Shared Infrastructure)

Não aplicável — nenhuma infraestrutura nova, nenhuma dependência nova (ver plan.md — Primary Dependencies: "Nenhuma nova dependência"). Nenhuma tarefa de setup necessária.

---

## Phase 2: Foundational (Blocking Prerequisites)

Não aplicável — não há infraestrutura compartilhada bloqueante a criar antes da história de usuário; a única mudança é isolada ao método `RelatorioService.ObterHistoricoAlunoAsync` e ao helper de mapeamento extraído (ver research.md — R2), que já fazem parte da implementação da própria US1.

---

## Phase 3: User Story 1 - Percentual de frequência nunca ultrapassa 100% com filtro aplicado (Priority: P1) 🎯 MVP

**Goal**: `GET /api/relatorios/historico-aluno/{alunoId}` passa a calcular `PercentualFrequencia` a partir de presenças reais do aluno dentro do filtro aplicado (quando `inicio`, `fim`, `status` ou `turmaId` restringir a consulta), em vez do contador vitalício `Aluno.Frequencia`, eliminando percentuais acima de 100%. Sem nenhum filtro restritivo, mantém o comportamento atual.

**Independent Test**: Para um aluno com histórico de presenças anterior ao período filtrado, chamar `GET /api/relatorios/historico-aluno/{alunoId}` com um filtro de período mais restrito que o histórico total do aluno, e confirmar que o percentual retornado nunca excede 100% e corresponde exatamente às presenças reais dentro desse período (Acceptance Scenario 1 de spec.md).

### Tests for User Story 1 ⚠️

> **NOTE: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação (o comportamento atual usa `Aluno.Frequencia` como numerador em todos os casos).**

- [X] T001 [P] [US1] Criar `tests/SPI.Application.Tests/Relatorios/RelatorioServiceHistoricoAlunoTests.cs` com um `FakeAulaRepository`/`FakeAlunoRepository` mínimo (seguindo o padrão manual já usado em `RelatorioServiceLancamentosTests.cs`/`RelatorioServiceFinanceiroPorTurmaTests.cs`, sem biblioteca de mock) e o teste do Acceptance Scenario 1: aluno com `Aluno.Frequencia = 50`, mas apenas 8 de 10 aulas `Realizada` no filtro de período com `AulaAluno.Presente == true` → `PercentualFrequencia` deve ser `80.0m`, nunca `500.0m`. Neste mesmo teste, também assertar FR-005 (nenhum outro campo da resposta muda): `AlunoId`, `AlunoNome` e `Ra` devem corresponder aos dados do aluno do fixture, e a lista `Aulas` da resposta deve corresponder exatamente (mesma quantidade e mesmo mapeamento `Aula → RelatorioAgendaItem`) às aulas retornadas pelo fake `IAulaRepository`, confirmando que a correção não alterou o comportamento de mapeamento existente.
- [X] T002 [US1] Em `RelatorioServiceHistoricoAlunoTests.cs`, adicionar o teste do Acceptance Scenario 2 (filtro de `turmaId` sem período) — mesmo cenário de presenças/Realizadas do T001, mas disparado só por `turmaId`, esperando o mesmo recálculo por presenças reais (depende do fixture criado em T001).
- [X] T003 [US1] Em `RelatorioServiceHistoricoAlunoTests.cs`, adicionar o teste do Acceptance Scenario 2 (filtro de `status` sem período nem turma) — mesmo cenário, disparado só por `status`, esperando o mesmo recálculo por presenças reais (depende do fixture criado em T001).
- [X] T004 [US1] Em `RelatorioServiceHistoricoAlunoTests.cs`, adicionar o teste do Acceptance Scenario 3 (nenhum filtro de período/status/turma aplicado) — `PercentualFrequencia` deve continuar usando `Aluno.Frequencia` como numerador (comportamento inalterado), confirmando FR-002 (depende do fixture criado em T001).
- [X] T005 [US1] Em `RelatorioServiceHistoricoAlunoTests.cs`, adicionar o teste do Acceptance Scenario 4 (zero aulas `Realizada` no filtro, com e sem filtro restritivo aplicado) — `PercentualFrequencia` deve ser `0m`, sem `DivideByZeroException`, confirmando FR-004 (depende do fixture criado em T001).

### Implementation for User Story 1

- [X] T006 [US1] Em `src/SPI.Application/Relatorios/Services/RelatorioService.cs`, extrair o mapeamento inline `Aula → RelatorioAgendaItem` (hoje dentro de `ObterAgendaAsync`) para um método privado estático `MapearAgendaItem(Aula aula)`, reaproveitado por `ObterAgendaAsync` sem alterar seu comportamento externo (research.md — R2).
- [X] T007 [US1] Em `src/SPI.Application/Relatorios/Services/RelatorioService.cs`, alterar `ObterHistoricoAlunoAsync` (linhas ~49-71) para chamar `_aulaRepository.ListarAsync(status, turmaId, alunoId, inicio, fim, cancellationToken)` diretamente (em vez de `ObterAgendaAsync`), usando `MapearAgendaItem` (T006) para montar a lista `Aulas` da resposta a partir da mesma lista crua de `Aula` (research.md — R2; data-model.md).
- [X] T008 [US1] Em `ObterHistoricoAlunoAsync`, calcular `bool temFiltroRestritivo = inicio.HasValue || fim.HasValue || turmaId.HasValue || !string.IsNullOrWhiteSpace(status);` exatamente como definido em research.md — R3 (FR-001, FR-002; `alunoId` explicitamente excluído da condição).
- [X] T009 [US1] Em `ObterHistoricoAlunoAsync`, calcular o numerador conforme data-model.md — Regra de cálculo passo 4: se `temFiltroRestritivo`, contar aulas com `Status == "Realizada"` em que `AulaAluno.Presente == true` para o `alunoId` consultado; senão, usar `aluno.Frequencia` (FR-001, FR-002).
- [X] T010 [US1] Em `ObterHistoricoAlunoAsync`, manter `PercentualFrequencia = totalRealizadasNoFiltro == 0 ? 0m : Math.Round(numerador / (decimal)totalRealizadasNoFiltro * 100, 1)` — proteção de divisão por zero já existente, preservada (FR-003, FR-004; data-model.md passo 5).
- [X] T011 [US1] Rodar `dotnet test --filter RelatorioServiceHistoricoAlunoTests` e confirmar que todos os testes de T001-T005 passam com a implementação de T006-T010 (quickstart.md — Validação automatizada).

**Checkpoint**: Neste ponto, a User Story 1 está completa e testável de forma independente — todos os Acceptance Scenarios de spec.md cobertos por teste automatizado.

---

## Phase Final: Polish & Cross-Cutting Concerns

**Purpose**: Conformidade com Princípio V da constituição e validação de regressão.

- [X] T012 [P] Atualizar `specs/013-relatorio-historico-aluno/spec.md` (Acceptance Scenario 2, FR-002, Edge Cases, Assumptions) para refletir o novo cálculo por presenças reais quando há filtro restritivo, referenciando esta spec (033) como a correção aplicada — conforme Princípio V da constituição e plan.md — Constitution Check.
- [X] T013 Rodar a suíte de testes completa (`dotnet test`) para confirmar que nenhum outro fluxo foi afetado, em especial o Relatório de Agenda (Estória 12), que compartilha `MapearAgendaItem` (T006) via `ObterAgendaAsync` (quickstart.md — Regressão).
- [ ] T014 Validação manual complementar contra dados reais, seguindo os 5 passos de quickstart.md — Validação manual (requer credencial temporária de teste conforme a regra de trabalho combinada; não criar usuário de teste persistente sem perguntar antes).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup / Foundational**: Não aplicáveis nesta feature (ver Phase 1/2 acima) — a implementação começa direto na Phase 3.
- **User Story 1 (Phase 3)**: Sem dependência de fases anteriores.
- **Polish (Phase Final)**: Depende da Phase 3 completa (T001-T011).

### Within User Story 1

- T001 cria o fixture compartilhado; T002-T005 dependem dele (mesmo arquivo, adições sequenciais).
- T006 (extração do helper) deve vir antes de T007 (que usa o helper).
- T007 → T008 → T009 → T010: todas no mesmo método (`ObterHistoricoAlunoAsync`), edições sequenciais no mesmo arquivo.
- T011 depende de T001-T010 completos (roda os testes contra a implementação final).

### Parallel Opportunities

- T001 pode começar em paralelo com o início da leitura de T006-T010 (arquivos diferentes: teste vs. serviço), mas como o objetivo é TDD (testes falhando primeiro), a ordem recomendada é escrever T001-T005 antes de T006-T010.
- T012 (atualização de spec.md 013) pode rodar em paralelo com T013/T014, já que é um arquivo de documentação isolado.

---

## Parallel Example: User Story 1

```bash
# T001 (arquivo de teste) pode ser escrito enquanto T006 (extração do helper) é planejado,
# mas ambos tocam RelatorioService.cs indiretamente (T006-T010 são sequenciais entre si).
Task: "Criar RelatorioServiceHistoricoAlunoTests.cs com fixture e Acceptance Scenario 1 (T001)"
Task: "Extrair MapearAgendaItem em RelatorioService.cs (T006)"
```

---

## Implementation Strategy

### MVP First (única história)

1. T001-T005: escrever os testes (devem falhar contra o comportamento atual).
2. T006-T010: implementar a correção em `RelatorioService.cs`.
3. T011: confirmar que os testes passam.
4. **STOP and VALIDATE**: User Story 1 completa e testável de forma independente.
5. T012-T014: Polish (atualização retroativa de spec 013, regressão, validação manual opcional).

### Incremental Delivery

Feature de escopo único — não há incremento por história, apenas o fluxo T001 → T011 (US1) seguido de Polish (T012-T014).

---

## Notes

- [P] = arquivos diferentes, sem dependência sequencial.
- Nenhuma tarefa introduz mudança de contrato de API, migração de banco ou nova dependência (ver plan.md).
- Verificar que os testes falham antes da implementação (T001-T005 antes de T006-T010).
- Rodar `dotnet test` completo (T013) para confirmar ausência de regressão no Relatório de Agenda (Estória 12).
