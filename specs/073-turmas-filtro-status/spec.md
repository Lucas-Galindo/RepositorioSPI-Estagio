# Feature Specification: Turmas — filtro de status (ativas/inativas)

**Feature Branch**: `073-turmas-filtro-status`

**Created**: 2026-11-05

**Status**: Draft

> **Nota (2026-11-05, spec 076):** a tela de Turmas ganhou também o filtro de matéria (com a opção "Sem matéria"), junto do filtro de status; ver `specs/076-turmas-filtro-materia/`.

**Input**: Pedido do usuário (2026-11-05), card do Trello "Turmas: filtro de status (ativas/inativas)" (dividido do card "Turmas: filtros por status e matéria"): "Dentro da aba turma, coloque filtro para observarmos turmas inativas e ativas". O filtro de matéria ficou em outro card, que depende do vínculo Turma ↔ Matéria.

## Contexto (estado atual, lido do código)

- A tela de Turmas (cards) só tem a **busca por nome**; cada card já mostra a pílula Ativo/Inativo e a contagem "N turma(s) encontrada(s)" aparece acima.
- A API de turmas (`GET /api/turmas`) **já aceita o filtro `ativo`** e o cliente do frontend (`listarTurmas`) já tem o parâmetro; a tela só não o usa.
- A lista de Alunos já tem o padrão de filtro de status: um seletor "Status — todos / Ativo / Inativo" e o botão "Limpar filtros" quando há filtro aplicado.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver só turmas ativas ou só inativas (Priority: P1)

A professora abre Turmas e usa um seletor de status, no mesmo padrão da lista de Alunos, para ver todas, só as ativas ou só as inativas, combinando com a busca por nome.

**Why this priority**: É o pedido inteiro.

**Independent Test**: Abrir Turmas, escolher "Inativo" e ver só as turmas inativas; escolher "Ativo" e ver só as ativas; voltar a "todos".

**Acceptance Scenarios**:

1. **Given** a tela de Turmas, **When** ela abre, **Then** o seletor mostra "Status — todos" e todas as turmas aparecem (como hoje).
2. **Given** o seletor, **When** ela escolhe "Ativo" (ou "Inativo"), **Then** só as turmas daquele status aparecem e a contagem "N turma(s) encontrada(s)" acompanha.
3. **Given** um status escolhido e um texto na busca, **Then** os dois filtros valem juntos.
4. **Given** algum filtro aplicado, **Then** aparece o botão "Limpar filtros", que volta tudo ao padrão (status "todos" e busca vazia).
5. **Given** um filtro sem resultado, **Then** aparece o aviso "Nenhuma turma encontrada" já existente.

### Edge Cases

- Telas estreitas (360 px): a barra de filtros não gera rolagem horizontal da página.
- Modos claro e escuro: usa as mesmas classes do seletor da lista de Alunos (cores e contraste já verificados).
- Clicar num card continua abrindo a turma.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tela de Turmas MUST ter um seletor de status com "Status — todos", "Ativo" e "Inativo", antes da busca por nome, no mesmo visual do seletor da lista de Alunos.
- **FR-002**: O filtro MUST ser aplicado pelo servidor (parâmetro `ativo` que a API já aceita); o frontend MUST NOT filtrar, reordenar nem contar as turmas por conta própria.
- **FR-003**: O seletor MUST combinar com a busca por nome; o botão "Limpar filtros" MUST aparecer só com filtro aplicado e limpar os dois.
- **FR-004**: A mudança MUST NOT alterar o backend, os contratos da API, o banco nem o `package.json`, e MUST NOT criar arquivo novo.
- **FR-005**: O resultado sem filtro MUST NOT mudar (mesmas turmas, mesma ordem, mesmos cards).

### Key Entities

Nenhuma nova. Usa a turma (`ativo`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Com "Ativo" só aparecem turmas ativas e com "Inativo" só inativas, em 100% dos casos testados; a contagem bate com os cards.
- **SC-002**: Sem filtro, a lista é idêntica à de antes.
- **SC-003**: 0 linha de backend, banco ou dependência alterada; 0 arquivo novo; sem rolagem horizontal em 360 px.

## Assumptions

- O padrão é "todos" (como hoje).
- Os textos seguem a lista de Alunos ("Status — todos", "Ativo", "Inativo", "Limpar filtros").

## Fora de escopo

- Filtro de matéria (card separado, depende do vínculo Turma ↔ Matéria).
- Filtro por tipo da turma (Mensalidade/Pacote).
