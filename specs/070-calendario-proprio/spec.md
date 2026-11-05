# Feature Specification: Calendário próprio no padrão do sistema

**Feature Branch**: `070-calendario-proprio`

**Created**: 2026-11-05

**Status**: Draft

**Input**: Retorno do usuário sobre a spec 069 (2026-11-05): "Não gostei dele ser do navegador. Vamos ter que criar um com base nas cores e no design do programa. [...] ao apertar o botão (dentro do filtro de calendário) de abrir o calendário, também deve ser construído e criado com base no padrão do sistema." Card do Trello "Calendário próprio no padrão do sistema (substitui o do navegador)" (Backlog).

## Contexto (estado atual, lido do código)

- A spec 069 (entregue em 2026-11-04) colocou um **botão de calendário dentro de todo campo de data** (`DateInput`, 23 usos em 14 arquivos) e fez o ícone seguir o tema. O calendário que abre, porém, é o **do navegador** (um `<input type="date">` invisível aberto por `showPicker()`): o desenho depende do navegador e não segue o visual do SPI.
- Os 4 campos de **competência** (mês e ano, em contas a pagar e a receber) são `<input type="month">` nativos, também com seletor do navegador.
- O sistema tem modos claro e escuro (classe `dark-mode` na raiz), modos de cor e destaque (spec 051/057), tokens de cor verificados por contraste (48 combinações), raios e sombras próprios (`--r-sm/md/lg`, `--shadow-*`) e painéis flutuantes já existentes (o sino de notificações).
- Os modais do sistema **rolam e cortam** o que sai deles (`overflow-y: auto`, camadas até 110) e as tabelas rolam na horizontal; um calendário desenhado dentro desses contêineres seria cortado.

## Clarificações (respondidas pelo usuário em 2026-11-05)

- **Competência**: os 4 campos de mês e ano também ganham um **seletor próprio** no mesmo padrão (não ficam nativos).
- **Celular (360 px)**: o calendário abre como **janela centralizada sobre a tela**, com fundo escurecido (padrão dos modais do sistema).
- **Semana**: começa no **domingo** (D S T Q Q S S).
- **Recursos além de escolher o dia**: atalhos **"Hoje" e "Limpar"**; **troca rápida de mês e ano** (clicar no título); **dia de hoje destacado**; **dias bloqueados** pelo mínimo (`min`).
- Entendido e mantido: o **campo de texto `dd/mm/aaaa` continua**, com o botão de calendário dentro dele (spec 069); o calendário próprio **substitui o do navegador**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Escolher o dia num calendário do SPI (Priority: P1)

A professora clica no botão de calendário dentro de um campo de data e vê um calendário com o visual do SPI (cores, bordas, sombras e tipografia do sistema, claro ou escuro). Ela clica num dia e o campo mostra `dd/mm/aaaa`; o calendário fecha.

**Why this priority**: É o pedido central do retorno ("criar um com base nas cores e no design do programa").

**Independent Test**: Abrir Contas a Receber, clicar no botão do filtro "a partir de", escolher um dia e conferir o campo e a lista filtrada, no modo claro e no escuro.

**Acceptance Scenarios**:

1. **Given** um campo de data vazio, **When** ela abre o calendário, **Then** ele mostra o mês de hoje, com o dia de hoje destacado, e a semana começando no domingo.
2. **Given** um campo com data completa, **When** ela abre o calendário, **Then** ele abre no mês dessa data, com o dia escolhido marcado.
3. **Given** um texto parcial ou inválido no campo, **When** ela abre o calendário, **Then** ele abre no mês de hoje, sem alterar o que foi digitado até ela escolher um dia.
4. **Given** o calendário aberto, **When** ela clica num dia, **Then** o campo recebe a data em `dd/mm/aaaa`, o calendário fecha e a tela usa o mesmo valor que usaria se a data tivesse sido digitada.
5. **Given** o calendário aberto, **When** ela clica fora, aperta Esc ou clica de novo no botão, **Then** ele fecha sem alterar o campo.
6. **Given** os modos claro e escuro (e qualquer modo de cor/destaque), **When** o calendário é exibido, **Then** todo texto e marcação têm contraste mínimo de 4,5:1 contra o fundo do calendário.

---

### User Story 2 - Navegar rápido, ver hoje e usar atalhos (Priority: P1)

Dentro do calendário ela passa para o mês anterior/seguinte pelas setas, clica no título (ex.: "novembro de 2026") para escolher outro mês ou outro ano numa lista, usa "Hoje" para escolher a data atual e "Limpar" para esvaziar o campo.

**Why this priority**: Sem isso, datas distantes exigem muitos cliques; "Limpar" é essencial nos filtros.

**Independent Test**: Num filtro, depois de abrir o calendário chegar a março de 2024 em 4 cliques (título, ano, 2024, março), depois "Hoje", depois "Limpar".

**Acceptance Scenarios**:

1. **Given** o calendário no modo de dias, **When** ela clica nas setas, **Then** o mês anterior ou seguinte é exibido (virando o ano quando preciso).
2. **Given** o título "novembro de 2026", **When** ela clica nele, **Then** vê os 12 meses do ano; clicar de novo no ano mostra uma lista de anos; escolher um ano leva aos meses e escolher um mês volta aos dias desse mês.
3. **Given** o calendário aberto, **When** ela clica em "Hoje", **Then** o campo recebe a data de hoje e o calendário fecha (desabilitado se hoje for anterior ao mínimo).
4. **Given** um filtro preenchido, **When** ela clica em "Limpar", **Then** o campo fica vazio, o filtro é removido e o calendário fecha.
5. **Given** um campo com mínimo (ex.: "Fim da turma" com início já escolhido), **When** o calendário abre, **Then** os dias anteriores ao mínimo aparecem apagados e não podem ser escolhidos.

---

### User Story 3 - Teclado, leitor de tela e celular (Priority: P1)

O calendário é totalmente usável pelo teclado e por leitor de tela, e em tela estreita abre como janela centralizada fácil de tocar.

**Why this priority**: Acessibilidade e uso em celular já fazem parte do padrão do sistema (360 px).

**Independent Test**: Abrir o calendário só com teclado e escolher um dia; abrir em 360 px.

**Acceptance Scenarios**:

1. **Given** o botão de calendário focado, **When** ela aperta Enter ou Espaço, **Then** o calendário abre com o foco no dia escolhido (ou no de hoje) e o foco fica preso dentro dele até fechar.
2. **Given** o foco num dia, **When** ela usa as setas, **Then** o foco anda um dia (esquerda/direita) ou uma semana (cima/baixo), trocando de mês quando preciso; PageUp/PageDown trocam o mês; Home/End vão ao início/fim da semana; Enter ou Espaço escolhe o dia; Esc fecha e devolve o foco ao botão.
3. **Given** um leitor de tela, **When** ela navega, **Then** o calendário é anunciado como janela "Calendário", o mês exibido é anunciado e cada dia tem nome completo (ex.: "5 de novembro de 2026"), com o escolhido, o de hoje e os bloqueados indicados.
4. **Given** uma tela de 360 px, **When** ela abre o calendário, **Then** ele aparece centralizado com fundo escurecido, cabe na largura sem rolagem horizontal, e tocar fora fecha.
5. **Given** o calendário aberto num campo dentro de um modal, de uma tabela ou perto da borda da tela, **When** ele aparece, **Then** não é cortado nem escondido atrás de nada (abre para cima se faltar espaço embaixo).

---

### User Story 4 - Competência (mês e ano) no mesmo padrão (Priority: P2)

Os 4 campos de competência ganham um campo com botão que abre o seletor de **mês e ano** do SPI (os 12 meses do ano, com setas de ano e troca rápida de ano), mantendo o valor `aaaa-mm`.

**Why this priority**: Fecha o último controle nativo de data/mês e deixa tudo igual; depende da US1.

**Independent Test**: Abrir "Nova conta a receber", escolher a competência pelo seletor e salvar; conferir o mesmo valor de antes.

**Acceptance Scenarios**:

1. **Given** o campo de competência, **When** ela clica no botão, **Then** abre o seletor de meses do ano atual do campo (ou de hoje), com o mês escolhido marcado.
2. **Given** o seletor aberto, **When** ela escolhe um mês, **Then** o campo mostra o mês e o ano (ex.: `11/2026`), o valor emitido é `2026-11` e o seletor fecha.
3. **Given** o campo, **When** ela digita `11/2026`, **Then** o valor é `2026-11`, como o campo nativo dava.

---

### Edge Cases

- Ano bissexto (29/02), meses de 28, 29, 30 e 31 dias, e mês que começa em qualquer dia da semana: a grade sempre mostra o mês certo, com os dias dos meses vizinhos apagados (ou vazios) completando as semanas.
- Datas distantes (anos 1900 a 2100): a lista de anos e as setas funcionam sem erro; fora desse intervalo o seletor só limita a navegação, e um ano fora da faixa digitado no campo faz o calendário abrir em hoje, sem alterar o texto.
- Mudança de fuso/horário de verão do navegador: o dia mostrado nunca muda por causa do fuso (nenhuma conversão de data pelo fuso local).
- Campo desabilitado: o botão está desabilitado e o calendário não abre.
- Dois campos "de/até" na mesma tela: cada botão abre o calendário do seu campo; abrir um fecha o outro.
- Rolagem da página ou redimensionamento com o calendário aberto: ele acompanha o campo (ou fecha) sem ficar solto na tela.
- Troca de tema com o calendário aberto: as cores mudam na hora.
- Mínimo posterior à data atual do campo (ex.: início depois do fim): o dia escolhido aparece marcado mas bloqueado para novas escolhas, sem erro.
- "Hoje" bloqueado (hoje antes do mínimo): o atalho fica desabilitado.
- Clicar no botão com o calendário aberto fecha; abrir pelo teclado não "vaza" o Enter para o formulário.
- Modo de cores e destaque personalizados (spec 051): o calendário acompanha os tokens, sem cor fixa.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O botão de calendário dentro de todo campo de data (`DateInput`) MUST abrir um **calendário próprio do sistema**; o calendário do navegador (`<input type="date">` auxiliar e `showPicker()`) MUST deixar de ser usado.
- **FR-002**: O calendário MUST seguir o padrão visual do sistema: só tokens de cor do tema (claro/escuro, modos de cor e destaque), os raios, sombras e tipografia do sistema; nenhuma cor literal.
- **FR-003**: Todo texto e marcação do calendário (dias, dia escolhido, dia de hoje, setas, atalhos, meses e anos) MUST ter contraste mínimo de 4,5:1 contra o fundo em todas as combinações verificadas por `npm run verificar-contraste` (dias bloqueados ficam apagados, mas legíveis como "indisponível").
- **FR-004**: A grade MUST começar no domingo, mostrar os nomes dos meses e dos dias da semana em português e destacar o **dia de hoje**.
- **FR-005**: O calendário MUST ter navegação por setas de mês, **troca rápida de mês e ano** (clicar no título abre a lista de meses; clicar no ano abre a lista de anos) e voltar aos dias ao escolher.
- **FR-006**: O calendário MUST ter os atalhos **"Hoje"** (escolhe a data atual) e **"Limpar"** (esvazia o campo); "Hoje" MUST ficar desabilitado quando hoje for anterior ao mínimo.
- **FR-007**: O calendário MUST suportar **dias bloqueados** pelo mínimo (`min`): dias anteriores não podem ser escolhidos nem recebem foco de seleção; o servidor continua sendo quem valida a regra (a digitação não é bloqueada).
- **FR-008**: O valor trocado com as telas MUST continuar sendo `yyyy-MM-dd` (ou vazio) nos campos de data e `yyyy-MM` nos de competência; o componente MUST NOT converter datas pelo fuso do navegador de um jeito que mude o dia (nenhuma data vira objeto de data local para ser exibida).
- **FR-009**: O calendário MUST ser desenhado **fora** do contêiner do campo (na raiz da página), posicionado pelo campo, para nunca ser cortado por modais, tabelas ou rolagens; MUST abrir para cima quando faltar espaço embaixo; MUST acompanhar o campo na rolagem e no redimensionamento ou fechar.
- **FR-010**: Em tela estreita (até 640 px, como o painel de notificações) o calendário MUST abrir como **janela centralizada** com fundo escurecido, sem rolagem horizontal; tocar fora MUST fechar.
- **FR-011**: O calendário MUST fechar com Esc, clique/toque fora, escolha de um dia (ou mês, na competência), "Limpar" ou "Hoje", devolvendo o foco ao botão; abrir um calendário MUST fechar outro aberto.
- **FR-012**: O calendário MUST ser acessível: papel de janela com nome "Calendário", foco que entra no dia escolhido (ou em hoje) e **fica preso** dentro dele até fechar, navegação completa por teclado (setas, PageUp/PageDown, Home/End, Enter/Espaço, Esc), nome completo em cada dia, estados (escolhido, hoje, bloqueado) indicados a leitores de tela e indicador de foco visível.
- **FR-013**: Os 4 campos de competência MUST passar a usar um campo próprio (texto `mm/aaaa` com botão) que abre o seletor de **mês e ano** do sistema e emite `yyyy-MM`; MUST NOT restar nenhum `<input type="month">` nativo.
- **FR-014**: MUST NOT restar nenhum `<input type="date">` nem `<input type="month">` no sistema (inclusive o auxiliar da spec 069), nem as regras de CSS `color-scheme` criadas só para ele.
- **FR-015**: A mudança MUST NOT adicionar dependência de pacote (nenhuma biblioteca de calendário) e MUST NOT alterar o backend, os contratos de API nem o resultado de qualquer consulta ou gravação.
- **FR-016**: A digitação mascarada `dd/mm/aaaa` e as propriedades do `DateInput` (`value`, `onChange`, `required`, `disabled`, `min`, `ariaLabel`, `style`…) MUST continuar iguais, para nenhuma tela precisar mudar.
- **FR-017**: A lógica pura do calendário (montar o mês, dias dos meses vizinhos, bissexto, limites, comparação com o mínimo, nomes) MUST ficar num módulo próprio verificável sem navegador, com verificação automatizada de casos de borda.

### Key Entities

Nenhuma entidade de dados. Componentes novos de frontend (permitidos por esta spec): o calendário (painel), o campo de competência e um módulo de lógica pura; o `DateInput` existente é alterado.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos campos de data (23 usos do `DateInput`) e de competência (4) abrem o calendário próprio; 0 campos `type="date"`/`type="month"` no sistema.
- **SC-002**: Escolher uma data leva no máximo 4 cliques para qualquer dia do ano atual (abrir, título, mês e dia; 3 quando o mês é vizinho, com a seta) e no máximo 6 para um ano a até 12 anos de distância (abrir, título, ano, escolher o ano, mês e dia).
- **SC-003**: 0 pares do calendário abaixo de 4,5:1 nas combinações verificadas (claro/escuro × modos de cor × destaques).
- **SC-004**: 0 mudanças de valor: a mesma data digitada ou escolhida gera a mesma requisição, em filtros, formulários e competência.
- **SC-005**: 0 casos em que o calendário aparece cortado (em modal, tabela, borda da tela) e 0 rolagens horizontais em 360 px.
- **SC-006**: Uso completo só pelo teclado (abrir, navegar, escolher, fechar) em todos os modos (dias, meses, anos).
- **SC-007**: 0 dependências novas no `package.json`; 0 linhas de backend alteradas.

## Assumptions

- O calendário é de **uma data só** (não há intervalo de datas num campo); os filtros "de/até" continuam sendo dois campos.
- Idioma fixo: português do Brasil (nomes de meses e dias); semana começa no domingo (decidido).
- A faixa de anos navegável é de 1900 a 2100 (as datas do sistema são de aulas, contas e turmas).
- O campo de texto continua sendo a forma principal de digitar; o calendário é a alternativa visual.
- Hora (`TimeInput`) não muda.

## Fora de escopo

- Seleção de intervalo (de/até no mesmo calendário), seleção de várias datas, semanas numeradas, feriados.
- Campos de hora.
- Internacionalização (outros idiomas).
- Validação de datas impossíveis no cliente (continua no servidor).
