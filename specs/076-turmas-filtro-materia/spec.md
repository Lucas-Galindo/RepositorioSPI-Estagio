# Feature Specification: Turmas — filtro de matéria

**Feature Branch**: `076-turmas-filtro-materia`

**Created**: 2026-11-05

**Status**: Draft

**Input**: Pedido do usuário (2026-11-05), card do Trello "Turmas: filtro de matéria (depende do vínculo Turma ↔ Matéria)" (dividido do card de filtros de Turmas; retomado depois de ficar em stand by): "filtro de matéria cadastrada para aquela turma".

## Contexto (estado atual, lido do código)

- A tela de Turmas tem a busca por nome e o filtro de status (spec 073) e mostra a matéria de cada turma no card (spec 075).
- A turma tem uma matéria **opcional** (`turma.materia_id`, spec 075). No banco local, 3 das 4 turmas estão **sem matéria** (só a "Turma teste" tem "Matemática").
- A API de turmas (`GET /api/turmas`) filtra por `nome` e `ativo`; **não filtra por matéria**. O seletor de matérias do sistema já existe (`listarMaterias`).

## Clarificações

- Decisão de escopo desta spec: além de escolher uma matéria, o filtro oferece **"Sem matéria"**, porque as turmas existentes podem estar sem matéria (veja Assumptions); a professora pode vetar isso na revisão.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Filtrar as turmas por matéria (Priority: P1)

A professora escolhe, num seletor da tela de Turmas, uma matéria (ou "Sem matéria") e vê só as turmas que têm aquela matéria, combinando com o status e a busca por nome.

**Why this priority**: É o pedido inteiro.

**Independent Test**: Em Turmas, escolher "Matemática" e ver só as turmas dessa matéria; escolher "Sem matéria" e ver as sem matéria; voltar a "todas".

**Acceptance Scenarios**:

1. **Given** a tela de Turmas, **When** ela abre, **Then** o seletor mostra "Matéria — todas" e todas as turmas aparecem (como hoje).
2. **Given** o seletor, **Then** ele lista "Matéria — todas", "Sem matéria" e as matérias cadastradas.
3. **Given** uma matéria escolhida, **Then** só as turmas dessa matéria aparecem e a contagem "N turma(s) encontrada(s)" acompanha.
4. **Given** "Sem matéria", **Then** só as turmas sem matéria aparecem.
5. **Given** matéria + status + busca por nome, **Then** os três filtros valem juntos (E).
6. **Given** algum filtro aplicado (inclusive a matéria), **Then** "Limpar filtros" aparece e volta tudo ao padrão (status e matéria "todos", busca vazia).
7. **Given** uma matéria que foi inativada depois, **Then** ela continua no seletor e as turmas dela continuam filtráveis.
8. **Given** um filtro sem resultado, **Then** aparece "Nenhuma turma encontrada".

### Edge Cases

- Matéria sem nenhuma turma: lista vazia, sem erro.
- Telas estreitas (360 px): a barra de filtros quebra em linhas, sem rolagem horizontal da página.
- Clicar no card continua abrindo a turma; a ordem da lista (por nome) não muda.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `GET /api/turmas` MUST aceitar os parâmetros opcionais `materiaId` (só turmas dessa matéria) e `semMateria=true` (só turmas sem matéria), aplicados **no servidor**, junto de `nome` e `ativo`.
- **FR-002**: A tela de Turmas MUST ter o seletor de matéria ("Matéria — todas", "Sem matéria" e as matérias cadastradas, inclusive inativas) antes da busca por nome, e o "Limpar filtros" MUST limpá-lo também.
- **FR-003**: O frontend MUST NOT filtrar, reordenar nem contar as turmas por conta própria: só envia os parâmetros e exibe a lista e o total recebidos.
- **FR-004**: Sem os parâmetros novos, `GET /api/turmas` e todos os consumidores atuais (Aulas, Alunos, Lembretes, Financeiro, Relatórios...) MUST NOT mudar de resultado.
- **FR-005**: `materiaId` e `semMateria=true` juntos são uma combinação inválida: o servidor MUST recusá-la com 400 (o seletor da tela nunca envia os dois).
- **FR-006**: A mudança MUST NOT criar tabela, coluna ou migração, MUST NOT adicionar dependência e MUST NOT criar arquivo novo no frontend.

### Key Entities

Nenhuma nova. Usa a matéria da turma (spec 075).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Cada filtro de matéria devolve exatamente as turmas esperadas, conferido contra a lista completa do banco local, sozinho e combinado com status e nome.
- **SC-002**: Sem os parâmetros novos, as respostas de `GET /api/turmas` são idênticas às de antes.
- **SC-003**: 0 migração, 0 dependência, 0 arquivo novo no frontend; sem rolagem horizontal em 360 px.

## Assumptions

- "Sem matéria" é útil porque, após a migração 27, só as turmas cujas aulas usam uma única matéria receberam uma (no banco local, 3 de 4 estão sem matéria).
- O seletor lista **todas** as matérias (ativas e inativas): turmas antigas podem ter matéria depois inativada.
- Matéria e "Sem matéria" são excludentes (um único seletor).

## Fora de escopo

- Filtro por várias matérias ao mesmo tempo, por tipo de turma e ordenação por matéria.
- Mudar o formulário de turma ou o filtro de status.
