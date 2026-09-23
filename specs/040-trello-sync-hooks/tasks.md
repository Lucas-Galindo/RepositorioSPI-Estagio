# Tasks: Sincronização Automática Spec Kit → Trello

**Input**: Design documents from `specs/040-trello-sync-hooks/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: research.md R8 decidiu explicitamente NÃO gerar testes automatizados (Pester/unit) para `sync-card.ps1` — a validação é feita via `-DryRun` e os cenários de [quickstart.md](./quickstart.md), rodados manualmente contra um quadro Trello de teste. Por isso não há fase "Tests" separada: cada user story termina numa tarefa de validação via quickstart.

**Organization**: Tasks agrupadas pelas 4 user stories de [spec.md](./spec.md) (US1/US2/US4 = P1, US3 = P2).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1, US2, US3, US4 — mapeia a tarefa à user story de spec.md
- Caminhos de arquivo são sempre absolutos a partir da raiz do repositório (`.specify/...`, `.claude/...`)

## Nota estrutural (mesmo padrão usado nas specs 038/039)

`sync-card.ps1` é um único script despachado por `-Fase` (research.md R2: um só comando `trello.sync-card`, diferenciado pelo `prompt`). O núcleo compartilhado (autenticação, log, wrapper `try/catch`+`exit 0`, helpers de API) é construído uma única vez na Fase 2 (Foundational) porque toda user story depende dele para ser testável — não é possível "entregar" esse núcleo fatiado por story sem recriar retrabalho. Cada user story então adiciona **só o branch de `-Fase` que lhe compete** dentro do mesmo arquivo, e termina com uma tarefa de validação via quickstart.md (não uma tarefa de implementação nova). US4 (nunca travar o Spec Kit) é uma propriedade transversal já embutida no wrapper desde a Fase 2 — sua tarefa própria (T014) audita explicitamente que os branches adicionados por US1/US2/US3 não quebraram essa garantia, e sua validação (T015) prova isso fim a fim.

---

## Phase 1: Setup

**Purpose**: Preparar o terreno com segurança antes de qualquer segredo existir no disco.

- [X] T001 Adicionar ao `.gitignore` da raiz do repositório as entradas `.specify/hooks/trello/.env` e `.specify/hooks/trello/*.log` (FR-012, SC-005) — deve ser a primeira alteração, antes de `.env.example`/script existirem, para que nenhum arquivo de segredo real corra risco de ser rastreado mesmo por um instante.

**Checkpoint**: `.gitignore` protege os dois caminhos sensíveis antes de qualquer outro arquivo da feature ser criado.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Núcleo compartilhado que TODAS as user stories exigem para ser testável.

**⚠️ CRITICAL**: Nenhuma user story pode ser validada antes desta fase estar completa.

- [X] T002 [P] Criar `.specify/hooks/trello/.env.example` com as 3 chaves documentadas em contracts/sync-card-cli.md ("Entradas implícitas"): `TRELLO_API_KEY=`, `TRELLO_TOKEN=`, `TRELLO_BOARD_ID=` — sem nenhum valor real, só os nomes das variáveis e um comentário de exemplo (research.md R9, mesmo padrão de `frontend/.env.local`).
- [X] T003 [P] Criar `.specify/hooks/trello/README.md` documentando: como obter API Key + Token do Trello (passo de setup do ambiente, per Assumptions de spec.md), como obter o `TRELLO_BOARD_ID` do quadro "Estágio SPI", como preencher `.env` a partir de `.env.example`, e a lista das 5 fases válidas de `-Fase` (contracts/sync-card-cli.md).
- [X] T004 Criar `.specify/hooks/trello/sync-card.ps1` com o núcleo compartilhado:
  - Parâmetros `-Fase` (obrigatório, valores válidos: `backlog`, `design`, `a-fazer`, `em-andamento`, `revisao-codigo` — contracts/sync-card-cli.md) e `-DryRun` (switch, opcional).
  - Carregamento de credenciais: variáveis de ambiente `TRELLO_API_KEY`/`TRELLO_TOKEN`/`TRELLO_BOARD_ID`, com fallback para `.specify/hooks/trello/.env` (research.md R9).
  - Leitura de `FeatureId` a partir de `.specify/feature.json` (campo `feature_directory`, sem o prefixo `specs/` — data-model.md "Identificador de feature").
  - Função de log `Write-SyncLog` gravando em `.specify/hooks/trello/sync.log` com timestamp, fase e resultado (contracts/sync-card-cli.md "Log").
  - Wrapper `try/catch` global: qualquer exceção (timeout, `401`/`403`, `404`, corpo inesperado — contracts/trello-api.md "Falhas tratadas") é capturada, logada, e o script **sempre** termina com `exit 0` (FR-011, research.md R6) — este é o contrato central da feature e deve envolver todo o corpo do script, não só os helpers de API.
  - Helpers de API do Trello (autenticação sempre via query string `?key={TRELLO_API_KEY}&token={TRELLO_TOKEN}`, nunca em header — contracts/trello-api.md): `Get-TrelloLists` (`GET /1/boards/{TRELLO_BOARD_ID}/lists`, mapa nome→id), `Get-TrelloCards` (`GET /1/boards/{TRELLO_BOARD_ID}/cards?fields=name,desc,idList`), `Find-FeatureCard` (localiza o card cuja `desc` termina em `Feature: <FeatureId>` — data-model.md).
  - Dispatcher por `-Fase`: para qualquer fase ainda não implementada nesta tarefa, apenas loga "fase reconhecida, lógica pendente" e sai (placeholder a ser substituído pelas tarefas de cada user story).
  - Quando `-DryRun` estiver presente, toda a lógica de busca/decisão roda normalmente, mas nenhuma chamada `POST`/`PUT` de escrita é feita — só é impresso o que seria feito (contracts/sync-card-cli.md).
- [X] T005 [P] Criar `.claude/skills/trello-sync-card/SKILL.md`: skill fina que recebe a fase via argumento (`$ARGUMENTS`), resolve o caminho absoluto de `.specify/hooks/trello/sync-card.ps1` a partir da raiz do repositório, e o invoca com `-Fase <fase>` (repassando `-DryRun` se presente no argumento). Documentar explicitamente que a skill nunca trata um erro reportado pelo script como falha do comando do Spec Kit que a invocou (o contrato de `exit 0` de contracts/sync-card-cli.md já garante isso) — a skill apenas repassa a linha de stdout como uma nota breve ao usuário.
- [X] T006 Criar `.specify/extensions.yml` com as 5 entradas exatas de [contracts/extensions-yml.md](./contracts/extensions-yml.md) (`after_specify`, `after_plan`, `after_tasks`, `before_implement`, `after_implement`), todas com `extension: "trello"`, `command: "trello.sync-card"`, `optional: false`, e o campo `prompt` carregando a fase (`backlog`/`design`/`a-fazer`/`em-andamento`/`revisao-codigo` respectivamente) — nenhuma entrada usa `condition` (research.md R3, Constitution Principle III).

**Checkpoint**: `sync-card.ps1 -Fase backlog -DryRun` roda sem erro e imprime o que faria; `.specify/extensions.yml` existe e será lido pelas próximas invocações de `/speckit-specify`, `/speckit-plan`, `/speckit-tasks`, `/speckit-implement`.

---

## Phase 3: User Story 1 - Ver o Backlog do Trello se preencher sozinho ao especificar uma feature (Priority: P1) 🎯 MVP

**Goal**: Um card aparece automaticamente em "Backlog" quando `/speckit-specify` termina, sem duplicar em execuções repetidas.

**Independent Test**: Rodar `/speckit-specify` para uma feature nova e confirmar (quickstart.md §2, passos 1-2) que um card com o título certo aparece em "Backlog", com a descrição terminando em `Feature: <id>`, e que rodar de novo para a mesma feature não cria um segundo card.

### Implementation for User Story 1

- [X] T007 [US1] Implementar o branch `-Fase backlog` em `.specify/hooks/trello/sync-card.ps1`: chamar `Find-FeatureCard` primeiro (FR-003 — toda criação MUST verificar existência antes); se encontrado, não fazer nada (idempotente, data-model.md "Efeito por fase"); se não encontrado, ler o título da feature na primeira linha `# Feature Specification: <título>` de `specs/<feature>/spec.md` (data-model.md "Nome do card"), montar a descrição como um resumo do spec seguido da última linha fixa `Feature: <FeatureId>` (FR-001/FR-002), e `POST /1/cards` na lista "Backlog" (resolvida via `Get-TrelloLists` por nome exato).
- [X] T008 [US1] (parte manual) Validar User Story 1 rodando [quickstart.md](./quickstart.md) §1 (`-DryRun`) e §2 passos 1-2, contra um quadro Trello de teste real — confirmar card único criado com título e marcador `Feature: <id>` corretos, e ausência de duplicata numa segunda execução (SC-001, SC-002).

**Checkpoint**: User Story 1 funcional e testável de forma independente — MVP entregue.

---

## Phase 4: User Story 2 - Acompanhar o card se mover sozinho conforme o trabalho avança (Priority: P1)

**Goal**: O card se move automaticamente por Design → A Fazer → Em andamento conforme `/speckit-plan`, `/speckit-tasks` e `/speckit-implement` (início) rodam.

**Independent Test**: Rodar `/speckit-plan`, `/speckit-tasks`, `/speckit-implement` (início) para uma feature com card já criado e confirmar (quickstart.md §2, passos 3-5) as 3 movimentações, cada uma localizando o card certo pelo identificador.

### Implementation for User Story 2

- [X] T009 [US2] Implementar os branches `-Fase design`, `-Fase a-fazer` e `-Fase em-andamento` em `.specify/hooks/trello/sync-card.ps1`: cada um chama `Find-FeatureCard`; se não encontrado, loga "card não encontrado" e não faz nada (FR-011, data-model.md linha "Card não existe" — mesmo tratamento de falha de US4, não é um caminho de erro novo); se encontrado, `PUT /1/cards/{id}` com o `idList` da lista de destino resolvida por nome ("Design", "A Fazer", "Em andamento" respectivamente — FR-004/FR-005/FR-006). Repetir a mesma fase mais de uma vez para o mesmo card MUST deixá-lo na lista já alcançada, sem duplicar nem regredir (Acceptance Scenario 4 de US2).
- [X] T010 [US2] (parte manual) Validar User Story 2 rodando [quickstart.md](./quickstart.md) §2 passos 3-5 contra o mesmo card criado em T008 — confirmar as 3 movimentações na ordem certa (SC-004).

**Checkpoint**: User Stories 1 e 2 funcionam juntas e de forma independente — o card acompanha visivelmente Backlog → Design → A Fazer → Em andamento.

---

## Phase 5: User Story 3 - Ver um resumo do que foi implementado quando o card chega em Revisão de código (Priority: P2)

**Goal**: Ao `/speckit-implement` terminar com sucesso, o card se move para "Revisão de código" com um comentário-resumo; caso contrário, permanece em "Em andamento" com o motivo registrado.

**Independent Test**: Rodar `/speckit-implement` até o fim para uma feature com card em "Em andamento" e confirmar (quickstart.md §2, passos 6-7) tanto o caminho de sucesso quanto o caminho bloqueado (FR-007a).

### Implementation for User Story 3

- [X] T011 [US3] Implementar em `.specify/hooks/trello/sync-card.ps1` a contagem de `TarefasBloqueantes` para a fase `revisao-codigo`: ler `specs/<feature>/tasks.md` e contar as linhas `- [ ] T###` que **não** contêm o marcador `(parte manual)` (case-insensitive) — tarefas de validação manual MUST ser excluídas dessa contagem (FR-007, research.md R7, data-model.md `TarefasBloqueantes`).
- [X] T012 [US3] Implementar em `.specify/hooks/trello/sync-card.ps1` a checagem `TestesPassaram` para a fase `revisao-codigo`: rodar `dotnet test "tests/SPI.Application.Tests"` e capturar o código de saída (data-model.md `TestesPassaram`, research.md R7).
- [X] T013 [US3] Implementar o branch `-Fase revisao-codigo` em `.specify/hooks/trello/sync-card.ps1` usando os resultados de T011/T012: chamar `Find-FeatureCard`; se não encontrado, mesmo tratamento de US4 (log + segue); se encontrado e `TarefasBloqueantes == 0` **e** `TestesPassaram`, `PUT /1/cards/{id}` movendo para "Revisão de código" **e** `POST /1/cards/{id}/actions/comments` com um resumo derivado de `tasks.md` (quais fases/tarefas foram concluídas — não texto do chat, per Assumptions de spec.md) (FR-007/FR-008); caso contrário, **não** mover, e logar o motivo específico (quantas tarefas pendentes e/ou testes falhando) como as demais falhas de sincronização (FR-007a, FR-011).
- [X] T014 [US3] (parte manual) Validar User Story 3 rodando [quickstart.md](./quickstart.md) §2 passos 6-7 contra o mesmo card — confirmar o caminho de sucesso (card movido + comentário novo) e o caminho bloqueado (card permanece em "Em andamento", motivo no `sync.log`).

**Checkpoint**: As 5 movimentações automáticas (Backlog → Design → A Fazer → Em andamento → Revisão de código, condicional) funcionam de ponta a ponta.

---

## Phase 6: User Story 4 - O Spec Kit nunca trava por causa do Trello (Priority: P1)

**Goal**: Garantir, para os 3 branches recém-adicionados nas Fases 3-5, que nenhuma falha do Trello (indisponibilidade, credencial inválida, card/quadro/lista não encontrado) jamais propaga uma exceção não tratada nem um código de saída diferente de `0`.

**Independent Test**: Configurar uma credencial inválida (ou derrubar conectividade) e rodar os 4 comandos do Spec Kit normalmente, confirmando (quickstart.md §3) que todos completam seu trabalho e o erro fica só no log.

### Implementation for User Story 4

- [X] T015 [US4] Revisar `.specify/hooks/trello/sync-card.ps1` confirmando que os branches `backlog` (T007), `design`/`a-fazer`/`em-andamento` (T009) e `revisao-codigo` (T011-T013) estão todos dentro do escopo do wrapper `try/catch` global criado em T004 — nenhum deles pode ter um caminho de saída próprio que contorne o `exit 0` final (FR-011). Esta tarefa é uma auditoria/ajuste do código já escrito, não uma nova branch de fase.
- [X] T016 [US4] (parte manual) Validar User Story 4 rodando [quickstart.md](./quickstart.md) §3 (`TRELLO_TOKEN` inválido, `-Fase backlog`, confirmar `LASTEXITCODE = 0` e entrada de erro em `sync.log`) e depois confirmar que `/speckit-specify` completa normalmente com a mesma credencial inválida ainda ativa, sem nenhuma mensagem de erro do Spec Kit em si (SC-003).

**Checkpoint**: Todas as 4 user stories completas e validadas de forma independente.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verificações finais que atravessam todas as user stories.

- [X] T017 [P] Rodar [quickstart.md](./quickstart.md) §4 (segurança, SC-005): `git status --short -- .specify/hooks/trello/.env .specify/hooks/trello/sync.log` deve retornar vazio ou `??` (nunca `A`/`M`); confirmar por leitura que `sync-card.ps1` não contém nenhum valor literal de API Key/Token.
- [X] T018 [P] Rodar [quickstart.md](./quickstart.md) §5 (não-regressão, FR-013): `git diff --stat -- src frontend database` deve retornar vazio — esta feature não toca nenhum código de domínio do SPI.
- [X] T019 (parte manual) Rodar o ciclo completo de [quickstart.md](./quickstart.md) §2 numa única sequência contínua (specify → plan → tasks → implement) para uma feature de teste descartável, confirmando as 5 movimentações em ordem numa única passada de ponta a ponta (SC-004) — valida a integração real dos hooks em `.specify/extensions.yml` (T006) disparando `/trello-sync-card` (T005) a cada fase, e não só cada branch isoladamente como nas validações T008/T010/T014/T016.
- [X] T020 [P] Rodar `Select-String` em `.specify/hooks/trello/sync-card.ps1` confirmando (a) nenhuma ocorrência de "Fase de teste" ou "Concluído" como valor resolvido/atribuído a `idList` (FR-009) e (b) nenhuma chamada de escrita em arquivo do repositório (`Set-Content`, `Out-File`, `Add-Content`, `>`/`>>` redirecionado para um caminho fora de `.specify/hooks/trello/sync.log`) — confirmando que o script só lê `spec.md`/`tasks.md`/`.specify/feature.json` e nunca grava de volta no repositório (FR-010).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências — roda primeiro (segurança do `.gitignore` antes de qualquer segredo existir).
- **Foundational (Phase 2)**: Depende do Setup — BLOQUEIA todas as user stories (T004 é o núcleo que T007/T009/T011-013/T015 editam).
- **User Story 1 (Phase 3)**: Depende só da Foundational — é o MVP.
- **User Story 2 (Phase 4)**: Depende da Foundational; sua validação (T010) reutiliza o card criado em T008, então roda depois de US1 na prática, mas sua implementação (T009) não depende do código de T007.
- **User Story 3 (Phase 5)**: Depende da Foundational; sua validação (T014) reutiliza o card movido em T010.
- **User Story 4 (Phase 6)**: Depende de T007+T009+T011-013 existirem (T015 audita esse código) — é a única story cuja implementação depende do código de outras stories, por ser uma propriedade transversal.
- **Polish (Phase 7)**: Depende de todas as user stories completas.

### Parallel Opportunities

- T002, T003, T005 podem rodar em paralelo entre si (arquivos diferentes) e em paralelo com T004 (T004 é o único que não pode ser paralelo pois é o maior/central).
- T017, T018 e T020 podem rodar em paralelo (comandos `git`/`Select-String` independentes, todos só leitura).

---

## Implementation Strategy

### MVP First (User Story 1 apenas)

1. Completar Phase 1: Setup (T001)
2. Completar Phase 2: Foundational (T002-T006) — CRÍTICO, bloqueia tudo
3. Completar Phase 3: User Story 1 (T007-T008)
4. **PARAR e VALIDAR**: card aparece em "Backlog" sem duplicar
5. Já é demonstrável: todo `/speckit-specify` futuro cria o card automaticamente

### Incremental Delivery

1. Setup + Foundational → base pronta
2. US1 → card nasce sozinho (MVP)
3. US2 → card se move sozinho pelas 3 fases intermediárias
4. US3 → card chega em "Revisão de código" com resumo (ou fica retido, corretamente)
5. US4 → auditoria final de resiliência sobre tudo que foi construído
6. Polish → segurança + não-regressão + prova de ponta a ponta
