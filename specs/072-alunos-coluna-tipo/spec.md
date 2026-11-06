# Feature Specification: Alunos — coluna "Tipo" de cobrança na tabela

**Feature Branch**: `072-alunos-coluna-tipo`

**Created**: 2026-11-05

**Status**: Draft

**Input**: Pedido do usuário (2026-11-05), card do Trello "Alunos: nova coluna 'Tipo' de cobrança na tabela": "na aba alunos e na tabela [...] uma coluna depois de 'Cobrança' para que tenha disposto o tipo de cobrança (Pacote, anuidade, mensalidade...)".

## Contexto (estado atual, lido do código)

- A tabela da lista de Alunos tem as colunas Aluno, RA, Turma(s), Telefone, Valor da aula, **Cobrança** e Status.
- A coluna "Cobrança" usa o selo da spec 068 (`SeloCobranca`, modo "lista"): uma **pílula com o tipo** (Mensalidade, Pacote com "· restam N", Semestral, Anual, Avulsa ou "Sem vínculo"), um **"+N"** quando o aluno tem outros vínculos (com a lista completa na dica ao passar o mouse), e abaixo o **valor** e a **turma** (mais "pacote esgotado" quando é o caso).
- O resumo vem pronto do servidor; o frontend só exibe (Princípio II). O mesmo selo também é usado, no modo "grupo", na página da turma e nos detalhes da aula.

## Clarificações (respondidas pelo usuário em 2026-11-05)

- Nova coluna **"Tipo"** com a pílula; a coluna **"Cobrança"** passa a mostrar **só valor e turma** (sem repetir o tipo). A nova coluna fica **depois de "Cobrança"**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver o tipo de cobrança numa coluna própria (Priority: P1)

A professora abre a lista de alunos e vê, depois da coluna "Cobrança", uma coluna **"Tipo"** com a pílula do tipo de cobrança de cada aluno; a coluna "Cobrança" mostra o valor e a turma.

**Why this priority**: É o pedido inteiro; deixa o tipo visível e alinhado numa coluna, sem misturar com o valor.

**Independent Test**: Abrir Alunos e conferir, para cada linha, a pílula na coluna "Tipo" e o valor e a turma na coluna "Cobrança".

**Acceptance Scenarios**:

1. **Given** a lista de alunos, **When** ela abre a tela, **Then** o cabeçalho mostra "...Valor da aula, Cobrança, Tipo, Status", nessa ordem.
2. **Given** um aluno com pacote, **When** ela olha a linha, **Then** a coluna "Tipo" mostra a pílula "Pacote · restam N" (na mesma cor de hoje) e a coluna "Cobrança" mostra o valor e a turma (e "pacote esgotado" quando for o caso), sem repetir o tipo.
3. **Given** um aluno com Mensalidade, Semestral, Anual ou Avulsa, **Then** a coluna "Tipo" mostra a pílula do tipo correspondente.
4. **Given** um aluno sem vínculo ativo, **Then** a coluna "Tipo" mostra "Sem vínculo" e a coluna "Cobrança" continua mostrando o valor da aula como o servidor informa.
5. **Given** um aluno com mais de um vínculo ativo, **Then** a coluna "Tipo" mostra a pílula principal com o "+N", e ao passar o mouse a dica lista todos os vínculos (como hoje).
6. **Given** um aluno sem resumo de cobrança, **Then** as duas colunas mostram "—".

---

### Edge Cases

- Telas estreitas (360 px): a tabela já rola na horizontal; a coluna nova não pode quebrar o layout da página (sem rolagem horizontal da página inteira).
- Modos claro e escuro e modos de cor: a pílula usa as mesmas classes e cores de hoje (contraste já verificado).
- A página da turma e os detalhes da aula (modo "grupo") **não mudam**.
- Clicar na linha continua abrindo o aluno; a coluna nova não tem ação própria.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tabela de Alunos MUST ter a coluna **"Tipo"** imediatamente depois de "Cobrança" e antes de "Status".
- **FR-002**: A coluna "Tipo" MUST mostrar a pílula do tipo (com "· restam N" no pacote), o "+N" de outros vínculos e a dica com todos os vínculos, exatamente como a pílula de hoje (mesmas classes, cores e texto).
- **FR-003**: A coluna "Cobrança" MUST mostrar só o valor e a turma (e o aviso "pacote esgotado" quando houver), sem a pílula nem o "+N".
- **FR-004**: O frontend MUST NOT decidir, calcular ou reordenar nada do resumo de cobrança (tipo, valor, vínculos, aviso): só exibe o que o servidor envia.
- **FR-005**: O selo no modo "grupo" (página da turma e detalhes da aula) MUST NOT mudar.
- **FR-006**: A mudança MUST NOT criar arquivo novo, alterar o backend, os contratos da API, o banco ou o `package.json`.

### Key Entities

Nenhuma. Usa o resumo de cobrança (`cobranca`) que já vem em cada aluno da lista.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das linhas da lista mostram o tipo na coluna "Tipo" e nenhuma repete o tipo na coluna "Cobrança".
- **SC-002**: A ordem do cabeçalho é "Aluno, RA, Turma(s), Telefone, Valor da aula, Cobrança, Tipo, Status".
- **SC-003**: 0 diferença na página da turma e nos detalhes da aula; 0 linha de backend, banco ou dependência alterada; 0 arquivo novo.
- **SC-004**: Sem rolagem horizontal da página em 360 px.

## Assumptions

- Cada aluno tem um único resumo de cobrança (`cobranca`), com a categoria principal e a contagem de outros vínculos (spec 068).
- O nome da coluna é "Tipo" (como pedido no card).

## Fora de escopo

- Uma linha por vínculo, filtro por tipo e ordenação por tipo.
- Mudanças nas outras telas que usam o selo.
