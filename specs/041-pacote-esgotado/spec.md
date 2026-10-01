# Feature Specification: Esgotamento de Pacote de Aulas (Alerta e Bloqueio)

**Feature Branch**: `041-pacote-esgotado`

**Created**: 2026-09-23

**Status**: Draft

**Input**: User description: "Gerenciar o esgotamento de Pacotes de aula, com aviso antecipado e bloqueio real ao chegar a zero. (1) Alerta antecipado: vínculos de cobrança Pacote com saldo de aulas 2 ou menos aparecem num painel novo na Home, com nome do aluno, turma/contexto (ou 'Atendimento individual') e saldo restante, atualizado dinamicamente. (2) Bloqueio ao zerar: com saldo 0, impedir o registro de presença daquele aluno numa aula, com mensagem clara de pacote esgotado; o bloqueio é por aluno/vínculo, não pela aula inteira. (3) Renovação: pela edição já existente do vínculo (specs/037), atualizando o saldo manualmente — isso libera o aluno sem ação adicional. (4) O painel distingue 'Atenção' (saldo 1 ou 2) de 'Esgotado/Bloqueado' (saldo 0). (5) Fora de escopo: alertar o aluno; renovação ou cobrança automática ao esgotar."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar):

- **Débito do Pacote (specs/038)**: em `AulaService.GerarContasAReceberAsync`, cada presença de aluno com vínculo `Pacote` ativo decrementa `SaldoAulas` em 1, **somente quando o saldo é maior que zero** — nunca abaixo de zero, e nunca altera um saldo nunca informado (nulo). Hoje um aluno com saldo 0 **continua tendo a presença registrada normalmente**, sem cobrança e sem aviso: é exatamente a lacuna que esta feature fecha (a spec 038 deixou "Pacote esgotado" explicitamente para outra fatia).
- **Registro da sessão**: `AulaService.RegistrarSessaoAsync` recebe as presenças de todos os alunos da aula de uma vez, marca cada um como presente/ausente, muda a aula para `Realizada` e só então gera as cobranças/débitos. Não existe hoje nenhum ponto que recuse a presença de um aluno individual.
- **`VinculoCobranca` (specs/037)**: já tem `AlunoId`, `TurmaId` (nulo = atendimento individual), `Modalidade`, `Valor`, `SaldoAulas` (opcional, só Pacote) e `Ativo`; já é editável pela professora, inclusive o `SaldoAulas` — a renovação (Requisito 3) não exige nenhuma tela ou endpoint novo.
- **Home**: a tela inicial autenticada da professora é o painel `dashboard` (`frontend/app/(app)/dashboard`); a rota `/` é a tela de login.
- **Princípio III da constituição**: esta feature é o que dá efeito real ao `SaldoAulas` além do simples débito — não introduz nenhuma opção configurável nova sem implementação.

## Clarifications

### Session 2026-09-23

- Q (revisão, decisão do usuário, 2026-09-23): o motivo do bloqueio deve ser persistido ou basta o aviso na resposta (aluno bloqueado indistinguível de falta no histórico)? → A: Persistir — o motivo é gravado no registro do aluno na aula e aparece no histórico (FR-012).
- Q: Quando um aluno com pacote esgotado é marcado como presente numa aula com outros alunos, a aula deve ser registrada como Realizada para os demais (aluno bloqueado fica de fora), ou o registro da aula inteira deve ser recusado? → A: Opção A — a aula é registrada como Realizada normalmente para os demais alunos; o aluno esgotado fica sem presença, frequência e débito registrados, e a resposta traz um aviso claro de que o pacote está esgotado e precisa ser renovado.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ser avisada, na Home, de quem está acabando o pacote (Priority: P1)

Como professora, quero abrir a tela inicial e ver de imediato quais alunos estão com o pacote de aulas acabando, sem precisar abrir o cadastro de cada aluno, para renovar com antecedência.

**Why this priority**: É o valor central do pedido — hoje a professora só descobre que o pacote acabou por conta própria. Sozinha, esta story já entrega utilidade (aviso), mesmo sem o bloqueio.

**Independent Test**: Cadastrar vínculos Pacote com saldos 5, 2, 1 e 0 e abrir a Home; confirmar que só os de saldo 2, 1 e 0 aparecem no painel, cada um com nome do aluno, contexto (turma ou "Atendimento individual") e saldo restante.

**Acceptance Scenarios**:

1. **Given** um vínculo Pacote ativo com saldo 2, **When** a professora abre a Home, **Then** o painel lista esse aluno com o nome, o contexto (nome da turma, ou "Atendimento individual") e o saldo 2, no estado "Atenção".
2. **Given** um vínculo Pacote ativo com saldo 1, **When** a professora abre a Home, **Then** ele aparece no estado "Atenção" com saldo 1.
3. **Given** um vínculo Pacote ativo com saldo 0, **When** a professora abre a Home, **Then** ele aparece no estado "Esgotado/Bloqueado", visualmente distinto de "Atenção".
4. **Given** um vínculo Pacote ativo com saldo 3 ou mais, **When** a professora abre a Home, **Then** ele **não** aparece no painel.
5. **Given** um saldo que muda (uma presença o reduz de 3 para 2, ou a professora o edita), **When** a Home é carregada em seguida (aberta ou recarregada), **Then** o painel reflete o saldo novo sem nenhuma ação manual de "recalcular".
6. **Given** que nenhum vínculo Pacote está com saldo 2 ou menos, **When** a professora abre a Home, **Then** o painel não exibe alunos e mostra uma mensagem clara de que ninguém precisa de atenção (não aparece vazio ou quebrado).

---

### User Story 2 - Impedir presença de aluno com pacote esgotado (Priority: P1)

Como professora, quero que o sistema não me deixe registrar a presença de um aluno cujo pacote de aulas já acabou, com uma mensagem clara dizendo que o pacote precisa ser renovado, para não dar aula "de graça" sem perceber.

**Why this priority**: É a metade "real" do pedido — o alerta sozinho é só informativo; o bloqueio é o que impede o consumo de aulas além do contratado.

**Independent Test**: Numa aula com dois alunos presentes, um com pacote esgotado (saldo 0) e outro com saldo positivo, tentar registrar a sessão marcando os dois como presentes; confirmar que o aluno esgotado é bloqueado com mensagem clara e que o outro aluno é processado normalmente.

**Acceptance Scenarios**:

1. **Given** um aluno com vínculo Pacote ativo e saldo 0 no contexto da aula, **When** a professora tenta registrar a presença dele como "Presente", **Then** o sistema não registra essa presença (sem presença, frequência ou débito) e a resposta traz um aviso claro de que o pacote está esgotado e precisa ser renovado.
2. **Given** uma aula com vários alunos presentes e apenas um com pacote esgotado, **When** a sessão é registrada, **Then** a aula fica Realizada e os demais alunos são processados normalmente (presença, frequência, cobrança/débito) — o bloqueio vale só para o aluno/vínculo esgotado, nunca para a aula inteira.
3. **Given** um aluno com pacote esgotado marcado como **Ausente** na aula, **When** a sessão é registrada, **Then** a ausência é aceita normalmente (falta não consome nem exige saldo) e **sem** motivo de bloqueio — continua sendo uma falta comum.
3a. **Given** um aluno barrado por pacote esgotado, **When** a professora consulta essa aula depois, **Then** o histórico mostra que ele não teve a presença registrada por causa do pacote esgotado, distinguível de um aluno que apenas faltou.
4. **Given** um aluno cujo vínculo Pacote tem saldo nulo (nunca informado), **When** a presença é registrada, **Then** **não** há bloqueio (saldo nunca informado não é "esgotado") — comportamento atual preservado.
5. **Given** um aluno com vínculo Pacote de **outro** contexto (outra turma) esgotado, mas sem vínculo esgotado no contexto desta aula, **When** a presença é registrada nesta aula, **Then** não há bloqueio — o bloqueio olha só o vínculo do contexto da aula.

---

### User Story 3 - Renovar o pacote e liberar o aluno na hora (Priority: P2)

Como professora, quero renovar um pacote (esgotado ou quase) apenas atualizando o saldo pela edição do vínculo que já existe, e ter o aluno liberado para novas presenças sem mais nenhuma ação.

**Why this priority**: Fecha o ciclo aviso → bloqueio → renovação. Não exige tela nova (a edição já existe), por isso é P2: o que importa é garantir que o bloqueio e o painel respondem ao saldo atualizado.

**Independent Test**: Com um aluno bloqueado (saldo 0), editar o vínculo definindo saldo 10; confirmar que a presença dele passa a ser aceita e que ele sai do painel da Home.

**Acceptance Scenarios**:

1. **Given** um aluno bloqueado por pacote esgotado, **When** a professora edita o vínculo e define um saldo maior que zero, **Then** a presença desse aluno passa a ser aceita na aula seguinte, sem nenhuma outra ação.
2. **Given** o mesmo aluno, **When** o saldo passa a ser 3 ou mais, **Then** ele deixa de aparecer no painel da Home; se o saldo novo for 1 ou 2, ele permanece no painel como "Atenção".

---

### Edge Cases

- **Vínculo Pacote inativo (`Ativo == false`)**: não aparece no painel e não bloqueia presença — vínculo excluído não tem efeito (mesma regra de todas as modalidades).
- **Modalidade diferente de Pacote** (Avulsa, Mensalidade): nunca aparece no painel e nunca bloqueia, mesmo que `SaldoAulas` tenha algum valor residual.
- **Saldo nulo**: nunca aparece no painel e nunca bloqueia (saldo nunca informado não é "esgotado" nem "acabando").
- **Aluno sem vínculo no contexto da aula**: comportamento atual preservado (cobrado por `Valor da aula`), sem bloqueio.
- **Aula com todos os alunos bloqueados**: se todo aluno marcado como presente estiver esgotado, a aula ainda é registrada como Realizada (ela aconteceu), sem nenhuma presença/frequência/débito, e a resposta traz o aviso de pacote esgotado para cada um (FR-005).
- **Aluno com pacote esgotado e vínculo em duas turmas diferentes**: cada vínculo é avaliado isoladamente; aparece uma linha no painel por vínculo (aluno + contexto), não uma por aluno.
- **Renovação pela edição durante o dia**: o painel e o bloqueio sempre leem o saldo atual — não existe cache de "estado esgotado" a invalidar.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST exibir na Home um painel listando todo vínculo de cobrança **ativo**, de modalidade **Pacote**, com saldo de aulas **maior ou igual a 0 e menor ou igual a 2** (saldo informado; nulo fica de fora).
- **FR-002**: Cada linha do painel MUST mostrar o nome do aluno, o contexto (nome da turma, ou "Atendimento individual" quando o vínculo não tem turma) e o saldo restante.
- **FR-003**: O painel MUST distinguir visualmente dois estados: "Atenção" (saldo 1 ou 2) e "Esgotado/Bloqueado" (saldo 0), e MUST listar os esgotados antes dos em atenção para que a professora priorize o mais urgente.
- **FR-004**: O painel MUST refletir sempre o saldo atual (dinâmico: lido a cada carga da Home, sem cache) — sem exigir nenhuma ação manual para recalcular — e MUST mostrar uma mensagem clara quando nenhum vínculo estiver em atenção ou esgotado.
- **FR-005**: Ao registrar a sessão de uma aula, o sistema MUST recusar a presença ("Presente") de um aluno cujo vínculo Pacote ativo no contexto da aula tenha saldo igual a 0: a aula MUST ser registrada como Realizada normalmente para os demais alunos, e o aluno esgotado MUST ficar sem presença, sem frequência e sem débito/cobrança registrados, com um aviso claro na resposta do registro informando que o pacote está esgotado e precisa ser renovado (o registro da aula nunca é recusado por causa de um aluno esgotado). O motivo MUST ser **persistido** no registro do aluno na aula (ver FR-012), para que o histórico da aula distinga esse caso de uma falta comum.
- **FR-006**: O bloqueio MUST valer por aluno/vínculo, nunca pela aula inteira: os demais alunos da mesma aula MUST continuar sendo processados normalmente (presença, frequência, cobrança/débito de saldo).
- **FR-007**: O sistema MUST NOT bloquear: ausência de aluno com pacote esgotado; aluno com saldo nulo; aluno com vínculo esgotado apenas em **outro** contexto; vínculos inativos; vínculos de modalidade diferente de Pacote.
- **FR-008**: O sistema MUST NOT exigir nenhuma ação adicional além da edição já existente do vínculo (specs/037) para liberar um aluno bloqueado: após o saldo passar a ser maior que zero, a presença MUST ser aceita e o painel MUST refletir o novo saldo.
- **FR-009**: Esta feature MUST NOT alterar o débito de saldo existente (specs/038): presença continua consumindo 1 do saldo, nunca abaixo de zero.
- **FR-010**: Esta feature MUST NOT alertar o próprio aluno, e MUST NOT renovar pacote nem gerar cobrança automaticamente ao esgotar — a renovação é sempre uma decisão e ação manual da professora.
- **FR-011**: O painel MUST herdar a mesma restrição de acesso do dashboard existente (somente o perfil Professor autenticado) e MUST NOT criar nenhum endpoint novo de acesso a esses dados — o sistema é single-tenant (uma professora por instalação), então não há isolamento entre professoras a implementar.
- **FR-012**: Quando a presença de um aluno não for registrada por pacote esgotado, o sistema MUST gravar o motivo ("Pacote esgotado") junto ao registro do aluno na aula, de modo que o histórico da aula distinga esse aluno de um que simplesmente faltou. Em qualquer outro caso (presente, ou falta comum) o motivo MUST permanecer vazio — vazio nunca significa bloqueio. O motivo MUST aparecer também ao consultar a aula depois do registro, não só na resposta imediata.

### Key Entities

- **Vínculo de Cobrança (existente, specs/037)**: fonte única do saldo. Nenhum atributo novo é necessário; o "estado" Atenção/Esgotado é derivado do `SaldoAulas` atual (2 ou menos / 0), nunca armazenado.
- **Registro de presença do aluno na aula (existente)**: ganha um atributo opcional "motivo de não registro", preenchido só quando a presença é barrada por pacote esgotado (valor único nesta fatia: pacote esgotado); vazio em todos os demais casos, inclusive nas aulas já existentes.
- **Item do painel de atenção**: visão derivada (não persistida) com aluno, contexto, saldo e estado (Atenção ou Esgotado/Bloqueado) — calculada a cada consulta a partir dos vínculos Pacote ativos.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A professora identifica, só abrindo a Home, 100% dos alunos com pacote de 2 aulas ou menos, sem abrir o cadastro de nenhum aluno.
- **SC-002**: 0 presenças de aluno com pacote esgotado (saldo 0) são aceitas, em qualquer aula, enquanto o saldo continuar em 0.
- **SC-003**: Em uma aula com vários alunos, o bloqueio de um aluno esgotado nunca impede o processamento correto dos demais (100% dos outros alunos têm presença, frequência e débito/cobrança corretos).
- **SC-004**: Após a professora atualizar o saldo de um pacote esgotado, o aluno é liberado e o painel se atualiza sem nenhuma ação adicional em 100% dos casos.
- **SC-005**: A professora distingue, num relance, "Atenção" de "Esgotado/Bloqueado" no painel (dois estados visualmente distintos, esgotados primeiro).

## Assumptions

- "Home (tela inicial)" é o painel `dashboard` da professora autenticada; o painel novo é uma seção dessa tela, não uma tela nova.
- "Dinâmico"/"atualizado" significa que o painel lê o saldo atual toda vez que a Home é carregada ou recarregada; não há atualização ao vivo (sem polling nem push) enquanto a professora deixa a tela aberta.
- O limite de alerta (saldo 2 ou menos) é uma regra fixa desta fatia, não um campo configurável (evita qualquer opção sem implementação real — Princípio III).
- "Contexto" do vínculo segue a convenção já usada em 038/039: nome da turma quando há turma; "Atendimento individual" quando não há.
- Saldo nulo em vínculo Pacote significa "nunca informado" e é tratado como fora do escopo de alerta e de bloqueio (preserva o comportamento atual de 038).
- A mensagem de bloqueio segue o padrão de mensagens de erro de negócio já usado pelo sistema (em português, clara e acionável).
- Fora de escopo, reafirmando o pedido: alertar o aluno; renovação ou cobrança automática ao esgotar.
