# Research: Corrigir Borda Curva em `.week-event`

## Causa raiz confirmada

`frontend/styles/dashboard.css:533-542` define:

```css
.week-event {
  background: var(--c-bg);
  border-radius: 11px;
  border-left: 3px solid var(--c-accent);
  padding: 8px 9px;
  margin-bottom: 7px;
  font-size: 11px;
  cursor: pointer;
  box-shadow: var(--shadow-out-sm);
}
```

`border-radius: 11px` arredonda os 4 cantos da caixa, incluindo os dois cantos onde a
`border-left` colorida (3px, `var(--c-accent)`, teal) encontra o contorno do card. Nesse ponto,
o navegador curva a borda colorida para acompanhar o arredondamento do canto — visualmente um
arco/crescente que, em cards baixos (fonte 11px, padding vertical pequeno, vários eventos
empilhados por dia), invade a área onde o texto (`.t`/`.s`) começa.

## Reavaliação do componente de referência `.cal-event`

O pedido do usuário parte da premissa de que `.cal-event` (linhas 708-723) já resolve isso com
"reset explícito dos cantos onde a borda colorida fica". Reexaminando o CSS real desse bloco:

```css
.cal-event {
  background: var(--c-bg);
  border-left: 3px solid var(--c-accent);
  border-radius: 7px;
  padding: 4px 6px;
  ...
  border-top: none;
  border-right: none;
  border-bottom: none;
  ...
}
```

`.cal-event` **não** tem `border-top-left-radius`/`border-bottom-left-radius` zerados — ele só
reduz o `border-radius` geral para 7px (vs 11px) e desliga os outros *lados* da borda
(`border-top/right/bottom: none`), o que não é a mesma coisa que zerar o raio dos cantos onde a
borda colorida está. Ou seja, `.cal-event` provavelmente tem uma versão mais discreta do mesmo
artefato, só menos perceptível por ser um card bem menor (fonte 9.5px, padding 4px 6px, texto em
uma linha com `white-space: nowrap`/`text-overflow: ellipsis` — o texto raramente chega perto do
canto esquerdo). Isso é registrado aqui para transparência: a correção abaixo não copia
literalmente o CSS de `.cal-event` (que não contém a técnica descrita no pedido), mas aplica a
técnica correta e mais direta para o problema — documentada como decisão própria.

## Decisão (revisada): copiar fielmente `border-radius`, `border-left` e `padding` de `.cal-event`

**Histórico**: A primeira iteração desta correção zerou `border-top-left-radius`/
`border-bottom-left-radius` em `.week-event`, eliminando totalmente a curvatura. Validação visual
do usuário rejeitou esse resultado: o objetivo nunca foi uma borda 100% reta, e sim a mesma
curvatura sutil já usada em `.cal-event` — pequena o bastante para não cobrir texto, mas ainda
visível, mantendo a identidade visual dos dois componentes consistente. Esta seção substitui a
decisão original.

**Decision**: Copiar para `.week-event` os valores exatos hoje em uso em `.cal-event` para as
três propriedades relevantes ao artefato — `border-radius: 7px` (era `11px`) e
`padding: 4px 6px` (era `8px 9px`); `border-left: 3px solid var(--c-accent)` já era idêntico nos
dois componentes, então não muda. Nenhum `border-top-left-radius`/`border-bottom-left-radius` é
adicionado, porque `.cal-event` também não os usa — a redução de `border-radius` sozinha (de
11px para 7px), somada ao padding proporcionalmente menor, já é suficiente para reduzir a curva
a um tamanho que não invade a área de texto, replicando o comportamento visual de `.cal-event`
com fidelidade exata (FR-006).

**Rationale**: FR-001/FR-005/FR-006 (revisados) pedem a mesma curvatura de `.cal-event`, não a
eliminação da curvatura. Copiar os valores exatos, em vez de reinterpretar a técnica, garante
paridade visual comprovada — `.cal-event` já é usado em produção com esses valores e já não
apresenta o problema relatado.

**Alternatives considered**:
- **Zerar os cantos esquerdos (`border-top-left-radius: 0`/`border-bottom-left-radius: 0`)**:
  abordagem da primeira iteração — rejeitada após validação visual do usuário por produzir uma
  borda totalmente reta, diferente do resultado esperado (curva sutil igual à de `.cal-event`).
- **Reduzir só o `border-radius` geral, sem tocar no `padding`**: rejeitada — o pedido do
  usuário foi explícito em copiar também o `padding` de `.cal-event`, não só o raio; manter o
  padding maior (`8px 9px`) deixaria o card com proporções diferentes das de `.cal-event` sem
  necessidade.
- **Trocar `border-left` por um elemento separado (ex.: uma faixa absolutamente posicionada)**:
  rejeitada — mudança de estrutura desproporcional a um ajuste puramente de CSS, e sairia do
  escopo "somente estilo" assumido na spec.

## Causa raiz adicional: estilo de link do navegador e separador de texto

Durante a validação visual pós-correção da borda, o usuário identificou uma segunda divergência
entre `.week-event` e `.cal-event`, não relacionada à curvatura: o texto de `.week-event`
aparecia azul e sublinhado (estilo padrão de `<a>`), e o separador entre horário e matéria era
um ponto médio (`·`), enquanto `.cal-event` exibe texto em cor neutra, sem sublinhado, e sem
nenhum separador (só espaço).

**Causa**: `.week-event` é renderizado via `<Link href=...>` (`frontend/app/(app)/dashboard/page.tsx`)
— uma âncora real, que herda os estilos padrão de link do navegador (cor azul, sublinhado) a
menos que sejam explicitamente resetados. `.cal-event`
(`frontend/app/(app)/agenda/page.tsx`) é renderizado como `<div onClick=...>` — nunca foi uma
âncora, então nunca teve esse problema por natureza, não por ter um reset de estilo mais
completo. O separador `·` é um caractere literal no JSX de `.week-event`
(`{fmtHora(a.horaInicio)} · {a.materiaNome}`); `.cal-event` usa `<b>{fmtHora(...)}</b> {a.materiaNome}`
— hora em negrito, só espaço como separador.

**Decision**: Adicionar `color: inherit;` e `text-decoration: none;` à regra `.week-event`
(reset mínimo equivalente ao que `.cal-event` obtém "de graça" por não ser uma âncora); remover
o caractere `·` do JSX de `.week-event`, deixando só espaço entre horário e matéria. O `href`/
navegação do `<Link>` não muda — só a aparência do texto (FR-007, FR-008).

**Rationale**: É o reset mínimo necessário para igualar visualmente os dois componentes sem
alterar a semântica de `.week-event` continuar sendo um link real (diferente de `.cal-event`,
que depende de `onClick` + navegação programática). Não bolda o horário como `.cal-event` faz
(`<b>`), porque `.week-event .t` já aplica `font-weight: 700` à linha inteira via CSS — bold
parcial só no horário não é necessário para atingir paridade visual de legibilidade.

## Causa raiz adicional: borda decorativa renderizando separada do texto (`display` ausente)

Após o reset de estilo de texto, o usuário reportou que a borda colorida passou a aparecer numa
faixa isolada acima do texto do card, em vez de ao lado dele — só em `.week-event`, nunca em
`.cal-event`.

**Causa**: `.week-event` (`<Link>`, que o Next.js renderiza como `<a>`) nunca teve nenhuma
propriedade `display` definida — o valor inicial de `<a>` é `display: inline`. Seus dois filhos
diretos, `<div className="t">` e `<div className="s">`, são elementos de bloco. Um elemento
inline contendo filhos de bloco diretamente é o cenário clássico de "block-in-inline" do CSS:
dependendo do navegador/engine, a caixa da âncora (que carrega `border-left`, `background`,
`border-radius`, `box-shadow`) pode ser fragmentada em caixas anônimas ao redor do conteúdo de
bloco, fazendo a borda/fundo renderizar como uma tira separada em vez de envolver o card inteiro
como uma única caixa.

Confirmação de que este é o padrão já resolvido no projeto: `.lesson-item`
(`frontend/styles/dashboard.css:901-911`) — usado em várias telas para o mesmo caso (`<Link>`
envolvendo `<div>`s filhos, ex. lista de aulas em `materias/[id]/page.tsx`) — define
explicitamente `display: flex; align-items: center; gap: 13px;` exatamente para evitar esse
problema. `.week-event` nunca teve o equivalente.

**Decision**: Adicionar `display: block;` a `.week-event`. Diferente de `.lesson-item` (que
usa `flex` para alinhar filhos lado a lado horizontalmente), os filhos de `.week-event`
(`.t` e `.s`) já devem empilhar verticalmente (horário+matéria em cima, turma/aluno embaixo) —
`display: block` é suficiente e correto para isso, sem introduzir `flex`/`gap` desnecessários
(FR-009).

**Rationale**: Definir `display: block` explicitamente elimina de vez a ambiguidade de
"block-in-inline" — a âncora passa a estabelecer um bloco normal desde o início, sem geração de
caixas anônimas, garantindo que borda, fundo, padding e border-radius sejam sempre pintados como
uma única caixa ao redor de todo o conteúdo do card, em qualquer largura de tela.

**Alternatives considered**:
- **`display: flex; flex-direction: column;`**: equivalente em efeito prático (resolve o mesmo
  problema), mas desnecessariamente mais verboso para um empilhamento vertical simples que
  `display: block` já resolve — sem gap/alinhamento horizontal a controlar como em
  `.lesson-item`.

## Escopo confirmado

Nenhuma outra regra CSS depende de `.week-event` ter os 4 cantos igualmente arredondados —
`box-shadow: var(--shadow-out-sm)` e `background: var(--c-bg)` não são afetados pela mudança de
raio. `.week-event.st-realizada`/`.week-event.st-cancelada` (linhas 550-557) só sobrescrevem
`border-left-color` e `opacity`, nada relacionado a raio — continuam funcionando sem alteração.
