---

description: "Task list for Cores de Texto (Contraste Automático e Paleta Restrita)"
---

# Tasks: Cores de Texto (Contraste Automático e Paleta Restrita)

**Input**: Design documents from `specs/057-cores-de-texto/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: não há suíte de testes de frontend; a verificação é o script de contraste (T004) mais buscas/leitura de diff para os requisitos "MUST NOT" (FR-001, FR-006, FR-007), conforme o `CLAUDE.md`.

**Organization**: a paleta e os tokens (Phase 2) servem US1 e US2; US1 troca os literais dos CSS; US2 refaz a tela de Configurações; US3 é a migração das preferências.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Ler `frontend/styles/tokens.css`, `frontend/styles/dashboard.css` e `frontend/styles/login.css` e listar, por seletor, cada cor de texto literal e o fundo sobre o qual ela fica (base para T008/T009).

---

## Phase 2: Foundational (Blocking Prerequisites)

- [X] T002 `frontend/lib/color.ts`: acrescentar `luminancia(hex)`, `contraste(a, b)`, `misturar(a, b, t)` e `ajustarAteContraste(cor, fundos, alvo)` (research.md R2), com comentário de que seguem a fórmula WCAG 2.x; remover helpers que ficarem sem uso.
- [X] T003 Criar `frontend/lib/paleta.ts` conforme data-model.md e contracts/tokens-de-tema.md:
  - `NEUTROS`, `DESTAQUES` (ids `turquesa`, `dourado`, `azul`, `laranja`, `rosa` e `verde`, nessa ordem) e `PALETAS_DE_MODO`;
  - `tokensDoTema({ dark, colorMode, destaque })`, que devolve todas as variáveis do contrato (derivação de research.md R2/R3);
  - `amostraMaisProxima(hex)` (distância RGB; empate pela ordem da lista);
  - comentários explicando a derivação.
- [X] T004 Criar `frontend/scripts/verificar-contraste.mjs`. Ele importa `../lib/paleta.ts`, percorre 2 × 4 × 6 combinações e confere cada par da tabela "Pares conferidos" do contrato (tinta composta sobre `--c-surface`). Imprime "N combinações, M pares abaixo de 4.5:1", lista os reprovados e sai com código 1 se M > 0. Rodar e ajustar a derivação até M = 0.
- [X] T005 `frontend/contexts/ThemeContext.tsx`:
  - preferências novas `{ dark, colorMode, destaque }`; remover `accentColor`/`custom*` e seus setters;
  - `setDestaque(id)`;
  - efeito que aplica `tokensDoTema(prefs)` no `document.documentElement` e põe/tira a classe `dark-mode` no `<html>` (research.md R4).
- [X] T006 `frontend/app/layout.tsx`: `style` do `<html>` com `tokensDoTema` do padrão (primeiro desenho, inclusive login). `frontend/components/layout/AppShell.tsx`: remover o estilo inline, as classes `dark-mode`/`cb-*` e o import de `lib/color`.
- [X] T007 `frontend/styles/tokens.css`: remover as cores (que agora vêm de `paleta.ts`) e as classes `.cb-*`; manter fontes, raios e sombras (sombras do escuro sob `.dark-mode`, agora no `<html>`).

**Checkpoint**: tema aplicado a partir da paleta, script de contraste com 0 reprovados.

---

## Phase 3: User Story 1 - Texto sempre legível nos dois modos (Priority: P1) 🎯 MVP

- [X] T008 [US1] `frontend/styles/dashboard.css`: trocar cada cor literal de texto pelo token do contrato. Mapeamento:
  - **Sobre a barra lateral, o aviso flutuante, o tooltip, o botão escuro e o card escuro**: `#fff` → `--c-on-sidebar`; cinzas → `--c-on-sidebar-muted`.
  - **Sobre fundo de destaque** (menu ativo, botão principal, selo Hoje, paginação ativa, marca): `#1a2024` → `--c-on-accent`.
  - **Aviso/pendente**: `#8a6d00` → `--c-accent3-text`.
  - **Avatares**: `#fff` → `--c-on-strong`, com o fundo vindo de `--c-*-strong`.
  - **Destaque usado como texto**: `color: var(--c-accent-deep)` → `--c-accent-text`; `var(--c-accent2)` → `--c-accent2-text`; `var(--c-danger)` → `--c-danger-text`.
  - **Destaque sobre a barra lateral** (`.card-eyebrow`, `.fav-time`, `.toast svg`): → `--c-accent-on-sidebar`.
  - **Gradiente** `#8f6f3d` → `--c-accent2-deep`.
  - **Regras `.dark-mode ...`** que hoje só existem para recolorir texto: remover se ficarem redundantes.
- [X] T009 [P] [US1] `frontend/styles/login.css`: mesmo mapeamento do T008 para o painel escuro, o botão de entrar, a etiqueta de protótipo, os links e os ícones.
- [X] T010 [P] [US1] Remover os literais restantes fora dos CSS:
  - `frontend/app/(app)/relatorios/alunos/page.tsx` e `frontend/app/(app)/relatorios/turmas/page.tsx`: o gradiente `#8f6f3d` passa a usar `var(--c-accent2-deep)`;
  - `frontend/lib/format.ts`: `AV_COLORS` passa a usar os tokens `--c-*-strong`;
  - `frontend/components/admin/CadastrarProfessoraForm.tsx`: `var(--border, #e5e5e5)` aponta para um token que não existe; trocar por uma borda do tema.

---

## Phase 4: User Story 2 - Cor de destaque só da paleta (Priority: P1)

- [X] T011 [US2] `frontend/app/(app)/configuracoes/page.tsx`:
  - swatches de modo de cores lidos de `paleta.ts`, sem o modo "Personalizado";
  - "Cor de destaque" vira uma grade de amostras nomeadas (`DESTAQUES`), com a atual marcada e o clique em `setDestaque`;
  - desabilitada com a explicação atual quando há modo de daltonismo;
  - remover todo `input type="color"`, o texto do código hex e os botões de "Restaurar padrão" (a amostra padrão fica marcada como "(padrão)" na grade, então voltar a ela é um clique);
  - estilos da grade em `dashboard.css`, reaproveitando `colormode-card`/`swatch` quando possível; remover os estilos `accent-*` que ficarem sem uso.

---

## Phase 5: User Story 3 - Preferência antiga migrada (Priority: P2)

- [X] T012 [US3] `frontend/lib/paleta.ts`: função pura `migrarPreferencias(salvo: unknown): { preferencias, mudou }` com as regras de research.md R5. Entradas: Personalizado → Padrão + amostra mais próxima de `customAccent`; `accentColor` → amostra mais próxima; `destaque`/`colorMode` desconhecidos → padrão; texto que não é JSON → padrão. `ThemeContext.tsx` só lê o `localStorage`, chama a função e regrava quando `mudou`. Fica em `paleta.ts` (sem React) para o script de T004 poder importá-la. Comentar o porquê de cada regra.
- [X] T013 [US3] Estender `frontend/scripts/verificar-contraste.mjs` para conferir as 3 entradas de quickstart.md §3 com `migrarPreferencias` (resultado esperado e `mudou = true`), além de uma preferência já no formato novo (`mudou = false`).

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T014 FR-001/SC-002 ("MUST NOT" cor de texto fixa): rodar `grep -rnE "#[0-9a-fA-F]{3,8}\b" frontend/styles frontend/app frontend/components frontend/lib frontend/contexts` e confirmar que só sobra `frontend/lib/paleta.ts`. Ler também o diff de `dashboard.css`/`login.css` e confirmar que nenhuma `color:` ficou com valor literal.
- [X] T015 FR-006/FR-007 ("MUST NOT" cor livre): `grep -rn 'type="color"\|personalizado\|customAccent\|accentColor' frontend --include=*.ts --include=*.tsx` sem ocorrências fora de `migrarPreferencias` em `lib/paleta.ts` (onde a migração ficou, ver T012). Ler o diff de `ThemeContext.tsx`/`paleta.ts` e confirmar que a cor aplicada vem sempre de `DESTAQUES` por id, nunca de um hex salvo.
- [X] T016 FR-012: nota "Regra substituída pela spec 057" no topo de `specs/051-cores-personalizadas/spec.md` (o modo Personalizado e a cor livre foram removidos).
- [X] T017 `git status --porcelain -- frontend specs`: arquivos novos esperados só `frontend/lib/paleta.ts`, `frontend/scripts/verificar-contraste.mjs` e os documentos de `specs/057-cores-de-texto/`.
- [X] T018 `node scripts/verificar-contraste.mjs`, `npx tsc --noEmit`, `npx eslint` nos arquivos tocados e `npm run build` em `frontend/`; `dotnet build` não muda (só frontend).
- [ ] T019 (parte manual) Validar quickstart.md §2 num navegador real. Na sessão de implementação não havia navegador: conferido só o primeiro desenho do login via HTML servido pelo build de produção (tokens presentes no `<html>`); a validação visual fica pendente no relatório.

---

## Dependencies & Execution Order

- Phase 1 → Phase 2 (T002 → T003 → T004; T005 depende de T003; T006/T007 dependem de T005) → US1 e US2 (paralelas) → US3 → Polish.
- T012 pode ser feita junto com T005 (mesmo arquivo), mas fica listada em US3 por ser independente do resto.

## Implementation Strategy

MVP = Phases 1–3 (texto legível com a paleta derivada). US2 fecha a restrição de cores, US3 cuida de quem já personalizou.
