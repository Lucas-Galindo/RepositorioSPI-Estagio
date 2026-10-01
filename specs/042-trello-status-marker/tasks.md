---

description: "Lista de tarefas para Marcador de STATUS nos Cards do Trello"
---

# Tasks: Marcador de STATUS nos Cards do Trello

**Input**: Documentos de design em `/specs/042-trello-status-marker/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/status-line-convention.md](./contracts/status-line-convention.md), [contracts/sync-card-cli-delta.md](./contracts/sync-card-cli-delta.md), [contracts/extensions-yml-delta.md](./contracts/extensions-yml-delta.md), [quickstart.md](./quickstart.md)

**Tests**: research.md R7 mantém a decisão da spec 040 (sem suíte Pester/unitária para o script) — validação via `-DryRun` e `quickstart.md`, contra o quadro Trello real. Tarefas de validação manual carregam `(parte manual)`.

**Organization**: Tarefas agrupadas por user story (US1=P1 reescrita nas fases existentes; US2=P1 analyze/clarify; US3=P2 auto-preenchimento ao ler; US4=P3 retroativa; US5=P1 nunca travar).

**Nota estrutural importante**: quase todas as edições de código caem no **mesmo arquivo**, `.specify/hooks/trello/sync-card.ps1` — por isso a maioria das tarefas de implementação NÃO é `[P]`, mesmo pertencendo a user stories diferentes (edições sequenciais no mesmo arquivo colidiriam). Só `.claude/skills/trello-sync-card/SKILL.md` e `.specify/extensions.yml` são arquivos separados, e ganham `[P]` quando cabe.

**Cuidado de nomenclatura (aplica-se a T010/T011/T013)**: os blocos `design`/`a-fazer`/`em-andamento` e `revisao-codigo` já usam uma variável local `$detalhe` para o texto de log (ex.: "moveria card para..."). O texto novo da linha `STATUS:` MUST usar um nome diferente (ex.: `$statusDetalhe`) para não sombrear ou colidir com o `$detalhe` existente.

**User Story 3 não tem código de produção próprio** — a mesma lógica de extrair-e-reconstruir da US1 (T003/T004) já resolve "card sem STATUS ganha um" como consequência natural (research.md R1), então US3 só tem uma tarefa de validação.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1, US2, US3, US4 ou US5 (só nas fases de user story)
- Caminhos relativos à raiz do repositório `C:\PROJETO - SPI`

## Path Conventions

Ferramenta de processo (FR-013) — vive inteiramente em `.specify/hooks/trello/sync-card.ps1` e `.claude/skills/trello-sync-card/SKILL.md` (+ `.specify/extensions.yml`). Nenhum arquivo em `src/SPI.*`/`frontend/`/`database/`.

---

## Phase 1: Setup

**Purpose**: linha de base antes de qualquer edição.

- [X] T001 Rodar `.specify/hooks/trello/sync-card.ps1 -Fase revisao-codigo -DryRun` contra o quadro real, e confirmar que o comportamento bate com [specs/040-trello-sync-hooks/quickstart.md](../040-trello-sync-hooks/quickstart.md) — linha de base antes de estender o script (`-DryRun` não muta nada, então o alvo real — a própria feature 042, via `.specify/feature.json` — é irrelevante aqui; só interessa confirmar que o script ainda roda sem erro antes das edições).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: plumbing compartilhado que toda user story usa. Nenhuma user story começa antes desta fase.

**⚠️ CRITICAL**: sem estas funções, nenhuma fase nova ou reescrita de STATUS funciona.

- [X] T002 Em `.specify/hooks/trello/sync-card.ps1`: estender o `ValidateSet` do parâmetro `-Fase` para incluir `'analyze'`, `'clarify'`, `'retroativo'` (contracts/sync-card-cli-delta.md); adicionar o parâmetro opcional `[string]$Detalhe` (padrão vazio) ao bloco `param()`.
- [X] T003 Em `.specify/hooks/trello/sync-card.ps1`: implementar `Extrair-CorpoSemStatus($DescAtual)` — se a 1ª linha bate com `^STATUS:.*$`, remove essa linha e uma linha em branco imediatamente seguinte (se houver); caso contrário devolve `$DescAtual` inalterada (contracts/status-line-convention.md, passo 1).
- [X] T004 Em `.specify/hooks/trello/sync-card.ps1`: implementar `Montar-DescricaoComStatus($Detalhe, $Corpo)` — devolve `"STATUS: $Detalhe"` sozinho quando `$Corpo` está vazio/`$null`, ou `"STATUS: $Detalhe`n`n$Corpo"` caso contrário (contracts/status-line-convention.md, passo 2).
- [X] T005 Em `.specify/hooks/trello/sync-card.ps1`: implementar `Update-TrelloCardDescription($Cred, $CardId, $Desc)` — `PUT https://api.trello.com/1/cards/{id}?key=...&token=...&desc=<valor>`, com `<valor> = [System.Uri]::EscapeDataString($Desc)` (research.md R6 — diferente de `Move-TrelloCard`, `$Desc` pode ter espaços/acentos/quebras de linha e precisa de URL-encoding explícito). Mesmo padrão de `Move-TrelloCard`: sem `-Body`, tudo na query string.
- [X] T006 Em `.specify/hooks/trello/sync-card.ps1`: implementar dois helpers reaproveitando o padrão de `Get-TarefasBloqueantes`/`Get-ImplementationSummary` já existentes: `Obter-ContadorTarefas($FeatureId)` (devolve `@{Total=N; Concluidas=N}`, contando toda linha `- [ ]`/`- [X] T###` em `tasks.md`, com ou sem `(parte manual)`) e `Obter-TarefasManuaisPendentes($FeatureId)` (devolve a lista de IDs `T###` cujas linhas casam `- \[ \] T\d+` **e** contêm `(?i)\(parte manual\)`, ou seja, tarefas manuais ainda não marcadas).
- [X] T007 Em `.specify/hooks/trello/sync-card.ps1`: implementar `Obter-StatusDetalhePadrao($Fase, $FeatureId, $Pendentes, $TestesOk, $Movido)` seguindo a tabela de research.md R4: `backlog` → `"/speckit-specify concluído"`; `design` → `"/speckit-plan concluído"`; `a-fazer` → `"/speckit-tasks concluído, {Total} tarefas geradas"` (via T006); `em-andamento` → `"/speckit-implement rodando, {Concluidas}/{Total} tarefas concluídas"` (via T006); `revisao-codigo` com `$Movido = $true` → `"/speckit-implement concluído"`, ou `"/speckit-implement concluído, aguardando validação manual ({ids join '/'})"` quando `Obter-TarefasManuaisPendentes` devolver alguma; `revisao-codigo` com `$Movido = $false` → `"/speckit-implement rodando, {Pendentes} tarefa(s) pendente(s), testes {ok|falharam}"` (reaproveita `$Pendentes`/`$TestesOk` já calculados pela chamada, sem recalcular); `analyze` → `"/speckit-analyze concluído"`; `clarify` → `"/speckit-clarify concluído"`.
- [X] T008 Rodar `[System.Management.Automation.Language.Parser]::ParseFile(".specify/hooks/trello/sync-card.ps1", [ref]$tokens, [ref]$errors)` e confirmar `$errors.Count -eq 0` — checkpoint de sintaxe antes de ligar as user stories aos helpers novos.

**Checkpoint**: helpers prontos e sem erro de sintaxe — US1 a US4 podem começar.

---

## Phase 3: User Story 1 - Ver, em cada card, o último passo e a situação exata da feature (Priority: P1) 🎯 MVP

**Goal**: as 5 fases já existentes passam a reescrever a 1ª linha da descrição com `STATUS: <detalhe>`, sem tocar no resto.

**Independent Test**: rodar `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` para uma feature de teste e confirmar, a cada passo, que a 1ª linha muda para um `STATUS:` coerente, sem duplicar e sem alterar o resumo/marcador.

### Implementation for User Story 1

- [X] T009 [US1] Em `.specify/hooks/trello/sync-card.ps1`, branch `'backlog'` (só no ramo onde o card é **criado**, não no ramo "já existe" — data-model.md confirma que `STATUS:` não é reescrito num card já existente nesta fase): calcular `$statusDetalhe = if ($Detalhe) { $Detalhe } else { Obter-StatusDetalhePadrao -Fase 'backlog' -FeatureId $featureId }`; trocar `$desc = Get-SpecSummary -FeatureId $featureId` por `$corpo = Get-SpecSummary -FeatureId $featureId` seguido de `$desc = Montar-DescricaoComStatus -Detalhe $statusDetalhe -Corpo $corpo`, mantendo o restante do bloco (incluindo o `New-TrelloCard -Desc $desc`) inalterado.
- [X] T010 [US1] Em `.specify/hooks/trello/sync-card.ps1`, branch compartilhado `{ $_ -in @('design', 'a-fazer', 'em-andamento') }`, no ramo onde o card **é** encontrado: antes/junto de `Move-TrelloCard`, calcular `$corpoAtual = Extrair-CorpoSemStatus -DescAtual $card.desc`, `$statusDetalhe = if ($Detalhe) { $Detalhe } else { Obter-StatusDetalhePadrao -Fase $Fase -FeatureId $featureId } }`, `$novaDesc = Montar-DescricaoComStatus -Detalhe $statusDetalhe -Corpo $corpoAtual`, e chamar `Update-TrelloCardDescription -Cred $cred -CardId $card.id -Desc $novaDesc` quando `-not $DryRun` (mesma condição já usada para `Move-TrelloCard`).
- [X] T011 [US1] Em `.specify/hooks/trello/sync-card.ps1`, branch `'revisao-codigo'`, **nos dois sub-ramos** (moveu e não moveu): calcular `$corpoAtual = Extrair-CorpoSemStatus -DescAtual $card.desc` e ``$statusDetalhe = if ($Detalhe) { $Detalhe } else { Obter-StatusDetalhePadrao -Fase 'revisao-codigo' -FeatureId $featureId -Pendentes $pendentes -TestesOk $testesOk -Movido <$true no sub-ramo que move, $false no que não move> }` (reaproveitando `$pendentes`/`$testesOk` já calculados no topo do branch, sem recalcular; ver detalhamento de cada sub-ramo logo abaixo); no sub-ramo que **move** o card: calcular `$statusDetalhe` chamando `Obter-StatusDetalhePadrao` com `-Movido $true`, e chamar `Update-TrelloCardDescription` junto de `Move-TrelloCard`/`Add-TrelloComment` (dentro do `if (-not $DryRun)`); no sub-ramo que **não** move: calcular `$statusDetalhe` chamando `Obter-StatusDetalhePadrao` com `-Movido $false`, e chamar `Update-TrelloCardDescription` mesmo assim (o STATUS reflete o motivo do bloqueio, mesmo sem o card se mover — FR-003).
- [ ] T012 [US1] (parte manual) Validar User Story 1 rodando [quickstart.md](./quickstart.md) §1 contra um quadro Trello de teste real: confirmar `STATUS:` correto em cada fase, sem duplicar em execuções repetidas, com resumo e marcador `Feature: <id>` intactos.

**Checkpoint**: User Story 1 funcional e testável de forma independente — MVP entregue.

---

## Phase 4: User Story 2 - Status atualizado também após /speckit-analyze e /speckit-clarify (Priority: P1)

**Goal**: `-Fase analyze`/`-Fase clarify` atualizam só o `STATUS:` do card da feature atual, sem mover de lista.

**Independent Test**: numa feature com card, rodar `sync-card.ps1 -Fase clarify` e depois `-Fase analyze` e confirmar que o `STATUS:` muda a cada um, sem o card mudar de coluna (quickstart.md §2).

### Implementation for User Story 2

- [X] T013 [US2] Em `.specify/hooks/trello/sync-card.ps1`: adicionar o branch `{ $_ -in @('analyze', 'clarify') }` no `switch ($Fase)`: `$cred = Get-TrelloCredentials`; `$featureId = Get-FeatureId`; `$cards = Get-TrelloCards -Cred $cred`; `$card = Find-FeatureCard -Cards $cards -FeatureId $featureId`; se `$card` não existir, log/skip no mesmo padrão de `design`/`a-fazer`/`em-andamento` ("card não encontrado, nada a fazer"); se existir, `$corpoAtual = Extrair-CorpoSemStatus -DescAtual $card.desc`, `$statusDetalhe = if ($Detalhe) { $Detalhe } else { Obter-StatusDetalhePadrao -Fase $Fase -FeatureId $featureId } }`, `$novaDesc = Montar-DescricaoComStatus -Detalhe $statusDetalhe -Corpo $corpoAtual`, `Update-TrelloCardDescription` quando `-not $DryRun`. Este branch **nunca** chama `Get-TrelloLists` nem `Move-TrelloCard` (FR-004 — nunca move de lista).
- [X] T014 [P] [US2] Em `.specify/extensions.yml`: adicionar as entradas `after_analyze` e `after_clarify` exatamente como em [contracts/extensions-yml-delta.md](./contracts/extensions-yml-delta.md) (`extension: "trello"`, `command: "trello.sync-card"`, `prompt: "analyze"`/`"clarify"`, `optional: false`, sem `condition`), preservando as 5 entradas já existentes.
- [X] T015 [P] [US2] Em `.claude/skills/trello-sync-card/SKILL.md`: documentar o formato de argumento `<fase>[: <detalhe>]`, listar `analyze`, `clarify` e `retroativo` como fases válidas (além das 5 já documentadas), e registrar a convenção de research.md R3 — quando o agente que dispara o hook após `/speckit-analyze`/`/speckit-clarify` tiver um detalhe relevante da conversa (contagem de achados, perguntas respondidas), ele invoca a skill com `"analyze: <detalhe>"`/`"clarify: <detalhe>"` em vez do argumento literal `"analyze"`/`"clarify"`.
- [ ] T016 [US2] (parte manual) Validar User Story 2 rodando [quickstart.md](./quickstart.md) §2: `clarify`/`analyze` com e sem `-Detalhe`, confirmando que o card nunca muda de lista.

**Checkpoint**: US1 e US2 funcionam juntas — toda fase (existente ou nova) mantém o `STATUS:` correto.

---

## Phase 5: User Story 3 - Card sem STATUS ganha um automaticamente ao ser lido (Priority: P2)

**Goal**: um card de feature já existente, sem `STATUS:`, ganha a linha na próxima vez que qualquer fase o tocar — sem nenhum código novo (consequência de T003/T004/T010/T011/T013, que sempre reconstroem a descrição a partir do que já está lá).

**Independent Test**: remover manualmente o `STATUS:` de um card de feature (via Trello), rodar qualquer fase dessa feature, confirmar que ele reaparece com resumo/marcador intactos (quickstart.md §3).

- [ ] T017 [US3] (parte manual) Validar User Story 3 rodando [quickstart.md](./quickstart.md) §3. **Inclui a metade negativa de FR-007**: antes de rodar a fase de teste, anotar (copiar) a descrição de um card **manual** não relacionado (sem marcador `Feature:`) já existente no quadro; depois de rodar a fase sobre a feature de teste, confirmar que a descrição desse card manual continua **idêntica**, byte a byte — nenhuma fase além de `retroativo` (US4) pode tocá-lo.

**Checkpoint**: US1, US2 e US3 completas.

---

## Phase 6: User Story 4 - Cards já existentes recebem o STATUS manual retroativamente (Priority: P3)

**Goal**: `-Fase retroativo` insere `STATUS: card criado manualmente, sem spec formal ainda` em todo card sem o marcador `Feature: <id>` que ainda não tenha `STATUS:`, uma única vez, de forma idempotente.

**Independent Test**: rodar `-Fase retroativo` contra o quadro real e conferir que os cards manuais (hoje 12) ganham a linha; rodar de novo e confirmar que nada muda (quickstart.md §4).

### Implementation for User Story 4

- [X] T018 [US4] Em `.specify/hooks/trello/sync-card.ps1`: adicionar o branch `'retroativo'` no `switch ($Fase)` — **não** chama `Get-FeatureId` (não é sobre a feature atual). `$cred = Get-TrelloCredentials`; `$cards = Get-TrelloCards -Cred $cred`; para cada card em `$cards` cuja `desc` **não** termina em `Feature: \S+` (regex genérica `'Feature:\s*\S+\s*$'`, sem casar um id específico) **e** cuja 1ª linha não bate `^STATUS:`, montar `$novaDesc = Montar-DescricaoComStatus -Detalhe "card criado manualmente, sem spec formal ainda" -Corpo $_.desc` e chamar `Update-TrelloCardDescription` quando `-not $DryRun`; contar `$atualizados`/`$jaTinhaStatus`; log e stdout resumindo (`"[trello-sync] retroativo: {atualizados} cards atualizados, {jaTinhaStatus} ja tinham STATUS"`).
- [ ] T019 [US4] (parte manual) Validar User Story 4 rodando [quickstart.md](./quickstart.md) §4 contra o quadro real: confirmar que os 12 cards manuais ganham `STATUS:` e que uma segunda execução não altera nada.

**Checkpoint**: US1 a US4 completas.

---

## Phase 7: User Story 5 - O Spec Kit nunca trava por causa do STATUS (Priority: P1)

**Goal**: garantir que os branches novos (`analyze`/`clarify`/`retroativo`) e as chamadas novas de `Update-TrelloCardDescription` dentro dos branches existentes nunca escapam do wrapper `try/catch` global nem do `exit 0` (mesma garantia da spec 040, estendida ao STATUS).

**Independent Test**: com uma credencial inválida, rodar `-Fase clarify` e `-Fase retroativo` e confirmar que ambos completam com `exit 0` e o erro fica só no log (quickstart.md §5).

- [X] T020 [US5] Revisar `.specify/hooks/trello/sync-card.ps1` confirmando que os branches `analyze`/`clarify` (T013) e `retroativo` (T018), e as chamadas novas de `Update-TrelloCardDescription` dentro de `backlog`/`design`/`a-fazer`/`em-andamento`/`revisao-codigo` (T009-T011), estão todos dentro do escopo do `try` global criado na spec 040 — nenhum caminho de saída próprio que contorne o `exit 0` final. Auditoria/ajuste do código já escrito, não uma branch nova.
- [ ] T021 [US5] (parte manual) Validar User Story 5 rodando [quickstart.md](./quickstart.md) §5 (`TRELLO_TOKEN` inválido, `-Fase clarify`, confirmar `LASTEXITCODE = 0` e log) e confirmar que `/speckit-clarify` real continua completando normalmente com a credencial inválida ainda ativa.

**Checkpoint**: todas as 5 user stories completas e validadas de forma independente.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão e verificações finais que atravessam todas as user stories.

- [ ] T022 [P] (parte manual) Rodar [quickstart.md](./quickstart.md) §6 (não-regressão): repetir os cenários de criação/movimentação/comentário do quickstart da spec 040 (agora com `STATUS:` a mais) e confirmar `git diff --stat -- src frontend database` vazio (FR-013).
- [X] T023 [P] `Select-String`/leitura confirmando que o branch `'retroativo'` nunca chama `Get-FeatureId` nem lê `.specify/feature.json`, e que nenhum branch usa o resultado de `Get-TrelloCards`/`Find-FeatureCard` para decidir pular, repetir ou alterar o comportamento de um comando `/speckit-*` (FR-011 — a leitura serve só para montar `$corpoAtual`).
- [X] T024 Rodar `-DryRun` nas 8 fases válidas (`backlog`, `design`, `a-fazer`, `em-andamento`, `revisao-codigo`, `analyze`, `clarify`, `retroativo`) contra o quadro real, confirmando que nenhuma falha ao executar em modo seco — checkpoint final antes de considerar a feature pronta para revisão.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências — roda primeiro.
- **Foundational (Phase 2)**: depende do Setup — BLOQUEIA todas as user stories (T002-T007 tocam o mesmo arquivo, então rodam em sequência, não em paralelo; T008 fecha a fase).
- **US1 (Phase 3)**: depende só da Foundational — é o MVP.
- **US2 (Phase 4)**: depende da Foundational; T013 (mesmo arquivo de US1) roda depois de US1 estar no arquivo, mas não depende logicamente do conteúdo de US1 — só da ordem de edição no mesmo arquivo.
- **US3 (Phase 5)**: depende de US1 (T003/T004/T010) já estarem implementadas — é validação pura, sem código.
- **US4 (Phase 6)**: depende só da Foundational (T018 é um branch independente).
- **US5 (Phase 7)**: depende de US1, US2 e US4 (T020 audita o código que essas três produziram).
- **Polish (Phase 8)**: depende de todas as user stories.

### Parallel Opportunities

- Dentro da Foundational: nenhuma (todas em `sync-card.ps1`).
- US2: T014 (`extensions.yml`) e T015 (`SKILL.md`) em paralelo entre si e com T013.
- Polish: T022 e T023 em paralelo.

---

## Implementation Strategy

### MVP First (User Story 1 apenas)

1. Setup (T001) → Foundational (T002-T008) — CRÍTICO, bloqueia tudo.
2. US1 (T009-T012) → **PARAR e VALIDAR**: toda fase existente já mostra `STATUS:` correto.

### Incremental Delivery

1. Foundational → plumbing pronto.
2. US1 → STATUS nas 5 fases existentes (MVP).
3. US2 → `analyze`/`clarify` sem mover de lista.
4. US3 → confirmar que cards antigos se auto-corrigem (sem código novo).
5. US4 → retroativa em lote para os 12 cards manuais.
6. US5 → auditoria final de resiliência.
7. Polish → não-regressão + verificação dos requisitos negativos + `-DryRun` completo.
