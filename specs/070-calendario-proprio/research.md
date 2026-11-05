# Research: Calendário próprio no padrão do sistema (spec 070)

Decisões baseadas na leitura do código (2026-11-05), nas respostas do usuário e no que a spec 069 já entregou (botão dentro do campo, ícone por tema, campos nativos migrados para o `DateInput`).

## R1 — Três peças novas e uma alterada (sem biblioteca)

**Decisão**:
- `frontend/lib/calendario.ts`: **lógica pura** (montar a grade do mês, dias do mês, dia da semana, navegar meses/anos, comparar com o mínimo, nomes em português). Sem React, sem `Date`.
- `frontend/components/shared/Calendario.tsx`: o **painel** (dias, meses e anos) e o **posicionamento** (portal na raiz da página; janela centralizada no celular).
- `frontend/components/shared/MonthInput.tsx`: campo de **competência** (texto `mm/aaaa` + botão) que abre o mesmo painel no modo mês.
- `DateInput.tsx` (existente): deixa de usar o `<input type="date">` auxiliar e abre o `Calendario`.

**Por quê**: o pedido é um calendário "com base nas cores e no design do programa"; uma biblioteca traria o visual dela, mais peso e mais manutenção. A lógica é pequena e verificável. Separar a lógica pura da interface deixa os casos de borda (bissexto, virada de ano, dia da semana) testáveis sem navegador, como já é feito com `verificar-contraste` e `sugerirParcelas`.

**Alternativas descartadas**: (a) biblioteca de calendário (visual alheio, dependência nova, proibida por FR-015); (b) manter o auxiliar nativo e só estilizar (o desenho do calendário aberto não é estilizável por CSS); (c) um único componente gigante (mistura regra e interface).

## R2 — Datas sem objeto `Date` (nenhum risco de fuso)

**Decisão**: o dia da semana e o tamanho do mês saem de **aritmética** (regra do bissexto e a fórmula de Sakamoto), e as datas são manipuladas como texto `yyyy-MM-dd`/`yyyy-MM` (a comparação de texto nesse formato já ordena as datas). O único relógio é o "hoje", que já existe em `lib/format.ts` (`dataIsoLocal`, campos locais do navegador).

**Por quê**: FR-008 proíbe que o fuso mude o dia mostrado; construir um `Date` a partir de texto é exatamente o erro que o `DateInput` já evita (comentário do componente). Com aritmética não há horário de verão, fuso nem mudança de ano de 1900.

## R3 — Onde o painel é desenhado: portal na raiz, posicionado pelo campo

**Decisão**: o painel é renderizado com `createPortal` em `document.body`, `position: fixed`, camada **120** (acima dos modais, 100 a 110). A posição vem do retângulo do invólucro do campo (`getBoundingClientRect`): abaixo do campo; se não couber e couber acima, abre para cima; fica sempre dentro da largura da tela (margem de 8 px). O cálculo roda ao abrir e a cada rolagem (captura, para pegar rolagem de modais e tabelas) e redimensionamento, **mexendo direto no estilo do elemento** (sem estado), e o painel fecha se o campo sair totalmente da tela.

**Por quê**: os modais têm `overflow-y: auto` e as tabelas `overflow-x: auto`; um painel `absolute` dentro deles seria cortado (FR-009). O ESLint do projeto proíbe `setState` direto em efeito, então a posição é aplicada no estilo do elemento.

## R4 — Celular: janela centralizada

**Decisão**: até 640 px (mesmo corte do painel de notificações) o painel vira uma janela **centralizada** com o fundo escurecido da classe `.modal-overlay` (a mesma dos modais); tocar no fundo fecha. A decisão de usar o modo celular vem de `matchMedia` lido com `useSyncExternalStore` (sem estado em efeito).

**Por quê**: resposta do usuário (janela centralizada) e padrão já existente; evita calcular posição em tela pequena.

## R5 — Visual (clay morphism do SPI) só com tokens

**Decisão**: painel `--c-surface`, texto `--c-text`, raio `--r-md`, sombra `--shadow-out`; dias em botões arredondados (`--r-sm`); **escolhido**: fundo `--c-accent`, texto `--c-on-accent`, sombra `--shadow-out-sm`; **hover/foco**: `--c-accent-tint` com `--c-accent-text`; **hoje**: anel de 2 px `--c-accent-text`; dias de outro mês: `--c-text-muted`; **bloqueados**: apagados (opacidade) e sem clique; cabeçalho dos dias da semana em `--c-text-muted`; atalhos com as classes de botão existentes (`btn btn-ghost btn-sm`). **Nenhuma cor literal**; o fundo escuro do celular reaproveita `.modal-overlay`.

**Por quê**: cada par texto/fundo acima **já está nos 48 pares** de `verificar-contraste` (`--c-text` e `--c-text-muted` sobre `--c-surface`; `--c-on-accent` sobre `--c-accent`; `--c-accent-text` sobre `--c-surface` e sobre a tinta de destaque), então "claro no escuro e escuro no claro" e os modos de cor/destaque saem por construção e a troca de tema vale na hora (variáveis CSS).

## R6 — Navegação e visões

**Decisão**: três visões no mesmo painel: **dias** (grade de 6 semanas × 7 dias, sempre 6 linhas para o painel não "pular" de altura), **meses** (12 meses do ano) e **anos** (página de 12 anos). O título do mês abre os meses; o título do ano abre os anos; escolher um ano volta aos meses e escolher um mês volta aos dias. Setas de mês na visão de dias, de ano na de meses e de página na de anos. Faixa navegável: 1900 a 2100; se o texto do campo tiver um ano fora dessa faixa (a máscara aceita qualquer ano de 4 dígitos), o calendário abre em hoje, sem erro e sem mexer no texto, e o servidor segue validando. Rodapé: **Hoje** (ou "Este mês" no modo competência) e **Limpar**.

**Por quê**: SC-002 (no máximo 3 cliques no ano atual e 5 em ano distante).

## R7 — Teclado e leitor de tela

**Decisão**: ao abrir, o foco vai ao dia escolhido (ou ao de hoje) e fica **preso** no painel (Tab circula entre setas, título, grade e atalhos); a grade usa foco "móvel" (só o dia atual da navegação tem `tabIndex=0`): setas movem um dia/semana, PageUp/PageDown o mês, Home/End início/fim da semana, Enter/Espaço escolhem, Esc fecha e devolve o foco ao botão. O Esc usa escuta em **captura** e interrompe a propagação para não fechar também o modal que contém o campo. Papéis: painel `role="dialog"` com `aria-label="Calendário"`, grade `role="grid"`, dias `role="gridcell"` com `aria-selected`, `aria-current="date"` para hoje e `aria-disabled` para bloqueados; cada dia com `aria-label` completo ("5 de novembro de 2026"); título do mês em região `aria-live="polite"`.

**Por quê**: FR-012 e SC-006.

## R8 — Fechar, abrir e conflitos

**Decisão**: fecha com Esc, clique/toque fora (escuta de `pointerdown` no documento, ignorando o painel), escolha, "Limpar" e "Hoje". Clicar em outro botão de calendário já é um clique fora do primeiro, então **abrir um fecha o outro** sem mecanismo extra. Abrir pelo teclado usa o clique do botão `type="button"`, que não envia o formulário.

## R9 — Competência (`MonthInput`)

**Decisão**: campo de texto `mm/aaaa` (máscara, 6 dígitos, mês 1 a 12) + o mesmo botão (classes `.date-wrap`/`.date-cal-btn` da 069) que abre o `Calendario` já na visão de meses. O valor emitido e recebido é `yyyy-MM`, igual ao campo nativo que substitui; "Hoje" vira "Este mês". Troca direta nas 4 telas (`<input type="month" ...>` → `<MonthInput ...>`).

## R10 — Limpeza da 069

**Decisão**: removem-se o `<input type="date">` auxiliar do `DateInput`, o seletor `.date-nativo` e as regras `color-scheme` que só existiam para ele e para o `type="month"` (FR-014). O botão, o invólucro `.date-wrap`, a propriedade `min` e a regra "o texto exibido manda" continuam.

## R11 — Testes

Não há framework de testes de interface. Entregas de verificação: (1) `scripts/verificar-calendario.mjs` (Node executa o `.ts` direto, como no `verificar-contraste`) com os casos de borda da lógica pura, exposto como `npm run verificar-calendario`; (2) `verificar-contraste`, `tsc`, `eslint` e build; (3) **Chrome automatizado** com capturas (agora o calendário é parte da página e **aparece nas capturas**), em claro e escuro, 360 px, dentro de modal e de tabela, com teclado e com contraste medido nas cores calculadas.

## R12 — Riscos

| Risco | Mitigação |
|---|---|
| Painel cortado por modal/tabela/borda | portal na raiz + `fixed` + abre para cima + margem de 8 px; testado dentro do modal do plano e perto do rodapé |
| Painel solto ao rolar | recálculo em rolagem (captura) e redimensionamento; fecha se o campo sai da tela |
| Esc fecha o modal junto | escuta em captura com `stopPropagation` |
| Dia da semana errado em casos de borda | aritmética verificada por script (1900 não bissexto, 2000 e 2024 bissextos, datas de referência) |
| Foco perdido ao fechar | foco devolvido ao botão |
| Muito código num componente só | lógica em `lib/calendario.ts`, interface em `Calendario.tsx`, campo de mês em `MonthInput.tsx` |
