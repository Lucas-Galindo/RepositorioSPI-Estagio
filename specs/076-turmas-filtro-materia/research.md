# Research: Turmas — filtro de matéria (spec 076)

## R1 — Parâmetros novos opcionais na mesma cadeia

**Decisão**: acrescentar `int? materiaId = null, bool? semMateria = null` **depois** do `CancellationToken` em `ITurmaRepository.ListarAsync`, `TurmaRepository.ListarAsync`, `ITurmaService.ListarAsync`/`TurmaService.ListarAsync` e como `[FromQuery]` em `TurmasController.Listar` (mesmo padrão das specs 071 e 074). Os demais chamadores não passam os parâmetros (valem `null`), então o resultado deles não muda (FR-004). Os **5 fakes** de `ITurmaRepository` em `tests/` ganham os dois parâmetros.

## R2 — Consulta

**Decisão**: em `TurmaRepository.ListarAsync`: `if (materiaId.HasValue) Where(t => t.MateriaId == materiaId)` e `if (semMateria == true) Where(t => t.MateriaId == null)`; ordenação por nome e `Include(Materia)` inalterados.

## R3 — Combinação inválida

**Decisão**: `materiaId` + `semMateria=true` juntos são recusados no **controller** (`TurmasController.Listar` devolve `BadRequest` com mensagem clara antes de chamar o serviço e declara `[ProducesResponseType(StatusCodes.Status400BadRequest)]`, como já faz `Cadastrar`). É uma regra entre dois parâmetros de consulta; o seletor da tela nunca envia os dois.

## R4 — Frontend

**Decisão**: em `turmas/page.tsx`: estado `materia` (`""` = todas, `"sem"` = sem matéria, ou o id), `listarMaterias(token)` (sem filtro de `ativo`) no carregamento, `<select className="filter-select">` com "Matéria — todas", "Sem matéria" e as matérias, **depois** do seletor de status; envio: `materiaId: /^\d+$/.test(materia) ? Number(materia) : undefined`, `semMateria: materia === "sem" ? true : undefined`; `semFiltros` e "Limpar filtros" passam a considerar `materia`. `lib/api/turmas.ts`: `listarTurmas` ganha `materiaId?: number; semMateria?: boolean` no tipo dos filtros. Sem CSS e sem arquivo novos.

## R5 — Testes

**Decisão**: `FakeTurmaRepositoryParaVinculo` guarda os últimos filtros extras; teste do `TurmaService.ListarAsync` confere o repasse (com e sem). O filtro SQL é provado na **API real** (banco local: 4 turmas; "Turma teste" com Matemática e 3 sem matéria) e, com dados de teste "TESTE-076", em mais de uma matéria.

## R6 — Riscos

| Risco | Mitigação |
|---|---|
| Mudar o resultado dos outros consumidores | parâmetros opcionais nulos; comparar `GET /api/turmas` sem parâmetros antes/depois |
| Quebrar fakes | 5 fakes atualizados; `dotnet test` sem `--no-build` |
| Ambiente do usuário ligado | `--artifacts-path` e `page.route`; não encerrar nem buildar por cima |
