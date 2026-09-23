# Feature Specification: Sincronização Automática Spec Kit → Trello

**Feature Branch**: `040-trello-sync-hooks`

**Created**: 2026-09-23

**Status**: Draft

**Input**: User description: "Criar uma integração entre o Spec Kit e o Trello, usando os hooks nativos do Spec Kit (.specify/extensions.yml), para sincronizar o progresso das features com um quadro Kanban no Trello. Script de ferramenta de processo (não é domínio de negócio do SPI), vive em .specify/hooks/ ou equivalente. Quadro 'Estágio SPI', 7 listas: 1) Backlog — card criado no after_specify (nome = título da feature; descrição = resumo do spec.md); 2) Design — movido no after_plan; 3) A Fazer — movido no after_tasks; 4) Em andamento — movido quando /speckit-implement começa; 5) Revisão de código — movido quando /speckit-implement termina com testes passando, com comentário resumindo o que foi implementado; 6) Fase de teste e 7) Concluído — NÃO movidas automaticamente, ficam manuais. Requisitos técnicos: autenticação via API Key + Token do Trello em variáveis de ambiente (nunca commitadas/hardcoded); falha no hook (Trello fora do ar, credencial inválida, card não encontrado) NÃO trava nem interrompe o Spec Kit — só registra o erro e segue; cada card guarda o identificador da feature (ex: '037-vinculo-cobranca') de forma visível, para localizar o card certo sem duplicar; script na linguagem a decidir em /speckit-plan (Node.js ou PowerShell disponíveis). Fora de escopo: mover cards para 'Fase de teste'/'Concluído' automaticamente; qualquer leitura do Trello de volta para o Spec Kit (via de mão única)."

## Nota de investigação prévia

Confirmado por leitura do projeto atual (grounding antes de especificar):

- **`.specify/extensions.yml`**: não existe hoje neste projeto — nenhuma integração de hook foi configurada ainda. Esta feature é quem cria esse arquivo pela primeira vez.
- **Mecanismo de hook do Spec Kit**: documentado de forma consistente em todas as skills `speckit-*` já usadas nesta sessão (`speckit-specify`, `speckit-plan`, `speckit-tasks`, `speckit-implement`). Cada uma tem um passo "Pre-Execution Checks"/"Mandatory Post-Execution Hooks" que lê `hooks.before_<fase>`/`hooks.after_<fase>` de `.specify/extensions.yml` e, para cada hook habilitado, **invoca outro comando do Spec Kit** (o campo `command`, convertido de notação `a.b.c` para `/a-b-c`) — não existe, no mecanismo hoje documentado, um jeito de o hook rodar um script arbitrário diretamente; ele sempre passa por invocar um comando `/speckit-*`. Os pontos de hook relevantes para este pedido já existem nas skills atuais: `before_specify`/`after_specify`, `before_plan`/`after_plan`, `before_tasks`/`after_tasks`, `before_implement`/`after_implement` — cobrindo exatamente as 5 fases automáticas pedidas (Backlog, Design, A Fazer, Em andamento, Revisão de código).
- **Implicação para esta feature**: para que os hooks de `extensions.yml` de fato disparem a sincronização com o Trello, esta feature precisa registrar, além do `extensions.yml`, um novo comando (skill) que os hooks apontam — e é esse comando que executa o script de verdade (Node.js ou PowerShell, a decidir em `/speckit-plan`) contra a API do Trello. O campo `condition` de um hook é um texto opaco avaliado por um "HookExecutor" fora do controle do Spec Kit em si — não é um lugar razoável para colocar lógica como "só se os testes passaram"; essa lógica precisa viver dentro do próprio comando/script invocado.
- **Padrão de segredos já estabelecido no projeto**: para o backend .NET, a connection string vive em User Secrets, nunca em `appsettings.json`. Não há um mecanismo `.env` versionado hoje no repositório para scripts fora do .NET (o `frontend/.env.local` é gitignored e serve de precedente equivalente para scripts Node/PowerShell fora do domínio .NET).

## Clarifications

### Session 2026-09-23

- Q: Ao ativar esta integração, ela deve criar cards retroativamente para as 39 features já especificadas, ou só passa a valer a partir da próxima feature especificada? → A: Só a partir de agora — nenhum card retroativo; a integração passa a agir só em features especificadas depois de ativada.
- Q: O card deve se mover para "Revisão de código" sempre que `/speckit-implement` terminar, ou só quando `tasks.md` estiver 100% concluído e os testes passarem? → A: Condicional, com uma exceção: a checagem de "100%" conta apenas tarefas de implementação/teste automatizado — tarefas explicitamente marcadas como validação manual (a mesma categoria hoje reportada como "pendente, aguardando você", ex.: specs 034/036/037) não bloqueiam a entrada em "Revisão de código", pois pertencem à fase seguinte ("Fase de teste"). Se alguma tarefa não-manual ficar incompleta, ou os testes automatizados falharem, o card permanece em "Em andamento" com o erro registrado (FR-011).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver o Backlog do Trello se preencher sozinho ao especificar uma feature (Priority: P1)

Como responsável pelo projeto, quero que, toda vez que uma nova feature for especificada com `/speckit-specify`, um card apareça automaticamente na lista "Backlog" do quadro "Estágio SPI" no Trello, com o título da feature e um resumo do que ela faz — sem eu precisar criar esse card manualmente.

**Why this priority**: É o ponto de entrada de todo o fluxo — sem o card nascer no Backlog, não há nada para mover nas fases seguintes.

**Independent Test**: Rodar `/speckit-specify` para uma feature nova e confirmar que um card com o título certo aparece na lista "Backlog" do quadro "Estágio SPI", com uma descrição resumindo o spec e o identificador da feature (ex.: "037-vinculo-cobranca") visível no card.

**Acceptance Scenarios**:

1. **Given** o quadro "Estágio SPI" configurado no Trello, **When** `/speckit-specify` termina de gerar um novo `spec.md`, **Then** um novo card aparece na lista "Backlog", com o título igual ao nome da feature e a descrição resumindo o conteúdo do spec.
2. **Given** um card já existe no Trello para uma feature (identificador visível no card), **When** `/speckit-specify` é rodado de novo para essa mesma feature (ex.: uma correção no spec), **Then** nenhum card duplicado é criado — o card existente permanece único.

---

### User Story 2 - Acompanhar o card se mover sozinho conforme o trabalho avança (Priority: P1)

Como responsável pelo projeto, quero que o card da feature se mova automaticamente pelas listas "Design", "A Fazer", "Em andamento" e "Revisão de código" conforme eu uso `/speckit-plan`, `/speckit-tasks` e `/speckit-implement`, para ter uma visão sempre atualizada do estágio de cada feature sem precisar arrastar cards manualmente.

**Why this priority**: É o valor central do pedido — visibilidade do progresso sem trabalho manual de atualizar o quadro.

**Independent Test**: Rodar `/speckit-plan`, depois `/speckit-tasks`, depois `/speckit-implement` para uma feature com card já criado, e confirmar que o card se move para "Design", depois "A Fazer", depois "Em andamento" (assim que a implementação começa) — cada movimentação encontrando o card certo pelo identificador da feature, sem duplicar.

**Acceptance Scenarios**:

1. **Given** um card de uma feature na lista "Backlog", **When** `/speckit-plan` termina de gerar o plano dessa feature, **Then** o card se move para a lista "Design".
2. **Given** um card de uma feature na lista "Design", **When** `/speckit-tasks` termina de gerar a lista de tarefas, **Then** o card se move para a lista "A Fazer".
3. **Given** um card de uma feature na lista "A Fazer", **When** `/speckit-implement` começa a executar, **Then** o card se move para a lista "Em andamento".
4. **Given** um card de uma feature em qualquer lista automática, **When** a fase correspondente roda de novo (ex.: `/speckit-plan` é repetido), **Then** o card permanece na lista certa (não duplica, não regride, não quebra).

---

### User Story 3 - Ver um resumo do que foi implementado quando o card chega em Revisão de código (Priority: P2)

Como responsável pelo projeto, quero que, quando `/speckit-implement` terminar, o card se mova para "Revisão de código" com um comentário resumindo o que foi implementado, para revisar o trabalho direto pelo Trello sem precisar abrir o repositório primeiro.

**Why this priority**: Agrega valor sobre a US2 (mover o card), mas depende dela existir primeiro — é um complemento informativo, não o mecanismo central de movimentação.

**Independent Test**: Rodar `/speckit-implement` até o fim para uma feature com card em "Em andamento" e confirmar que o card se move para "Revisão de código" com um novo comentário resumindo o trabalho.

**Acceptance Scenarios**:

1. **Given** um card na lista "Em andamento", **When** `/speckit-implement` termina de processar todas as tarefas, **Then** o card se move para "Revisão de código" e recebe um comentário novo resumindo o que foi implementado.

---

### User Story 4 - O Spec Kit nunca trava por causa do Trello (Priority: P1)

Como responsável pelo projeto, quero ter certeza de que, se o Trello estiver fora do ar, com credencial inválida, ou o card não for encontrado, o comando do Spec Kit que estou rodando continua funcionando normalmente — a sincronização com o Trello é um extra, nunca um bloqueio.

**Why this priority**: Sem essa garantia, uma integração "de bônus" passaria a arriscar todo o fluxo de trabalho real (specify/plan/tasks/implement) — inaceitável mesmo que as outras stories funcionem perfeitamente.

**Independent Test**: Configurar uma credencial do Trello inválida (ou derrubar a conectividade) e rodar `/speckit-specify`, `/speckit-plan`, `/speckit-tasks` e `/speckit-implement` normalmente — confirmar que todos completam seu trabalho normal no Spec Kit, e que o erro da sincronização fica registrado em algum lugar identificável, sem aparecer como uma falha do comando em si.

**Acceptance Scenarios**:

1. **Given** uma credencial do Trello inválida ou o Trello inacessível, **When** qualquer um dos 4 comandos (`specify`/`plan`/`tasks`/`implement`) roda, **Then** o comando completa seu trabalho normal (spec/plano/tarefas/implementação) e o erro da tentativa de sincronização é registrado, sem interromper nem exibir uma falha do comando.
2. **Given** um card que deveria existir no Trello mas foi apagado manualmente por alguém, **When** uma fase tenta mover esse card, **Then** o comando do Spec Kit continua normalmente e o erro "card não encontrado" é registrado.

---

### Edge Cases

- O que acontece se `/speckit-plan` ou `/speckit-tasks` rodar para uma feature que nunca teve um card criado no Backlog (ex.: a integração foi ligada depois que a feature já existia, ou é uma das 39 specs anteriores a esta integração, FR-014)? A movimentação falha silenciosamente (card não encontrado) e é registrada como qualquer outra falha (US4) — esta feature não cria retroativamente cards para features já existentes.
- O que acontece se `/speckit-implement` for interrompido no meio (não processar todas as tarefas não-manuais) e o comando ainda assim chegar ao fim da sua execução? O card não se move para "Revisão de código" — permanece em "Em andamento", com o erro/estado registrado (FR-007a).
- O que acontece se todas as tarefas não-manuais estiverem concluídas e os testes passarem, mas ainda restarem tarefas de validação manual pendentes (o padrão já observado nesta sessão em specs como 034/036/037)? O card MUST se mover normalmente para "Revisão de código" — tarefas de validação manual não bloqueiam essa movimentação (FR-007); elas continuam sendo resolvidas manualmente, fora do escopo automático, antes de alguém mover o card para "Fase de teste".
- O que acontece se duas features diferentes tiverem nomes muito parecidos? A busca do card certo usa o identificador da feature (ex.: "037-vinculo-cobranca"), não o título, então nomes parecidos não causam confusão.
- O que acontece se o quadro "Estágio SPI" ou uma das 7 listas não existir no Trello (nome errado, quadro apagado)? Mesmo tratamento de falha do US4 — registra e segue, sem travar o Spec Kit.
- Esta feature move cards para "Fase de teste" ou "Concluído"? Não — fora de escopo, confirmado pelo usuário; essas duas movimentações continuam manuais.
- Esta feature lê informações do Trello de volta para o Spec Kit (ex.: um humano moveu o card manualmente, e o Spec Kit deveria saber disso)? Não — via de mão única (Spec Kit → Trello), confirmado pelo usuário.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST criar um card na lista "Backlog" do quadro "Estágio SPI" sempre que `/speckit-specify` terminar de gerar um novo `spec.md` para uma feature que ainda não tem card — com o título do card igual ao nome da feature, e a descrição resumindo o conteúdo do spec gerado.
- **FR-002**: O sistema MUST gravar, de forma visível no card (descrição ou campo equivalente), o identificador da feature (ex.: "037-vinculo-cobranca"), para permitir localizar o card correto em movimentações futuras.
- **FR-003**: O sistema MUST NOT criar um segundo card para uma feature que já tem um card no Trello (identificado pelo identificador de FR-002) — toda operação de "criar" MUST primeiro verificar se já existe um card para aquela feature.
- **FR-004**: O sistema MUST mover o card da feature para a lista "Design" quando `/speckit-plan` terminar de gerar os artefatos de planejamento dessa feature.
- **FR-005**: O sistema MUST mover o card da feature para a lista "A Fazer" quando `/speckit-tasks` terminar de gerar a lista de tarefas dessa feature.
- **FR-006**: O sistema MUST mover o card da feature para a lista "Em andamento" quando `/speckit-implement` começar a executar essa feature.
- **FR-007**: O sistema MUST mover o card da feature para a lista "Revisão de código" somente quando, ao `/speckit-implement` terminar de executar essa feature, todas as tarefas **não-manuais** de `tasks.md` (implementação e teste automatizado) estiverem concluídas **e** a suíte de testes automatizados passar — tarefas explicitamente marcadas como validação manual (ex.: cenários que exigem credencial, ambiente rodando, ou confirmação humana) MUST ser excluídas dessa checagem de completude, pois pertencem à fase seguinte ("Fase de teste"), fora do escopo automático desta feature.
- **FR-007a**: Quando alguma tarefa não-manual permanecer incompleta, ou a suíte de testes automatizados falhar, o sistema MUST manter o card na lista "Em andamento" (não mover para "Revisão de código") e MUST registrar essa situação como as demais falhas de sincronização (FR-011).
- **FR-008**: O sistema MUST incluir, ao mover o card para "Revisão de código", um comentário novo no card resumindo o que foi implementado.
- **FR-009**: O sistema MUST NOT mover automaticamente nenhum card para as listas "Fase de teste" ou "Concluído" — essas duas movimentações permanecem manuais, feitas por uma pessoa.
- **FR-010**: O sistema MUST NOT implementar nenhuma leitura de dados do Trello de volta para o Spec Kit — a sincronização é estritamente unidirecional (Spec Kit → Trello).
- **FR-011**: Quando uma tentativa de sincronização com o Trello falhar por qualquer motivo (indisponibilidade, credencial inválida, quadro/lista/card não encontrado, ou qualquer outro erro), o comando do Spec Kit em execução MUST completar seu trabalho normal (spec/plano/tarefas/implementação) exatamente como se a integração com o Trello não existisse, e o erro MUST ser registrado de forma identificável (não deve passar despercebido nem aparecer como uma falha do próprio comando do Spec Kit).
- **FR-012**: A autenticação com o Trello MUST usar API Key e Token armazenados fora do controle de versão (variável de ambiente ou mecanismo equivalente nunca commitado) — nunca hardcoded no script nem em qualquer arquivo versionado.
- **FR-013**: Esta feature MUST NOT viver dentro do domínio de negócio do SPI (`src/SPI.*` ou `frontend/`) — é uma ferramenta de processo de desenvolvimento, separada do produto.
- **FR-014**: Esta feature MUST NOT criar cards retroativamente para features já especificadas antes desta integração existir (`specs/001` a `specs/039`) — a sincronização só passa a agir a partir da próxima feature especificada depois de ativada.

### Key Entities

- **Card do Trello**: representa, no quadro "Estágio SPI", o estágio atual de uma feature do Spec Kit. Atributos: título (nome da feature), descrição (resumo do spec + identificador da feature), lista atual (uma das 7), comentários (histórico, incluindo o resumo de implementação). Relaciona-se 1:1 com uma feature/spec do Spec Kit, localizado sempre pelo identificador gravado nele (nunca pelo título, que pode não ser único ou pode mudar).
- **Quadro "Estágio SPI"**: o quadro Kanban do Trello com as 7 listas fixas (Backlog, Design, A Fazer, Em andamento, Revisão de código, Fase de teste, Concluído) — as duas últimas geridas manualmente, fora do escopo automático desta feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das features novas especificadas depois desta integração estar ativa recebem um card no Backlog do Trello, sem intervenção manual.
- **SC-002**: 0 cards duplicados são observados no quadro para uma mesma feature, mesmo depois de rodar `/speckit-specify`, `/speckit-plan` ou `/speckit-tasks` várias vezes para a mesma feature.
- **SC-003**: 100% das vezes que o Trello está indisponível ou mal configurado, os comandos `/speckit-specify`, `/speckit-plan`, `/speckit-tasks` e `/speckit-implement` completam seu trabalho normal sem nenhuma interrupção perceptível pelo usuário além do trabalho de verdade do Spec Kit.
- **SC-004**: Uma pessoa olhando só o quadro "Estágio SPI" consegue identificar corretamente, sem abrir o repositório, em qual fase do ciclo Spec Kit cada feature está — para as 5 fases automáticas.
- **SC-005**: Nenhuma credencial do Trello aparece em texto plano em nenhum arquivo versionado no repositório, em nenhum momento.

## Assumptions

- O quadro "Estágio SPI" e suas 7 listas já existem no Trello com esses nomes exatos antes desta feature ser usada — esta feature não cria o quadro nem as listas, só interage com eles (criar/mover cards, comentar).
- A sincronização depende de os hooks (`before_specify`/`after_specify`, `before_plan`/`after_plan`, `before_tasks`/`after_tasks`, `before_implement`/`after_implement`) invocarem um comando novo do Spec Kit (registrado nesta mesma feature) que executa o script de integração de verdade — o mecanismo de hook do Spec Kit só sabe invocar outros comandos, nunca um script arbitrário diretamente (Nota de investigação prévia).
- O resumo colocado na descrição do card (FR-001) e o comentário de "Revisão de código" (FR-008) são derivados automaticamente dos artefatos em disco (`spec.md`, `tasks.md`, estado de conclusão das tarefas) — não uma cópia literal do texto que aparece no chat, já que o mecanismo de hook não tem acesso ao histórico da conversa, só aos arquivos do repositório.
- Falhas de sincronização são registradas em um local identificável (ex.: um arquivo de log dentro de `.specify/`, fora do controle de versão) e também sinalizadas de forma breve na saída do próprio hook, sem nunca aparecer como um erro do comando do Spec Kit em si.
- Esta feature não define o processo de obtenção/configuração inicial da API Key e do Token do Trello (isso é um passo de setup do ambiente do desenvolvedor, documentado como parte da entrega, mas não uma tela ou fluxo do sistema).
- A checagem de completude de FR-007 depende de distinguir, dentro de `tasks.md`, tarefas de implementação/teste automatizado de tarefas de validação manual. Esta spec não prescreve o mecanismo exato dessa distinção (ex.: um marcador textual reconhecível na descrição da tarefa) — como as tasks.md geradas nesta sessão já vêm anotando tarefas desse tipo de forma inconsistente (texto livre como "PENDENTE (parte manual)"), pode ser necessário, em `/speckit-plan`, definir ou formalizar um padrão de marcação reconhecível pelo script, para que a distinção não dependa de interpretação de linguagem natural.
- Fora de escopo, reafirmando o pedido original: mover cards automaticamente para "Fase de teste" ou "Concluído"; qualquer leitura do Trello de volta para o Spec Kit.
