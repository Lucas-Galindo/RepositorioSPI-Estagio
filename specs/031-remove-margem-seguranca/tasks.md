---

description: "Task list for 031-remove-margem-seguranca"
---

# Tasks: Remover Indicador "Margem de Segurança %"

**Input**: Design documents from `/specs/031-remove-margem-seguranca/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required for user stories), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Não incluídos — a spec não pede TDD, não há teste existente que referencie o indicador (confirmado em [research.md — R4](./research.md#r4--nenhum-teste-automatizado-cobre-o-indicador-hoje)), e a remoção do campo no backend já é validada pelo compilador. A validação funcional (UI e payload) é feita via `quickstart.md`.

**Organization**: Tarefas agrupadas por user story (US1 = P1, US2 = P2), conforme [spec.md](./spec.md). US1 remove a exibição visual (o card na aba Indicadores); US2 remove o campo do contrato de API e o cálculo interno (backend) e o tipo espelhado do frontend.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1 ou US2, conforme spec.md
- Caminhos de arquivo são relativos à raiz do repositório

## Path Conventions

Projeto web já existente (Option 2): backend .NET em `src/SPI.Application`, frontend em `frontend/app/(app)/relatorios/financeiro/` e `frontend/lib/api/` (ver [plan.md](./plan.md), Structure Decision). Nenhum arquivo em `SPI.Domain`, `SPI.Infrastructure` ou `SPI.Api` é tocado.

---

## Phase 1: Setup

**Purpose**: Nenhuma inicialização de projeto é necessária — nenhuma dependência nova, nenhuma ferramenta nova (ver [plan.md](./plan.md), Technical Context).

Nenhuma tarefa nesta fase. Prosseguir direto para Phase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Não há pré-requisito bloqueante compartilhado entre as duas user stories — a mudança não depende de nenhum dado, schema ou infraestrutura nova (ver [data-model.md](./data-model.md): sem entidade, sem coluna de banco).

Nenhuma tarefa nesta fase. Prosseguir direto para Phase 3.

---

## Phase 3: User Story 1 - Não ver mais um indicador com nome enganoso (Priority: P1) 🎯 MVP

**Goal**: O card "Margem de segurança" deixa de aparecer na aba Indicadores do Relatório Financeiro, em qualquer filtro/estado, com os 2 cards restantes (Inadimplência, Prazo médio de atraso) ocupando a fileira de forma equilibrada.

**Independent Test**: Abrir Relatórios → Relatório Financeiro → aba Indicadores e confirmar visualmente a ausência do card e o preenchimento equilibrado da fileira — Cenários 1-2 do [quickstart.md](./quickstart.md).

### Implementation for User Story 1

- [X] T001 [US1] Em `frontend/app/(app)/relatorios/financeiro/page.tsx`, remover o bloco JSX do card "Margem de segurança" (linhas ~369-377: o terceiro `<div className="kpi">` dentro da fileira, com o label "Margem de segurança", o valor `indicadoresDados.indicadores.margemSegurancaPercentual` e o texto "Folga do caixa após despesas"). Manter os dois primeiros cards (Inadimplência, Prazo médio de atraso) inalterados.
- [X] T002 [US1] No mesmo arquivo, trocar a classe do container da fileira (linha ~350) de `className="kpi-row kpi-row-3"` para `className="kpi-row kpi-row-2"` — reaproveita a classe já existente em `frontend/styles/dashboard.css:365-367` (ver [research.md — R3](./research.md#r3--padrão-de-reflow-visual-para-a-fileira-de-2-cards-restantes)), sem necessidade de CSS novo. Depende de T001 (mesmo bloco de código).
- [X] T003 [US1] Rodar `npx tsc --noEmit` (ou `npm run build`) em `frontend/` e confirmar que a remoção não introduz erro de compilação/tipo — depende de T001, T002.

**Checkpoint**: Neste ponto, o card não aparece mais para a professora (o risco de leitura errada do indicador já está eliminado — valor principal da mudança entregue), mesmo que o backend ainda calcule e retorne o campo internamente (limpeza de contrato pendente, User Story 2). **Backend continua enviando o campo `margemSegurancaPercentual` na resposta, mas nada no frontend mais o lê — isso é seguro.** Não confundir com o estado oposto (evitar): campo já removido da API mas card ainda tentando lê-lo, o que exibiria "undefined%" para a professora — ver "Ordem que MUST ser evitada" em Dependencies.

---

## Phase 4: User Story 2 - Contrato de API sem o indicador removido (Priority: P2)

**Goal**: `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros` param de calcular e retornar `margemSegurancaPercentual`; o tipo espelhado do frontend também deixa de declarar o campo.

**Independent Test**: Chamar os dois endpoints (autenticado) e confirmar, no JSON de resposta, a ausência da chave `margemSegurancaPercentual` dentro de `indicadores` — Cenários 3-4 do [quickstart.md](./quickstart.md).

### Implementation for User Story 2

- [X] T004 [US2] Em `src/SPI.Application/Dashboard/Dtos/IndicadoresFinanceirosResponse.cs`, remover a propriedade `public decimal MargemSegurancaPercentual { get; set; }` (linha ~25) e seu comentário XML associado, do `record IndicadoresFinanceirosResponse`. Manter todas as demais propriedades (`TaxaInadimplenciaPercentual`, `PrazoMedioAtrasoDias`, `FluxoCaixaOperacional`, `GargaloCaixa`) inalteradas. **MUST ser aplicada junto com T005 e T006** (não deixar commitado/compilado sozinha): remover só a propriedade sem remover as atribuições que a preenchem quebra a build (`MargemSegurancaPercentual` deixaria de existir no tipo usado no inicializador de `DashboardService.cs`/`RelatorioService.cs`).
- [X] T005 [US2] Em `src/SPI.Application/Dashboard/Services/DashboardService.cs`, no método `ObterIndicadoresFinanceirosAsync`, remover tanto a linha de cálculo `var margemSeguranca = valorFaturado == 0 ? 0m : Math.Round(fluxoCaixaOperacional / valorFaturado * 100, 1);` (linha ~75) quanto a linha de atribuição `MargemSegurancaPercentual = margemSeguranca,` (linha ~86) — as duas juntas nesta tarefa (ao contrário da spec 030, não há necessidade de estágio intermediário aqui, já que a spec 031 não separa "remover contrato" de "parar de calcular" em stories distintas). Aplicar junto com T004: sozinha (sem T004) compila normalmente — a propriedade só deixaria de ser preenchida — mas o objetivo desta tarefa só fica completo quando o campo também sai do DTO.
- [X] T006 [US2] Em `src/SPI.Application/Relatorios/Services/RelatorioService.cs`, no método `ObterIndicadoresFinanceirosAsync`, remover a mesma dupla de linhas equivalente (cálculo em ~206, atribuição em ~218). Mesma regra de T005: aplicar junto com T004.
- [X] T007 [US2] Em `frontend/lib/api/dashboard.ts`, remover o campo `margemSegurancaPercentual: number;` (linha ~15) da interface `IndicadoresFinanceiros`. Não requer edição em `frontend/lib/api/relatorios.ts` (reaproveita o mesmo tipo sem declaração própria — ver [data-model.md](./data-model.md)). **Depende de T001**: só é seguro remover este campo do tipo depois que o JSX que o lia (T001) já não existe mais — caso contrário `page.tsx` acessaria uma propriedade inexistente no tipo.
- [X] T008 [US2] Compilar o backend (`dotnet build`) e confirmar que T004-T006 não introduzem nenhum erro de compilação — depende de T004, T005, T006.
- [X] T009 [US2] Rodar `npx tsc --noEmit` (ou `npm run build`) em `frontend/` novamente e confirmar que a remoção do campo do tipo (T007) não introduz erro — depende de T007, T003 (T001 já aplicado).

**Checkpoint**: As duas user stories completas — nenhum código relacionado ao indicador de margem de segurança permanece calculando, expondo ou exibindo nada.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Atualizar a documentação retroativa afetada (Princípio V da constituição) e confirmar ausência total de referências residuais e de regressão nos demais indicadores.

- [X] T010 [P] Atualizar `specs/015-relatorio-financeiro/spec.md`: no texto da User Story 3, no Acceptance Scenario 1 e em FR-005, remover a menção a "Margem de segurança %" da lista de indicadores exibidos/calculados. Adicionar uma nota "Atualização (2026-09-18, ver [031-remove-margem-seguranca](../031-remove-margem-seguranca/spec.md))" seguindo o mesmo padrão já usado duas vezes nesse arquivo (specs 029 e 030) — conforme [research.md — R5](./research.md#r5--specs-retroativas-que-precisam-de-atualização-princípio-v). Não editar `specs/024-remover-card-cobertura-custos/spec.md` (confirmado que é um registro histórico correto de uma mudança já concluída, não uma afirmação sobre o estado atual).
- [X] T011 [P] Buscar por `MargemSegurancaPercentual` (backend, case-sensitive) e `margemSegurancaPercentual` (frontend) em todo o repositório (`src/`, `frontend/`, `tests/`) e confirmar zero ocorrências em código-fonte — cobre SC-005 da spec e Cenário 6 do quickstart.md. Ocorrências em arquivos `specs/*.md` que documentam o histórico da mudança (incluindo a atualização feita em T010) são esperadas e não contam como residuais.
- [X] T012 Rodar a suíte de testes completa (`dotnet test` a partir da raiz do repositório) e confirmar que todos os testes existentes continuam passando sem alteração — cobre FR-006/SC-004 da spec — depende de T008.
- [ ] T013 Executar manualmente os 6 cenários do [quickstart.md](./quickstart.md) (UI no navegador contra o frontend rodando localmente, e chamadas HTTP contra o backend rodando localmente), solicitando uma credencial temporária no momento do teste (não reutilizar nem persistir credenciais — regra de trabalho combinada), confirmando os resultados esperados antes de considerar a feature concluída — depende de T003, T009, T012.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Vazia.
- **Foundational (Phase 2)**: Vazia.
- **User Story 1 (Phase 3)**: Sem dependência de fase anterior (ambas vazias). Autocontida — só toca `page.tsx`.
- **User Story 2 (Phase 4)**: T004-T006, T008 (backend) não dependem de US1 para compilar. **T007 (remoção do campo no tipo TypeScript) depende de T001 (US1)** — ver nota de segurança abaixo. Na prática, US1 deve ser aplicada antes ou junto com T007.
- **Polish (Phase 5)**: T010/T011 podem rodar em paralelo com qualquer fase (documentação e busca textual). T012/T013 dependem de US1 e US2 completas.

### User Story Dependencies

- **User Story 1 (P1)**: Entrega o valor principal sozinha e é um checkpoint de deploy seguro por si só — o card para de enganar a professora, e o backend continuar calculando o valor internamente (sem nenhum lugar exibindo-o) não representa nenhum risco visível. **Este é o oposto do padrão observado na spec 029**: lá, parar em US1 deixava uma regra de negócio formalmente violada; aqui, parar em US1 é estritamente seguro.
- **User Story 2 (P2)**: Pode ser feita em paralelo com US1 do lado do backend (T004-T006, T008), mas a parte do frontend (T007) só é segura depois que T001 remove o único lugar que lia o campo. **Ordem que MUST ser evitada**: remover o campo do contrato de API (T004-T006) e do tipo TypeScript (T007) *antes* de T001 — nesse caso, a resposta HTTP já não traria `margemSegurancaPercentual`, mas o JSX ainda tentaria renderizá-lo, exibindo "undefined%" para a professora até T001 ser aplicada. Como as duas stories fazem parte da mesma entrega, essa ordem intermediária não deve existir em nenhum commit intermediário nem deploy parcial.

### Within Each User Story

- T001 antes de T002 (mesmo bloco/arquivo).
- T004, T005 e T006 MUST ser aplicadas juntas (nenhuma ordem entre elas quebra a build isoladamente, mas T004 sozinha sem T005/T006 quebra — ver nota em T004).
- T001 antes de T007 (ver nota de segurança acima).

### Parallel Opportunities

- T004-T006 (backend) podem ser feitas em paralelo com T001-T002 (frontend) — arquivos completamente diferentes — desde que T007 só seja aplicada depois de T001 (ver dependência acima).
- T010 e T011 (Polish) podem ser feitas em paralelo entre si e com as fases de código.

---

## Parallel Example: User Story 1 e backend de User Story 2

```bash
# Podem ser feitas em paralelo (arquivos diferentes, sem dependência entre si):
Task: "Remover card JSX em frontend/app/(app)/relatorios/financeiro/page.tsx (T001-T002)"
Task: "Remover propriedade MargemSegurancaPercentual em src/SPI.Application/Dashboard/Dtos/IndicadoresFinanceirosResponse.cs (T004)"
Task: "Remover calculo/atribuicao em src/SPI.Application/Dashboard/Services/DashboardService.cs (T005)"
Task: "Remover calculo/atribuicao em src/SPI.Application/Relatorios/Services/RelatorioService.cs (T006)"

# NAO pode ser feita em paralelo com o que acima -- so depois que T001 estiver pronta:
# Task: "Remover campo do tipo em frontend/lib/api/dashboard.ts (T007)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 3 (User Story 1 — T001-T003): o card para de enganar a professora, que é o pedido central da mudança.
2. **PARAR e VALIDAR**: rodar T003 (typecheck) e os Cenários 1-2 do quickstart.md.
3. Diferente da spec 029 (onde parar em US1 deixava uma regra de negócio violada), aqui **é seguro parar em US1**: o backend seguir calculando e retornando um campo que nenhuma tela mais lê não é um problema visível nem uma violação de requisito — é só uma limpeza de contrato adiável (User Story 2).

### Incremental Delivery

1. User Story 1 (T001-T003) → remove o card enganoso → MVP pronto.
2. User Story 2 backend (T004-T006, T008) → remove o campo do contrato de API e o cálculo → pode ser feito em paralelo com US1.
3. User Story 2 frontend (T007, T009) → remove o campo do tipo TypeScript → só depois de T001 (ver Dependencies).
4. Polish (T010-T013) → atualiza spec 015, confirma zero referências residuais, roda suíte completa, valida manualmente os 6 cenários.

## Notes

- [P] = arquivos diferentes ou tarefas sem dependência entre si.
- [US1]/[US2] mapeiam cada tarefa à user story correspondente da spec, para rastreabilidade.
- A única dependência cross-story real desta feature é T007 → depende de T001 (ver Dependencies e Implementation Strategy) — documentada explicitamente para não repetir o tipo de achado que o `/speckit-analyze` identificou na spec 029 (ordem de aplicação que deixaria um estado intermediário inválido/enganoso).
- Nenhuma tarefa desta lista requer migração de banco, nova dependência, ou teste automatizado novo (ver [research.md](./research.md) e [data-model.md](./data-model.md)).
- T013 segue a regra de trabalho combinada sobre credenciais de teste: pedir uma credencial temporária no momento do teste, nunca criar/persistir uma nova, nunca gravá-la em arquivo.
