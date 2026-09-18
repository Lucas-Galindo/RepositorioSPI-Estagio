---

description: "Task list for 029-fix-relatorio-financeiro-turma"
---

# Tasks: Corrigir Agrupamento por Turma no Relatório Financeiro

**Input**: Design documents from `/specs/029-fix-relatorio-financeiro-turma/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required for user stories), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Incluídos — a spec descreve cenários de aceitação verificáveis e o projeto já tem um padrão de teste estabelecido (`RelatorioServiceLancamentosTests.cs`) para `RelatorioService`; os testes cobrem exatamente os Acceptance Scenarios de cada user story.

**Organization**: Tarefas agrupadas por user story (US1 = P1, US2 = P2), conforme [spec.md](./spec.md).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1 ou US2, conforme spec.md
- Caminhos de arquivo são absolutos em relação à raiz do repositório

## Path Conventions

Projeto web já existente (Option 2): backend .NET em `src/SPI.*`, testes em `tests/SPI.Application.Tests/`, frontend em `frontend/` (sem alteração nesta feature — ver [plan.md](./plan.md), Structure Decision).

---

## Phase 1: Setup

**Purpose**: Nenhuma inicialização de projeto é necessária — todos os projetos, dependências e frameworks de teste já existem e são reaproveitados sem alteração (ver [plan.md](./plan.md), Technical Context: nenhuma dependência nova).

Nenhuma tarefa nesta fase. Prosseguir direto para Phase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Garantir que os dados necessários para determinar a turma de um pagamento (`Pagamento.PagamentosAula → Aula.TurmaId/Aula.Turma.Nome`) estejam de fato carregados antes de qualquer lógica de agrupamento poder usá-los — pré-requisito de ambas as user stories (ver [research.md — R2](./research.md#r2--carregamento-dos-dados-necessários-ef-core-include)).

**⚠️ CRITICAL**: Nenhuma user story pode ser implementada corretamente antes desta fase — sem o `Include`, `Pagamento.PagamentosAula` chega vazio em memória (o projeto não usa lazy-loading proxies).

- [X] T001 Em `src/SPI.Infrastructure/Repositories/RelatorioRepository.cs`, no método `ListarPagosNoPeriodoAsync` (linhas ~222-256), adicionar ao carregamento existente (`.Include(p => p.Aluno)...Include(p => p.FormaPagamento)`) a cadeia `.Include(p => p.PagamentosAula).ThenInclude(pa => pa.Aula).ThenInclude(a => a.Turma)`, garantindo que `Pagamento.PagamentosAula`, `PagamentoAula.Aula.TurmaId` e `Aula.Turma.Nome` estejam materializados nos objetos retornados, sem alterar nenhum dos filtros (`Where`/parâmetros) já existentes no método.

**Checkpoint**: Com o `Include` em vigor, `pagos` (a lista já materializada em `ObterFinanceiroAsync`) contém todos os dados necessários para as duas user stories abaixo.

---

## Phase 3: User Story 1 - Ver o valor financeiro atribuído à turma correta (Priority: P1)

**Goal**: O agrupamento "Por Turma" do Relatório Financeiro passa a atribuir o valor de cada pagamento à turma da aula que ele efetivamente cobre (via `PagamentoAula`/`Aula.TurmaId`), em vez de usar a primeira turma da lista geral de turmas do aluno — corrigindo o caso principal relatado (aluno em múltiplas turmas tendo seu valor todo atribuído incorretamente a uma só).

**Independent Test**: Cadastrar um aluno em duas turmas, registrar um pagamento "Pago" vinculado a uma aula de cada turma, e confirmar via `GET /api/relatorios/financeiro` que cada pagamento aparece somado na turma correta (Cenário 1 do [quickstart.md](./quickstart.md)).

### Tests for User Story 1

> **NOTE**: Escrever estes testes primeiro; confirmar que falham contra o código atual (que usa `Aluno.AlunosTurma.FirstOrDefault()`) antes de implementar a correção.

- [X] T002 [P] [US1] Em `tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPorTurmaTests.cs` (novo arquivo), criar um fake manual de `IRelatorioRepository` seguindo o padrão de `FakeRelatorioRepository` já usado em `RelatorioServiceLancamentosTests.cs`, com suporte a `ListarPagosNoPeriodoAsync` retornando uma lista de `Pagamento` configurável em memória (incluindo `PagamentosAula` com `Aula.TurmaId`/`Aula.Turma.Nome` populados) e `ObterValorPagoNoPeriodoAsync`/`ObterReceitasPendentesSegregadasAsync`/`ObterDespesasPendentesSegregadasAsync` retornando valores neutros (zero) por padrão.
- [X] T003 [P] [US1] No mesmo arquivo de teste, caso `Pagamento_De_Turma_Unica_E_Agrupado_Na_Turma_Correta_Mesmo_Com_Aluno_Em_Outra_Turma`: aluno vinculado às turmas "Turma A" e "Turma B" (via `Aluno.AlunosTurma`), um `Pagamento` de `ValorFinal = 100` vinculado (via `PagamentoAula`) a uma `Aula` com `TurmaId` apontando para "Turma A"; chamar `RelatorioService.ObterFinanceiroAsync` e afirmar que `PorTurma` contém um item `{ Chave = "Turma A", Total = 100 }` e **não** contém "Turma B" (cobre Acceptance Scenario 1 da US1).
- [X] T004 [P] [US1] No mesmo arquivo, caso `Pagamento_De_Aluno_Em_Turma_Unica_Continua_Correto` (regressão): aluno vinculado a apenas "Turma A", pagamento vinculado a aula de "Turma A"; afirmar que o resultado é idêntico ao comportamento anterior (cobre Acceptance Scenario 2 da US1).
- [X] T005 [P] [US1] No mesmo arquivo, caso `Pagamento_De_Aula_Individual_Sem_Turma_Vai_Para_Atendimento_Particular`: pagamento vinculado a uma `Aula` com `TurmaId = null`; afirmar que `PorTurma` contém `{ Chave = "Atendimento particular", Total = <valor> }` (cobre Acceptance Scenario 3 da US1).
- [X] T006 [P] [US1] No mesmo arquivo, caso `Pagamento_Sem_Nenhuma_Aula_Vinculada_Vai_Para_Atendimento_Particular`: pagamento com `PagamentosAula` vazio; afirmar que cai em `{ Chave = "Atendimento particular", ... }` (cobre Acceptance Scenario 4 da US1).

### Implementation for User Story 1

- [X] T007 [US1] Em `src/SPI.Application/Relatorios/Services/RelatorioService.cs`, extrair um método privado estático `DeterminarChaveTurma(Pagamento pagamento)` que calcula o conjunto de `TurmaId` distintos a partir de `pagamento.PagamentosAula.Select(pa => pa.Aula.TurmaId)` e retorna: `"Atendimento particular"` se o conjunto de `TurmaId` não nulos for vazio (nenhuma aula vinculada, ou todas sem turma); o `Nome` da turma se houver exatamente um `TurmaId` não nulo distinto **e** nenhuma aula vinculada sem turma; e (fallback provisório, **que viola FR-004 e MUST ser substituído em T014 antes de qualquer deploy** — ver Implementation Strategy) o nome da primeira turma encontrada nos demais casos. Não depende de T001 para ser escrita ou testada (os testes usam um repositório fake); T001 só passa a importar quando o método roda contra o backend real (T018).
- [X] T008 [US1] No mesmo arquivo, em `ObterFinanceiroAsync` (linhas ~137-141), substituir a expressão atual `pagos.GroupBy(p => p.Aluno.AlunosTurma.Select(at => at.Turma.Nome).FirstOrDefault() ?? "Atendimento particular")` por `pagos.GroupBy(p => DeterminarChaveTurma(p))`, mantendo inalterado o restante da projeção (`Select(g => new RelatorioFinanceiroItem { Chave = g.Key, Total = g.Sum(p => p.ValorFinal) }).OrderByDescending(i => i.Total)`) — depende de T007.
- [X] T009 [US1] Rodar os testes de T002-T006 (`dotnet test --filter RelatorioServiceFinanceiroPorTurmaTests`) e confirmar que todos passam antes de prosseguir para a User Story 2 — depende de T008.

**Checkpoint**: Neste ponto, pagamentos de turma única (ou sem turma) já são agrupados corretamente, mesmo quando o aluno participa de outras turmas, e isso já é testável de forma independente (T002-T009). **Este checkpoint não é um ponto de entrega válido em produção**: o método `DeterminarChaveTurma` ainda usa um fallback provisório para pagamentos de múltiplas turmas que viola FR-004 (atribui a uma turma arbitrária em vez de "Múltiplas turmas") — ver Implementation Strategy abaixo. A User Story 2 (T010-T015) é obrigatória antes de qualquer deploy desta feature.

---

## Phase 4: User Story 2 - Ver com clareza quando um pagamento cobre mais de uma turma (Priority: P2)

**Goal**: Quando um pagamento (lançado manualmente) cobre aulas de mais de uma turma, seu valor aparece uma única vez sob o rótulo "Múltiplas turmas", sem duplicação nem atribuição arbitrária a uma das turmas — preservando a consistência da soma total do relatório.

**Independent Test**: Registrar manualmente um pagamento vinculado a aulas de duas turmas diferentes e confirmar que ele aparece uma única vez em `PorTurma` sob `"Múltiplas turmas"`, e que a soma de todos os grupos de `PorTurma` continua igual a `TotalRecebido` (Cenários 2 e 3 do [quickstart.md](./quickstart.md)).

### Tests for User Story 2

> **NOTE**: Escrever estes testes primeiro; confirmar que falham contra o fallback provisório de T007 (que atribuiria a uma única turma) antes de implementar a regra final.

- [X] T010 [P] [US2] Em `tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPorTurmaTests.cs`, caso `Pagamento_Com_Aulas_De_Turmas_Diferentes_Vai_Para_Multiplas_Turmas`: um único `Pagamento` de `ValorFinal = 200` vinculado a uma aula de "Turma A" e a uma aula de "Turma B"; afirmar que `PorTurma` contém exatamente um item `{ Chave = "Múltiplas turmas", Total = 200 }`, e que **não** existe nenhum item somando esse valor em "Turma A" nem em "Turma B" (cobre Acceptance Scenario 1 da US2).
- [X] T011 [P] [US2] No mesmo arquivo, caso `Soma_Dos_Grupos_Por_Turma_Bate_Com_Total_Recebido`: conjunto misto de pagamentos (turma única, "Atendimento particular", "Múltiplas turmas"); afirmar que `resposta.PorTurma.Sum(i => i.Total) == resposta.TotalRecebido` (cobre Acceptance Scenario 2 da US2 e SC-003 da spec).
- [X] T012 [P] [US2] No mesmo arquivo, caso `Pagamento_De_Duas_Aulas_Da_Mesma_Turma_Nao_Duplica`: pagamento vinculado a duas aulas diferentes, ambas de "Turma A"; afirmar que `PorTurma` contém um único item `{ Chave = "Turma A", Total = <valor do pagamento, uma vez> }` (cobre o primeiro Edge Case da spec).
- [X] T013 [P] [US2] No mesmo arquivo, caso `Pagamento_Com_Turma_E_Aula_Sem_Turma_Vai_Para_Multiplas_Turmas`: pagamento vinculado a uma aula de "Turma A" e a uma aula individual (`TurmaId = null`); afirmar que cai em `"Múltiplas turmas"`, não em "Turma A" nem em "Atendimento particular" (cobre o segundo Edge Case da spec).

### Implementation for User Story 2

- [X] T014 [US2] Em `src/SPI.Application/Relatorios/Services/RelatorioService.cs`, no método `DeterminarChaveTurma` criado em T007, substituir o fallback provisório: quando o conjunto de `TurmaId` distintos das aulas vinculadas tiver mais de um valor, **ou** quando houver ao menos uma aula com `TurmaId` não nulo combinada com ao menos uma aula com `TurmaId == null` no mesmo pagamento, retornar `"Múltiplas turmas"` — depende de T008.
- [X] T015 [US2] Rodar todos os testes de `RelatorioServiceFinanceiroPorTurmaTests` (T002-T006 e T010-T013) e confirmar que passam sem quebrar nenhum caso da User Story 1 — depende de T014.

**Checkpoint**: As duas user stories funcionam de forma independente e em conjunto — todo pagamento "Pago" no período cai em exatamente um grupo (turma única, "Atendimento particular" ou "Múltiplas turmas"), sem perda nem duplicação de valor.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Confirmar ausência de regressão nos demais agrupamentos/indicadores da mesma tela e manter a documentação retroativa do domínio financeiro consistente com o novo comportamento (Princípio V da constituição — ver [plan.md](./plan.md), Constitution Check).

- [X] T016 [P] Em `tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPorTurmaTests.cs`, caso `PorFormaPagamento_E_PorAluno_Nao_Sao_Afetados`: com o mesmo conjunto de dados de teste usado nas fases anteriores, afirmar que `PorFormaPagamento` e `PorAluno` retornam exatamente os mesmos valores que retornariam com a lógica anterior (cobre FR-006/SC-004 da spec e Cenário 4 do quickstart.md).
- [X] T017 Atualizar `specs/015-relatorio-financeiro/spec.md` para documentar a nova regra de agrupamento "Por Turma" (turma da aula coberta pelo pagamento, com os grupos especiais "Atendimento particular" e "Múltiplas turmas"), referenciando esta spec (`specs/029-fix-relatorio-financeiro-turma/spec.md`) como a mudança que introduziu a correção — conforme exigido pelo Princípio V da constituição (documentação retroativa vinculante) e já registrado como pendência no `plan.md` desta feature.
- [ ] T018 Executar manualmente os 4 cenários do [quickstart.md](./quickstart.md) contra o backend rodando localmente (`dotnet run` em `src/SPI.Api`), confirmando os resultados esperados em `GET /api/relatorios/financeiro` antes de considerar a feature concluída.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Vazia — nenhuma dependência, nenhum bloqueio.
- **Foundational (Phase 2)**: Sem dependência de Setup (vazia). T001 bloqueia o comportamento correto em produção/integração (T018, contra o backend real), pois é o que garante que `Pagamento.PagamentosAula`/`Aula.Turma` cheguem carregados fora dos testes unitários. **Não bloqueia a escrita nem a execução dos testes unitários** (T002-T006, T010-T013), que usam um `IRelatorioRepository` fake e nunca passam pelo `Include` real do EF Core — esses podem ser escritos e rodados a qualquer momento, independentemente de T001 já estar feito.
- **User Story 1 (Phase 3)**: Não depende de T001 para ser testada em memória (fake repository). Não depende de US2 para ser implementada. **Depende de US2 (T014) para ser válida em produção** — ver nota abaixo.
- **User Story 2 (Phase 4)**: Depende de T007/T008 (US1) — estende o mesmo método `DeterminarChaveTurma` criado na US1, em vez de duplicá-lo. Não é estritamente independente de código (mesmo arquivo/método), mas é independentemente testável: os testes de US1 continuam passando isoladamente, e os testes de US2 podem ser escritos e rodados (falhando) antes de T014.
- **Polish (Phase 5)**: Depende de US1 e US2 completas (T016 verifica ambos os fluxos; T017/T018 documentam e validam o resultado final). T018 depende também de T001 (validação manual contra o backend real).

### User Story Dependencies

- **User Story 1 (P1)** e **User Story 2 (P2)** formam, juntas, um único incremento indivisível de entrega. Não existe um ponto de deploy válido com apenas US1: sem T014 (US2), o método `DeterminarChaveTurma` usa um fallback que atribui pagamentos de múltiplas turmas a uma turma arbitrária, violando FR-004 (o próprio defeito de classe que esta feature corrige, só que para o caso raro de múltiplas turmas). US1 pode ser implementada e testada primeiro (T002-T009 antes de T010-T015), mas T014 MUST estar concluída antes de qualquer deploy/merge desta feature.

### Within Each User Story

- Testes escritos e falhando antes da implementação (T002-T006 antes de T007-T009; T010-T013 antes de T014-T015).
- Dentro de cada story, a implementação segue a ordem: mudança no método de agregação → integração no `GroupBy` → validação via testes.

### Parallel Opportunities

- T002-T006 (todos os testes de US1) podem ser escritos em paralelo entre si (mesmo arquivo, mas casos de teste independentes — marcar `[P]` é válido para redação simultânea; a execução do `dotnet test` roda todos de qualquer forma).
- T010-T013 (todos os testes de US2) podem ser escritos em paralelo entre si, e em paralelo com T002-T006 (nenhum depende do outro para ser *escrito*, só para *passar*).
- T016 pode ser escrito em paralelo com as demais tarefas de teste.
- T001 (Foundational) é pré-requisito serial de tudo — não paralelizável com as fases seguintes.

---

## Parallel Example: User Story 1

```bash
# Escrever todos os testes de User Story 1 em paralelo (mesmo arquivo, casos independentes):
Task: "Caso Pagamento_De_Turma_Unica_E_Agrupado_Na_Turma_Correta_Mesmo_Com_Aluno_Em_Outra_Turma em tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPorTurmaTests.cs"
Task: "Caso Pagamento_De_Aluno_Em_Turma_Unica_Continua_Correto em tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPorTurmaTests.cs"
Task: "Caso Pagamento_De_Aula_Individual_Sem_Turma_Vai_Para_Atendimento_Particular em tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPorTurmaTests.cs"
Task: "Caso Pagamento_Sem_Nenhuma_Aula_Vinculada_Vai_Para_Atendimento_Particular em tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPorTurmaTests.cs"
```

---

## Implementation Strategy

### Incremento único e indivisível (sem MVP parcial)

Esta feature **não tem uma fatia US1-only entregável em produção**: o defeito que a User Story 1 corrige (atribuição da turma via `Aluno.AlunosTurma.FirstOrDefault()`) e o defeito que a User Story 2 corrige (fallback arbitrário para múltiplas turmas) são a mesma classe de bug — atribuir um valor financeiro a uma turma errada por adivinhação em vez de usar o vínculo real. Implementar só US1 e parar deixaria essa mesma classe de bug viva para o caso de múltiplas turmas, violando FR-004. Por isso, T001 + US1 (T002-T009) + US2 (T010-T015) MUST ser tratadas como um único incremento — a ordem de implementação abaixo é sequencial por conveniência de teste (US1 primeiro, por ser o caso mais comum), não uma sugestão de entrega parcial.

1. Foundational (T001) → garante que os dados (`Pagamento.PagamentosAula`/`Aula.Turma`) cheguem carregados fora dos testes unitários (pode ser feito em paralelo com a escrita dos testes de US1/US2, que usam repositório fake).
2. User Story 1 (T002-T009) → implementa e testa a correção para os casos de turma única e "Atendimento particular" (a maioria dos pagamentos), deixando o caso de múltiplas turmas com um fallback provisório e não-conforme (documentado em T007).
3. User Story 2 (T010-T015) → substitui o fallback provisório pela regra final ("Múltiplas turmas"), o que torna FR-004 finalmente satisfeita — **obrigatório antes de qualquer merge/deploy**.
4. Polish (T016-T018) → confirma ausência de regressão nos demais agrupamentos, atualiza a spec retroativa 015, valida manualmente os 4 cenários do quickstart.md contra o backend real (T018, que é o ponto em que T001 realmente importa).

## Notes

- [P] = arquivos diferentes ou casos de teste independentes dentro do mesmo arquivo, sem dependência de tarefa incompleta.
- [US1]/[US2] mapeiam cada tarefa à user story correspondente da spec, para rastreabilidade.
- T007/T008 e T014 tocam o mesmo método (`DeterminarChaveTurma`) em sequência (US1 cria, US2 estende) — isso é intencional: é um único ponto de lógica pequeno, e duplicá-lo entre stories seria pior do que o fallback provisório documentado em T007.
- Nenhuma tarefa desta lista requer migração de banco, novo endpoint ou mudança de contrato de API (ver [data-model.md](./data-model.md)).
