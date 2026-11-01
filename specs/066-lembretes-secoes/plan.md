# Implementation Plan: Aba Lembretes dividida em "Lembretes de aula" e "Lembretes de pagamento"

**Branch**: `066-lembretes-secoes` | **Date**: 2026-11-01 | **Spec**: [spec.md](spec.md)

## Summary

Mudança **só de apresentação** em um único arquivo: `frontend/app/(app)/lembretes/page.tsx`. Os indicadores, filtros, contagem, botão e tabela de lembretes de aula passam a ficar dentro de uma seção com título "Lembretes de aula" e uma frase explicativa; o bloco já existente de pagamento (`<LembretePagamentoSection />`) passa a ficar dentro de uma segunda seção com título "Lembretes de pagamento", separada da primeira por uma linha divisória. Nenhum estado, efeito, filtro, handler, chamada de API ou regra é alterado. Detalhes e alternativas em [research.md](research.md).

## Technical Context

**Language/Version**: TypeScript 5 / Next.js 16.3.4 / React 19.2.8 (App Router)

**Primary Dependencies**: nenhuma nova; estilos globais existentes (`section-title`, `section-sub`, tokens `--c-*` em `frontend/styles/dashboard.css`) e o componente `Icon`

**Storage**: N/A (sem banco)

**Testing**: `npx tsc --noEmit`, `npx eslint` nos arquivos tocados, `npm run verificar-contraste`, `npm run build` e validação visual no Chrome (claro e escuro) com comparação de capturas das demais telas; não há suíte de testes de frontend no projeto

**Target Platform**: navegador desktop (layout responsivo para celular está fora do escopo)

**Project Type**: aplicação web (somente `frontend/` é tocado)

**Performance Goals**: sem impacto (nenhuma chamada nova, mesmo número de elementos ± 2 títulos)

**Constraints**: nada em `src/`, `tests/` ou `database/`; nenhum arquivo novo de código; só estilos e componentes já existentes; sem lógica nova no frontend

**Scale/Scope**: 1 arquivo de código, ~15 linhas de JSX adicionadas

## Constitution Check

| Princípio | Avaliação |
|---|---|
| I. Exclusão lógica | ✅ Sem impacto (nenhum dado é tocado) |
| II. Validação única no backend | ✅ Nenhuma regra é criada nem duplicada no frontend (FR-008) |
| III. Sem capacidade configurável sem implementação | ✅ Nenhuma opção nova |
| IV. Segredos | ✅ Sem impacto |
| V. Documentação como registro vinculante | ✅ O comportamento não muda, então nenhuma spec anterior precisa de nota; a 060 (que criou a aba) continua descrevendo o comportamento vigente. Registro em relatório ao final |

Sem violações. Complexity Tracking vazio.

## Project Structure

### Documentation (this feature)

```text
specs/066-lembretes-secoes/
├── spec.md
├── plan.md            # este arquivo
├── research.md        # decisões de apresentação
├── quickstart.md      # validação
├── checklists/requirements.md
└── tasks.md           # /speckit-tasks
```

(Sem `data-model.md` nem `contracts/`: a feature não tem entidade nem interface nova.)

### Source Code (repository root)

```text
frontend/app/(app)/lembretes/page.tsx   # ÚNICO arquivo de código alterado
docs/relatorios/2026-11-01-lembretes-secoes.md   # relatório (documentação)
```

**Structure Decision**: aplicação web existente; apenas o frontend. O `LembretePagamentoSection.tsx` **não é alterado** (já tem título e frase próprios, que passam a ser o subtítulo da segunda seção).

## Desenho da tela

```text
┌ Lembretes ────────────────────────────── (cabeçalho da página, como hoje)
│
│  ▣ Lembretes de aula                       ← section-title (com ícone)
│    E-mails enviados aos alunos e responsáveis antes de cada aula da turma. ← section-sub
│    [Pendentes] [Enviados] [Falharam] [Cancelados]
│    [filtros ........................................]
│    N lembrete(s) encontrado(s)            [+ Novo lembrete]
│    ┌ tabela ────────────────────────────┐
│    └────────────────────────────────────┘
│  ───────────────────────────────────────── (linha divisória, margem superior)
│  ▣ Lembretes de pagamento                  ← section-title (com ícone)
│    E-mail semanal para alunos e responsáveis com pagamentos vencidos. ← section-sub
│    ┌ Pagamento pendente (bloco atual, intacto) ┐
└────────────────────────────────────────────────
```

## Verificações "MUST NOT" que o /speckit-tasks deve cobrir (regra do CLAUDE.md)

| Requisito | Verificação exigida |
|---|---|
| FR-005 não mudar comportamento / FR-008 sem lógica nova | **Leitura direta do diff** de `page.tsx`: só JSX de títulos e wrappers; estados, efeitos, filtros, handlers e chamadas de API intactos (nenhuma linha removida de lógica) |
| FR-006 não tocar backend/banco/API | `git status --porcelain -- src tests database` vazio e `git diff --stat -- src tests database` vazio |
| FR-007 sem arquivo novo | `git status --porcelain -- frontend` mostra só `page.tsx` modificado, nenhum `??` |
| SC-002 demais telas idênticas | comparação de capturas antes/depois; só `/lembretes` difere |
