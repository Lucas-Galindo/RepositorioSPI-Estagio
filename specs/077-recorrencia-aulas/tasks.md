# Tasks: Recorrência na criação de aulas

**Input**: `specs/077-recorrencia-aulas/` (spec, plan, research, data-model, contracts/serie-aulas.md, quickstart). **Ambiente**: `dotnet test` **sem** `--no-build` (com `--artifacts-path` se a API do usuário travar as DLLs); `NODE_OPTIONS=--max-old-space-size=1536`; senha só por `SPI_TESTE_SENHA`; credencial do banco nunca impressa; **não encerrar nem buildar por cima de processos do usuário**; sem commit/push até o usuário pedir.

## Phase 1: Linha de base

- [X] T001 Linha de base: `git status --porcelain` limpo; `dotnet test` (715 aprovados); `tsc` e `eslint` limpos; com uma API no ar, gravar no scratchpad `GET /api/aulas` completo, a contagem de linhas de todas as tabelas e `ra_controle`; anotar um corpo de `POST /api/aulas` (aula única) e a resposta de uma aula criada e removida (para o T015).

## Phase 2: Backend (US1, US2)

- [X] T002 [US1] DTOs em `src/SPI.Application/Aulas/Dtos/`: `RecorrenciaAulaRequest` (descricao, materiaId, turmaId, alunoId, dataInicial, horarios, quantidadeAulas, dataFinal), `HorarioSerieRequest` (diaSemana 0–6, horaInicio, horaFim), `PreviaSerieResponse` (total, limite, conflitos, podeCriar, aulas) com `OcorrenciaSerieResponse` (data, diaSemana, horaInicio, horaFim, conflito) e `SerieCriadaResponse` (quantidade, aulas: `AulaResponse`).
- [X] T003 [US1] `src/SPI.Application/Aulas/Services/GeradorSerieAulas.cs`: função **estática e pura** `Gerar(DateOnly dataInicial, IReadOnlyList<HorarioSerieRequest> horarios, int? quantidade, DateOnly? dataFinal, int limite)` → `ResultadoGeracao` (ocorrências em ordem + `Excedeu` + `Total` quando passar de `limite`); percorre dia a dia, ocorrências do dia ordenadas pelo início, para na quantidade ou na data final (inclusive) e **interrompe ao passar do limite**.
- [X] T004 [P] [US1] `tests/SPI.Application.Tests/Aulas/GeradorSerieAulasTests.cs` (novo): um dia por semana; dois dias; data inicial fora dos dias (começa no próximo dia válido); término por quantidade e por data final (inclusive); dois horários no mesmo dia; virada de mês e de ano; domingo e sábado; limite (60 aceita, 61 excede) e `dataFinal` muito distante excede sem varrer tudo.
- [X] T005 [US2] `src/SPI.Application/Aulas/Validators/RecorrenciaAulaRequestValidator.cs` (FluentValidation, estilo do `AulaRequestValidator`): `materiaId > 0`; turma XOR aluno (mesma regra da aula única); `horarios` não vazio; `diaSemana` 0–6; `horaFim > horaInicio`; **sem sobreposição de horários no mesmo dia da semana**; **exatamente um** entre `quantidadeAulas` (1–60) e `dataFinal` (≥ `dataInicial`); e `tests/SPI.Application.Tests/Aulas/RecorrenciaAulaRequestValidatorTests.cs` (novo) cobrindo cada regra.
- [X] T006 [US1][US2] `src/SPI.Application/Aulas/Services/IAulaService.cs` e `AulaService.cs`: `PreVisualizarSerieAsync(professorId, request)` e `CadastrarSerieAsync(professorId, request)`, **sem alterar** `CadastrarAsync`/`AtualizarAsync`: (a) `dataInicial >= hoje` (`_relogio.AgoraLocal()`, só a data) senão `ConflitoException`; (b) gerar com o limite 60 e, se excedeu, `ConflitoException` com o total; (c) validar referências (matéria/turma/aluno) com o `ValidarReferenciasAsync` existente; (d) marcar `conflito` por ocorrência com `ExisteConflitoAsync`; (e) prévia devolve tudo e `podeCriar`; (f) criar: se houver conflito, `ConflitoException` com até 10 datas ("dd/MM/aaaa HH:mm") e "e mais N" e **nada** gravado; senão montar todas as `Aula` ("Agendada", `Ativo`) **com `AulaAlunos` já preenchidos** (alunos ativos da turma ou o aluno), `AdicionarAsync` em cada uma, **um único** `SalvarAlteracoesAsync`, recalcular o lembrete da turma **uma vez** e devolver `SerieCriadaResponse`.
- [X] T007 [US1][US2] `src/SPI.Api/Controllers/AulasController.cs`: `POST serie/previa` (200) e `POST serie` (201), com o validador (400 com a lista de mensagens, como o cadastro) e `[ProducesResponseType]` de **400, 404 e 409** declarados (ação nova sem declarar vira 500); comentários XML.
- [X] T008 [P] [US1][US2] `tests/SPI.Application.Tests/Aulas/AulaServiceSerieTests.cs` (novo, com os fakes do cenário de cadastro): cria N aulas "Agendadas" com os alunos; **conflito bloqueia tudo e não grava nenhuma**; passado recusado; mais de 60 recusado; matéria/turma/aluno inexistentes; lembrete recalculado **uma vez**; a prévia marca o conflito e `podeCriar = false`; **cadastro de aula única inalterado** (mesmo resultado de antes).
- [X] T009 `dotnet test` (sem `--no-build`) até passar tudo (715 + novos).

## Phase 3: Frontend (US1, US2)

- [X] T010 [P] [US1] `frontend/lib/api/aulas.ts`: tipos `HorarioSerie`, `SerieAulasRequest`, `OcorrenciaSerie`, `PreviaSerie`, `SerieCriada` e as funções `previaSerieAulas` e `cadastrarSerieAulas` (POST nos dois endpoints).
- [X] T011 [US1] `frontend/components/aulas/RecorrenciaAulas.tsx` (novo): data inicial (`DateInput` com `min` = hoje), linhas de dia da semana (select "Domingo"… "Sábado") + início + fim (`TimeInput`) com "Adicionar dia" e remover, término (quantidade OU data final), botão "Ver prévia" e a lista devolvida (data, dia, horário, selo de conflito, total). **Só** estado dos campos e exibição: sem calcular datas, conflitos nem contagens.
- [X] T012 [US1] `frontend/components/aulas/AulaForm.tsx`: caixa "Repetir esta aula" **só quando `!editando`**; marcada, mostra o `RecorrenciaAulas` no lugar da seção "Data e horário" e o envio chama `cadastrarSerieAulas`; sucesso: toast "N aulas agendadas", `onSalvo(primeira aula)` no pop-up ou `router.push("/aulas")`; erro 409/400 mostra a mensagem. Sem a opção marcada, **nada muda**.
- [X] T013 `cd frontend`: `npx tsc --noEmit` e `npx eslint` (0 problemas).

## Phase 4: Acabamento e validação

- [X] T014 [P] Nota na `specs/064-chamada-so-nos-detalhes/spec.md` (regras de aula criada "Agendada"): "specs/077: o cadastro de nova aula ganhou a opção Repetir (série semanal, até 60 aulas, de hoje em diante); cada aula criada segue estas regras; ver `specs/077-recorrencia-aulas/`." (Princípio V).
- [X] T015 **Verificação dos "MUST NOT" FR-001 (edição), FR-009, FR-010, FR-011**: (a) FR-009: `git diff -- src/SPI.Application/Aulas/Services/AulaService.cs` mostrando só **acréscimos** (nenhuma linha de `CadastrarAsync`/`AtualizarAsync` alterada) e o `POST /api/aulas` e `PUT` iguais aos do T001; (b) FR-001: ler o diff do `AulaForm.tsx` e o Chrome confirmando que "Repetir" **não** aparece na edição; (c) FR-010: ler o diff de `RecorrenciaAulas.tsx`/`AulaForm.tsx` e `grep` de `new Date(`, `getDay`, `setDate`, `.filter(`/`.sort(` sobre datas: nenhum cálculo de datas, conflitos ou contagem no frontend; (d) FR-011: `git status --porcelain -- database` sem nada, `git diff -- frontend/package.json "src/**/*.csproj"` vazio.
- [X] T016 API real (quickstart §2), com dados "TESTE-077" e **limpeza** no fim: os 10 casos, conferindo a contagem de aulas antes/depois de cada tentativa.
- [X] T017 Chrome com login real (quickstart §3), claro, escuro e 360 px, com capturas: os 10 passos e 0 erros de console.
- [X] T018 Final: `dotnet test`, `tsc`, `eslint` e, se o ambiente do usuário estiver parado, `npm run build`.
- [X] T019 Fechamento: `docs/relatorios/<data>-recorrencia-aulas.md`; texto completo e link da spec no card do Trello; hook `revisao-codigo` (se recusar por DLLs travadas, mover à mão com comentário); memória do projeto; **sem commit/push**.

## Dependências e ordem

T001 → T002 → T003 → T004 → T005 → T006 → T007 → T008 → T009; T010–T013 (após T007); T015–T017 só com tudo pronto → T018 → T019.

## Cobertura dos requisitos

| Requisito | Tarefas |
|---|---|
| FR-001 (opção em nova aula, não na edição) | T012, **T015(b)**, T017 |
| FR-002 (dias+horários, término exclusivo) | T002, T005, T011 |
| FR-003 (datas no servidor, prévia) | T003, T006, T007, T016 |
| FR-004 (hoje em diante, máx. 60) | T003, T005, T006, T008, T016 |
| FR-005 (conflito bloqueia tudo) | T006, T008, T016 |
| FR-006 (atômica) | T006, T008, T016 |
| FR-007 (aulas comuns, alunos, lembrete) | T006, T008, T016 |
| FR-008 (referências) | T006, T008, T016 |
| FR-009 (**MUST NOT** mudar a aula única) | T008, **T015(a)** |
| FR-010 (**MUST NOT** calcular no frontend) | **T015(c)** |
| FR-011 (**MUST NOT** banco/dependência) | **T015(d)** |
| SC-001 a SC-005 | T016, T017 |
