# Tasks: Aulas — filtros de data, horário, matéria e status

**Input**: `specs/074-aulas-filtros/` (spec, plan, research, contracts/aulas.md, quickstart). **Ambiente**: `dotnet test` **sem** `--no-build`; `NODE_OPTIONS=--max-old-space-size=1536`; senha só por `SPI_TESTE_SENHA`; não encerrar processos que não subi; sem commit/push até o usuário pedir.

## Phase 1: Linha de base

- [X] T001 Linha de base: `git status --porcelain` limpo; `dotnet test tests/SPI.Application.Tests` (698 aprovados); `tsc` e `eslint` limpos; com a API local no ar, gravar no scratchpad `GET /api/aulas` **sem parâmetros**, e com `dataInicio`/`dataFim` de um mês, e a contagem de aulas por matéria e por horário de início (para conferir os filtros à mão). Base: 11 aulas (Matemática 8, "teste" 3; inícios 10:00×5, 12:00×1, 15:00×1, 16:00×4), lida da API do usuário (7002, código antigo); 698 testes.

## Phase 2: Backend (US1)

- [X] T002 [US1] `src/SPI.Domain/Repositories/IAulaRepository.cs` e `src/SPI.Infrastructure/Repositories/AulaRepository.cs`: `ListarAsync(..., CancellationToken cancellationToken = default, int? materiaId = null, TimeOnly? horaInicioDe = null, TimeOnly? horaInicioAte = null)`; `Where` condicionais `a.MateriaId == materiaId`, `a.HoraInicio >= horaInicioDe`, `a.HoraInicio <= horaInicioAte` (limites inclusos); ordenação inalterada.
- [X] T003 [P] [US1] Atualizar a assinatura de `ListarAsync` nos 5 fakes de `IAulaRepository`: `Aulas/AulaServiceCadastroFakes.cs`, `Aulas/AulaServiceFakes.cs`, `Notificacoes/NotificacaoFakes.cs`, `Relatorios/RelatorioServiceHistoricoAlunoTests.cs`, `VinculosCobranca/VinculoCobrancaFakes.cs`; no de `AulaServiceCadastroFakes.cs`, guardar os três valores recebidos em propriedade pública (`UltimosFiltrosExtras`).
- [X] T004 [US1] `src/SPI.Application/Aulas/Services/IAulaService.cs` e `AulaService.cs`: os mesmos três parâmetros opcionais no fim, repassados ao repositório.
- [X] T005 [US1] `src/SPI.Api/Controllers/AulasController.cs`: `[FromQuery] int? materiaId`, `[FromQuery] TimeOnly? horaInicioDe`, `[FromQuery] TimeOnly? horaInicioAte` e comentários XML; repassar ao serviço.
- [X] T006 [P] [US1] `tests/SPI.Application.Tests/Aulas/AulaServiceListarFiltrosTests.cs` (novo): com o cenário de cadastro, `ListarAsync(null, null, null, null, null, materiaId: 3, horaInicioDe: 09:00, horaInicioAte: 12:00)` chega ao repositório com esses valores; sem eles chegam nulos.
- [X] T007 [US1] `dotnet test tests/SPI.Application.Tests` (sem `--no-build`) até passar tudo (698 + novos). 701 aprovados (698 + 3 novos). Como a API do usuário (porta 7002) travava as DLLs de `bin`, os testes e a API nova rodaram com `--artifacts-path` em pasta do scratchpad.

## Phase 3: Frontend (US1)

- [X] T008 [US1] `frontend/lib/api/aulas.ts`: no tipo dos filtros de `listarAulas`, acrescentar `materiaId?: number; horaInicioDe?: string; horaInicioAte?: string`.
- [X] T009 [US1] `frontend/app/(app)/aulas/page.tsx`: estados `dataDe`, `dataAte` (`DateInput`), `horaDe`, `horaAte` (`TimeInput`, classe `filter-input filter-date`), `materiaId` (select "Matéria — todas" com `listarMaterias`), mantendo Status e Turma; enviar `dataInicio: dataDe || undefined`, `dataFim: dataAte || undefined`, `materiaId`, `horaInicioDe`, `horaInicioAte` (vazios = `undefined`); rótulos em texto `count-text` ("Data" … "a", "Horário" … "a"); botão `.filter-clear` "Limpar filtros" quando algum filtro estiver preenchido; `useEffect` dependendo de todos. **Nenhum** filtro/ordenação/contagem no frontend; sem CSS novo.
- [X] T010 [US1] `cd frontend`: `npx tsc --noEmit` e `npx eslint` (0 problemas).

## Phase 4: Acabamento e validação

- [X] T011 **Verificação dos "MUST NOT" FR-003 a FR-006**: (a) FR-003: ler o `git diff` de `aulas/page.tsx` e `aulas.ts` e confirmar que não há `.filter`, `.sort`, `.reduce` nem contagem nova sobre as aulas; (b) FR-004: `git diff` dos chamadores (`RelatorioService.cs`, `agenda`, `dashboard` etc.) vazio e `GET /api/aulas` sem parâmetros **idêntico** ao do T001; (c) FR-005: `git status --porcelain -- database frontend` sem migração e **só** `aulas/page.tsx` e `lib/api/aulas.ts` no frontend, nenhum arquivo não rastreado novo no frontend, `git diff -- frontend/package.json` vazio e nenhum `.csproj` alterado por esta spec; (d) FR-006: `grep -n 'type="date"\|type="time"' frontend/app/(app)/aulas/page.tsx` sem resultado.
- [X] T012 API real (quickstart §2): os 6 casos, conferindo cada resultado contra a lista completa do T001 (matéria, horário, datas, interseção, De > Até vazio, horário inválido 400). **11 PASS, 0 FAIL** (JSON sem parâmetros idêntico à base, matéria, horário inclusivo, combinação, De > Até vazio, horário inválido 400, ordem).
- [X] T013 Chrome com login real (quickstart §3), claro, escuro e 360 px, com capturas: os 10 passos, conferindo também a requisição enviada e 0 erros de console. **22 PASS, 0 FAIL** pelo Next do usuário (porta 3000), com as chamadas `/api` redirecionadas pelo Playwright para a API nova (5099).
- [X] T014 Final: `dotnet test` (sem `--no-build`), `tsc`, `eslint`, `npm run build` (API parada; `.next/dev` removido antes). **`npm run build` NÃO foi rodado**: havia `next dev` do usuário em execução (porta 3000) e o build mexe na pasta `.next` dele; ficaram `tsc` e `eslint` limpos e a tela validada no Next em desenvolvimento.
- [X] T015 Fechamento: `docs/relatorios/<data>-aulas-filtros.md`; hook `revisao-codigo` do Trello (API parada); memória do projeto; **sem commit/push**.

## Dependências e ordem

T001 → T002 → T003 → T004 → T005 → T006 → T007; T008–T010 (após T005); T011–T013 só com tudo pronto → T014 → T015.

## Cobertura dos requisitos

| Requisito | Tarefas |
|---|---|
| FR-001 (filtros e Limpar filtros) | T009, T013 |
| FR-002 (parâmetros no servidor) | T002, T004, T005, T006, T012 |
| FR-003 (**MUST NOT** filtrar no cliente) | **T011(a)**, T013 |
| FR-004 (**MUST NOT** mudar os consumidores) | **T011(b)**, T001, T014 |
| FR-005 (**MUST NOT** migração/dependência/arquivo novo) | **T011(c)** |
| FR-006 (**MUST NOT** campo nativo) | **T011(d)** |
| SC-001 a SC-003 | T012, T013, T011 |
