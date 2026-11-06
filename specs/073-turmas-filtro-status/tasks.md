# Tasks: Turmas — filtro de status (ativas/inativas)

**Input**: `specs/073-turmas-filtro-status/` (spec, plan, research, quickstart). **Ambiente**: `NODE_OPTIONS=--max-old-space-size=1536`; senha só por `SPI_TESTE_SENHA`; não encerrar processos que não subi; sem commit/push até o usuário pedir. Frontend apenas.

## Phase 1: Linha de base

- [X] T001 Anotar a linha de base: `git status --porcelain` limpo; `cd frontend`: `npx tsc --noEmit`, `npx eslint` (0 problemas); registrar as turmas atuais e a ordem (via Chrome ou `GET /api/turmas`: 4 turmas, 3 ativas e "Turma teste" inativa). Base: `GET /api/turmas` = 4 turmas (Matematica basica 1, Turma A, Turma B ativas; Turma teste inativa).

## Phase 2: User Story 1 — Filtro de status (P1) 🎯 MVP

- [X] T002 [US1] `frontend/app/(app)/turmas/page.tsx`: estado `status` (`"" | "Ativo" | "Inativo"`); `<select className="filter-select">` com "Status — todos", "Ativo", "Inativo" **antes** da busca por nome; `listarTurmas(token, { nome: nome || undefined, ativo: status === "" ? undefined : status === "Ativo" })`; `useEffect` dependendo também de `status`; botão `.filter-clear` ("Limpar filtros", ícone `x`) só quando `nome` ou `status` estiverem preenchidos, limpando os dois (mesmo padrão de `alunos/page.tsx`). Nada mais muda.
- [X] T003 [US1] `cd frontend`: `npx tsc --noEmit` e `npx eslint` (0 problemas).

## Phase 3: Validação e fechamento

- [X] T004 **Verificação dos "MUST NOT" FR-002, FR-004, FR-005**: (a) FR-002: ler o `git diff` de `turmas/page.tsx` e confirmar que não há `.filter`, `.sort`, `.reduce` nem contagem nova sobre as turmas (só o parâmetro `ativo` enviado ao servidor); (b) FR-004: `git status --porcelain -- frontend src tests database` com **só** `turmas/page.tsx` modificado, nenhum arquivo novo, `git diff -- frontend/package.json src/` vazio; (c) FR-005: sem filtro, as 4 turmas e a ordem iguais às do T001.
- [X] T005 Chrome com login real (quickstart §2), claro, escuro e 360 px, com capturas: os 9 passos do quickstart, incluindo a conferência da requisição (`ativo=true/false`) e 0 erros de console. **15 PASS, 0 FAIL.**
- [X] T006 Final: `npx tsc --noEmit`, `npx eslint`, `npm run build` (API parada; `.next/dev` removido antes).
- [X] T007 Fechamento: `docs/relatorios/<data>-turmas-filtro-status.md`; hook `revisao-codigo` do Trello (API parada); memória do projeto; **sem commit/push**.

## Dependências e ordem

T001 → T002 → T003 → T004/T005 → T006 → T007.

## Cobertura dos requisitos

| Requisito | Tarefas |
|---|---|
| FR-001 (seletor) | T002, T005 |
| FR-002 (**MUST NOT** filtrar no cliente) | **T004(a)**, T005 |
| FR-003 (combina com a busca; Limpar filtros) | T002, T005 |
| FR-004 (**MUST NOT** backend/banco/arquivo novo/dependência) | **T004(b)** |
| FR-005 (**MUST NOT** mudar sem filtro) | T001, **T004(c)** |
| SC-001 a SC-003 | T005, T004 |
