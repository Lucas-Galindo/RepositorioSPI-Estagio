# Research: Padronizar Formato Brasileiro de Data e Hora

## Contexto levantado no código atual

- `frontend/lib/format.ts` já existe e já produz o formato correto: `fmtData` (dd/mm/aaaa),
  `fmtHora` (HH:mm), `fmtDataHora` (dd/mm/aaaa HH:mm), `fmtMesAno`/`fmtMesAbreviado` (mês por
  extenso/abreviado em português). Todas essas funções fazem parsing manual de string
  (`split`/`slice`), evitando deliberadamente `new Date(iso)` para strings de data pura — um
  comentário no arquivo explica que isso evita um bug de deslocamento de fuso horário (UTC) já
  corrigido uma vez no projeto.
- 22 arquivos já importam de `lib/format.ts` — adoção já é ampla, não um mecanismo a criar do
  zero.
- Apenas 1 ocorrência confirmada de exibição de data que contorna `lib/format.ts`:
  `frontend/app/(app)/dashboard/page.tsx:27` usa `dia.toLocaleDateString("pt-BR", {day:
  "2-digit", month: "2-digit"})` diretamente. O resultado já é visualmente correto (pt-BR,
  dd/mm), mas diverge do padrão central — mantê-lo assim contraria FR-004/US3 (única fonte de
  formatação), então entra no escopo como correção de consistência, não de bug visual.
- As demais ocorrências fora de `lib/format.ts` (ex.: `dataIso.split("-")[1]` em
  `pagamentos/page.tsx:26`, `.toISOString().slice(0,10)` em `dashboard/page.tsx` e
  `alunos/[id]/page.tsx`) são derivação de chave/valor interno (para agrupar por mês, ou gerar o
  valor default de um campo), não exibição para a usuária — fora do escopo desta funcionalidade
  (FR-001/FR-002 falam de exibição).
- 21 ocorrências de `<input type="date">`/`<input type="time">` em 20 arquivos: 3 dentro do
  único componente de formulário reutilizável que usa data/hora (`components/aulas/AulaForm.tsx`
  — 1 date + 2 time) e as 18 restantes espalhadas em formulários/filtros de página (Financeiro,
  Relatórios, Lembretes). Todas MUST ser substituídas pelo novo componente (FR-008).
- Não existe hoje nenhum componente compartilhado de input (`components/ui/` não existe); cada
  formulário escreve `<input>` cru com `className`. `frontend/components/shared/` já é o lugar
  convencionado para componentes transversais pequenos (`Icon`, `StatusPill`, `ConfirmModal`,
  `EmptyState`, `ThemeToggle`, `StatusBadge`).
- `frontend/package.json` não tem nenhuma biblioteca de date-picker instalada (nem
  react-day-picker, react-datepicker, date-fns, dayjs, luxon). O stack de dependências do
  frontend é enxuto por convenção do projeto.

## Decisão 1: Não adicionar biblioteca de date-picker — construir componente próprio

**Decision**: Implementar `DateInput` e `TimeInput` como componentes React próprios, leves,
baseados em `<input type="text">` com máscara/validação de dígitos, sem nenhuma dependência nova.

**Rationale**: O projeto não tem nenhuma dependência de data/hora hoje e mantém o `dependencies`
do frontend deliberadamente mínimo (5 pacotes, todos fonte/framework). Adicionar uma biblioteca
de terceiros só para dois campos de formulário é desproporcional ao problema (formato de
exibição/entrada, não um calendário visual complexo) e introduz superfície de manutenção nova
(versionamento, bundle size, compatibilidade com React 19) sem necessidade — o requisito
(FR-008) é sobre o formato do valor exibido/aceito, não sobre navegação visual por um calendário.

**Alternatives considered**:
- **Biblioteca de terceiros (ex. react-day-picker)**: rejeitada — dependência nova
  desproporcional ao escopo, e nenhuma já está instalada.
- **Manter `<input type="date">`/`type="time"` nativo**: era a opção mais simples, mas foi
  explicitamente rejeitada na clarificação da spec (Session 2026-09-17) — o formato exibido
  dentro do seletor nativo depende do idioma/SO da usuária, e a decisão registrada foi garantir
  dd/mm/aaaa e 24h no código, independentemente disso.

## Decisão 2: Contrato de valor dos novos componentes = o mesmo já usado hoje

**Decision**: `DateInput` recebe/emite valor no formato `yyyy-MM-dd` (o mesmo que
`<input type="date">` já produz) e `TimeInput` recebe/emite `HH:mm`. Cada componente só troca a
*representação visual* (o que a usuária vê e digita) — o valor que trafega para o estado do
formulário e para a API MUST permanecer no mesmo formato de hoje.

**Rationale**: FR-007 exige que nenhum dado armazenado/enviado mude de comportamento. Mantendo o
contrato de valor idêntico ao do `<input type="date/time">` nativo, a troca em cada formulário é
uma substituição de componente (mesmo `value`/`onChange` shape), sem tocar o restante do
formulário, o payload enviado ao backend, nem qualquer validação de negócio já existente
(Princípio II da constituição — a validação de negócio continua só no backend).

**Alternatives considered**:
- **Novo formato de valor (ex. `Date` object ou string dd/mm/aaaa)**: rejeitado — obrigaria
  alterar todo ponto de chamada (18 páginas + `AulaForm`) para converter o valor antes de
  montar o payload da API, ampliando o raio de mudança e o risco de regressão sem benefício
  correspondente.

## Decisão 3: Local dos novos componentes

**Decision**: Criar `frontend/components/shared/DateInput.tsx` e
`frontend/components/shared/TimeInput.tsx`.

**Rationale**: `components/shared/` já é o diretório usado para componentes pequenos e
transversais (`Icon`, `StatusPill`, `ConfirmModal`, `EmptyState`, `ThemeToggle`,
`StatusBadge`), reaproveitados por múltiplas telas. `DateInput`/`TimeInput` são exatamente desse
tipo — sem estado de domínio, reutilizáveis por qualquer formulário.

**Alternatives considered**:
- **Novo diretório `components/ui/`**: rejeitado — criaria uma segunda convenção de
  "componentes genéricos" convivendo com `components/shared/` já estabelecida, sem necessidade.

## Decisão 4: Parsing de data/hora dentro dos novos componentes

**Decision**: `DateInput`/`TimeInput` MUST usar o mesmo método de parsing manual de string
(`split`/slice de dígitos) já usado em `lib/format.ts`, nunca `new Date(stringDataPura)`, ao
converter entre o valor digitado (dd/mm/aaaa ou HH:mm) e o valor emitido (`yyyy-MM-dd`/`HH:mm`).

**Rationale**: `new Date("yyyy-MM-dd")` é interpretado como UTC meia-noite pelo JavaScript, o que
já causou um bug de deslocamento de data (dia anterior/seguinte dependendo do fuso do navegador)
corrigido uma vez em `lib/format.ts` — reintroduzir esse padrão nos novos componentes
reintroduziria a mesma classe de bug.

## Decisão 5: Corrigir o único ponto de exibição que hoje contorna `lib/format.ts`

**Decision**: `frontend/app/(app)/dashboard/page.tsx:27` passa a usar `fmtData` (ou uma variante
dela) de `lib/format.ts` em vez de `toLocaleDateString("pt-BR", ...)` direto.

**Rationale**: Resultado visual não muda (já é dd/mm hoje), mas atende FR-004/US3 — nenhuma tela
reimplementa sua própria lógica de formatação divergente do ponto central.
