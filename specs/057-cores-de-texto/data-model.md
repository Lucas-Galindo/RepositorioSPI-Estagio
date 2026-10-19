# Data Model: Cores de Texto (spec 057)

Sem dados no servidor. Duas estruturas no frontend.

## Paleta (`frontend/lib/paleta.ts`, constante)

- **`NEUTROS.claro` / `NEUTROS.escuro`**: `bg`, `surface`, `surface2`, `text`, `textMuted` (tom-base, ajustado para ≥ 4.5), `sidebar`, `sidebar2`. São os valores atuais de `tokens.css`.
- **`DESTAQUES`**: lista ordenada de `{ id, nome, cor }`. Os ids são `turquesa` (padrão), `dourado`, `azul`, `laranja`, `rosa` e `verde`.
- **`PALETAS_DE_MODO`**: as três cores de apoio de cada modo de cores, `{ accent2, accent3, danger }`.
  - `normal`: `#bb9457`, `#ffe66d`, `#c0392b`, com o destaque vindo da amostra escolhida.
  - Modos de daltonismo: as cores de hoje, incluindo o destaque fixo do modo (`#0072b2` ou `#cc79a7`).

## Preferência de aparência (`localStorage["spi:preferencias-tema"]`)

| Campo | Tipo | Regra |
|---|---|---|
| `dark` | boolean | padrão `false` |
| `colorMode` | `"normal" \| "protanopia" \| "deuteranopia" \| "tritanopia"` | valor desconhecido → `normal` |
| `destaque` | id de `DESTAQUES` | desconhecido → `turquesa`; só vale com `colorMode = normal` |

**Formato antigo** (migrado ao carregar, research.md R5): `accentColor`, `customAccent`, `customAccent2`, `customAccent3`, `customDanger` e `colorMode = "personalizado"`. Depois da migração esses campos não são mais gravados.
