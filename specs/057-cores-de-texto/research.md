# Research: Cores de Texto (spec 057)

Medições feitas com um protótipo do cálculo WCAG sobre as cores atuais (2026-10-19).

## R1 — Onde mora a paleta (fonte única)

**Decision**: `frontend/lib/paleta.ts` guarda todas as cores base (neutras de claro/escuro, 6 amostras de destaque, cores secundária/terciária/perigo do modo Padrão e as 3 paletas de daltonismo) e exporta `tokensDoTema({ dark, colorMode, destaque })`, função pura que devolve o mapa completo de variáveis CSS. `styles/tokens.css` deixa de ter cores (fica com raios, fontes e sombras).

**Rationale**: a mesma lista é usada por três consumidores — aplicação do tema, tela de Configurações (swatches) e script de contraste. Em CSS, a tela e o script teriam de repetir os valores (é o que acontece hoje com `COLOR_MODES` em `configuracoes/page.tsx`).

**Alternatives considered**: manter classes `.cb-*` e variantes por amostra em CSS — exigiria ~6×2×4 blocos escritos à mão e não seria verificável automaticamente.

## R2 — Cor de texto derivada automaticamente

**Decision**: para cada cor de papel (destaque, secundária, terciária, perigo) o tema deriva:

- **`base`**: a cor da paleta. Usada como fundo (botão principal, menu ativo, selo "Hoje", paginação ativa, marcadores).
- **`on`**: o texto sobre a base. Fica `#1a2024` ou `#ffffff`, o que tiver maior contraste; precisa dar ≥ 4.5.
- **`deep`**: o fim dos gradientes. É a base escurecida até 22%, limitada para que `on` continue ≥ 4.5 nela.
  - O protótipo mostrou que hoje o gradiente `accent → accent-deep` com texto escuro cai para 3.1–3.7 em Dourado, Rosa e Verde.
- **`forte`**: a base escurecida até ≥ 4.5 contra os fundos claros e contra a tinta translúcida da própria cor. Não muda com o modo, e branco sobre ela também passa de 4.5. Uso:
  - texto/ícone de destaque no modo claro;
  - fundo de avatar com texto branco.
- **`texto`**: a variante usada como cor de texto. No claro é a `forte`; no escuro é a base clareada até ≥ 4.5 contra os fundos escuros e a tinta.
- **`sobreLateral`**: a base clareada até ≥ 4.5 contra os dois tons da barra lateral (e do aviso flutuante), que é escura nos dois modos.
- **`tint`**: a base com transparência, com os mesmos alfas de hoje.

Valores medidos (texto claro / texto escuro): Turquesa `#2d7772` / `#4ecdc4`; Azul `#006ba7` / `#5ca5ce`; Verde `#007857` / `#3db595`; Dourado `#7f653b` / `#c29f68`, todos ≥ 5.2.

**Rationale**: atende "cor de fonte definida automaticamente" e garante por construção que nenhuma amostra gere texto ilegível; ajustes de tom preservam a identidade da cor (Assumption da spec).

## R3 — Neutros que hoje já reprovam

**Decision**: corrigir junto, porque também são cor de texto. Valores medidos hoje:

| Cor | Sobre | Contraste |
|---|---|---|
| `--c-text-muted` `#7c848c` | branco | 3.79 |
| `#69717a` (rótulo da barra lateral) | `#262c33` | 2.85 |
| `#8a6d00` (pendente/aviso), no modo escuro | tinta amarela escura | 1.89 |

O que muda:
- **Texto secundário** passa a ser derivado como os demais (escurecido/clareado até ≥ 4.5 nos fundos e cartões do modo).
- **Barra lateral** ganha dois tokens:
  - `--c-on-sidebar` (`#ffffff`, 14:1);
  - `--c-on-sidebar-muted`, que é o tom dos rótulos atuais clareado até ≥ 4.5 contra `--c-sidebar` e `--c-sidebar-2` dos dois modos.
- **"Pendente"/aviso** passa a usar `--c-accent3-text`, com variante própria para o escuro.

## R4 — Onde os tokens são aplicados

**Decision**: o `ThemeProvider` grava os tokens no `<html>` (`document.documentElement.style.setProperty` em efeito) e a classe `dark-mode` também no `<html>` (para as sombras). `app/layout.tsx` põe `tokensDoTema(PADRAO)` como `style` do `<html>` no servidor, para o primeiro desenho (inclusive a tela de login) já sair com as cores certas. `AppShell` deixa de montar estilo inline e classes `cb-*`.

**Rationale**:
- O aviso flutuante (toast) é renderizado fora do `.app`. Hoje ele nunca segue o modo escuro nem o destaque; no `<html>`, todas as telas e camadas recebem o mesmo tema.
- `login.css` já usa só tokens, então ele também passa a acompanhar o modo.

**Alternatives considered**: manter no `.app` — deixaria toast e login fora da regra "todas as telas".

## R5 — Preferências e migração

**Decision**: o formato novo é `{ dark, colorMode: "normal" | "protanopia" | "deuteranopia" | "tritanopia", destaque: <id da amostra> }`. Ao carregar:

1. `colorMode === "personalizado"`: vira `normal`, com `destaque` = amostra mais próxima de `customAccent`.
2. Senão, se existe `accentColor` (formato antigo): `destaque` = amostra mais próxima dele.
3. `destaque` desconhecido, ou JSON inválido: padrão (`turquesa`).
4. Se algo mudou, regrava no `localStorage` já no formato novo, sem os campos antigos.

A amostra mais próxima é a de menor distância RGB; no empate vale a ordem da lista.

**Rationale**: FR-008/FR-009; o código antigo some e não convive com o novo.

## R6 — Verificação

**Decision**: `frontend/scripts/verificar-contraste.mjs` importa `lib/paleta.ts` (Node 24 executa TS) e, para 2 modos × 4 modos de cores × 6 amostras, confere a lista de pares texto × fundo do [contrato](./contracts/tokens-de-tema.md); sai com código ≠ 0 e lista os pares reprovados. A ausência de literais de cor fora de `lib/paleta.ts` é conferida por `grep` (tasks).

**Rationale**: não há suíte de testes de frontend; o script é determinístico e roda sem navegador.

## R7 — Amostras oferecidas

**Decision**: Turquesa (padrão, `#4ecdc4`), Dourado (`#bb9457`), Azul (`#0072b2`), Laranja (`#e69f00`), Rosa (`#cc79a7`), Verde (`#009e73`) — todas já existem na paleta do sistema. Amarelos (`#ffe66d`, `#f0e442`) e vermelhos (`#c0392b`, `#d55e00`) ficam fora como destaque (assumption da spec).
