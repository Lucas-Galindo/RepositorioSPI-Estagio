# Tasks: Turmas — filtro de matéria

**Input**: `specs/076-turmas-filtro-materia/` (spec, plan, research, contracts/turmas.md, quickstart). **Ambiente**: `dotnet test` **sem** `--no-build` (com `--artifacts-path` se a API do usuário travar as DLLs); `NODE_OPTIONS=--max-old-space-size=1536`; senha só por `SPI_TESTE_SENHA`; **não encerrar nem buildar por cima de processos do usuário**; credencial do banco nunca impressa; sem commit/push até o usuário pedir.

## Phase 1: Linha de base

- [X] T001 Linha de base: `git status --porcelain` limpo; `dotnet test` (712 aprovados); `tsc` e `eslint` limpos; com uma API no ar, gravar no scratchpad `GET /api/turmas` **sem parâmetros** (4 turmas, 1 com Matemática) e o dump de `turma` (id, nome, ativo, materia_id); contagens das tabelas. Base: `GET /api/turmas` = 4 turmas (15, 4, 5 sem matéria; 3 com Matemática), salva no scratchpad; 712 testes.

## Phase 2: Backend (US1)

- [X] T002 [US1] `src/SPI.Domain/Repositories/ITurmaRepository.cs` e `src/SPI.Infrastructure/Repositories/TurmaRepository.cs`: `ListarAsync(string? nome, bool? ativo, CancellationToken cancellationToken = default, int? materiaId = null, bool? semMateria = null)`; `Where(t => t.MateriaId == materiaId)` quando `materiaId.HasValue` e `Where(t => t.MateriaId == null)` quando `semMateria == true`; ordenação por nome e `Include` inalterados.
- [X] T003 [P] [US1] Atualizar a assinatura de `ListarAsync` nos 5 fakes de `ITurmaRepository`: `Aulas/AulaServiceCadastroFakes.cs`, `Aulas/AulaServiceFakes.cs`, `Lembretes/LembreteServiceAtualizarTests.cs`, `Lembretes/LembreteServiceExcluirTests.cs`, `VinculosCobranca/VinculoCobrancaFakes.cs`; no `FakeTurmaRepositoryParaVinculo`, guardar `(materiaId, semMateria)` recebidos em propriedade pública (`UltimosFiltrosExtras`).
- [X] T004 [US1] `src/SPI.Application/Turmas/Services/ITurmaService.cs` e `TurmaService.cs`: os dois parâmetros opcionais no fim, repassados ao repositório.
- [X] T005 [US1] `src/SPI.Api/Controllers/TurmasController.cs`: `[FromQuery] int? materiaId` e `[FromQuery] bool? semMateria`; `materiaId` + `semMateria == true` → `BadRequest` com mensagem clara; `[ProducesResponseType(StatusCodes.Status400BadRequest)]`; comentários XML.
- [X] T006 [P] [US1] `tests/SPI.Application.Tests/Turmas/TurmaServiceListarFiltrosTests.cs` (novo): `ListarAsync(null, null, materiaId: 3)` e `ListarAsync(null, null, semMateria: true)` chegam ao repositório; sem eles chegam nulos.
- [X] T007 [US1] `dotnet test` (sem `--no-build`) até passar tudo (712 + novos). 715 aprovados (712 + 3 novos).

## Phase 3: Frontend (US1)

- [X] T008 [P] [US1] `frontend/lib/api/turmas.ts`: no tipo dos filtros de `listarTurmas`, acrescentar `materiaId?: number; semMateria?: boolean`.
- [X] T009 [US1] `frontend/app/(app)/turmas/page.tsx`: estado `materia` (`""` todas, `"sem"` sem matéria ou o id), `listarMaterias(token)` (sem filtro de `ativo`) no carregamento; `<select className="filter-select">` ("Matéria — todas", "Sem matéria" e as matérias) **depois** do seletor de status; enviar `materiaId` (id numérico) ou `semMateria: true`; `semFiltros` e "Limpar filtros" consideram `materia`; `useEffect` dependendo de `materia`. **Nenhum** filtro/ordenação/contagem no frontend; sem CSS novo.
- [X] T010 [US1] `cd frontend`: `npx tsc --noEmit` e `npx eslint` (0 problemas).

## Phase 4: Acabamento e validação

- [X] T011 [P] Nota na `specs/073-turmas-filtro-status/spec.md` (filtro de status das Turmas): "specs/076: a tela ganhou também o filtro de matéria (e "Sem matéria"); ver `specs/076-turmas-filtro-materia/`." (Princípio V).
- [X] T012 **Verificação dos "MUST NOT" FR-003, FR-004, FR-006**: (a) FR-003: ler o `git diff` de `turmas/page.tsx` e `turmas.ts` e confirmar que não há `.filter`, `.sort`, `.reduce` nem contagem nova sobre as turmas (só os parâmetros enviados ao servidor); (b) FR-004: `GET /api/turmas` sem parâmetros **idêntico** ao do T001 e nenhum chamador do repositório/serviço de turmas alterado (`git diff --name-only` só nos arquivos previstos); (c) FR-006: `git status --porcelain -- database frontend` sem migração e **só** `turmas/page.tsx` e `lib/api/turmas.ts` no frontend, nenhum arquivo não rastreado novo no frontend, `git diff -- frontend/package.json "src/**/*.csproj"` vazio.
- [X] T013 API real (quickstart §2), com dados de teste "TESTE-076" e **limpeza** no fim: os 7 casos, conferindo cada resultado contra a lista completa do T001. **13 PASS, 0 FAIL** (JSON sem parâmetros idêntico à base; materiaId; semMateria; combinações; inexistente = 200 vazio; ambos = 400; ordem; limpeza).
- [X] T014 Chrome com login real (quickstart §3), claro, escuro e 360 px, com capturas: os 10 passos, conferindo a requisição enviada e 0 erros de console. **19 PASS, 0 FAIL.**
- [X] T015 Final: `dotnet test`, `tsc`, `eslint` e, se o ambiente do usuário estiver parado, `npm run build` (senão registrar que não rodou). 715 testes, `tsc`, `eslint` e `npm run build` ok (ambiente do usuário parado).
- [X] T016 Fechamento: `docs/relatorios/<data>-turmas-filtro-materia.md`; hook `revisao-codigo` do Trello (se recusar por causa de DLLs travadas, mover à mão com comentário); memória do projeto; **sem commit/push**.

## Dependências e ordem

T001 → T002 → T003 → T004 → T005 → T006 → T007; T008–T010 (após T005); T012–T014 só com tudo pronto → T015 → T016.

## Cobertura dos requisitos

| Requisito | Tarefas |
|---|---|
| FR-001 (parâmetros no servidor) | T002, T004, T005, T006, T013 |
| FR-002 (seletor e Limpar filtros) | T009, T014 |
| FR-003 (**MUST NOT** filtrar no cliente) | **T012(a)**, T014 |
| FR-004 (**MUST NOT** mudar os consumidores) | **T012(b)**, T001, T015 |
| FR-005 (combinação inválida = 400) | T005, T013 |
| FR-006 (**MUST NOT** migração/dependência/arquivo novo) | **T012(c)** |
| SC-001 a SC-003 | T013, T014, T012 |
