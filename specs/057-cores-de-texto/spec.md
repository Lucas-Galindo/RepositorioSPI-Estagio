# Feature Specification: Cores de Texto (Contraste Automático e Paleta Restrita)

**Feature Branch**: `057-cores-de-texto`

**Created**: 2026-10-19

**Status**: Draft

**Input**: Card Trello "Cores de texto" — Parte A (contraste automático da fonte) e Parte B (personalização restrita à paleta oficial).

## Contexto (estado atual, lido do código)

- A interface já tem modo claro e escuro, e três modos de cores para daltonismo (Protanopia, Deuteranopia, Tritanopia), além do modo "Padrão".
- A tela de Configurações permite hoje escolher **qualquer** cor como "Cor de destaque" (seletor livre) e tem um modo "Personalizado" com 4 cores livres (destaque principal, secundário, terciário e alerta). Isso foi introduzido pela spec 051.
- Cerca de 35 cores de texto estão fixas nas folhas de estilo (texto branco na barra lateral, nos avisos flutuantes e nos avatares; texto quase preto no botão principal, no item de menu ativo e no selo "Hoje"; texto marrom-escuro em "Pendente" e em avisos; cinzas da barra lateral e da tela de login). Essas cores não mudam com o modo nem com a cor de destaque, então uma cor de destaque escura deixa, por exemplo, o texto do botão principal ilegível.
- As preferências de aparência ficam guardadas só no navegador da professora.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Texto sempre legível nos dois modos (Priority: P1)

A professora alterna entre modo claro e escuro (e entre os modos de cores) e todo texto da interface continua legível: menus, tabelas, cards, modais, formulários, botões, selos de status, avisos flutuantes, barras de relatório e a tela de login.

**Why this priority**: É o problema central do card — texto invisível ou de baixo contraste impede o uso do sistema.

**Independent Test**: Para cada combinação de modo (claro/escuro) × modo de cores × opção de destaque, conferir que cada par "texto × fundo" da interface atinge o contraste mínimo.

**Acceptance Scenarios**:

1. **Given** o modo claro, **When** a professora muda para o modo escuro, **Then** os textos sobre fundo escuro ficam claros e os textos sobre fundo claro ficam escuros, sem nenhum texto invisível.
2. **Given** qualquer opção de destaque da paleta, **When** ela aparece como fundo de um botão, item de menu ativo, selo ou paginação, **Then** o texto sobre ela tem cor definida automaticamente com contraste mínimo de 4.5:1.
3. **Given** um modo de daltonismo ativo, **When** a professora usa o sistema em claro ou escuro, **Then** as mesmas garantias de contraste valem.

---

### User Story 2 - Cor de destaque só da paleta oficial (Priority: P1)

A professora escolhe a cor de destaque entre amostras nomeadas da paleta oficial do sistema; não existe mais seletor de cor livre nem campo de código de cor, e o modo "Personalizado" deixa de existir.

**Why this priority**: Sem a restrição, qualquer cor livre pode quebrar a legibilidade garantida pela US1.

**Independent Test**: Abrir Configurações e verificar que só há amostras nomeadas; escolher cada uma e conferir a interface nos dois modos.

**Acceptance Scenarios**:

1. **Given** a tela de Configurações, **When** a professora abre "Cor de destaque", **Then** vê apenas amostras nomeadas da paleta oficial, com a atual marcada, e nenhum seletor livre.
2. **Given** a lista de modos de cores, **When** a professora a abre, **Then** vê Padrão, Protanopia, Deuteranopia e Tritanopia — sem "Personalizado".
3. **Given** um modo de daltonismo ativo, **When** a professora olha "Cor de destaque", **Then** a escolha fica desabilitada com a explicação de hoje (a paleta de acessibilidade prevalece).

---

### User Story 3 - Preferência antiga migrada sem susto (Priority: P2)

Quem já tinha salvo uma cor livre ou o modo "Personalizado" abre o sistema e vê a opção da paleta mais próxima do que tinha escolhido, sem erro e sem precisar reconfigurar.

**Why this priority**: Afeta só quem personalizou antes; sem isso a preferência antiga ficaria "presa" numa cor fora da paleta.

**Independent Test**: Gravar no navegador uma preferência antiga (cor livre e modo Personalizado), recarregar e conferir a opção resultante.

**Acceptance Scenarios**:

1. **Given** uma cor de destaque livre salva, **When** o sistema carrega, **Then** a cor vira a amostra da paleta mais próxima e essa escolha fica salva.
2. **Given** o modo "Personalizado" salvo, **When** o sistema carrega, **Then** o modo vira "Padrão" com a amostra mais próxima do destaque principal que estava salvo.
3. **Given** uma preferência ilegível ou de formato desconhecido, **When** o sistema carrega, **Then** usa o padrão do sistema.

### Edge Cases

- Preferência salva com a cor de destaque igual a uma amostra (ex.: o turquesa padrão): permanece como está.
- Preferência salva com valor que não é cor válida: cai no padrão.
- Selos de status cuja cor de texto é marrom-escuro sobre amarelo claro ("Pendente", avisos): no modo escuro precisam continuar legíveis.
- Barra lateral é escura nos dois modos: seus textos devem ser claros nos dois modos, não "seguir o modo".
- Tela de login (fora da área logada): também sem cores de texto fixas fora dos tokens, e legível.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Toda cor de texto da interface MUST vir de um token de tema; o sistema MUST NOT ter cor de texto fixa (código de cor literal) nas folhas de estilo ou nos componentes, exceto na definição dos próprios tokens.
- **FR-002**: Para cada superfície que pode receber texto (fundo da página, cartões, barra lateral, cor de destaque e suas variações, cores de status, avisos flutuantes), o tema MUST definir a cor de texto correspondente, para modo claro e escuro.
- **FR-003**: Todo par texto × fundo definido pelo tema MUST ter contraste mínimo de 4.5:1, em todas as combinações de modo (claro/escuro), modo de cores e opção de destaque.
- **FR-004**: A paleta oficial MUST estar documentada num único lugar (fonte única de verdade), com nome de cada cor e suas variantes para claro e escuro; telas e estilos MUST usá-la por referência, nunca repetindo os valores.
- **FR-005**: A escolha de cor de destaque MUST ser feita só entre amostras nomeadas da paleta oficial.
- **FR-006**: O sistema MUST NOT oferecer seletor de cor livre, campo de código de cor ou o modo de cores "Personalizado".
- **FR-007**: O sistema MUST NOT aplicar uma cor de destaque fora da paleta, mesmo que ela esteja salva no navegador.
- **FR-008**: Preferência salva com cor de destaque fora da paleta MUST ser migrada para a amostra mais próxima (distância de cor), e a preferência migrada MUST ser salva.
- **FR-009**: Preferência salva com modo "Personalizado" MUST ser migrada para o modo "Padrão" com a amostra mais próxima do destaque principal salvo.
- **FR-010**: Os modos de daltonismo MUST continuar existindo, com as mesmas cores de hoje, e continuam desabilitando a escolha de cor de destaque.
- **FR-011**: A troca de modo claro/escuro e de destaque MUST refletir imediatamente em todas as telas, sem recarregar.
- **FR-012**: A spec 051 MUST receber nota apontando para esta spec como a regra vigente de personalização (Princípio V).

### Key Entities

- **Opção da paleta (amostra)**: nome exibido, cor base, variante para modo claro, variante para modo escuro, cor de texto sobre ela.
- **Preferência de aparência** (no navegador): modo claro/escuro, modo de cores (Padrão ou um dos 3 de daltonismo), amostra de destaque escolhida.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos pares texto × fundo do tema atingem 4.5:1 em todas as combinações (2 modos × 4 modos de cores × amostras de destaque), conferido por cálculo automático.
- **SC-002**: Zero códigos de cor literais usados como cor de texto fora da definição dos tokens.
- **SC-003**: Zero formas, na interface, de escolher uma cor fora da paleta.
- **SC-004**: Uma preferência antiga (cor livre ou "Personalizado") é convertida na primeira abertura, sem erro e sem ação da professora.

## Assumptions

- Amostras de destaque oferecidas (derivadas das cores já existentes no sistema): **Turquesa** (padrão atual), **Dourado**, **Azul**, **Laranja**, **Rosa** e **Verde**. Amarelo e vermelho da paleta não viram opção de destaque: o amarelo não dá contraste suficiente como cor de texto/link num fundo claro, e o vermelho é reservado a alerta/perigo.
- "Distância de cor" para a migração é a distância entre as cores no espaço RGB; empate resolve pela primeira opção da lista.
- Ajustes de tom para atender ao contraste (variante mais escura no claro / mais clara no escuro) são permitidos, desde que mantenham a identidade da cor.
- Gráficos do sistema são barras e indicadores desenhados com as mesmas cores do tema (não há biblioteca de gráficos); os dois gradientes fixos dos relatórios entram na regra FR-001.
- Elementos decorativos sem texto (sombras, bordas sutis) não precisam de 4.5:1.
- Não há testes automatizados de frontend no projeto; a verificação de contraste é feita por um cálculo sobre os tokens, e a de cores fixas por busca no código.
