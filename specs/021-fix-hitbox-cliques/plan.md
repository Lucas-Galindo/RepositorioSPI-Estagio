# Implementation Plan: Área de Clique Expandida no Menu do Financeiro e Correção de Hitbox na Tabela de Contas a Pagar

**Branch**: `021-fix-hitbox-cliques` | **Date**: 2026-09-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/021-fix-hitbox-cliques/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Duas mudanças independentes, ambas restritas ao frontend: (1) **US1 — melhoria de UX nova**: o menu de abas do módulo Financeiro (`frontend/app/(app)/financeiro/layout.tsx`) hoje só responde a clique exatamente sobre o texto/botão de cada aba; o espaço vazio entre abas (produzido por `justify-content: space-between` na classe compartilhada `.row-gap`) não faz nada. A correção adiciona resposta a clique em toda a faixa do menu, com a área extra de cada aba dividida estaticamente pelo layout renderizado (metade do gap para cada vizinho), implementada via CSS (cada `<Link>` passa a ocupar toda a largura disponível do seu "slot" através de `flex: 1 1 0` em um contêiner com uma classe nova e escopada — não a `.row-gap` compartilhada, que é reusada em 14 outros arquivos do frontend e não deve ter seu comportamento alterado globalmente), sem nenhuma lógica de JavaScript de cálculo de distância por clique. (2) **US2 — correção de bug preexistente, direção inalterada desde a spec anterior**: a tabela de Contas a Pagar (`frontend/app/(app)/financeiro/contas-a-pagar/page.tsx`) tem, segundo a investigação de código, o `onClick` de navegação corretamente escopado ao `<tr className="row-link">` do `tbody` (não há nenhuma lógica de ordenação em `<thead>`); a causa mais provável do clique indevido perto do cabeçalho é a ausência de separação geométrica suficiente entre `thead th` e a primeira `tbody tr.row-link` em `frontend/styles/dashboard.css`, a ser confirmada por inspeção interativa (DevTools) como primeira tarefa de implementação, igual à spec anterior desta feature.

## Technical Context

**Language/Version**: TypeScript / React 19 (Next.js 16, App Router) — mesmo escopo de frontend das features anteriores (020, 021 anterior); nenhuma mudança em C#/.NET

**Primary Dependencies**: Next.js, React — nenhuma dependência nova; solução usa CSS/JSX já presente no projeto (flexbox, já em uso extensivo em `frontend/styles/dashboard.css`)

**Storage**: N/A — nenhuma mudança de dado, schema ou chamada de API (confirmado por FR-007 do spec)

**Testing**: Verificação manual com DevTools do navegador (inspeção de caixa/computed style, `getBoundingClientRect()`, e teste de clique em pontos específicos) — o projeto não tem framework de teste de UI automatizado (sem Jest/Vitest/Playwright em `frontend/package.json`), mesma limitação já registrada em specs 020 e na versão anterior desta 021

**Target Platform**: Navegador web, incluindo touch (mobile/tablet) — spec Edge Cases exige que a divisão estática do espaço se adapte a layouts responsivos (quebra de linha em mobile) e valha para toque, não só cursor de mouse

**Project Type**: Web application (frontend Next.js + backend ASP.NET Core já existentes) — esta feature toca somente `frontend/`

**Performance Goals**: N/A — melhoria/correção de interação, sem impacto de performance

**Constraints**: US1 MUST NOT alterar rotas de destino de nenhuma aba, MUST NOT introduzir cálculo de distância via JavaScript por clique (decisão de UX: divisão estática via layout/CSS, não dinâmica via posição do mouse — ver spec.md Assumptions), e MUST NOT alterar o comportamento da classe `.row-gap` compartilhada por 14 outras telas do frontend. US2 MUST NOT alterar o comportamento de abrir detalhe de registro ao clicar em uma linha de dados real (FR-007).

**Scale/Scope**: 2 arquivos de frontend com correção confirmada (`frontend/app/(app)/financeiro/layout.tsx`, `frontend/app/(app)/financeiro/contas-a-pagar/page.tsx`) + ajuste em `frontend/styles/dashboard.css` — uma classe **nova** e escopada para o menu do Financeiro (US1, não reaproveitando `.row-gap`) e um ajuste pontual em `thead th`/`tbody td`/`tbody tr` (US2, dependendo da causa raiz confirmada na tarefa de investigação inicial)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Avaliação contra os 5 princípios ratificados em `.specify/memory/constitution.md` (v1.0.0):

| Princípio | Aplicável? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhuma exclusão de registro envolvida — é interação de navegação/clique no frontend |
| II. Validação de Negócio Única e Centralizada no Backend | Não | Não há regra de negócio nem validação de request envolvida — nenhuma chamada de API nova; a "regra" de qual aba abrir é puramente de apresentação (rota de navegação client-side), não uma regra de domínio |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Não | Não introduz nenhuma opção configurável pelo usuário — a área de clique ampliada não é uma configuração, é comportamento fixo de UI |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Não envolve autenticação, senha ou segredo |
| V. Documentação Retroativa como Registro Histórico Vinculante | Sim (parcial) | Nenhuma spec retroativa (001-019) descreve o comportamento de navegação do menu Financeiro nem da tabela de Contas a Pagar em nível de área de clique — não há conflito a resolver; esta feature também substitui/corrige a direção da versão anterior desta própria spec 021 (não uma spec retroativa 001-019, mas a mesma feature em progresso), registrado explicitamente no histórico de `spec.md` |

**Resultado**: PASS — nenhum princípio é violado por esta feature.

## Project Structure

### Documentation (this feature)

```text
specs/021-fix-hitbox-cliques/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

Sem diretório `contracts/`: esta feature não expõe nem altera nenhuma interface externa (API, schema, contrato de dados) — é uma melhoria/correção de área de clique em JSX/CSS já existente.

### Source Code (repository root)

```text
# Option 2: Web application (frontend Next.js + backend ASP.NET Core já existentes)
# Esta feature toca SOMENTE o frontend — nenhum arquivo de backend/ é alterado.

frontend/
├── app/(app)/
│   └── financeiro/
│       ├── layout.tsx              # US1: menu de abas — nova classe CSS escopada para
│       │                           # substituir o comportamento de `.row-gap` apenas aqui
│       └── contas-a-pagar/
│           └── page.tsx            # US2: tabela de registros — thead/tbody, onClick já
│                                   # escopado ao <tr> do tbody; correção provável só em CSS
└── styles/
    └── dashboard.css               # US1: nova classe (ex. `.financeiro-tabs`) com
                                     # `display:flex` + `flex:1 1 0` em cada aba, substituindo
                                     # `.row-gap` no menu do Financeiro (sem alterar `.row-gap`
                                     # em si, usada por 14 outros arquivos);
                                     # US2: ajuste pontual em `thead th`/`tbody td`/`tbody tr`
                                     # dependendo da causa raiz confirmada na tarefa de
                                     # investigação inicial
```

**Structure Decision**: Aplicação web já existente. Esta feature é restrita a 2 arquivos de página (`.tsx`) dentro de `frontend/app/(app)/financeiro/` e a ajustes em `frontend/styles/dashboard.css`. Para US1, a decisão de design é **não** modificar a classe `.row-gap` compartilhada (usada em `pagamentos`, `turmas`, `lembretes`, `dashboard`, `contas-a-receber`, `relatorios/financeiro`, `agenda`, `relatorios/pagamentos`, `alunos`, `aulas`, `materias` — 14 arquivos ao todo), e sim introduzir uma classe nova aplicada apenas ao container do menu de abas do Financeiro, para que a mudança de comportamento de clique fique isolada a essa tela.

## Complexity Tracking

> Sem violações de constituição a justificar (Constitution Check acima = PASS). Nenhuma
> complexidade adicional é introduzida — a solução de US1 é puramente CSS (flexbox), sem novo
> estado, dependência ou padrão de arquitetura.
