---

description: "Task list for Progresso de Pacote de Aulas"
---

# Tasks: Progresso de Pacote de Aulas

**Input**: Design documents from `specs/058-progresso-pacote/`

**Tests**: incluídos (xUnit com fakes). Os "MUST NOT" FR-001 (falta/cancelada não contam) e FR-011 (sem campo ou tabela nova) têm verificação explícita, conforme o `CLAUDE.md`.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Foundational

- [X] T001 `src/SPI.Application/VinculosCobranca/Services/ProgressoPacote.cs` + `Dtos/ProgressoPacoteResponse.cs`: `Calcular(VinculoCobranca, int aulasDadas)` conforme research.md R2, usando `VinculoCobranca.LimiteAlertaPacote`.
- [X] T002 Repositórios:
  - `IAulaRepository`/`AulaRepository` ganham `ContarPresencasPorVinculosAsync(ids, de, ate)` → `Dictionary<int,int>` (GROUP BY) e `ListarPresencasDoVinculoAsync(vinculoId)`, que inclui `Aula.Materia`/`Aula.Turma` e ordena das mais recentes para as mais antigas;
  - `IVinculoCobrancaRepository`/`VinculoCobrancaRepository` ganham `ListarPacotesAsync(alunoId?)`, com pacotes ativos e inativos e o `Aluno`;
  - atualizar os fakes de teste que implementam essas interfaces.
- [X] T003 [P] Testes em `tests/SPI.Application.Tests/VinculosCobranca/ProgressoPacoteTests.cs`:
  - total informado;
  - total estimado (dadas + saldo);
  - sem total nem saldo → `SemTotal`;
  - restantes nunca negativos;
  - limites `Atencao` (1 e 2) e `Esgotado` (0);
  - tamanhos diferentes (3 de 4, 3 de 20).

## Phase 2: User Story 1 + 2 - Ficha do aluno e cadastro (P1) 🎯 MVP

- [X] T004 [US1] `VinculoCobrancaService`:
  - `ListarPorAlunoAsync` e `ObterPorIdAsync` preenchem `Progresso`, com contagem em lote;
  - remover o `AulasDadas` antigo (total − saldo);
  - `AtualizarAsync` valida o total contra as presenças contadas;
  - `CadastrarAsync`: Pacote com total e sem saldo ganha saldo = total;
  - novo `ListarAulasDoPacoteAsync(alunoId, id)`, com `NaoEncontradoException` para vínculo de outro aluno.
- [X] T005 [US1] `VinculosCobrancaController`: `GET {id}/aulas`.
- [X] T006 [P] [US1] Testes, atualizando `VinculoCobrancaServiceAuditoriaTests.cs` e criando `VinculoCobrancaServiceProgressoTests.cs`:
  - progresso pela contagem de presenças (FR-001);
  - saldo inicial = total;
  - edição com total < presenças → 409 com a contagem;
  - aulas do pacote só do próprio aluno;
  - Mensalidade/Avulsa sem `Progresso`.
- [X] T007 [P] [US1] FR-001 ("MUST NOT" falta/cancelada contarem): teste em `AulaServiceCobrancaPorAlunoTests.cs`, com presença, falta e aula cancelada do mesmo aluno com pacote. Só a presença fica com `VinculoCobrancaId` do pacote, que é o que a contagem lê.
- [X] T008 [US2] Frontend:
  - `lib/api/vinculosCobranca.ts`: tipos `progresso`, `totalAulasPacote`, `AulaDoPacote` e `listarAulasDoPacote`;
  - `VinculoCobrancaFormModal.tsx`: campo "Total de aulas" para Pacote; o "Saldo de aulas" só aparece na edição;
  - `VinculosCobrancaSection.tsx`: barra "N de M aulas", restantes, estado (Atenção/Esgotado destacados, total "estimado" marcado) e botão "Ver aulas", que mostra a lista com data, horário, matéria, contexto e status;
  - a barra usa a porcentagem calculada só a partir dos números da API;
  - estilos via tokens.

## Phase 3: User Story 3 - Visão de negócio na Home (P2)

- [X] T009 [US3] `DashboardService`:
  - `ObterAsync(periodoInicio, periodoFim, alunoId)` monta `Pacotes` (research.md R5) com `ProgressoPacote` e a contagem em lote;
  - remover `MontarPacotesEmAtencao` por saldo e o DTO antigo;
  - a montagem fica estática e pura (`MontarResumoPacotes`), para testar sem banco;
  - `DashboardController` e `IDashboardService` recebem `alunoId`.
- [X] T010 [P] [US3] Testes em `tests/SPI.Application.Tests/Dashboard/DashboardPacotesTests.cs`, substituindo `DashboardPacotesEmAtencaoTests.cs`:
  - contagens de ativos, concluídos e próximos do fim;
  - somas de dadas e restantes;
  - renovação ordenada, só com ativos de alunos ativos;
  - inativo concluído conta em concluídos;
  - números iguais aos de `ProgressoPacote.Calcular` (SC-002).
- [X] T011 [US3] Frontend:
  - `lib/api/dashboard.ts`: `pacotes` e o parâmetro `alunoId`;
  - `components/dashboard/PacotesPanel.tsx` (substitui `PacotesEmAtencaoPanel.tsx`): indicadores e cartões de renovação com "N de M" e estado, sem reordenar nem recalcular;
  - `app/(app)/dashboard/page.tsx`: filtros de aluno (select de alunos ativos) e período (de/até) na seção "Pacotes de aulas".

## Phase 4: Polish

- [X] T012 FR-011 ("MUST NOT" campo/tabela nova): `git status --porcelain -- database` sem arquivo novo; ler o diff de `src/SPI.Domain/Entities` e `Persistence/Configurations` e confirmar que não há propriedade ou mapeamento novo.
- [X] T013 Princípio II: ler o diff do frontend e confirmar que a única conta é a porcentagem da barra (`dadas/total`), sem decidir estado nem ordenar a renovação.
- [X] T014 Notas nas specs `041-pacote-esgotado` (o painel virou progresso; o bloqueio continua por saldo) e `052-auditoria-vinculo-cobranca` ("aulas dadas" do pacote = presenças).
- [X] T015 Quickstart, cenários 1-8, com a API local (SMTP desligado, dados "TESTE-058" apagados ao final).
- [X] T016 `git status --porcelain -- src tests frontend`: arquivos novos só os do plano. `dotnet build`, `dotnet test`, `tsc`, `eslint` nos arquivos tocados e `npm run build`.
- [ ] T017 (parte manual) Conferir a ficha do aluno e a Home num navegador real.
