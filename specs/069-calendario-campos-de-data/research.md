# Research: Calendário nos campos de data (spec 069)

Decisões baseadas na leitura do código (2026-11-04), na resposta do usuário (calendário do navegador, todos os campos) e num experimento no Chrome (capturas `exp-data-light/dark.png` na pasta de trabalho, não versionadas).

## R1 — O botão e o calendário nativo ficam dentro do `DateInput` existente

**Decisão**: o `DateInput` (campo de texto mascarado) ganha, dentro de um invólucro `span`, (a) um **botão** de calendário posicionado na borda direita do campo e (b) um **`<input type="date">` auxiliar**, invisível, sem foco e fora da navegação por teclado, usado só para abrir o seletor do navegador e devolver o dia escolhido. Nenhum componente novo.

**Por quê**: o campo de texto mascarado existe de propósito (o formato não depende do navegador). O auxiliar reaproveita o calendário nativo (decisão do usuário) sem trocar o campo visível. Como todas as telas usam o `DateInput`, uma mudança só cobre os 23 usos.

**Alternativas descartadas**: (a) trocar o campo por `<input type="date">` nativo — perde a máscara e o formato fixo; (b) criar um calendário próprio — fora do escopo por decisão do usuário (fica como evolução).

## R2 — Como abrir o calendário

**Decisão**: o clique no botão chama `showPicker()` do auxiliar; se o método não existir ou lançar erro (navegadores antigos, gesto negado), o botão só **foca** o auxiliar, sem lançar erro. A escolha do dia dispara `change` no auxiliar e o componente chama `onChange(yyyy-MM-dd)` com o valor do próprio auxiliar.

**Por quê**: `showPicker()` abre o calendário a partir de um clique, é suportado pelos navegadores atuais e foi confirmado no experimento (abre sem erro com o auxiliar invisível). O valor `yyyy-MM-dd` que o navegador entrega é exatamente o contrato do componente, então **não há conversão de data** (nada de `new Date`, sem risco de dia deslocado por fuso, como já alerta o comentário do componente).

O auxiliar também recebe o `min` opcional (usado em "Fim da turma"), para o calendário não deixar escolher dias anteriores ao início; a digitação não é bloqueada e o servidor segue validando.

O auxiliar recebe o valor atual quando ele é uma data completa (o calendário abre no dia do campo) e vazio quando está vazio/parcial/inválido (abre em hoje, sem mexer no texto digitado).

## R3 — Tema do botão: só tokens

**Decisão**: o ícone usa `var(--c-text-muted)` e, ao passar o mouse ou receber foco, `var(--c-accent-text)`; o anel de foco usa `var(--c-accent-text)`. Nenhuma cor literal.

**Por quê**: `--c-text-muted` e `--c-accent-text` já têm contraste ≥ 4,5:1 contra os fundos `--c-bg`, `--c-surface` e `--c-surface-2` em todas as 48 combinações verificadas por `npm run verificar-contraste` (spec 057). Como os tokens mudam sozinhos com o modo claro/escuro e com o modo de cores, "fundo escuro → ícone claro" e o inverso acontecem por construção, e a troca de tema vale na hora (variável CSS).

## R4 — Tema do calendário aberto: `color-scheme` só nos controles de data

**Decisão**: `color-scheme: light` nos controles nativos de data/mês e `color-scheme: dark` com a classe `dark-mode` na raiz, **só** no auxiliar (`.date-nativo`) e nos campos `type="month"`; **não** na página inteira.

**Por quê** (experimento no Chrome): sem `color-scheme`, o ícone e o seletor nativos ficam escuros mesmo no modo escuro (ícone escuro sobre fundo escuro); com `color-scheme: dark` o ícone fica claro e o seletor usa o tema escuro. Aplicar na página toda mudaria barras de rolagem e listas (efeito colateral fora do pedido).

## R5 — Campos nativos que sobram

**Decisão**:
- `<input type="date">` (5, em `TurmaForm` e `VinculoCobrancaFormModal`) → trocados pelo `DateInput` (formato único, botão e cores iguais). O `VinculoCobrancaFormModal` passa a precisar de duas propriedades do `DateInput`: `ariaLabel` (as parcelas já tinham rótulos de acessibilidade) e `style` do invólucro (as parcelas usam `flex: 1`). Ambas opcionais.
- `<input type="month">` (4 campos de competência, contas a pagar e a receber, novo e editar) → **continuam nativos** (não existe máscara de mês no sistema e o pedido é só o tema): recebem a regra de `color-scheme` (R4), que corrige o ícone e o seletor.

**Por quê**: criar um campo de mês mascarado seria escopo novo; o problema relatado (cor do ícone) se resolve com a regra de tema.

## R6 — Layout

**Decisão**: o invólucro é `inline-block` (os filtros continuam em linha, com o `max-width` do `.filter-date` no campo) e `block` de largura total dentro de `.field` (formulários). O campo ganha um recuo à direita para o botão não cobrir o texto.

**Por quê**: o `.filter-bar` é `flex-wrap`; um invólucro inline-block com o campo dentro mantém a largura atual. Em formulários, `.field input` já ocupa a largura toda; o invólucro precisa acompanhar. Verificado depois por captura e por medida de rolagem horizontal em 360 px.

## R6b — CSS: duas armadilhas encontradas na análise

1. `.field` é uma coluna flex que **estica os filhos diretos**; com o campo agora dentro do invólucro, ele deixaria de esticar. Regra: `.field .date-wrap` em bloco e `.field .date-wrap .date-input { width: 100% }`.
2. `.field input` (preenchimento, sombra, fundo) atinge o auxiliar dentro de formulários. Regra: o auxiliar usa `.date-wrap .date-nativo`, mais específico, com `opacity: 0`, `height: 0` e sem sombra.

## R7 — Testes

Não há framework de testes de interface no projeto. A verificação é feita como nas specs 063 a 068: `tsc`, `eslint`, `npm run verificar-contraste`, build de produção e **Chrome automatizado** (Playwright) nas telas com campos de data, em claro e escuro, medindo: botão presente em cada campo, valor escolhido igual ao digitado, `color-scheme` calculado, cor do ícone, teclado, campo desabilitado e 360 px. **Limite conhecido**: o calendário aberto é uma janela do navegador e não aparece em captura de tela; o tema dele é conferido pelo `color-scheme` calculado do auxiliar e pelo comportamento do ícone nativo (experimento), não por imagem do calendário aberto.

## R8 — Riscos

| Risco | Mitigação |
|---|---|
| `showPicker()` ausente ou bloqueado | try/catch com `focus()`; digitação continua |
| Botão cobre o texto digitado | recuo à direita no campo |
| Invólucro quebra o layout dos filtros ou dos formulários | `inline-block`/`block` conforme o contexto; captura e medida de rolagem em 360 px em todas as telas |
| Auxiliar invisível rouba foco ou aparece para leitor de tela | `tabIndex=-1`, `aria-hidden`, `pointer-events: none` |
| Mudança de valor por conversão de data | o valor vem do próprio navegador em `yyyy-MM-dd`; revisão do diff confirma que não há `new Date` |
