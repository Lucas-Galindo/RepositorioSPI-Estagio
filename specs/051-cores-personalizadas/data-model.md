# Data Model: Modo de Cores Personalizado

Nenhuma entidade de backend. O "modelo" desta feature é a extensão do objeto `Preferencias` já persistido em `localStorage` (`frontend/contexts/ThemeContext.tsx`).

## `Preferencias` (existente, + 4 campos)

| Campo | Tipo | Já existia? | Valor padrão |
|---|---|---|---|
| `dark` | `boolean` | Sim | `false` |
| `colorMode` | `"normal" \| "protanopia" \| "deuteranopia" \| "tritanopia" \| "personalizado"` | Sim, tipo estendido | `"normal"` |
| `accentColor` | `string` (hex) | Sim | `"#4ecdc4"` |
| `customAccent` | `string` (hex) | **Novo** | `"#4ecdc4"` |
| `customAccent2` | `string` (hex) | **Novo** | `"#bb9457"` |
| `customAccent3` | `string` (hex) | **Novo** | `"#ffe66d"` |
| `customDanger` | `string` (hex) | **Novo** | `"#c0392b"` |

**Chave de armazenamento**: `spi:preferencias-tema` (`localStorage`) — inalterada, mesmo objeto, mais 4 campos.

## Fluxo de aplicação à interface (`AppShell.tsx`)

| `colorMode` | Variáveis CSS injetadas |
|---|---|
| `"normal"` (com `accentColor` diferente do padrão) | `--c-accent`, `--c-accent-deep`, `--c-accent-tint` — a partir de `accentColor` (inalterado) |
| `"protanopia"`/`"deuteranopia"`/`"tritanopia"` | Nenhuma variável injetada por `AppShell` — a classe `cb-<modo>` já aplica os valores fixos via `tokens.css` (inalterado) |
| `"personalizado"` | `--c-accent`/`--c-accent-deep`/`--c-accent-tint` (de `customAccent`), `--c-accent2`/`--c-accent2-tint` (de `customAccent2`), `--c-accent3`/`--c-accent3-tint` (de `customAccent3`), `--c-danger`/`--c-danger-tint` (de `customDanger`) — **novo** |

## Fluxo de reset

| Ação | Efeito |
|---|---|
| `resetAccentColor()` (existente) | `accentColor` volta a `"#4ecdc4"` — só afeta o modo "Padrão", inalterado |
| `resetCustomColors()` (**novo**) | `customAccent`/`customAccent2`/`customAccent3`/`customDanger` voltam aos 4 valores padrão — `colorMode` não é alterado (continua `"personalizado"`) |

## Nenhuma validação nova

Os 4 campos aceitam qualquer valor hexadecimal válido que o `<input type="color">` nativo do navegador produzir — nenhuma validação de contraste/legibilidade (spec.md FR-009).
