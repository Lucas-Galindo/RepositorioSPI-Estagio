# Feature Specification: Selo de cobrança do aluno nas telas da professora

**Feature Branch**: `068-badge-cobranca-aluno`

**Created**: 2026-11-03

**Status**: Draft

> **Nota (2026-11-05, spec 072):** na lista de Alunos, a pílula do tipo de cobrança passou para uma coluna própria, "Tipo", depois de "Cobrança"; a coluna "Cobrança" mostra só valor e turma. O modo "grupo" (turma e aula) não mudou. Ver `specs/072-alunos-coluna-tipo/`.

**Input**: Card do Trello "Exibir o vínculo de cobrança do aluno para o professor" (prioridade 3 do backlog). A professora precisa identificar, direto na tela e de forma rápida, **qual é o vínculo de cobrança** do aluno (mensalidade, pacote, avulsa; semestral e anual quando existirem), de preferência por um selo (badge), sem abrir a ficha do aluno.

## Contexto (estado atual, lido do código)

- Hoje os vínculos só aparecem na **ficha do aluno**, na seção "Vínculos de Cobrança" (modalidade, valor, contexto e, no pacote, a barra "N de M aulas").
- O aluno pode ter **vários vínculos ativos ao mesmo tempo** (specs 056 e 063): no máximo **um pacote por turma** e **um pacote de aulas individuais**, **uma avulsa por turma** e **uma individual**; a **mensalidade é exclusiva** (cobre todas as aulas e não convive com outro vínculo ativo). Por isso "a categoria do aluno" não é um valor único.
- A regra de qual vínculo vale em cada aula já existe no backend (`RegraCobrancaPresenca`): mensalidade primeiro; depois o pacote **do grupo da aula** (a turma, ou as aulas individuais); depois a avulsa do contexto; sem nada, cobra o **valor da aula** do aluno. Pacote esgotado sem avulsa para cair bloqueia a presença (spec 041).
- As telas candidatas e o que mostram hoje:
  - **Lista de alunos** (`/alunos`): colunas Aluno, RA, Turma(s), Telefone, Valor da aula e Status; nenhuma sobre cobrança.
  - **Alunos da turma** (`/turmas/{id}`): nome e RA de cada aluno.
  - **Chamada** (detalhes da aula, `/aulas/{id}`): nome e RA de cada aluno, mais os "Outros alunos" (reposição).
- O progresso do pacote ("N de M aulas", "restam X", estados Atenção/Esgotado) já é calculado no backend (spec 058).
- As modalidades Semestral e Anual (spec 062) **ainda não existem** no sistema.

## Clarificações (respondidas pelo usuário em 2026-11-03)

- Q: Em quais telas o selo aparece? → A: **Lista de alunos, alunos da turma e chamada (detalhes da aula)**, as três.
- Q: Com vários vínculos ativos, o que mostrar na lista de alunos? → A: **Resumo curto**: um selo com a categoria principal e "+N" quando houver mais; o detalhe aparece ao passar o mouse e na ficha.
- Q: O que o selo mostra além da categoria? → A: **Aulas restantes do pacote, valor e turma do vínculo.**

## Decisões menores adotadas (a confirmar na aprovação)

1. **Que vínculo cada tela mostra**:
   - **Lista de alunos**: o resumo de **todos** os vínculos ativos do aluno (principal + "+N").
   - **Alunos da turma**: o vínculo que **vale naquela turma** (a mesma regra que decide a cobrança de uma aula dessa turma).
   - **Chamada**: o vínculo que **vale para aquela aula** (turma da aula, ou aulas individuais), para os alunos da turma e para os "Outros alunos".
2. **Qual é o "principal" no resumo da lista** (decidido no servidor): **Mensalidade**, depois **Pacote**, depois **Avulsa**; entre vínculos da mesma categoria, o **mais recente**. O "+N" conta os demais vínculos ativos.
3. **Aluno sem vínculo ativo**: selo "**Sem vínculo (valor da aula)**", com o valor da aula do aluno.
4. **O que aparece em cada selo**:
   - **Categoria**: Mensalidade, Pacote ou Avulsa (Semestral e Anual entram quando a spec 062 for implementada, sem mudança no selo).
   - **Pacote**: "restam N" (ou "resta 1"); **Esgotado** com destaque; "Total não informado" quando não há total (mesma leitura da spec 058).
   - **Valor** do vínculo e **turma** do vínculo. A turma só aparece onde **não é redundante**: na lista, sim; na página da turma e na chamada, não.
5. **Detalhe ao passar o mouse**: na lista, a dica lista todos os vínculos ativos (categoria, turma, valor, aulas restantes).
6. **Pacote esgotado**: se há avulsa para cair, o selo mostra a **Avulsa** com um aviso "pacote esgotado"; se não há, mostra **Pacote · Esgotado** (a presença do aluno ficará barrada, como já ocorre).
7. **Conflito herdado** (combinação antiga hoje proibida, spec 056): fora do escopo; a ficha continua sendo o lugar onde ele aparece.
8. **Aluno inativo** na lista: mostra o selo normalmente (os vínculos não mudam ao inativar o aluno, spec 063).
9. **Somente leitura**: o selo não tem ação; criar e editar vínculos continua na ficha do aluno.
10. **O resumo da lista é sob demanda**: a lista de alunos é usada por várias telas só para escolher um aluno (aulas, lembretes, pagamentos, relatórios etc.); só a tela de Alunos pede o resumo de cobrança. As demais continuam recebendo a lista como hoje, sem custo extra.
11. **Vínculos duplicados herdados** (conflito antigo, spec 056): a lista escolhe o mais recente como principal, enquanto a turma e a chamada seguem a regra de cobrança (o mais antigo). Só pode diferir nesses dados já marcados como conflito.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver a cobrança de cada aluno na lista de alunos (Priority: P1)

Na lista de alunos, uma coluna nova mostra, para cada aluno, um selo com a categoria principal de cobrança e, quando há mais de um vínculo ativo, um "+N". Passar o mouse mostra todos.

**Why this priority**: É a visão geral e o pedido central do card.

**Independent Test**: Abrir a lista com um aluno de mensalidade, um de pacote, um com pacote e avulsa e um sem vínculo; conferir o selo de cada um, o "+1" e a dica.

**Acceptance Scenarios**:

1. **Given** um aluno com um vínculo ativo, **When** a lista abre, **Then** o selo mostra a categoria, o valor e a turma do vínculo.
2. **Given** um aluno com pacote, **When** a lista abre, **Then** o selo mostra "restam N" (ou "resta 1"); se esgotado, mostra o destaque de esgotado.
3. **Given** um aluno com mais de um vínculo ativo, **When** a lista abre, **Then** o selo mostra o principal (mensalidade, depois pacote, depois avulsa; o mais recente em empate) e "+N" com o número dos demais; **When** a professora passa o mouse, **Then** vê todos os vínculos ativos.
4. **Given** um aluno sem vínculo ativo, **When** a lista abre, **Then** aparece "Sem vínculo (valor da aula)" com o valor da aula dele.
5. **Given** muitos alunos na lista, **When** ela carrega, **Then** a cobrança chega junto com a lista, sem uma requisição por aluno.
6. **Given** uma tela que só usa a lista de alunos para escolher um aluno (ex.: formulário de aula, lembretes, pagamentos), **When** ela carrega a lista, **Then** não recebe nem calcula o resumo de cobrança (a lista vem como hoje).

---

### User Story 2 - Ver a cobrança que vale na turma (Priority: P1)

Na página da turma, cada aluno mostra o selo do vínculo que vale **naquela turma**.

**Why this priority**: Ajuda a ver quem está em pacote, mensalidade ou avulsa dentro de uma turma específica.

**Independent Test**: Abrir uma turma com alunos de vínculos diferentes e conferir que cada selo bate com a regra de cobrança daquela turma, inclusive o aluno com pacote de **outra** turma (que, nesta, cai na avulsa ou no valor da aula).

**Acceptance Scenarios**:

1. **Given** um aluno com pacote da turma, **When** a página abre, **Then** o selo mostra "Pacote" com as aulas restantes.
2. **Given** um aluno com pacote de outra turma e sem avulsa nesta, **When** a página abre, **Then** o selo mostra "Sem vínculo (valor da aula)" (o pacote da outra turma não vale aqui).
3. **Given** um aluno com mensalidade, **When** a página abre, **Then** o selo mostra "Mensalidade" (cobre todas as turmas).
4. **Given** qualquer selo nesta tela, **When** exibido, **Then** ele não repete o nome da turma.

---

### User Story 3 - Ver a cobrança de cada aluno na chamada (Priority: P1)

Nos detalhes da aula, ao lado de cada aluno (e dos "Outros alunos"), o selo mostra a cobrança que vale **para aquela aula**, antes de marcar a presença.

**Why this priority**: É onde a presença vira cobrança; saber de antemão evita surpresa (pacote esgotado, avulsa, valor).

**Independent Test**: Abrir a chamada de uma aula de turma com um aluno de pacote da turma, um de pacote esgotado, um de mensalidade, um sem vínculo e um de reposição; conferir cada selo.

**Acceptance Scenarios**:

1. **Given** um aluno com pacote da turma da aula, **When** os detalhes abrem, **Then** o selo mostra "Pacote" com as aulas restantes.
2. **Given** um aluno com pacote esgotado e sem avulsa, **When** os detalhes abrem, **Then** o selo mostra "Pacote · Esgotado" em destaque.
3. **Given** um aluno com pacote esgotado e com avulsa, **When** os detalhes abrem, **Then** o selo mostra "Avulsa" com o aviso "pacote esgotado".
4. **Given** um aluno de reposição com pacote de outra turma, **When** os detalhes abrem, **Then** o selo mostra a cobrança que vale para esta aula (avulsa ou valor da aula), não o pacote da outra turma.
5. **Given** uma aula já realizada, **When** os detalhes abrem, **Then** o selo continua mostrando o vínculo vigente (o histórico do que foi aplicado em cada presença continua na ficha e nos relatórios).

---

### Edge Cases

- **Semestral e Anual**: ainda não existem; quando a spec 062 chegar, o selo já os mostra (uma categoria a mais, sem mudar a tela). Antes disso, nada é exibido para elas.
- **Vínculo inativo** (excluído): nunca aparece no selo; só os ativos contam.
- **Aluno com muitos vínculos**: a coluna nunca cresce; o resumo fica em um selo e o detalhe vai na dica.
- **Falha ao carregar a cobrança**: a tela continua funcionando (o selo mostra um traço) em vez de quebrar a lista.
- **Pacote sem total** (estado "Total não informado"): o selo diz isso, como na ficha.
- **Modo claro e escuro**: o selo é legível nos dois (contraste mínimo 4.5:1).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A lista de alunos MUST mostrar, para cada aluno, um selo de cobrança com a **categoria principal** dos vínculos ativos e, havendo mais de um, o "+N" dos demais.
- **FR-002**: A página da turma MUST mostrar, para cada aluno da turma, um selo do vínculo que **vale naquela turma**; a chamada (detalhes da aula) MUST mostrar, para cada aluno e cada "Outro aluno", um selo do vínculo que **vale para aquela aula**. As duas telas MUST usar a mesma regra de cobrança que o sistema já aplica ao registrar a presença.
- **FR-003**: O selo MUST mostrar a **categoria**; no pacote, as **aulas restantes** (e o destaque de esgotado, como na spec 058); o **valor** do vínculo; e a **turma** do vínculo, esta só onde não for redundante (lista, sim; turma e chamada, não).
- **FR-004**: O aluno sem vínculo ativo MUST aparecer como **"Sem vínculo (valor da aula)"**, com o valor da aula dele.
- **FR-005**: Na lista, o servidor MUST escolher o vínculo principal (mensalidade, depois pacote, depois avulsa; o mais recente em empate) e o "+N"; a dica MUST listar todos os vínculos ativos do aluno.
- **FR-006**: A informação de cobrança MUST vir do servidor **junto com a listagem** (alunos, alunos da turma e participantes da aula). O sistema MUST NOT fazer uma consulta por aluno, nem no frontend nem no backend (consulta em lote).
- **FR-013**: O resumo de cobrança da **lista de alunos** MUST ser calculado **somente quando a tela pede** (tela de Alunos). Chamadas que usam a lista só para escolher um aluno MUST NOT pagar o custo do cálculo nem receber o resumo.
- **FR-007**: O frontend MUST NOT decidir qual vínculo vale, qual é o principal, se o pacote está esgotado nem quantas aulas restam; apenas exibe o que o servidor devolve.
- **FR-008**: A mudança MUST NOT alterar nenhuma regra de cobrança, nenhum vínculo nem nenhum dado (somente leitura), e MUST NOT exigir migração ou mudança em `database/`.
- **FR-009**: O selo MUST usar apenas as cores e tokens do tema (spec 057) e ser legível em modo claro e escuro (contraste mínimo 4.5:1, conferido por `npm run verificar-contraste`).
- **FR-010**: A mudança MUST NOT criar mais de **um** componente novo de frontend (o selo reutilizável de cobrança, usado nas três telas) e MUST NOT criar nenhuma tela nova.
- **FR-011**: As modalidades Semestral e Anual (spec 062), quando existirem, MUST aparecer no selo sem exigir mudança de tela.
- **FR-012**: O selo é somente leitura; criar, editar e excluir vínculos MUST continuar acontecendo só na ficha do aluno.

### Key Entities

- **Resumo de cobrança** (somente leitura, calculado): categoria, aulas restantes e estado do pacote, valor, turma do vínculo, e, na lista, a quantidade dos demais vínculos e a relação deles para a dica. Vem no corpo das respostas de alunos, de alunos da turma e de participantes da aula; **não é persistido**.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos alunos de cada uma das três telas mostram um selo (ou "Sem vínculo (valor da aula)"), conferido contra os vínculos ativos do banco local.
- **SC-002**: Nas telas da turma e da chamada, 100% dos selos coincidem com a decisão da regra de cobrança da presença (`RegraCobrancaPresenca`) para aquele aluno e aquela turma/aula.
- **SC-003**: 0 requisições adicionais por aluno: o número de requisições de cada tela é o mesmo de antes da mudança, e as telas que só escolhem um aluno recebem a lista exatamente como antes (sem o resumo).
- **SC-004**: 0 pares de cor abaixo de 4.5:1 na verificação de contraste, em modo claro e escuro.
- **SC-005**: 0 dados e 0 regras de cobrança alterados (contagens das tabelas e testes de cobrança existentes iguais antes e depois).
- **SC-006**: A professora identifica a cobrança de um aluno em uma olhada, sem abrir a ficha, nas três telas.

## Assumptions

- Sistema de uma professora; os selos só aparecem nas telas dela.
- Os vínculos ativos e a regra de cobrança são os das specs 056, 058 e 063, sem mudança.
- O layout do app segue desktop; celular é assunto de outro card.
- A spec 062 (Semestral e Anual) pode ou não estar implementada quando esta entrar; o selo funciona nos dois casos.

## Fora de escopo

- Criar, editar ou excluir vínculos pelo selo.
- Mostrar o selo em outras telas (Home, relatórios, financeiro).
- Resolver ou sinalizar conflitos herdados de vínculos (spec 056).
- Mostrar validade do pacote por data (o pacote guarda saldo de aulas, não data de validade; o período vale só para a turma de pacote).
- Layout responsivo para celular.
