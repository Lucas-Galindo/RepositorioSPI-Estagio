# Feature Specification: Relatório de Alunos

**Feature Branch**: `017-relatorio-alunos`

**Created**: 2026-09-11

**Status**: Implemented (reorganizado — a tela existe, mas não usa o endpoint originalmente previsto)

**Input**: Registro retroativo do comportamento real da Estória 17 (Relatório de Alunos) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

O endpoint dedicado (`GET /api/relatorios/alunos`) existe no backend, mas está órfão. Existe, porém, uma tela real com nome muito parecido ("Relatórios → Dashboard de Alunos", rota `/relatorios/alunos`) que **não usa esse endpoint** — ela calcula seus próprios indicadores no frontend, combinando dados de Alunos, Pagamentos, Aulas e Matérias obtidos de outras APIs.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver dashboard de indicadores de alunos (Priority: P1)

Como professora, eu acesso "Relatórios → Dashboard de Alunos" para ver a distribuição de alunos por turma e por situação (ativo/inativo/em turma/individual).

**Why this priority**: É a visão analítica real disponível hoje sobre a base de alunos.

**Independent Test**: Acessar a tela e conferir que a distribuição por turma/situação bate com os dados reais de Alunos e AlunoTurma.

**Acceptance Scenarios**:

1. **Given** a tela Dashboard de Alunos, **When** carregada, **Then** exibe a distribuição de alunos por matéria principal e por situação (Ativo/Inativo/Em turma/Individual).
2. **Given** a mesma tela, **When** a professora consulta a lista de alunos exibida, **Then** vê Nome, RA, Matéria Principal, Turma e Telefone de cada um, com filtro de busca por nome, RA ou matéria.

---

### Edge Cases

- A tela "Dashboard de Alunos" (`/relatorios/alunos`) usa o endpoint `/api/relatorios/alunos`? Não — consome `/api/alunos`, `/api/pagamentos`, `/api/aulas` e `/api/materias` diretamente, calculando tudo no cliente.
- Existe alguma outra tela chamando o endpoint de relatório de alunos dedicado? Não foi encontrada nenhuma.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer uma visão de indicadores agregados sobre a base de alunos: distribuição por matéria principal e por situação (Ativo/Inativo/Em turma/Individual).
- **FR-002**: Essa visão MUST incluir uma lista de alunos com Nome, RA, Matéria Principal, Turma e Telefone, com busca por nome, RA ou matéria.
- **FR-003**: O sistema também MUST manter disponível (mesmo sem consumidor de tela) uma consulta de relatório de alunos filtrável por Nome, Turma e Status de Atividade, retornando Nome, RA, telefones, email do responsável, turmas vinculadas, valor da aula, frequência e status.

### Key Entities *(include if feature involves data)*

- Não introduz entidades próprias — projeta dados de Aluno (ver [Gerenciar Alunos](../003-gerenciar-alunos/spec.md)).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A professora consegue visualizar, em uma única tela, quantos alunos tem por matéria e por situação, sem precisar somar manualmente a lista de alunos.

## Assumptions

- O endpoint dedicado de relatório de alunos é mantido no backend como capacidade latente, não como parte de um fluxo de usuário ativo hoje.
- A tela real ("Dashboard de Alunos") é tratada como a implementação de fato desta necessidade, ainda que tecnicamente desacoplada do endpoint que a ERS original previa.
