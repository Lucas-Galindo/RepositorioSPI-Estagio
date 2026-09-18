---

description: "Task list for 030-remove-indice-cobertura-custos"
---

# Tasks: Remover Índice de Cobertura de Custos Fixos do Backend

**Input**: Design documents from `/specs/030-remove-indice-cobertura-custos/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required for user stories), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Não incluídos — a spec não pede TDD, não há teste existente que referencie o campo (confirmado em [research.md — R2](./research.md#r2--nenhum-teste-automatizado-cobre-o-campo-hoje)), e a natureza da mudança (remover uma propriedade de um DTO fortemente tipado em C#) já é validada pelo próprio compilador: qualquer uso residual do campo quebra o build. A validação funcional é feita via `quickstart.md` (chamadas HTTP manuais).

**Organization**: Tarefas agrupadas por user story (US1 = P1, US2 = P2), conforme [spec.md](./spec.md). US1 remove o campo do contrato de resposta (visível externamente); US2 remove o cálculo interno que só existia para preenchê-lo — são sequenciais porque US2 depende do código deixado por US1, mas cada uma é independentemente verificável (ver Checkpoints).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1 ou US2, conforme spec.md
- Caminhos de arquivo são relativos à raiz do repositório

## Path Conventions

Projeto web já existente (Option 2): backend .NET em `src/SPI.Application`, frontend em `frontend/lib/api/` (ver [plan.md](./plan.md), Structure Decision). Nenhum arquivo em `SPI.Domain`, `SPI.Infrastructure` ou `SPI.Api` é tocado.

---

## Phase 1: Setup

**Purpose**: Nenhuma inicialização de projeto é necessária — nenhuma dependência nova, nenhuma ferramenta nova (ver [plan.md](./plan.md), Technical Context).

Nenhuma tarefa nesta fase. Prosseguir direto para Phase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Não há pré-requisito bloqueante compartilhado entre as duas user stories além do próprio código já existente — a mudança não depende de nenhum dado, schema ou infraestrutura nova (ver [data-model.md](./data-model.md): sem entidade, sem coluna de banco).

Nenhuma tarefa nesta fase. Prosseguir direto para Phase 3.

---

## Phase 3: User Story 1 - Contrato de API sem campos mortos (Priority: P1) 🎯 MVP

**Goal**: As respostas de `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros` deixam de incluir o campo `indiceCoberturaCustosFixos`, em qualquer combinação de filtros — o contrato de API passa a refletir só o que é realmente calculado com propósito.

**Independent Test**: Chamar os dois endpoints (autenticado) e confirmar, no JSON de resposta, a ausência da chave `indiceCoberturaCustosFixos` dentro de `indicadores`, e que os demais campos de indicadores permanecem inalterados — Cenários 1-3 do [quickstart.md](./quickstart.md).

### Implementation for User Story 1

- [X] T001 [US1] Em `src/SPI.Application/Dashboard/Dtos/IndicadoresFinanceirosResponse.cs`, remover a propriedade `public decimal? IndiceCoberturaCustosFixos { get; set; }` (linha ~31) e seu comentário XML associado, do `record IndicadoresFinanceirosResponse`. Mantém todas as demais propriedades (`TaxaInadimplenciaPercentual`, `PrazoMedioAtrasoDias`, `MargemSegurancaPercentual`, `FluxoCaixaOperacional`, `GargaloCaixa`) inalteradas.
- [X] T002 [US1] Em `src/SPI.Application/Dashboard/Services/DashboardService.cs`, no método `ObterIndicadoresFinanceirosAsync`, remover a linha `IndiceCoberturaCustosFixos = indiceCobertura,` (linha ~89) da inicialização de `IndicadoresFinanceirosResponse` — depende de T001 (senão não compila, já que a propriedade deixou de existir). A linha `decimal? indiceCobertura = ...` (linha ~76) permanece por enquanto (removida em T006, User Story 2) — vai gerar um aviso de variável não utilizada (CS0219), esperado e temporário neste checkpoint.
- [X] T003 [US1] Em `src/SPI.Application/Relatorios/Services/RelatorioService.cs`, no método `ObterIndicadoresFinanceirosAsync`, remover a linha `IndiceCoberturaCustosFixos = indiceCobertura,` (linha ~221) da inicialização de `IndicadoresFinanceirosResponse` — mesma regra de T002 (a linha de cálculo em ~207 permanece até T007). Depende de T001.
- [X] T004 [P] [US1] Em `frontend/lib/api/dashboard.ts`, remover o campo `indiceCoberturaCustosFixos: number | null;` (linha ~17) da interface `IndicadoresFinanceiros`. Não requer nenhuma outra edição em `frontend/lib/api/relatorios.ts` (que reaproveita o mesmo tipo sem declaração própria — ver [data-model.md](./data-model.md)).
- [X] T005 [US1] Compilar o backend (`dotnet build`) e confirmar que T001-T003 não introduzem nenhum erro de compilação (apenas o aviso CS0219 esperado em dois pontos) — depende de T001, T002, T003.

**Checkpoint**: Neste ponto, os dois endpoints já não retornam mais o campo removido (User Story 1 completa e verificável via Cenários 1-3 do quickstart), mas o backend ainda calcula `indiceCobertura` sem usá-lo em nenhum lugar (limpeza pendente, User Story 2).

---

## Phase 4: User Story 2 - Nenhum trabalho de cálculo desperdiçado (Priority: P2)

**Goal**: O backend para de executar o cálculo `valorFaturado / valorPago` que só existia para preencher o campo já removido — nenhuma variável órfã, nenhum aviso de compilação remanescente.

**Independent Test**: Inspecionar os dois métodos (`DashboardService.ObterIndicadoresFinanceirosAsync`, `RelatorioService.ObterIndicadoresFinanceirosAsync`) e confirmar que nenhum deles mais declara ou calcula uma divisão de valor recebido por valor pago com esse propósito; `dotnet build` não emite mais o aviso CS0219 introduzido no checkpoint da User Story 1.

### Implementation for User Story 2

- [X] T006 [US2] Em `src/SPI.Application/Dashboard/Services/DashboardService.cs`, remover a linha `decimal? indiceCobertura = valorPago == 0 ? null : Math.Round(valorFaturado / valorPago, 2);` (linha ~76, imediatamente após o cálculo de `margemSeguranca`) — depende de T002 (a variável já não é usada em nenhum lugar desde então).
- [X] T007 [US2] Em `src/SPI.Application/Relatorios/Services/RelatorioService.cs`, remover a mesma linha equivalente (linha ~207) — depende de T003.
- [X] T008 [US2] Compilar o backend (`dotnet build`) e confirmar que os avisos CS0219 introduzidos no checkpoint da User Story 1 desapareceram, e que nenhum novo erro/aviso foi introduzido — depende de T006, T007.

**Checkpoint**: As duas user stories completas — nenhum código relacionado ao índice de cobertura de custos permanece calculando ou expondo nada.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Atualizar a documentação retroativa afetada (Princípio V da constituição) e confirmar ausência total de referências residuais e de regressão nos demais indicadores.

- [X] T009 [P] Atualizar `specs/015-relatorio-financeiro/spec.md`: no texto da User Story 3 (linha 52) e no Acceptance Scenario 1 (linha 60), remover a menção a "Cobertura de custos (recebido ÷ pago)" da lista de indicadores exibidos pela aba "Visão de Indicadores"; em FR-005 (linha 79), remover "cobertura de custos" da lista de indicadores calculados. Referenciar esta spec (`specs/030-remove-indice-cobertura-custos/spec.md`) como a mudança que completou a remoção iniciada pela spec 024 — conforme [research.md — R3](./research.md#r3--specs-retroativas-que-precisam-de-atualização-princípio-v-da-constituição), seguindo o mesmo padrão de nota "Atualização (data, ver spec)" já usado na linha 15 deste mesmo arquivo (adicionada pela spec 029). Não editar `specs/011-dashboard/spec.md` nem `specs/024-remover-card-cobertura-custos/spec.md` (confirmado que não citam o indicador por nome ou já são neutras quanto ao resultado).
- [X] T010 [P] Buscar por `IndiceCoberturaCustosFixos` (backend, case-sensitive) e `indiceCoberturaCustosFixos` (frontend) em todo o repositório (`src/`, `frontend/`, `tests/`) e confirmar zero ocorrências em código-fonte — cobre SC-003 da spec e Cenário 4 do quickstart.md. Ocorrências em arquivos `specs/*.md` que documentam o histórico da mudança (incluindo a atualização feita em T009) são esperadas e não contam como residuais.
- [X] T011 Rodar a suíte de testes completa (`dotnet test` a partir da raiz do repositório) e confirmar que todos os testes existentes continuam passando sem alteração — cobre FR-004/SC-002 da spec (nenhum outro indicador afetado) — depende de T008.
- [X] T012 Executar manualmente os 4 cenários do [quickstart.md](./quickstart.md) contra o backend rodando localmente, solicitando uma credencial temporária no momento do teste (não reutilizar nem persistir credenciais — regra de trabalho combinada), confirmando os resultados esperados em `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros` antes de considerar a feature concluída — depende de T005, T008.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Vazia.
- **Foundational (Phase 2)**: Vazia.
- **User Story 1 (Phase 3)**: Sem dependência de fase anterior (ambas vazias). T002/T003 dependem de T001 (ordem de compilação: a propriedade precisa deixar de existir antes de parar de ser atribuída, senão não compila. Na prática, T001-T003 devem ser aplicadas juntas antes de qualquer `dotnet build`).
- **User Story 2 (Phase 4)**: Depende de US1 completa (T006/T007 removem variáveis que só ficam órfãs depois que T002/T003 removem seu único uso).
- **Polish (Phase 5)**: T009/T010 podem rodar em paralelo com qualquer fase (documentação e busca textual, sem dependência de código). T011/T012 dependem de US1 e US2 completas (T005, T008).

### User Story Dependencies

- **User Story 1 (P1)**: Entrega o valor principal sozinha — o contrato de API já fica correto ao final desta fase, mesmo com o cálculo interno ainda "morto" (não é um problema visível externamente).
- **User Story 2 (P2)**: Só faz sentido depois de US1 (remove código que só se torna órfão depois da remoção do campo) — não é uma dependência de "funcionalidade", é uma dependência de ordem de limpeza dentro da mesma mudança.

### Within Each User Story

- T001 antes de T002/T003 (compilação).
- T002/T003 antes de T006/T007 (variável só fica não utilizada depois que o único uso dela é removido).

### Parallel Opportunities

- T004 (frontend) pode ser feita em paralelo com T001-T003 (arquivos completamente diferentes, sem dependência entre si).
- T009 e T010 (Polish) podem ser feitas em paralelo entre si e com as fases de código, já que são documentação/busca textual.

---

## Parallel Example: User Story 1

```bash
# T001-T003 (backend) e T004 (frontend) podem ser feitas em paralelo:
Task: "Remover propriedade IndiceCoberturaCustosFixos em src/SPI.Application/Dashboard/Dtos/IndicadoresFinanceirosResponse.cs"
Task: "Remover campo indiceCoberturaCustosFixos em frontend/lib/api/dashboard.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 3 (User Story 1 — T001-T005): o contrato de API já fica correto (sem o campo morto), que é o pedido central da mudança.
2. **PARAR e VALIDAR**: rodar T005 (build) e os Cenários 1-3 do quickstart.md.
3. Diferente da feature anterior (029), aqui **é seguro parar em US1**: o cálculo interno remanescente (variável não utilizada) não viola nenhum requisito visível externamente — é só uma limpeza de código adiável sem risco funcional, ao contrário do fallback provisório de outras features que violava uma regra de negócio.

### Incremental Delivery

1. User Story 1 (T001-T005) → contrato de API limpo → MVP pronto.
2. User Story 2 (T006-T008) → remove o cálculo órfão → nenhum código morto restante.
3. Polish (T009-T012) → atualiza spec 015, confirma zero referências residuais, roda suíte completa, valida manualmente.

## Notes

- [P] = arquivos diferentes ou tarefas sem dependência entre si.
- [US1]/[US2] mapeiam cada tarefa à user story correspondente da spec, para rastreabilidade.
- Nenhuma tarefa desta lista requer migração de banco, nova dependência, ou teste automatizado novo (ver [research.md](./research.md) e [data-model.md](./data-model.md)).
- T012 segue a regra de trabalho combinada sobre credenciais de teste: pedir uma credencial temporária no momento do teste, nunca criar/persistir uma nova, nunca gravá-la em arquivo.
