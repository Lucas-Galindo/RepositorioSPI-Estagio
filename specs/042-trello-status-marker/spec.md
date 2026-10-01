# Feature Specification: Marcador de STATUS nos Cards do Trello

**Feature Branch**: `042-trello-status-marker`

**Created**: 2026-09-23

**Status**: Draft

**Input**: User description: "Estender a integração Spec Kit → Trello (spec 040) para incluir um marcador 'STATUS: ...' na primeira linha da descrição de cada card, indicando o estado detalhado da feature — o último comando /speckit-* executado e a situação exata em que ficou (ex.: 'STATUS: /speckit-analyze concluído, 2 achados aguardando decisão', 'STATUS: /speckit-implement rodando, 8/12 tarefas concluídas', 'STATUS: /speckit-implement concluído, aguardando validação manual (T018/T019)'). Atualizar a cada hook existente (after_specify, after_plan, after_tasks, after_implement) e também após /speckit-analyze e /speckit-clarify, que hoje não disparam hook — avaliar em research.md se isso exige registrar hooks novos. Falha na atualização não pode travar o Spec Kit. Ao ler um card cuja descrição não segue o padrão 'STATUS: ...' na primeira linha (ex.: criado manualmente), adicionar a linha automaticamente com o melhor entendimento do estado da feature correspondente (ou 'STATUS: card criado manualmente, sem spec formal ainda' quando não houver feature numerada). Atualizar retroativamente os 4 cards de 'Visão do Aluno' do Backlog com esse status manual. Fora de escopo: leitura recorrente do Trello disparar ações no Spec Kit."

## Nota de investigação prévia

Confirmado por leitura do projeto e do quadro real (grounding antes de especificar):

- **Como a integração 040 trata a descrição hoje**: `sync-card.ps1` cria o card com uma descrição = resumo do spec + uma última linha fixa `Feature: <identificador>` (é assim que o card é localizado, sem duplicar), e **nunca mais reescreve a descrição** depois — só move o card de coluna e, no fim do implement, adiciona um comentário. Esta feature é a primeira que passa a **editar a descrição** de um card existente; por isso a linha `STATUS:` (primeira linha) precisa conviver com o marcador `Feature:` (última linha) sem que uma edição destrua a outra.
- **Eventos de hook**: as skills `speckit-analyze` e `speckit-clarify` já documentam, exatamente como as demais, os passos `hooks.before_analyze`/`hooks.after_analyze` e `hooks.before_clarify`/`hooks.after_clarify` lendo `.specify/extensions.yml`. Ou seja, o mecanismo (dirigido por prompt, sem executor externo — ver spec 040) **já suporta** esses dois eventos; falta apenas registrá-los. `.specify/extensions.yml` hoje registra só 5 eventos (`after_specify`, `after_plan`, `after_tasks`, `before_implement`, `after_implement`). A skill `speckit-analyze` é declaradamente somente-leitura sobre os arquivos do repositório; um hook que escreve no Trello (sistema externo) não altera nenhum arquivo do repositório, mas isso é um ponto a registrar no plano.
- **Não há evento entre tarefas**: durante um `/speckit-implement` não existe hook a cada tarefa concluída — só `before_implement` (início) e `after_implement` (fim). Um status "rodando, 8/12" no meio da execução não tem evento próprio que o dispare.
- **Estado real do quadro "Estágio SPI" (lido hoje, somente leitura)**: 13 cards. Apenas 1 (o da própria feature 041) carrega o marcador `Feature: <id>`; os outros 12 são cards manuais, sem marcador e sem spec numerada — 8 em "A Fazer", 1 em "Backlog", 1 em "Em andamento", 2 em "Revisão de código". **Não existe nenhum card com nome ou etiqueta "Visão do Aluno"** no quadro (o Backlog tem um único card, "Colocar PonyTail e Caveman como skill"; não há lista arquivada nem card arquivado com esse nome). O pedido 5 parte de uma premissa ("4 cards de Visão do Aluno no Backlog") que não bate com o quadro atual — ver FR-009.
- **Sem mudança de credenciais ou de escopo de produto**: continua fora de `src/SPI.*` e `frontend/`; mesma autenticação (variáveis de ambiente / `.env` local gitignored) da spec 040.

## Clarifications

### Session 2026-09-23

- Q: Quando a integração lê o quadro (leitura já existente, sem polling novo), quais cards ganham o STATUS ao ser lidos sem tê-lo? → A: Só o card da feature em execução naquele comando — cards manuais alheios só ganham STATUS pela atualização retroativa (US4), não a cada leitura.
- Q: O STATUS pode conter informação que só existe na conversa (ex.: "2 achados aguardando decisão" no analyze), ou só o que dá para derivar dos arquivos do repositório? → B: Híbrido — o comando que dispara o hook repassa um detalhe curto quando houver algo relevante na conversa (achados do analyze, perguntas respondidas no clarify); na ausência desse detalhe, o STATUS cai no que é derivado dos arquivos.
- Q: Qual é o alvo real da atualização retroativa (US4), já que não existem cards "Visão do Aluno" no quadro hoje? → A: Os 12 cards manuais que existem hoje no quadro (todo card sem o marcador `Feature: <id>`), não um subconjunto por lista nem cards ainda a serem criados.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver, em cada card, o último passo e a situação exata da feature (Priority: P1)

Como responsável pelo projeto, quero que a primeira linha da descrição de cada card mostre um `STATUS:` com o último comando do Spec Kit executado e a situação em que a feature ficou, para saber olhando só o quadro — sem abrir o repositório — o que está pronto, o que está esperando decisão e o que está pendente.

**Why this priority**: É o valor central do pedido. A coluna sozinha só diz "em qual fase"; o STATUS diz "em que ponto exato dessa fase" (ex.: implementação terminou, mas restam validações manuais).

**Independent Test**: Rodar `/speckit-specify` e depois `/speckit-plan` e `/speckit-tasks` para uma feature nova e confirmar, a cada passo, que a primeira linha da descrição do card muda para um `STATUS:` coerente com o último comando, sem tocar no resto da descrição.

**Acceptance Scenarios**:

1. **Given** uma feature recém-especificada, **When** `/speckit-specify` termina, **Then** o card criado tem `STATUS: /speckit-specify concluído` (ou equivalente) na primeira linha, seguido do resumo do spec e, na última linha, o marcador `Feature: <id>` de sempre.
2. **Given** um card existente, **When** `/speckit-plan` termina, **Then** a primeira linha passa a refletir que o plano foi concluído, o card muda para "Design" como já acontece hoje, e o resumo e o marcador `Feature:` permanecem exatamente como estavam.
3. **Given** um card em "A Fazer" com `tasks.md` gerado, **When** o status é escrito após `/speckit-tasks`, **Then** ele informa o total de tarefas geradas (ex.: "31 tarefas").
4. **Given** `/speckit-implement` começa, **When** o card vai para "Em andamento", **Then** o STATUS indica que a implementação está rodando com o progresso de tarefas naquele instante.
5. **Given** `/speckit-implement` termina, **When** o card vai para "Revisão de código" (ou fica em "Em andamento" por tarefa pendente/teste falhando), **Then** o STATUS informa o resultado e o que ainda falta (ex.: tarefas de validação manual pendentes, quais tarefas não-manuais bloquearam).
6. **Given** o mesmo comando rodando duas vezes seguidas com o mesmo resultado, **When** o status é reescrito, **Then** a descrição continua com **uma única** linha `STATUS:` (a antiga é substituída, nunca duplicada).

---

### User Story 2 - Status atualizado também após /speckit-analyze e /speckit-clarify (Priority: P1)

Como responsável pelo projeto, quero que rodar `/speckit-analyze` ou `/speckit-clarify` também atualize o STATUS do card — mesmo sem mudar a coluna — porque esses comandos representam progresso real dentro da fase (achados aguardando decisão, esclarecimentos resolvidos) que hoje fica invisível no quadro.

**Why this priority**: Sem isso, o quadro parece parado durante todo o trabalho de análise/esclarecimento, e o "aguardando decisão" — exatamente a informação mais acionável — não aparece em lugar nenhum.

**Independent Test**: Numa feature com card, rodar `/speckit-clarify` e depois `/speckit-analyze` e confirmar que o STATUS muda a cada um, sem o card mudar de coluna.

**Acceptance Scenarios**:

1. **Given** um card de uma feature especificada, **When** `/speckit-clarify` termina, **Then** o STATUS reflete que houve esclarecimento (e o card não muda de coluna).
2. **Given** um card de uma feature com `tasks.md`, **When** `/speckit-analyze` termina, **Then** o STATUS reflete que a análise foi concluída (e o card não muda de coluna).
3. **Given** uma feature sem card no Trello (ex.: especificada antes da integração), **When** `/speckit-analyze` ou `/speckit-clarify` termina, **Then** nada é criado nem quebrado — o caso é só registrado no log (como nas demais fases da spec 040).

---

### User Story 3 - Card de uma feature já existente ganha STATUS mesmo sem tê-lo hoje (Priority: P2)

Como responsável pelo projeto, quero que o card de uma feature (já criado pela integração 040, mas de antes desta extensão existir — ex.: 037 a 041) receba a linha `STATUS:` na próxima vez que qualquer comando `/speckit-*` mexer nele, com o melhor entendimento possível do estado atual dessa feature, para que eu não precise editar cada card à mão só porque ele é anterior a esta extensão.

**Why this priority**: Complementa a US1 para os cards de feature que a integração já criou antes de existir o STATUS. É P2 porque a US1/US2 já entregam valor para toda feature nova; esta uniformiza as que já existiam. (Cards **manuais**, sem marcador `Feature:`, não passam por este fluxo — são cobertos só pela atualização retroativa em lote, US4/FR-009.)

**Independent Test**: Num card de uma feature já existente (com marcador `Feature: <id>`) mas sem `STATUS:`, disparar qualquer comando `/speckit-*` sobre essa feature e confirmar que o card passa a ter `STATUS:` refletindo o estado real da feature, sem duplicar nem apagar o marcador `Feature:`.

**Acceptance Scenarios**:

1. **Given** o card de uma feature numerada existente, sem `STATUS:` na primeira linha mas **com** o marcador `Feature: <id>`, **When** um comando `/speckit-*` dessa feature dispara o hook e lê o card, **Then** o STATUS é derivado do estado dessa feature nos arquivos (quais artefatos existem: spec, plano, tarefas, progresso das tarefas) e inserido como primeira linha, preservando o resto da descrição e o marcador `Feature:`.
2. **Given** um card **manual**, sem marcador `Feature:`, **When** qualquer comando `/speckit-*` roda (mesmo que opere sobre outra feature), **Then** esse card manual não é tocado por este fluxo — só a atualização retroativa (US4) o alcança.
3. **Given** um card cuja primeira linha já é `STATUS: ...`, **When** a integração o lê, **Then** nada é alterado (idempotente).
4. **Given** um card com a descrição vazia, **When** é preenchido, **Then** a descrição passa a conter só a linha `STATUS: ...`.

---

### User Story 4 - Cards já existentes recebem o STATUS manual retroativamente (Priority: P3)

Como responsável pelo projeto, quero que os cards manuais que já estão no quadro hoje recebam de uma vez o `STATUS: card criado manualmente, sem spec formal ainda`, para o quadro ficar uniforme desde o primeiro dia desta feature.

**Why this priority**: É um acerto único de dados existentes, não um comportamento contínuo.

**Independent Test**: Executar a atualização retroativa e conferir que todos os cards-alvo têm a linha, e que rodar de novo não muda nada.

**Acceptance Scenarios**:

1. **Given** os cards manuais-alvo definidos em FR-009, **When** a atualização retroativa roda, **Then** cada um tem `STATUS: card criado manualmente, sem spec formal ainda` como primeira linha, com o restante da descrição preservado.
2. **Given** que a atualização já rodou, **When** roda de novo, **Then** nenhum card é alterado.

---

### User Story 5 - O Spec Kit nunca trava por causa do STATUS (Priority: P1)

Como responsável pelo projeto, quero garantia de que uma falha ao escrever o STATUS (Trello fora do ar, credencial inválida, card não encontrado) nunca interrompe `/speckit-analyze`, `/speckit-clarify` nem os demais comandos — o STATUS é um extra, igual à sincronização de colunas.

**Why this priority**: Os dois novos hooks (analyze/clarify) entram em comandos que hoje não têm nenhuma dependência do Trello; sem essa garantia eles passariam a arriscar trabalho que antes estava a salvo.

**Independent Test**: Com uma credencial inválida, rodar `/speckit-clarify` e `/speckit-analyze` e confirmar que ambos completam normalmente e o erro fica só no log.

**Acceptance Scenarios**:

1. **Given** credencial inválida ou Trello inacessível, **When** qualquer comando com hook de STATUS roda, **Then** o comando completa seu trabalho normal e o erro é registrado sem aparecer como falha do comando.
2. **Given** um card apagado à mão, **When** o STATUS tenta ser escrito, **Then** o comando segue normalmente e "card não encontrado" é registrado.

---

### Edge Cases

- **Feature sem card (anterior à integração)**: `/speckit-analyze`/`/speckit-clarify` nada criam — só registram no log (a spec 040 continua proibindo cards retroativos por feature, FR-014 daquela spec).
- **STATUS editado à mão por uma pessoa**: a próxima atualização automática substitui a primeira linha `STATUS:` — é aceito que a integração seja a dona dessa linha.
- **Descrição com `STATUS:` que não está na primeira linha** (ex.: texto colado no meio): não conta como o padrão; a linha correta é inserida na primeira posição.
- **Dois comandos em sequência rápida**: vale o último a escrever (o status reflete o último comando concluído).
- **Card movido por uma pessoa para "Fase de teste"/"Concluído"**: não há evento do Spec Kit para isso, então o STATUS não é atualizado por essa movimentação — fica com o último estado escrito pela integração (a spec 040 mantém essas duas colunas como manuais).
- **Card manual com texto longo ou formatação (listas, negrito)**: o texto original é preservado integralmente abaixo da nova primeira linha.
- **Falha no meio da escrita**: nunca deixar a descrição pela metade — ou o STATUS novo entra, ou a descrição permanece como estava.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Todo card criado ou atualizado pela integração MUST ter, como **primeira linha** da descrição, exatamente uma linha no padrão `STATUS: <detalhe>`.
- **FR-002**: O `<detalhe>` MUST identificar o último comando `/speckit-*` executado para a feature e a situação em que ficou, em uma única linha, em português.
- **FR-003**: O STATUS MUST ser escrito/atualizado quando terminam `/speckit-specify`, `/speckit-plan` e `/speckit-tasks`, quando `/speckit-implement` começa (progresso de tarefas naquele instante) e quando `/speckit-implement` termina (resultado: movido para revisão, ou retido em andamento com o motivo; e as tarefas de validação manual ainda pendentes).
- **FR-004**: O STATUS MUST também ser escrito quando terminam `/speckit-analyze` e `/speckit-clarify`, **sem** mover o card de coluna. Para isso o sistema MUST registrar em `.specify/extensions.yml` os eventos `after_analyze` e `after_clarify` (o mecanismo de hook já suporta ambos — ver Nota de investigação prévia; a confirmação técnica final fica em `research.md`).
- **FR-005**: Atualizar o STATUS MUST NOT alterar nada além da primeira linha: o restante da descrição (resumo do spec ou texto manual) e, em cards de feature, o marcador `Feature: <id>` da última linha MUST permanecer intactos, e a localização do card por esse marcador (sem duplicar) MUST continuar funcionando.
- **FR-006**: Repetir uma atualização MUST NOT duplicar a linha `STATUS:` — a existente é substituída (idempotente); uma descrição nunca tem mais de uma linha `STATUS:` inicial.
- **FR-007**: Quando a integração lê o card da feature em execução naquele comando, e a primeira linha da descrição não segue o padrão `STATUS: ...`, ela MUST inserir essa linha como primeira linha, preservando todo o texto original abaixo. Se o card tiver o marcador `Feature: <id>` de uma feature numerada, o `<detalhe>` MUST refletir o estado atual dessa feature (a partir dos artefatos existentes em `specs/<id>/`); se não tiver, MUST ser `card criado manualmente, sem spec formal ainda`. Cards manuais alheios devolvidos por essa mesma leitura (que não são o card da feature em execução) MUST NOT ser alterados por este fluxo — eles são cobertos só pela atualização retroativa de FR-009.
- **FR-008**: O `<detalhe>` do STATUS MUST poder vir de duas fontes: (a) um texto curto opcional repassado pelo próprio comando `/speckit-*` que disparou o hook, quando esse comando tiver algo relevante só disponível na conversa naquele momento (ex.: `/speckit-analyze` repassando "2 achados aguardando decisão", `/speckit-clarify` repassando "2 perguntas respondidas"); (b) na ausência desse texto, MUST ser derivado do estado dos arquivos do repositório no instante da escrita (existência de `spec.md`/`plan.md`/`tasks.md`, contagem de tarefas concluídas e pendentes, tarefas de validação manual). A fonte (a) nunca é obrigatória — nenhum comando MUST falhar por não ter esse texto.
- **FR-009**: Esta feature MUST atualizar retroativamente, uma única vez e de forma idempotente, **todos os cards manuais hoje existentes no quadro "Estágio SPI"** — ou seja, todo card sem o marcador `Feature: <id>` na descrição, em qualquer lista — com `STATUS: card criado manualmente, sem spec formal ainda`. (O pedido original citou "4 cards de Visão do Aluno no Backlog"; nenhum card com esse nome existe no quadro hoje — ver Nota de investigação prévia. O alvo confirmado é o conjunto real de cards manuais existentes, hoje 12, não um subconjunto por nome ou por lista.)
- **FR-010**: Falha ao ler ou escrever o STATUS (Trello fora do ar, credencial inválida, card/quadro/lista não encontrado, ou qualquer outro erro) MUST NOT interromper nem falhar o comando do Spec Kit em execução, e MUST ser registrada de forma identificável (mesmo comportamento e mesmo log da spec 040).
- **FR-011**: Esta feature MUST NOT mover cards para "Fase de teste" ou "Concluído", e MUST NOT tomar nenhuma decisão de fluxo do Spec Kit a partir do que lê do Trello — a leitura serve apenas para descobrir se falta a linha `STATUS:` (fora de escopo: qualquer leitura recorrente do Trello disparando ações).
- **FR-012**: Esta feature MUST NOT regredir o comportamento da spec 040: criação do card no Backlog, movimentação por fase, comentário de resumo em "Revisão de código", regra de completude (`(parte manual)` excluída) e localização por `Feature: <id>` continuam como estão.
- **FR-013**: Esta feature MUST NOT introduzir nenhuma credencial ou segredo novo, nem viver em `src/SPI.*`/`frontend/` — mesmas regras de FR-012/FR-013 da spec 040.

### Key Entities

- **Card do Trello (existente, estendido)**: ganha uma convenção para a **primeira linha** da descrição (`STATUS: ...`); o restante da descrição e o marcador `Feature: <id>` da última linha não mudam de papel.
- **Linha de STATUS**: linha única, gerada pela integração, que resume o último comando `/speckit-*` e a situação da feature; substituível a cada atualização; nunca duplicada.
- **Estado da feature (fonte do STATUS)**: derivado de `specs/<id>/` (artefatos existentes, tarefas concluídas/pendentes/manuais) — nunca da conversa, salvo decisão em contrário em FR-008.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos cards criados ou atualizados pela integração após esta feature têm exatamente uma linha `STATUS:` como primeira linha da descrição.
- **SC-002**: Depois de `/speckit-analyze` e de `/speckit-clarify`, o STATUS do card da feature reflete esse comando em 100% das execuções em que o Trello está acessível, sem mudar o card de coluna.
- **SC-003**: 0 comandos do Spec Kit são interrompidos ou falham por causa de uma falha do STATUS (Trello fora do ar, credencial inválida, card não encontrado).
- **SC-004**: Em 100% das atualizações, o restante da descrição (resumo ou texto manual) e o marcador `Feature: <id>` ficam idênticos ao que eram antes; nenhuma atualização cria card duplicado.
- **SC-005**: Uma pessoa olhando só o quadro identifica, para todo card ligado a uma feature, qual foi o último comando `/speckit-*` e a situação em que a feature ficou, sem abrir o repositório.
- **SC-006**: Após a atualização retroativa, 100% dos cards-alvo definidos em FR-009 têm `STATUS:` como primeira linha, e uma segunda execução não altera nenhum card.

## Assumptions

- O STATUS é **uma linha só**, em português, com o prefixo exato `STATUS: ` — para ser reconhecível por um teste simples de "primeira linha começa com `STATUS: `".
- Os eventos `after_analyze` e `after_clarify` usam o mesmo mecanismo de hooks já em uso (mandatórios, `optional: false`, sem `condition`, sem executor externo), e o comando de sincronização da spec 040 é estendido (ou reaproveitado) para tratar o STATUS.
- Não existe evento a cada tarefa concluída durante o `/speckit-implement`; portanto o progresso "N/M" só é gravado nos pontos que já têm hook (início e fim). Atualizações no meio da execução dependeriam de o próprio agente acionar a sincronização manualmente — fora do que a integração garante.
- Movimentações manuais de card (ex.: para "Fase de teste"/"Concluído") não disparam nenhum evento do Spec Kit e portanto não atualizam o STATUS.
- Escritas de STATUS reutilizam a mesma autenticação e o mesmo log (`sync.log`, gitignored) da spec 040.
- A leitura do quadro que a integração já faz para localizar o card é o "mecanismo de leitura pontual" citado no pedido; nenhum polling ou leitura recorrente é adicionado.
- Fora de escopo, reafirmando o pedido: qualquer leitura recorrente do Trello que dispare ações no Spec Kit; mover cards para "Fase de teste"/"Concluído".
