# Research: Modo de Cores Personalizado

Decisões técnicas da Phase 0. A decisão de produto (modo separado, 4 cores, reset sem trocar de modo) já veio fechada da conversa que precedeu a spec; esta fase resolve os detalhes de implementação.

## R1. Nomenclatura dos 4 campos novos: espelhar as variáveis CSS, não inventar nomes genéricos

- **Decision**: os 4 campos novos em `Preferencias` se chamam `customAccent`, `customAccent2`, `customAccent3`, `customDanger` — mesmo sufixo/ordem das variáveis CSS que sobrescrevem (`--c-accent`, `--c-accent2`, `--c-accent3`, `--c-danger`), prefixados com `custom` para não colidir com o `accentColor` já existente (que continua sendo o campo usado pelo modo "Padrão", inalterado).
- **Rationale**: mantém a correspondência 1:1 entre o nome do campo em TypeScript e a variável CSS que ele alimenta — quem for ler `AppShell.tsx` depois não precisa adivinhar qual campo vai para qual variável.
- **Alternatives considered**: um array `customColors: [string, string, string, string]` — descartado: perderia a correspondência nomeada (teria que lembrar que o índice 0 é `--c-accent`, 1 é `--c-accent2`, etc.), mais propenso a erro de ordem do que 4 campos nomeados.

## R2. `AppShell.tsx`: estender o `style` condicional existente, mesmo helpers

- **Decision**: a lógica hoje é `colorMode === "normal" && accentColor !== ACCENT_PADRAO ? {--c-accent, --c-accent-deep, --c-accent-tint} : {}`. Vira: `colorMode === "normal" ? {...bloco atual, inalterado} : colorMode === "personalizado" ? {--c-accent: customAccent, --c-accent-deep: darkenHex(customAccent, 0.22), --c-accent-tint: hexToRgba(customAccent, 0.15), --c-accent2: customAccent2, --c-accent2-tint: hexToRgba(customAccent2, 0.15), --c-accent3: customAccent3, --c-accent3-tint: hexToRgba(customAccent3, 0.35), --c-danger: customDanger, --c-danger-tint: hexToRgba(customDanger, 0.12)} : {}` (valores de alpha de cada `-tint` copiados dos mesmos usados hoje em `tokens.css` `:root` para cada variável, para manter a mesma intensidade visual que os presets já usam).
- **Rationale**: FR-004 — reaproveita exatamente os mesmos 2 helpers já existentes (`darkenHex`/`hexToRgba`), só aplicados a 4 cores em vez de 1; não introduz nenhum cálculo novo.
- **Alternatives considered**: calcular `-deep` para `--c-accent2`/`--c-accent3`/`--c-danger` também, mesmo esses não tendo uma variante `-deep` hoje em `tokens.css` — descartado, não há nenhum uso de `--c-accent2-deep` etc. em nenhum lugar do CSS; adicionar isso seria uma variável nunca lida por nenhuma regra existente.

## R3. `configuracoes/page.tsx`: 5ª entrada computada no grid, bloco de cor condicional por modo

- **Decision**: o array `COLOR_MODES` (hoje `const` module-level, 4 entradas fixas) ganha uma 5ª entrada "Personalizado" calculada dentro do componente (não no `const` module-level, porque suas `swatches` precisam refletir os valores atuais de `customAccent`/`customAccent2`/`customAccent3`/`customDanger`, que só existem depois do `useTheme()`); o bloco "Cor de destaque" passa a renderizar: se `colorMode === "personalizado"`, 4 `<input type="color">` (um por campo, reaproveitando a mesma estrutura `accent-picker-row`/`accent-swatch-label` já existente, repetida 4 vezes); senão, o bloco atual de 1 seletor (inalterado, continua só ativo com `colorMode === "normal"`).
- **Rationale**: FR-001/FR-002 — o grid de modos precisa mostrar as cores reais que o usuário escolheu, não um preset fixo, para a pré-visualização ter sentido; o bloco de cor precisa saber em qual dos dois estados (1 cor vs. 4 cores) renderizar.
- **Alternatives considered**: manter `COLOR_MODES` 100% estático e esconder completamente as swatches do card "Personalizado" — descartado, um card sem nenhuma pré-visualização de cor destoaria visualmente dos outros 4 cards que sempre mostram suas 4 cores.

## R4. Reset: função dedicada, não reaproveitar `resetAccentColor`

- **Decision**: nova função `resetCustomColors()` no `ThemeContext`, que volta os 4 campos novos (`customAccent`, etc.) aos valores padrão (`#4ecdc4`, `#bb9457`, `#ffe66d`, `#c0392b`) — `resetAccentColor()` (já existente, reseta só `accentColor`) não é tocada nem reaproveitada, já que atua sobre um campo diferente.
- **Rationale**: FR-005 — são dois resets conceitualmente distintos (1 campo vs. 4 campos); forçar os dois a compartilhar uma função exigiria parâmetros/condicionais sem necessidade real, já que nenhum dos dois reset precisa saber da existência do outro.
- **Alternatives considered**: um reset genérico `resetCores(modo)` que decide internamente quais campos resetar — descartado, complexidade desproporcional para 2 casos concretos e simples.

## R5. Nenhum teste automatizado — validação manual via `quickstart.md`

- **Decision**: sem testes (xUnit não se aplica, é frontend; o projeto não tem suíte de teste de frontend configurada).
- **Rationale**: mesma decisão já aceita para toda mudança de frontend anterior nesta sessão (specs 045-048, 050).
- **Alternatives considered**: nenhuma.
