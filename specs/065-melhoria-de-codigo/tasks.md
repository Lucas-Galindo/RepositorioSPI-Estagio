# Tasks: Melhoria do CÃ³digo sem Mudar o Comportamento

**Input**: `specs/065-melhoria-de-codigo/` (spec.md, plan.md, research.md, data-model.md, contracts/, quickstart.md)
**Prerequisites**: nenhum. Esta spec **nÃ£o altera o banco** e deve ser implementada **antes** da 064.

**Formato**: `- [ ] TNNN [P] [USn] descriÃ§Ã£o com caminho`. `[P]` = pode rodar em paralelo. Regras do `CLAUDE.md`: cada "MUST NOT" tem tarefa de verificaÃ§Ã£o (leitura do diff, `grep` ou `git status`). Em toda tarefa de cÃ³digo: **build e testes verdes e instantÃ¢neo da API idÃªntico** antes de seguir.

**Zona protegida (FR-010, spec 064)**: nÃ£o tocar em `AulaService.CadastrarAsync`/`AplicarPresencasAsync`/`RegistrarSessaoAsync`, `AulaRequest.Presencas`, `PresencaChecklist.tsx`, `Notificacoes:AulasPendentesDesde` nem na leitura de `DateTime.Now`.

## Phase 1: Rede de seguranÃ§a (antes de qualquer mudanÃ§a)

- [X] T001 Rodar `database/backup/backup-database.ps1`; guardar a linha de base: `dotnet build` (0 erros e a contagem de avisos), `dotnet test` (464 aprovados), `npx tsc --noEmit` e `npx eslint` limpos; medir e anotar "antes": nÂº de blocos `catch` nos controllers (`grep -c "catch ("`), linhas de `src/SPI.Api/Controllers/*.cs`, classes CSS sem uso (18) e nÂº de mÃ©todos sem comentÃ¡rio da lista (research.md R6).
- [X] T002 [P] Criar (fora do repositÃ³rio, no scratchpad) o script do **instantÃ¢neo da API** conforme research.md R3 e rodar o "antes" com dados sintÃ©ticos "TESTE-065": listagens de todos os recursos; `GET` de id inexistente em cada controller (404); criaÃ§Ãµes invÃ¡lidas (400); conflitos (409: matrÃ­cula direta em turma de pacote, vÃ­nculo duplicado, vincular aluno repetido); login invÃ¡lido (401); e o erro inesperado conhecido (`POST /api/alunos` com e-mail â†’ 500). Normalizar `traceId`, datas de criaÃ§Ã£o e ids gerados; comparar o **status** de forma exata.
- [X] T003 [P] Criar (scratchpad) o script de **capturas de tela** conforme research.md R4 (Chrome headless, login de teste, temas claro e escuro, telas listadas) que grava os hashes dos PNG, e rodar o "antes".

**Checkpoint**: linha de base, instantÃ¢neo e capturas "antes" guardados.

## Phase 2: User Story 1 â€” Remover cÃ³digo morto (P1)

**Teste independente**: build, testes e `tsc` verdes; capturas idÃªnticas; `grep` das classes e funÃ§Ãµes removidas sem resultado.

- [X] T004 [US1] `frontend/styles/dashboard.css` e `frontend/styles/login.css`: remover as 18 classes sem uso (research.md R1). **Antes de cada uma**, `grep` do nome em todo o frontend, inclusive texto montado (`` `â€¦${x}` ``) e `.mjs`; sÃ³ sai a que nÃ£o tem nenhum uso (FR-003). Conferir as capturas depois.
- [X] T005 [P] [US1] `frontend/lib/jwt.ts` (`decodeJwtPayload`) e `frontend/lib/paleta.ts` (`amostraMaisProxima`; `misturar` e `CONTRASTE_MINIMO` ficam porque o script de contraste os usa): conferir primeiro se `frontend/scripts/verificar-contraste.mjs` os importa (o `grep` anterior sÃ³ olhou `.ts/.tsx`). Quem for usado sÃ³ no prÃ³prio arquivo deixa de ser exportado; quem nÃ£o for usado em lugar nenhum Ã© removido.
- [X] T006 [P] [US1] Corrigir o comentÃ¡rio de `tests/SPI.Application.Tests/Aulas/AulaServiceRegistrarSessaoPacoteRenovacaoTests.cs` (aponta para `MontarPacotesEmAtencao`; o mÃ©todo atual Ã© `MontarResumoPacotes`) e tentar remover o `PackageReference Microsoft.Extensions.Logging.Abstractions` de `src/SPI.Application/SPI.Application.csproj` (aviso `NU1510`); se o build ou os testes quebrarem, desfazer e registrar.

**Checkpoint**: US1 concluÃ­da; build, testes, `tsc`, `eslint` e `npm run verificar-contraste` verdes.

## Phase 3: User Story 2 â€” Centralizar o tratamento de erro dos controllers (P1)

**Teste independente**: instantÃ¢neo da API idÃªntico e `ErrosDeNegocioFilterTests` verdes.

- [X] T007 [US2] Criar `src/SPI.Api/Filters/ErrosDeNegocioFilter.cs` (`IExceptionFilter`) conforme contracts/erros-da-api.md e research.md R2; registrar em `src/SPI.Api/Program.cs` (`AddControllers(o => o.Filters.Add<ErrosDeNegocioFilter>())`). Em `tests/SPI.Application.Tests/SPI.Application.Tests.csproj` acrescentar `ProjectReference` para `SPI.Api` e criar `tests/SPI.Application.Tests/Api/ErrosDeNegocioFilterTests.cs`: `NaoEncontrado` com 404 declarado â†’ 404 com a mensagem; `Conflito` com 409 declarado â†’ 409; `Conflito` **sem** 409 declarado â†’ 500; `NaoEncontrado` sem 404 declarado â†’ 500; exceÃ§Ã£o qualquer â†’ 500 `ProblemDetails` com tÃ­tulo "Erro inesperado", detalhe e status 500 (igual ao que `ControllerBase.Problem` monta); o filtro marca a exceÃ§Ã£o como tratada.
- [X] T008 [P] [US2] Acrescentar `[ProducesResponseType(StatusCodes.Status409Conflict)]` em `LembretesController.Atualizar`, `TurmasController.Cadastrar` e `TurmasController.Atualizar` (jÃ¡ respondem 409; falta a declaraÃ§Ã£o de que o filtro depende).
- [X] T009 [US2] Script de transformaÃ§Ã£o dos controllers (scratchpad): em cada aÃ§Ã£o remover `try { â€¦ }` e os `catch` **padronizados** (`NaoEncontradoException`â†’`NotFound(e.Message)`, `ConflitoException`â†’`Conflict(e.Message)`, `Exception`â†’`Problem(â€¦ "Erro inesperado" â€¦)`, nas formas de uma e de vÃ¡rias linhas), dedentando o corpo; **manter** `try/catch` sÃ³ com os `catch` especiais (`CredenciaisInvalidasException`, `RefreshTokenInvalidoException` e o `ConflitoException`â†’`BadRequest` de `Auth/RedefinirSenha`). Aplicar nos 17 arquivos de `src/SPI.Api/Controllers`.
- [X] T010 [US2] Antes e depois do T009, gerar a **tabela aÃ§Ã£o Ã— (declara 404, declara 409, capturava 404, capturava 409)** das 87 aÃ§Ãµes (como na investigaÃ§Ã£o) e conferir que o conjunto que o filtro passa a cobrir Ã© o mesmo de hoje (o instantÃ¢neo sÃ³ amostra as aÃ§Ãµes). Compilar; remover `using` que sobrar; **ler o diff de cada controller**; rodar `dotnet test`; repetir o **instantÃ¢neo da API** e comparar com o "antes" (SC-002); rodar o instantÃ¢neo de erros 404, 409, 400, 401 e 500.

**Checkpoint**: US2 concluÃ­da; respostas idÃªnticas; blocos `catch` repetidos = 0.

## Phase 4: User Story 3 â€” Simplificar lÃ³gica repetida (P2)

- [X] T011 [US3] `src/SPI.Application/VinculosCobranca/Services/VinculoCobrancaService.cs`: o trecho "pacote â†’ `ValidarTurmaDePacoteAsync`, avulsa â†’ `ValidarTurmaAsync`" aparece em `CadastrarAsync` e `AtualizarAsync`; extrair `ValidarTurmaDoVinculoAsync(alunoId, modalidade, turmaId, exigirNaoIniciada)` (que tambÃ©m devolve a turma de pacote, quando houver) e usar nos dois. Mensagens, ordem das validaÃ§Ãµes e testes inalterados.
- [X] T012 (decisao: nada extraido -- os validadores divergem de proposito em condicoes e ordem das regras; registrado no relatorio) [US3] `src/SPI.Application/Alunos` (`CadastrarAlunoRequest`/`AtualizarAlunoRequest` e os dois validadores): extrair a parte comum **somente** se nenhuma mensagem nem a ordem de validaÃ§Ã£o mudar (comparar com `CadastrarAlunoRequestValidatorTests`/`AtualizarAlunoRequestValidatorTests` e o instantÃ¢neo); se nÃ£o der sem mudar o comportamento, deixar como estÃ¡ e registrar no relatÃ³rio.
- [X] T013 [US3] `dotnet test` e instantÃ¢neo da API (cadastro e atualizaÃ§Ã£o de aluno e de vÃ­nculo, inclusive recusas) idÃªnticos ao "antes".

## Phase 5: User Story 4 â€” Comentar regras complexas e de negÃ³cio (P2)

**Regra**: comentÃ¡rio diz o porquÃª (regra, ordem, risco), sem acento, sem repetir o cÃ³digo; o diff da tarefa tem **sÃ³** linhas de comentÃ¡rio (FR-008).

- [X] T014 [P] [US4] Ler e comentar: `PagamentoService.RegistrarAsync`, `AlunoService.CadastrarAsync`, `FinanceiroService.ObterVisaoGeralAsync`, `RelatorioService.ObterIndicadoresFinanceirosAsync`, `LembretePagamentoService.TentarEnviarAsync` (incluir o risco de e-mail duplicado em falha parcial, achado L3 da revisÃ£o de cÃ³digo).
- [X] T015 [P] [US4] Ler e comentar: `AlunoRepository.CadastrarViaProcedureAsync`, `AulaRepository.ListarAsync` (sÃ³ o que **nÃ£o** Ã© da zona 064), `ContaPagarRepository.ListarAsync`, `PagamentoRepository.ListarAsync`, `RelatorioRepository.ListarPagosNoPeriodoAsync`, `LembreteDispatcherService.ProcessarLembretesVencidosAsync`, `MensalidadeLembreteDispatcherService.ProcessarLembretesVencimentoAsync`.
- [X] T016 [P] [US4] Comentar as regras das specs 056 e 063: `TurmaService.CadastrarAsync`/`AtualizarAsync` (datas imutÃ¡veis, tipo fixo), `VinculoCobrancaService.AtualizarAsync` e `GarantirRegrasDeCombinacaoAsync` (um pacote por grupo, mensalidade exclusiva), e as regras de `NotificacaoService` que ainda estiverem sem explicaÃ§Ã£o (fora do corte de vigÃªncia, que Ã© da 064).
- [X] T017 [US4] Verificar que as tarefas T014 a T016 sÃ³ mudaram comentÃ¡rio: script que remove as linhas de comentÃ¡rio de cada arquivo alterado, no estado "antes" (`git show HEAD:arquivo`) e "depois", e compara; deve dar **idÃªntico**. Rodar build (o compilado nÃ£o muda).

## Phase 6: User Story 5 â€” Reduzir telas copiadas (P3, opcional)

- ~~T018~~ **NAO FEITA** (opcional P3, fora desta entrega; vira card proprio, ver docs/relatorios/2026-10-31-melhoria-de-codigo.md). Original: [US5] Contas a pagar Ã— a receber (detalhes e ediÃ§Ã£o, `frontend/app/(app)/financeiro/**`): extrair componente(s) compartilhado(s) para os trechos repetidos **somente se** as capturas de tela de antes e depois ficarem idÃªnticas (FR-014). Qualquer trecho que nÃ£o passar fica como estÃ¡ e vira card prÃ³prio.
- ~~T019~~ **NAO FEITA** (opcional P3, fora desta entrega; vira card proprio, ver docs/relatorios/2026-10-31-melhoria-de-codigo.md). Original: [US5] RelatÃ³rios de alunos Ã— turmas (`frontend/app/(app)/relatorios`): mesma regra. Opcional: se sobrar fÃ´lego depois das outras histÃ³rias.

## Phase 7: Polish e verificaÃ§Ãµes transversais

- [X] T020 FR-010 ("MUST NOT" tocar a zona da 064): `git diff --stat` e `git diff` em `AulaService.cs`, `AulaRequest.cs`, `PresencaChecklist.tsx`, `NotificacoesOptions.cs` e `appsettings.json` (seÃ§Ã£o Notificacoes): nenhuma linha de cÃ³digo alterada nos pontos da zona; ler o diff do restante dos arquivos tocados.
- [X] T021 FR-001/FR-013 ("MUST NOT" mudar comportamento): `dotnet test` (mesmos 464 testes aprovados e nenhum `Assert` alterado nos arquivos de teste existentes, conferido por `git diff -- tests` sÃ³ com o comentÃ¡rio e o `csproj`); instantÃ¢neo da API final idÃªntico; capturas de tela finais idÃªnticas; nenhum arquivo de `database/` alterado (`git status --porcelain -- database`).
- [X] T022 FR-003/FR-009 ("MUST NOT" remover CSS em uso; comentÃ¡rio que repete o cÃ³digo): `grep` final dos nomes removidos sem resultado; revisÃ£o dos comentÃ¡rios novos.
- [X] T023 `git status --porcelain -- src tests frontend database`: arquivos novos esperados sÃ³ `src/SPI.Api/Filters/ErrosDeNegocioFilter.cs` e `tests/SPI.Application.Tests/Api/ErrosDeNegocioFilterTests.cs` (mais componentes sÃ³ se a US5 for feita). Qualquer outro Ã© achado.
- [X] T024 Rodar `dotnet build` (sem o aviso `NU1510`), `dotnet test`, `npx tsc --noEmit`, `npx eslint` nos arquivos tocados, `npm run verificar-contraste` e `npm run build` (com `experimental.cpus: 2` temporÃ¡rio se faltar memÃ³ria, restaurando depois).
- [X] T025 Medir o "depois" (blocos `catch` e linhas dos controllers, classes CSS, mÃ©todos comentados), escrever `docs/relatorios/<data>-melhoria-de-codigo.md` com antes e depois, o que **nÃ£o** foi feito e os cards sugeridos (aviso de seguranÃ§a do NuGet; registrar erros inesperados; `Concluidos`/`AulasDadasNoPeriodo`), e comentar no card do Trello. Apagar os dados "TESTE-065" e conferir que os dados do usuÃ¡rio seguem intactos.

## DependÃªncias e ordem

- Phase 1 bloqueia tudo (T001 antes de T002 e T003).
- US1 (T004 a T006) vem antes da US2; T004 depende das capturas "antes" (T003).
- US2: T007 e T008 antes da T009; T009 antes da T010.
- US3 (T011 a T013) depois da US2 (usa o mesmo instantÃ¢neo).
- US4 (T014 a T017): T014, T015 e T016 em paralelo (arquivos diferentes); T017 por Ãºltimo.
- US5 (T018, T019) sÃ³ depois das outras e sÃ³ se as capturas confirmarem.
- Polish (T020 a T025) no fim.

## EstratÃ©gia

MVP = Phase 1 + US1 + US2 (o maior ganho, com a prova de igualdade). US3 e US4 sÃ£o seguras e rÃ¡pidas; US5 Ã© opcional. Entregar por fases, com build, testes e instantÃ¢neo verdes a cada uma; commit sÃ³ quando o usuÃ¡rio pedir.
