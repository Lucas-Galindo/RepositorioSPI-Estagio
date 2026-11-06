# Research: Turmas — filtro de status (spec 073)

## R1 — O servidor já filtra

**Decisão**: usar `listarTurmas(token, { nome, ativo })`; o parâmetro `ativo` já existe em `GET /api/turmas` (`TurmasController.Listar` → `ListarAsync(nome, ativo)`) e no cliente. Nenhuma mudança no backend (FR-002, FR-004).

## R2 — Padrão visual e de estado

**Decisão**: repetir o padrão da lista de Alunos em `frontend/app/(app)/turmas/page.tsx`: estado `status` (`"" | "Ativo" | "Inativo"`), `<select className="filter-select">` com "Status — todos / Ativo / Inativo" antes da busca por nome, `ativo: status === "" ? undefined : status === "Ativo"` na chamada, e o botão `.filter-clear` ("Limpar filtros") só quando há filtro. O `useEffect` passa a depender também de `status`.

## R3 — Sem arquivo novo, sem CSS novo

As classes `filter-select` e `filter-clear` já existem (usadas em Alunos). Só `turmas/page.tsx` muda.

## R4 — Validação

Chrome com login real (o banco local tem 4 turmas: 3 ativas e a "Turma teste" inativa), sem simular nada: todos (4), Ativo (3), Inativo (1), combinação com a busca, Limpar filtros, sem resultado, clique no card, claro/escuro e 360 px; conferência da requisição enviada (`ativo=true/false`).

## R5 — Riscos

| Risco | Mitigação |
|---|---|
| Filtrar no cliente por engano | leitura do diff (nenhum `.filter` sobre as turmas) e conferência da requisição |
| Mudar a lista sem filtro | comparar as 4 turmas e a ordem antes/depois |
