# Feature Specification: Aulas — filtros de data, horário, matéria e status

**Feature Branch**: `074-aulas-filtros`

**Created**: 2026-11-05

**Status**: Draft

**Input**: Pedido do usuário (2026-11-05), card do Trello "Aulas: filtros de data (De/Até), horário (De/Até), matéria e status": "Dentro da aba de aulas, colocar filtro de data (já com o calendário criado embutido), matéria, horário (de forma intervalada) e status".

## Contexto (estado atual, lido do código)

- A tela de Aulas tem só os filtros **Status** (Agendada/Realizada/Cancelada) e **Turma**; a tabela mostra Data, Horário, Matéria, Turma/Aluno e Status.
- `GET /api/aulas` já aceita `status`, `turmaId`, `alunoId`, `dataInicio` e `dataFim`. **Não filtra por matéria nem por horário.**
- O campo de data com o calendário do sistema (`DateInput`, spec 070) e o campo de horário `HH:mm` (`TimeInput`) já existem. Outras telas (Agenda, Home, Alunos, Turmas, Financeiro, Relatórios...) usam `GET /api/aulas` e `listarAulas` e não podem mudar de resultado.

## Clarificações (respondidas pelo usuário em 2026-11-05)

- **Data**: intervalo **De/Até** (dois campos com o calendário embutido).
- **Horário**: intervalo **De/Até**, sobre o **horário de início** da aula.
- **Matéria** e **Status** como filtros; a **Turma** continua.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Filtrar aulas por período, horário, matéria e status (Priority: P1)

A professora combina, na tela de Aulas, data De/Até (com calendário), horário De/Até, matéria, status e turma para achar as aulas que quer.

**Why this priority**: É o pedido inteiro.

**Independent Test**: Em Aulas, filtrar por uma matéria, depois por um intervalo de datas, depois por um intervalo de horários e conferir a lista e a contagem em cada passo.

**Acceptance Scenarios**:

1. **Given** a tela de Aulas sem filtro, **When** ela abre, **Then** mostra todas as aulas como hoje, com os campos "Data" (De/Até), "Horário" (De/Até), "Matéria", "Status" e "Turma" vazios.
2. **Given** a matéria "Matemática", **When** ela escolhe, **Then** só aulas dessa matéria aparecem.
3. **Given** data De e/ou Até, **Then** aparecem só aulas com data dentro do intervalo (limites inclusos; só "De" = a partir dessa data; só "Até" = até essa data).
4. **Given** horário De e/ou Até, **Then** aparecem só aulas cujo **horário de início** está no intervalo (limites inclusos; só "De" = a partir; só "Até" = até).
5. **Given** vários filtros, **Then** valem todos juntos (E).
6. **Given** algum filtro aplicado, **Then** aparece "Limpar filtros", que esvazia todos e a lista volta ao total.
7. **Given** filtros sem resultado, **Then** aparece "Nenhuma aula encontrada" e a contagem mostra 0.
8. **Given** um "De" posterior ao "Até" (data ou horário), **Then** a lista fica vazia, sem erro.

### Edge Cases

- Horário com só um dos limites ou campo incompleto (digitando): só vale quando completo (`HH:mm`); incompleto não filtra.
- Aula que começa exatamente no limite entra (limites inclusos).
- Telas estreitas (360 px): a barra de filtros quebra em linhas, sem rolagem horizontal da página.
- A ordem da lista continua por data e horário de início.
- Clicar numa linha continua abrindo a aula.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tela de Aulas MUST ter os filtros Data (De/Até, com o calendário do sistema), Horário (De/Até), Matéria, Status e Turma, e o botão "Limpar filtros" quando houver filtro.
- **FR-002**: `GET /api/aulas` MUST aceitar os parâmetros novos opcionais `materiaId`, `horaInicioDe` e `horaInicioAte` (`HH:mm`), aplicados **no servidor**, junto dos existentes; o horário compara o horário de início, com limites inclusos.
- **FR-003**: O frontend MUST NOT filtrar, ordenar nem contar as aulas por conta própria: só envia os parâmetros e exibe a lista e o total recebidos.
- **FR-004**: Sem os parâmetros novos, `GET /api/aulas` e todos os consumidores atuais (Agenda, Home, Alunos, Turmas, Financeiro, Relatórios, Lembretes, Matérias) MUST NOT mudar de resultado.
- **FR-005**: A mudança MUST NOT criar tabela, coluna ou migração, MUST NOT adicionar dependência e MUST NOT criar arquivo novo no frontend.
- **FR-006**: Os campos de data e de horário MUST usar os componentes existentes (`DateInput` e `TimeInput`); MUST NOT voltar a usar campo nativo de data/hora.

### Key Entities

Nenhuma nova. Usa a aula (data, horário de início, matéria, turma, status).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Cada filtro, sozinho e combinado, devolve exatamente as aulas esperadas, conferido contra a lista completa do banco local.
- **SC-002**: Sem filtro, a lista de Aulas e as respostas de `GET /api/aulas` dos consumidores atuais são idênticas às de antes.
- **SC-003**: 0 migração, 0 dependência, 0 arquivo novo no frontend; sem rolagem horizontal da página em 360 px.

## Assumptions

- "Horário" filtra o horário de **início** (o card diz "horário de forma intervalada"; a tabela mostra início — fim).
- A lista de matérias do filtro traz todas as matérias cadastradas (inclusive inativas, porque aulas antigas podem usá-las).
- Parâmetro de horário inválido recebe o 400 padrão da API.

## Fora de escopo

- Filtro por aluno na tela de Aulas, ordenação por coluna e paginação.
- Mudar a Agenda, a Home ou os relatórios.
