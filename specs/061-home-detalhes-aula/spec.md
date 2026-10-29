# Feature Specification: Detalhes da Aula em Pop-up na Agenda Semanal da Home

**Feature Branch**: `061-home-detalhes-aula`

**Created**: 2026-10-29

**Status**: Draft

**Input**: Card Trello "Home: abrir detalhes da aula ao clicar na agenda semanal" (origem: anotações da reunião do dia 22). O card pede para estudar a melhor opção, pop-up ou nova guia, e implementar a escolhida.

## Contexto (estado atual, lido do código)

- A Home mostra a agenda da semana (segunda a domingo), com as aulas de cada dia.
- Hoje, cada aula da agenda é um link que **navega na mesma guia** para a página completa da aula (`/aulas/{id}`). A professora sai da Home para ver um detalhe e precisa voltar.
- A página da aula já tem todos os detalhes e as ações (registrar presença, cancelar, editar, reposição).
- A página Agenda também abre um pop-up ao clicar numa aula, mas é um **formulário de edição** (criar e editar aula), e não uma visão de detalhes. Por isso ele não serve para "ver detalhes" na Home.
- Os dados de cada aula da agenda semanal já chegam completos para a Home (matéria, data, horário, turma ou alunos, status, descrição e a lista de alunos com a presença), então abrir os detalhes não exige nova consulta ao servidor.

## Decisão sobre o estudo pedido no card

Foram comparadas as duas opções:

| Opção | Vantagem | Desvantagem |
|---|---|---|
| **Pop-up** (escolhida) | A professora vê o resumo sem sair da Home e fecha com um clique; a Home continua como estava. Não precisa de rede. | Não substitui a página completa; por isso o pop-up leva um atalho para ela. |
| Nova guia | Mantém a Home aberta e mostra a página completa. | Acumula guias; no celular vira um caminho confuso; mostra a página inteira quando o pedido é só dar uma olhada. |

O pop-up é o padrão já usado pelo sistema para consultas rápidas (avisos de notificação). O pedido é de dar uma olhada, então o pop-up atende melhor.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver os detalhes da aula sem sair da Home (Priority: P1)

A professora clica numa aula da agenda semanal e abre um pop-up com os detalhes da aula. Ela fecha o pop-up e continua na Home, na mesma semana, sem recarregar.

**Why this priority**: É o objetivo do card.

**Independent Test**: Na Home, clicar em uma aula da semana e conferir que o pop-up abre com os dados corretos, sem trocar de página; fechar e conferir que a Home continua igual.

**Acceptance Scenarios**:

1. **Given** a agenda semanal com aulas, **When** a professora clica numa aula, **Then** abre um pop-up com matéria, data, horário, turma (ou "Atendimento individual" com o nome do aluno), status e, se houver, a descrição.
2. **Given** o pop-up aberto, **When** ela clica em "Fechar", fora do pop-up ou na tecla Esc, **Then** ele fecha e a Home continua na mesma posição.
3. **Given** uma aula realizada, **When** o pop-up abre, **Then** mostra a lista de alunos com "Presente" ou "Faltou". Numa aula agendada, mostra só os nomes.
4. **Given** uma aula de reposição, **When** o pop-up abre, **Then** o aluno de reposição aparece marcado como "Reposição".
5. **Given** uma aula cancelada, **When** o pop-up abre, **Then** ela aparece com o status "Cancelada".

---

### User Story 2 - Ir para a aula completa quando precisar agir (Priority: P1)

No pop-up, um botão "Abrir aula" leva à página completa, onde a professora registra a presença, cancela, edita ou adiciona reposição.

**Why this priority**: O pop-up só mostra; as ações continuam na página da aula.

**Independent Test**: Abrir o pop-up e clicar em "Abrir aula": a página `/aulas/{id}` da mesma aula abre.

**Acceptance Scenarios**:

1. **Given** o pop-up aberto, **When** ela clica em "Abrir aula", **Then** abre a página da mesma aula.
2. **Given** o pop-up aberto, **When** ela usa o teclado, **Then** consegue focar e acionar "Abrir aula" e "Fechar".

### Edge Cases

- **Aula sem descrição**: o campo simplesmente não aparece.
- **Aula individual**: mostra "Atendimento individual" e o nome do aluno, como nas notificações.
- **Muitos alunos na turma**: a lista de alunos rola dentro do pop-up, sem estourar a altura da tela.
- **Celular**: o pop-up ocupa a largura da tela, com margem, e nada transborda.
- **Troca de semana ou nova carga da Home** com o pop-up aberto: o pop-up mostra os dados que estavam carregados quando foi aberto, sem quebrar.
- **Clique com tecla de atalho** (Ctrl ou Cmd, ou botão do meio) numa aula da agenda: deixa de abrir em nova guia, pois a aula agora é um botão; o caminho para a guia nova é o botão "Abrir aula" do pop-up. Isso é aceitável porque a aula tem um destino único e o pop-up é o ponto de entrada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Clicar numa aula da agenda semanal da Home MUST abrir um pop-up de detalhes, sem navegar para outra página.
- **FR-002**: O pop-up MUST mostrar: matéria, data, horário de início e fim, turma (ou "Atendimento individual" com o nome do aluno), status e, quando existir, a descrição.
- **FR-003**: O pop-up MUST listar os alunos da aula. Numa aula realizada, MUST mostrar "Presente" ou "Faltou" de cada um. Aluno de reposição MUST aparecer marcado.
- **FR-004**: O pop-up MUST ter o botão "Abrir aula", que abre a página completa da mesma aula, e o botão "Fechar".
- **FR-005**: O pop-up MUST fechar por "Fechar", por clique fora dele e pela tecla Esc, devolvendo o foco para a aula que o abriu.
- **FR-006**: O pop-up MUST NOT permitir editar, cancelar, excluir nem registrar presença; essas ações continuam só na página completa da aula.
- **FR-007**: Abrir o pop-up MUST NOT fazer nova consulta ao servidor: usa os dados da agenda já carregados.
- **FR-008**: O pop-up MUST funcionar em modo claro e escuro e em telas estreitas, usando apenas as cores do tema, sem rolagem horizontal.
- **FR-009**: O pop-up MUST ser reutilizável, para outras telas poderem abri-lo com os mesmos dados (por exemplo, a Agenda no futuro), sem copiar o código.

### Key Entities

- **Aula** (já existente): matéria, data, horário, turma ou aluno, status, descrição e alunos com presença. Nenhum campo novo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Da Home, a professora vê os detalhes de qualquer aula da semana em 1 clique e volta à Home em 1 clique, sem recarregar a página.
- **SC-002**: O pop-up abre sem nova consulta ao servidor.
- **SC-003**: Do pop-up até a página completa da aula são 1 clique.
- **SC-004**: Zero ações de edição disponíveis no pop-up.

## Assumptions

- A escolha entre pop-up e nova guia fica registrada neste documento e no card, como pedido ("estudar a melhor opção").
- O pop-up usa o mesmo estilo de pop-up já usado pelas notificações, e as cores do tema da spec 057.
- A página Agenda não muda nesta entrega. Ela mantém o formulário de edição que já tem.
- Os dados de presença só existem em aulas realizadas; em aulas agendadas a lista mostra apenas os nomes.
