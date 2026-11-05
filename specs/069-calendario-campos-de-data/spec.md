# Feature Specification: Calendário nos campos de data

**Feature Branch**: `069-calendario-campos-de-data`

**Created**: 2026-11-04

**Status**: Draft

> **Nota (2026-11-05, spec 070):** o calendário que o botão abre passou a ser o **calendário próprio do sistema** (`specs/070-calendario-proprio/`), no lugar do calendário do navegador; o `<input type="date">` auxiliar, o `showPicker()` e as regras `color-scheme` desta spec foram removidos. O botão dentro do campo, o `min` e a regra "o texto exibido manda" continuam valendo.

**Input**: Card do Trello "Ajustar filtros de datas" (Backlog): "Fazer uma revisão geral no sistema procurando filtros e colocar o botão dentro do campo para abrir o calendário. Ajustar o botão de clicar no calendário em relação às cores dele, onde se a cor for escura o tema dele é claro (assim como outros elementos) e vice e versa."

## Contexto (estado atual, lido do código)

- Quase todos os campos de data do sistema usam o componente compartilhado `DateInput` (campo de texto com máscara `dd/mm/aaaa`, criado para o formato não depender do idioma do navegador). **Esse campo não tem calendário**: a professora só consegue digitar. São 23 usos em 14 arquivos: filtros de período (Home, Financeiro, Contas a Pagar e a Receber, Lembretes, os quatro Relatórios) e formulários (nova e editar conta, data da aula).
- Há **5 campos de data nativos** (`<input type="date">`) em dois formulários: as datas da turma de pacote (`TurmaForm`) e as do plano Semestral/Anual (`VinculoCobrancaFormModal`, spec 062). Mostram o ícone de calendário do navegador, mas o formato depende do navegador e o ícone **não acompanha o tema** (no modo escuro o ícone continua escuro, quase invisível).
- Há **4 campos de competência** nativos (`<input type="month">`) nas telas de nova/editar conta a pagar e a receber, com o mesmo problema de cor.
- O sistema tem modo claro e escuro (classe `dark-mode` na raiz) e tokens de cor verificados por contraste (spec 057).

## Clarificações (respondidas pelo usuário em 2026-11-04)

- **Calendário:** usar o **calendário do próprio navegador** por enquanto (não criar um calendário próprio).
- **Escopo:** a proposta apresentada foi "todos os campos de data, não só os filtros"; o usuário mandou seguir o fluxo normal com ela (registrado como decisão, revisável na aprovação).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Abrir o calendário dentro do campo de data (Priority: P1)

A professora, em qualquer campo de data (filtro de período ou formulário), vê um botão de calendário **dentro do campo**. Ao clicar nele, abre o calendário do navegador; ao escolher um dia, o campo passa a mostrar `dd/mm/aaaa`. Digitar continua funcionando como hoje.

**Why this priority**: É o pedido central do card e remove a obrigação de digitar a data.

**Independent Test**: Abrir a tela de Contas a Receber, clicar no botão do filtro "a partir de", escolher um dia e conferir o campo e a lista filtrada.

**Acceptance Scenarios**:

1. **Given** um campo de data vazio, **When** a professora clica no botão de calendário, **Then** o calendário do navegador abre e, ao escolher um dia, o campo mostra a data escolhida no formato `dd/mm/aaaa`.
2. **Given** um campo já preenchido (digitado ou escolhido), **When** ela abre o calendário, **Then** ele abre no dia/mês atuais do campo.
3. **Given** um campo parcialmente digitado ou inválido, **When** ela abre o calendário, **Then** ele abre no mês de hoje, sem erro e sem alterar o texto digitado até ela escolher um dia.
4. **Given** um campo desabilitado (por exemplo, a data de uma aula já realizada), **When** a tela é exibida, **Then** o botão também está desabilitado e não abre nada.
5. **Given** um filtro de período preenchido pelo calendário, **When** os resultados são carregados, **Then** o filtro usa exatamente o mesmo valor que usaria se a data tivesse sido digitada.
6. **Given** o teclado, **When** ela navega por Tab, **Then** o botão recebe foco com indicador visível e abre o calendário com Enter ou Espaço; o campo de texto continua acessível por Tab.

---

### User Story 2 - O botão e o calendário acompanham o tema (Priority: P1)

O botão de calendário usa as cores do tema atual (claro ou escuro, e os modos de cor): fundo claro → ícone escuro; fundo escuro → ícone claro, sempre legível. O calendário que o navegador abre também usa o tema claro ou escuro correspondente.

**Why this priority**: É a segunda metade do pedido do card (hoje o ícone some no modo escuro).

**Independent Test**: Alternar o modo claro/escuro numa tela com filtros de data e conferir o ícone nos dois; rodar a verificação de contraste.

**Acceptance Scenarios**:

1. **Given** o modo claro, **When** a tela é exibida, **Then** o ícone tem contraste mínimo de 4,5:1 contra o fundo do campo.
2. **Given** o modo escuro, **When** a tela é exibida, **Then** o ícone tem contraste mínimo de 4,5:1 contra o fundo do campo (e não fica escuro sobre fundo escuro).
3. **Given** qualquer modo de cores e destaque configurados, **When** a verificação de contraste roda, **Then** nenhum par do botão fica abaixo de 4,5:1.
4. **Given** a professora alterna o tema com um calendário já visível na tela (campo em foco), **When** o tema muda, **Then** o botão muda de cor na hora, sem recarregar.
5. **Given** o modo escuro, **When** o calendário do navegador é aberto, **Then** ele usa o tema escuro (e o claro no modo claro).

---

### User Story 3 - Todos os campos de data iguais (Priority: P2)

Os campos de data nativos dos formulários (turma de pacote, plano Semestral/Anual) passam a usar o mesmo campo compartilhado, com o mesmo formato e o mesmo botão; os campos de competência (mês e ano) passam a seguir o tema (ícone e calendário).

**Why this priority**: Consistência e correção da cor do ícone onde ele hoje some; depende da US1 e da US2.

**Independent Test**: Abrir "Nova turma de pacote", "Novo vínculo Semestral" e "Nova conta a receber" e conferir formato, botão e cores iguais aos filtros.

**Acceptance Scenarios**:

1. **Given** o formulário de turma de pacote, **When** a professora preenche início e fim, **Then** os campos mostram `dd/mm/aaaa`, têm o botão de calendário e salvam as mesmas datas de antes.
2. **Given** o modal do plano Semestral/Anual, **When** ela edita o início, o 1º vencimento e a data de cada parcela, **Then** as parcelas continuam sendo sugeridas e salvas como antes.
3. **Given** o campo de competência de uma conta, **When** o modo escuro está ativo, **Then** o ícone do calendário e o seletor de mês aparecem no tema escuro.

---

### Edge Cases

- Navegador sem suporte ao seletor de data programático: o botão deve continuar sem causar erro (cai num comportamento equivalente ou fica sem efeito), e a digitação segue funcionando.
- Data digitada válida no formato mas impossível (31/02): o comportamento atual é mantido (a máscara só aceita dia 1–31 e mês 1–12; o servidor valida o resto).
- Campo dentro de modal, de linha de tabela de parcelas ou em tela de 360 px: o botão não pode quebrar o layout nem causar rolagem horizontal.
- Preencher pelo calendário e depois apagar o texto: o valor volta a vazio (filtro limpo), como hoje.
- Dois campos de período (de/até) na mesma tela: cada botão abre o calendário do seu campo.
- "Fim da turma" com início já escolhido: o calendário não deixa escolher dia anterior ao início; se a professora **digitar** uma data anterior, o servidor recusa com a mensagem de sempre (a trava do navegador no envio do formulário, que só existia no campo nativo, deixa de existir).
- Reset de formulário/filtros ("Limpar filtros"): o campo e o calendário voltam a vazio.
- Preferência de cor/destaque personalizada (spec 051): o botão acompanha os tokens, sem cor fixa.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Todo campo de data do sistema (o componente compartilhado `DateInput`) MUST ter um botão de calendário **dentro do campo**, que abre o calendário do navegador; a data escolhida MUST preencher o campo em `dd/mm/aaaa`.
- **FR-002**: A digitação com máscara `dd/mm/aaaa` MUST continuar funcionando exatamente como hoje.
- **FR-003**: O valor trocado com as telas MUST continuar sendo o texto `yyyy-MM-dd` (ou vazio); o componente MUST NOT converter datas por objetos de data do navegador de um jeito que mude o dia por causa do fuso.
- **FR-004**: O botão MUST ser acessível: focável por Tab, com rótulo "Abrir calendário", ativável por Enter/Espaço, com indicador de foco visível; quando o campo está desabilitado o botão MUST estar desabilitado.
- **FR-005**: As cores do botão MUST vir só dos tokens do tema (nenhuma cor literal), com contraste mínimo de 4,5:1 contra o fundo do campo em todas as combinações de modo claro/escuro, modo de cores e destaque.
- **FR-006**: O calendário aberto pelo navegador MUST seguir o tema (escuro no modo escuro, claro no claro).
- **FR-007**: Os campos de data nativos dos formulários (`TurmaForm` e `VinculoCobrancaFormModal`) MUST passar a usar o `DateInput`, sem mudar as datas enviadas ao servidor.
- **FR-008**: Os campos de competência (`type="month"`) MUST seguir o tema (ícone e seletor), mantendo o valor `yyyy-MM` e o comportamento atuais.
- **FR-009**: O botão e o campo MUST caber no layout atual (filtros, formulários, modal, tabela de parcelas e 360 px) sem rolagem horizontal.
- **FR-010**: A mudança MUST NOT alterar o backend, os contratos de API nem o resultado de nenhuma consulta ou gravação; só o controle de entrada de data muda.
- **FR-011**: A mudança MUST NOT criar componente novo de frontend (o `DateInput` existente é estendido) e MUST NOT introduzir cor literal.
- **FR-013**: O campo "Fim da turma" MUST continuar impedindo, no calendário, a escolha de um dia anterior ao início (hoje feito por `min` do campo nativo); o servidor continua sendo quem valida a regra e devolve a mensagem de erro.
- **FR-012**: Nenhum campo de data nativo (`<input type="date">`) MUST permanecer fora do componente compartilhado.

### Key Entities

Nenhuma entidade de dados é criada ou alterada. O componente `DateInput` ganha o botão e o seletor nativo auxiliar; o contrato de entrada/saída (`value`/`onChange` em `yyyy-MM-dd`) é o mesmo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos campos de data (23 usos do `DateInput` + 5 nativos migrados) mostram o botão de calendário; 0 campos `<input type="date">` fora do componente compartilhado.
- **SC-002**: Escolher uma data pelo calendário leva no máximo 2 cliques (abrir e escolher o dia), em qualquer tela.
- **SC-003**: 0 pares do botão abaixo de 4,5:1 de contraste nas 48 combinações verificadas (claro/escuro × modos de cor × destaques).
- **SC-004**: 0 mudanças de valor: a mesma data digitada ou escolhida gera a mesma requisição (conferido em filtros e formulários).
- **SC-005**: 0 rolagens horizontais novas em 360 px nas telas com campos de data.

## Assumptions

- O calendário é o do navegador (Chrome, Edge, Firefox e Safari atuais); o desenho do calendário aberto depende do navegador, e o tema escuro dele depende do suporte do navegador ao esquema de cores.
- O campo continua sendo texto com máscara; o seletor nativo é só um auxiliar para escolher o dia.
- As telas de relatório e de Home já exibem filtros "de/até" que se aplicam sozinhos; isso não muda.

## Fora de escopo

- Criar um calendário próprio (visual único em todos os navegadores): pode ser uma evolução futura.
- Campos de hora (`type="time"`/máscara de hora).
- Validação de datas impossíveis no cliente (continua no servidor).
- Qualquer mudança no que cada filtro consulta.
