# Feature Specification: Ajustes de Layout — Turmas e Pagamentos

**Feature Branch**: `020-ajustes-layout-turmas-pagamentos`

**Created**: 2026-09-11

**Status**: Draft

**Input**: User description: "Três ajustes de UI: 1. Aba Turmas: quando não há turmas cadastradas, a mensagem \"não há turmas\" deve ficar centralizada na área de conteúdo (hoje não está). 2. Tela de detalhe/edição de Turma: o botão de vincular aluno está posicionado muito perto do campo \"selecione aluno\", causando cliques errados. Aumentar o espaçamento entre eles. 3. Aba Pagamentos: o filtro/seleção de \"métodos de pagamento\" deve ficar ACIMA da tabela de registros de pagamento (hoje está [onde estiver hoje — descreva a posição atual]). São ajustes visuais/de layout, sem mudança de comportamento ou dado."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Mensagem de "nenhuma turma" centralizada (Priority: P2)

Como professora, ao acessar a aba Turmas sem nenhuma turma cadastrada, eu vejo a mensagem de estado vazio centralizada na área de conteúdo, para que a tela não pareça quebrada ou desalinhada.

**Why this priority**: É um ajuste puramente visual, de baixo risco, mas afeta a primeira impressão de uma professora nova que ainda não cadastrou nenhuma turma.

**Independent Test**: Acessar a aba Turmas em uma conta sem nenhuma turma cadastrada e verificar visualmente que a mensagem aparece centralizada horizontalmente na área de conteúdo, não deslocada para a esquerda.

**Acceptance Scenarios**:

1. **Given** nenhuma turma cadastrada, **When** a professora acessa a aba Turmas, **Then** a mensagem de estado vazio ("Nenhuma turma encontrada") aparece centralizada horizontalmente em relação à largura total da área de conteúdo da página, e não apenas centralizada dentro de uma coluna estreita à esquerda.
2. **Given** pelo menos uma turma cadastrada, **When** a professora acessa a aba Turmas, **Then** a grade normal de cartões de turma é exibida como hoje, sem qualquer alteração de comportamento.

---

### User Story 2 - Espaçamento entre o campo "selecione aluno" e o botão "Vincular" (Priority: P1)

Como professora, na tela de detalhe de uma Turma, ao usar o campo de seleção de aluno para vinculá-lo à turma, eu vejo um espaçamento maior entre esse campo e o botão "Vincular", para não clicar no botão sem querer ao interagir com o campo.

**Why this priority**: É o ajuste com maior impacto funcional prático entre os três — hoje causa erros reais de clique (vincular um aluno não pretendido) durante o uso normal da tela.

**Independent Test**: Abrir o detalhe de uma turma com alunos disponíveis para vincular e verificar visualmente que há um espaçamento perceptivelmente maior entre o campo "Selecione um aluno..." e o botão "Vincular" do que existe hoje.

**Acceptance Scenarios**:

1. **Given** a tela de detalhe de uma turma com alunos disponíveis para vincular, **When** a professora visualiza o campo de seleção e o botão "Vincular" lado a lado, **Then** o espaço entre os dois elementos é visivelmente maior do que o espaçamento mínimo atual, reduzindo a chance de um clique destinado ao campo acabar acionando o botão (ou vice-versa).
2. **Given** o mesmo campo e botão, **When** a professora seleciona um aluno e clica deliberadamente em "Vincular", **Then** o vínculo é criado normalmente, sem nenhuma mudança no comportamento do clique em si — apenas no espaço visual entre os elementos.

---

### User Story 3 - Seção de métodos de pagamento posicionada acima da tabela de pagamentos (Priority: P2)

Como professora, na aba Pagamentos, eu vejo a seção "Métodos de pagamento" (com as formas de pagamento cadastradas no sistema) posicionada acima da tabela de registros de pagamento, em vez de abaixo dela como está hoje, para consultar rapidamente as formas disponíveis antes de olhar os lançamentos.

**Why this priority**: É um reordenamento de conteúdo já existente na página, sem introduzir informação nova — ajuste de prioridade visual, não de funcionalidade crítica.

**Independent Test**: Acessar a aba Pagamentos e verificar visualmente que a seção "Métodos de pagamento" (com a tabela de formas cadastradas) aparece antes da tabela de registros de pagamento na ordem de leitura da página, ao invés de depois, como ocorre atualmente.

**Acceptance Scenarios**:

1. **Given** a aba Pagamentos carregada, **When** a professora rola a página de cima para baixo, **Then** a seção "Métodos de pagamento" (título, subtítulo e a tabela de formas de pagamento cadastradas) aparece antes da tabela de registros de pagamento.
2. **Given** a mesma página, **When** a professora usa o filtro "Forma — todas" na barra de filtros (que já fica acima da tabela de registros), **Then** esse filtro continua funcionando exatamente como hoje — este ajuste não altera a barra de filtros, apenas a posição da seção informativa "Métodos de pagamento".
3. **Given** a reordenação aplicada, **When** a professora consulta os dados exibidos em qualquer uma das duas seções (filtros, tabela de pagamentos, tabela de métodos de pagamento), **Then** os dados exibidos são idênticos aos de hoje — só a posição vertical da seção "Métodos de pagamento" muda.

---

### Edge Cases

- O que acontece com a mensagem de "nenhuma turma" em telas muito estreitas (mobile)? Ela continua centralizada, apenas ocupando a largura disponível naquele tamanho de tela.
- O aumento de espaçamento entre o campo de seleção e o botão "Vincular" deve alterar a lógica de habilitar/desabilitar o botão (hoje desabilitado até que um aluno seja selecionado)? Não — o comportamento de habilitação permanece o mesmo, apenas o espaço visual muda.
- A reordenação da seção "Métodos de pagamento" deve alterar os dados carregados ou os filtros aplicáveis à tabela de pagamentos? Não — nenhum dado, filtro ou interação muda; apenas a ordem vertical das seções na página.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Na aba Turmas, quando não houver nenhuma turma cadastrada (ou nenhuma correspondente aos filtros aplicados), o sistema MUST exibir a mensagem de estado vazio centralizada em relação à largura total da área de conteúdo da página, não apenas centralizada dentro de uma coluna parcial.
- **FR-002**: Na tela de detalhe de Turma, o sistema MUST exibir um espaçamento maior entre o campo de seleção de aluno e o botão "Vincular" do que o espaçamento atual, de forma que os dois elementos sejam visualmente distinguíveis como alvos de clique separados.
- **FR-003**: Na aba Pagamentos, o sistema MUST exibir a seção "Métodos de pagamento" (título, subtítulo e tabela de formas de pagamento cadastradas) posicionada acima da tabela de registros de pagamento, em vez de abaixo dela.
- **FR-004**: Nenhum dos três ajustes MUST alterar dados exibidos, comportamento de filtros, validações, ou qualquer lógica de negócio das respectivas telas — as mudanças são estritamente de posicionamento e espaçamento visual.

### Key Entities *(include if feature involves data)*

- Não aplicável — esta funcionalidade não introduz nem altera entidades de dados; afeta somente a apresentação visual de telas já existentes (ver [Gerenciar Turma](../006-gerenciar-turma/spec.md) e [Registrar Pagamento](../009-registrar-pagamento/spec.md) para o comportamento funcional subjacente, que permanece inalterado).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em uma conta sem turmas cadastradas, a mensagem de estado vazio da aba Turmas aparece centralizada em relação à largura total da área de conteúdo, verificável visualmente em qualquer largura de tela suportada pelo sistema.
- **SC-002**: O espaço entre o campo de seleção de aluno e o botão "Vincular" na tela de detalhe de Turma aumenta de forma perceptível em relação ao estado atual, reduzindo a incidência de cliques não intencionais no botão.
- **SC-003**: Na aba Pagamentos, a seção "Métodos de pagamento" aparece antes da tabela de registros de pagamento na ordem de leitura da página, em 100% dos carregamentos da tela.
- **SC-004**: Nenhuma das três mudanças introduz regressão visível em dados exibidos, filtros ou ações disponíveis nas telas afetadas.

## Assumptions

- "Centralizada na área de conteúdo" significa centralizada horizontalmente em relação à largura total do painel de conteúdo da página (não apenas dentro da primeira coluna de uma grade de múltiplas colunas), já que hoje a mensagem de estado vazio da aba Turmas está contida como único item dentro de uma grade de 3 colunas, o que a desloca visualmente para a esquerda.
- "Aumentar o espaçamento" entre o campo de seleção e o botão "Vincular" é tratado como um ajuste de espaçamento visual (maior distância entre os dois elementos), não uma mudança de leiaute (por exemplo, não é pedido para empilhar os elementos verticalmente) — a decisão de quanto aumentar fica a critério de quem implementar, desde que perceptivelmente maior que o espaçamento mínimo atual.
- A "seção de métodos de pagamento" referida no pedido do usuário é a seção "Métodos de pagamento" (com a tabela de formas de pagamento cadastradas no sistema) da aba Pagamentos, que hoje está posicionada abaixo da tabela de registros de pagamento — e não o filtro "Forma — todas" da barra de filtros, que já está acima da tabela hoje e não precisa de ajuste.
- Este documento cobre apenas a especificação do comportamento esperado (o "o quê" e o "porquê"); a implementação do ajuste de CSS/layout em si está fora do escopo deste registro.
