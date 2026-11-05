# Implementation Plan: Calendário próprio no padrão do sistema

**Branch**: `070-calendario-proprio` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

## Summary

O botão de calendário que a spec 069 colocou dentro de todo campo de data passa a abrir um **calendário próprio** no visual do SPI, em vez do calendário do navegador. O painel tem três visões (dias, meses e anos), dia de hoje destacado, dias bloqueados pelo mínimo, atalhos "Hoje" e "Limpar", teclado completo e leitor de tela, e abre como janela centralizada no celular. É desenhado **na raiz da página** (portal) para nunca ser cortado por modais e tabelas. Os 4 campos de competência ganham um campo próprio (`mm/aaaa`) que abre o mesmo painel no modo mês. **Somente frontend**, sem biblioteca, sem backend e sem banco. Decisões e alternativas: [research.md](research.md); contratos: [contracts/calendario.md](contracts/calendario.md).

## Technical Context

**Language/Version**: TypeScript 5 / Next.js 16.3.4 / React 19.2.8 (somente frontend)

**Primary Dependencies**: nenhuma nova (proibido adicionar pacote); reaproveita `Icon`, `lib/format` (`dataIsoLocal`), os tokens de tema e as classes `.modal-overlay`, `.btn`

**Storage**: nenhuma mudança

**Testing**: `scripts/verificar-calendario.mjs` (lógica pura, Node 24 executa o `.ts`), `verificar-contraste`, `tsc`, `eslint`, `npm run build` e Chrome automatizado com capturas (claro, escuro, 360 px), como nas specs 063 a 069

**Target Platform**: navegador desktop e celular (360 px)

**Project Type**: aplicação web existente (`frontend/`)

**Performance Goals**: abrir o calendário sem atraso perceptível; nenhuma consulta nova

**Constraints**: valores `yyyy-MM-dd` e `yyyy-MM` inalterados; sem objeto `Date` para exibir/ordenar datas (só o "hoje" existente); sem cor literal; sem `setState` em efeito (regra do ESLint do projeto); backend intocado

**Scale/Scope**: 3 arquivos novos (`lib/calendario.ts`, `components/shared/Calendario.tsx`, `components/shared/MonthInput.tsx`) + 1 script de verificação; alterados: `DateInput.tsx`, `dashboard.css`, `package.json` (só o script), 4 páginas de conta (troca do campo de mês)

## Constitution Check

| Princípio | Avaliação |
|---|---|
| I. Exclusão lógica | ✅ Sem dados envolvidos |
| II. Validação única no backend | ✅ O calendário só facilita escolher; a validação real das datas segue no servidor (o mínimo só bloqueia a escolha, não a digitação) |
| III. Sem capacidade configurável sem implementação | ✅ Nenhuma opção nova |
| IV. Segredos | ✅ Sem impacto |
| V. Documentação como registro vinculante | ✅ Nota na spec 069 (o calendário do navegador foi substituído) e relatório ao final |

**Re-avaliação pós-design**: sem violações. Complexity Tracking vazio.

## Project Structure

### Documentation (this feature)

```text
specs/070-calendario-proprio/
├── spec.md
├── plan.md            # este arquivo
├── research.md        # decisões R1–R12
├── contracts/calendario.md
├── quickstart.md
├── checklists/requirements.md
├── tasks.md           # /speckit-tasks
└── analysis.md        # /speckit-analyze
```

### Source Code (repository root)

```text
frontend/
├── lib/calendario.ts                               # NOVO: lógica pura (grade, bissexto, dia da semana, navegação, nomes)
├── scripts/verificar-calendario.mjs                # NOVO: casos de borda da lógica pura (npm run verificar-calendario)
├── components/shared/Calendario.tsx                # NOVO: painel (dias/meses/anos) + portal + celular + teclado
├── components/shared/MonthInput.tsx                # NOVO: campo de competência (mm/aaaa + botão)
├── components/shared/DateInput.tsx                 # ALTERADO: abre o Calendario; sai o <input type="date"> auxiliar
├── styles/dashboard.css                            # ALTERADO: + .cal-*; - .date-nativo e color-scheme
├── package.json                                    # ALTERADO: só o script "verificar-calendario" (sem dependência)
└── app/(app)/financeiro/contas-a-pagar/{novo,[id]/editar}/page.tsx
    app/(app)/financeiro/contas-a-receber/{novo,[id]/editar}/page.tsx   # <input type="month"> -> <MonthInput>
```

**Structure Decision**: o painel e o campo de mês entram em `components/shared` junto do `DateInput`; a lógica pura fica em `lib/` (como `parcelas.ts`). **Nada em `src/`, `tests/` ou `database/`.**

## Ordem sugerida (insumo do /speckit-tasks)

1. Linha de base; `lib/calendario.ts` e o script de verificação **antes** do painel (a lógica é a parte que erra em silêncio).
2. Painel de dias + CSS + integração no `DateInput` (US1), depois as visões de mês/ano, "Hoje", "Limpar", mínimo e hoje destacado (US2).
3. Teclado/leitor de tela, janela do celular e posicionamento sem corte (US3).
4. `MonthInput` e as 4 páginas (US4).
5. Limpeza da 069, contraste, varredura final no Chrome (com capturas), builds, relatório e Trello.

## Verificações "MUST NOT" que o /speckit-tasks deve cobrir (regra do CLAUDE.md)

| Requisito | Verificação exigida |
|---|---|
| FR-008 sem conversão de data pelo fuso | **leitura do diff** de `lib/calendario.ts`, `Calendario.tsx`, `DateInput.tsx` e `MonthInput.tsx`: `grep -nE "new Date\(|Date\.parse|toISOString|getTimezoneOffset"` sem resultado (o "hoje" vem de `dataIsoLocal`); o script de borda confirma o dia da semana de datas conhecidas |
| FR-014 nada nativo restante | `grep -rn 'type="date"\|type="month"' frontend/app frontend/components` **sem resultado**; `grep -n "date-nativo\|color-scheme" frontend/styles/dashboard.css` sem resultado |
| FR-015 sem dependência nova e backend intocado | `git diff -- frontend/package.json frontend/package-lock.json` mostra **só** o script novo (nenhuma linha em `dependencies`); `git status --porcelain -- src tests database` e `git diff --stat -- src tests database` vazios |
| FR-002 sem cor literal | `grep -nE "#[0-9a-fA-F]{3,6}\b|rgb\(|rgba\("` nas linhas adicionadas a `dashboard.css`, `Calendario.tsx` e `MonthInput.tsx` sem resultado |
| FR-016 contrato do `DateInput` intacto | leitura do diff: as propriedades públicas e o valor `yyyy-MM-dd` não mudam; nenhuma das 14 telas que o usam é alterada |
| FR-009 sem corte | no Chrome: o calendário dentro do modal do plano, numa tabela e perto do rodapé fica 100% dentro da tela e é o elemento no topo (`elementFromPoint`) |
| Arquivos novos esperados | `git status --porcelain -- frontend` mostra **somente**: `lib/calendario.ts`, `scripts/verificar-calendario.mjs`, `components/shared/Calendario.tsx`, `components/shared/MonthInput.tsx` |

## Complexity Tracking

Sem violações da constituição a justificar.
