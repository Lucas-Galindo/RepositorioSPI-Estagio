# Contrato: componentes do calendário (spec 070)

## `frontend/lib/calendario.ts` (lógica pura, sem React e sem `Date`)

| Função | Resultado |
|---|---|
| `ehBissexto(ano)` / `diasDoMes(ano, mes)` | regra gregoriana; `mes` de 1 a 12 |
| `diaDaSemana(ano, mes, dia)` | 0 (domingo) a 6 (sábado), por aritmética |
| `montarGrade(ano, mes)` | 6 semanas × 7 células `{ iso, dia, doMes }`, começando no domingo; dias dos meses vizinhos com `doMes: false` |
| `adicionarMeses(ano, mes, delta)` / `adicionarDias(iso, delta)` | navegação com virada de ano; `adicionarDias` mantém o dia dentro do mês seguinte/anterior |
| `estaBloqueado(iso, min?, max?)` | comparação de texto `yyyy-MM-dd` |
| `lerIso(iso)` / `lerMes(aaaaMm)` | `{ ano, mes, dia }` ou `null` (formato e limites inválidos) |
| `tituloDoMes(ano, mes)` | "novembro de 2026" |
| `rotuloDoDia(iso)` | "5 de novembro de 2026" (para leitor de tela) |
| `anosDaPagina(ano)` | 12 anos da página que contém `ano`, limitados a 1900–2100 |
| constantes | `MESES`, `DIAS_ABREVIADOS` (`D S T Q Q S S`), `ANO_MIN = 1900`, `ANO_MAX = 2100` |

## `Calendario` (`components/shared/Calendario.tsx`)

| Propriedade | Tipo | Observação |
|---|---|---|
| `modo` | `"dia" \| "mes"` | `"dia"` abre nos dias; `"mes"` (competência) abre nos meses |
| `valor` | `string` | `yyyy-MM-dd` (modo dia) ou `yyyy-MM` (modo mês); vazio/inválido abre em hoje |
| `min` | `string` opcional | `yyyy-MM-dd` (dias anteriores bloqueados) |
| `ancora` | `HTMLElement \| null` | invólucro do campo, para posicionar |
| `aoEscolher` | `(valor: string) => void` | já no formato do modo |
| `aoLimpar` | `() => void` | "Limpar" |
| `aoFechar` | `() => void` | Esc, clique fora, escolha; o chamador devolve o foco ao botão |

Renderiza em portal (`document.body`), camada 120; até 640 px, como janela centralizada sobre `.modal-overlay`.

## `DateInput` (existente) e `MonthInput` (novo)

- `DateInput`: **mesmas propriedades de hoje** (`value`, `onChange`, `required`, `id`, `className`, `title`, `disabled`, `ariaLabel`, `style`, `min`); valor `yyyy-MM-dd` ou vazio; some o `<input type="date">` auxiliar.
- `MonthInput`: `value` (`yyyy-MM` ou vazio), `onChange(value)`, `required`, `id`, `className`, `title`, `disabled`, `ariaLabel`, `style`; texto `mm/aaaa`.

## Estrutura e CSS

- `.date-wrap`, `.date-cal-btn` (da 069) servem aos dois campos; novos seletores `.cal-*` em `styles/dashboard.css` (só tokens de cor).
- Saem `.date-nativo` e as regras `color-scheme`.

## Garantias

- Valores trocados com as telas: `yyyy-MM-dd` e `yyyy-MM`, como antes; nenhuma conversão com `Date`.
- Sem dependência nova; sem backend.
