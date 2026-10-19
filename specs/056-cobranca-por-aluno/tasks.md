---

description: "Task list for Cobrança por Aluno e Reposição de Aulas"
---

# Tasks: Cobrança por Aluno e Reposição de Aulas

**Input**: Design documents from `specs/056-cobranca-por-aluno/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: incluídos — muda regra de cobrança real (contas a receber, saldo de pacote) e há vários requisitos "MUST NOT" (FR-004, FR-010, FR-016, FR-021) que, pela regra do `CLAUDE.md`, exigem verificação explícita.

**Organization**: a mudança de regra de cobrança (Phase 2) serve US1 e US2 ao mesmo tempo; as fases de US1/US2 ficam com testes e telas específicos de cada uma. US3 (reposição) e US4 (transição) são independentes entre si.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Rodar `database/backup/backup-database.ps1` para copiar o banco local antes de qualquer alteração de dados (regra de segurança do usuário: nunca sobrescrever sem copiar antes).
- [X] T002 Criar `database/21_cobranca_por_aluno.sql` conforme research.md R8, nesta ordem: remover `uq_vinculocobranca_chave_ativa` e a coluna `chave_ativa`; `UPDATE vinculo_cobranca SET turma_id = NULL WHERE modalidade IN ('Mensalidade','Pacote')`; criar coluna gerada `chave_avulsa_ativa` (`IF(ativo AND modalidade = 'Avulsa', CONCAT(aluno_id, ':', IFNULL(turma_id, 0)), NULL)`) com índice único `uq_vinculocobranca_chave_avulsa_ativa`; `CHECK (turma_id IS NULL OR modalidade = 'Avulsa')`; `ALTER TABLE aula_aluno ADD COLUMN reposicao TINYINT(1) NOT NULL DEFAULT 0`. Aplicar no MySQL local e conferir com `SHOW CREATE TABLE`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: nova regra de cobrança e regras de combinação de vínculos — servem US1 e US2.

- [X] T003 [P] `src/SPI.Domain/Entities/AulaAluno.cs`: `public bool Reposicao { get; set; }` com comentário; mapear em `AulaAlunoConfiguration.cs` (`reposicao`, default false).
- [X] T004 Remover `ExisteAtivoAsync` e `ObterAtivoPorAlunoEContextoAsync` de `IVinculoCobrancaRepository` e de `VinculoCobrancaRepository` (research.md R4); ajustar a mensagem de `DuplicateKeyEntry` para "Ja existe um vinculo Avulsa ativo para este contexto."; atualizar os fakes de teste que implementam a interface (`Aulas/AulaServiceFakes.cs` — `FakeVinculoCobrancaRepositoryParaAula` passa a implementar `ListarPorAlunoAsync` filtrando por aluno/ativo —, `VinculosCobranca/VinculoCobrancaFakes.cs`, `Mensalidade/MensalidadeFakes.cs`, e qualquer outro apontado pelo compilador).
- [X] T005 Criar `src/SPI.Application/Aulas/Services/RegraCobrancaPresenca.cs`: `DecisaoCobranca` (Tipo, Vinculo, PacoteEsgotado) e `Decidir(IReadOnlyList<VinculoCobranca> ativosDoAluno, int? turmaIdDaAula)` exatamente na ordem de research.md R1/R2 (mensalidade mais antiga → pacote mais antigo com saldo > 0 → pacote com saldo nulo → avulsa com `TurmaId == turmaIdDaAula` → pacote esgotado ⇒ `Bloqueada` → `SemVinculo`).
- [X] T006 `AulaService.RegistrarSessaoAsync`: para cada presença marcada, buscar `ListarPorAlunoAsync(alunoId, true)` uma vez, decidir com `RegraCobrancaPresenca`, tratar `Bloqueada` como hoje (presença false, `MotivoPacoteEsgotado`, aviso de renovar) e adicionar o aviso "Pacote esgotado: {nome} foi cobrado como aula avulsa. Renove o pacote para voltar a usar o saldo." quando `Tipo == Avulsa && PacoteEsgotado`. `GerarContasAReceberAsync` passa a receber as decisões e só aplicar o efeito de `data-model.md` (nenhuma consulta de vínculo própria), gravando `AulaAluno.VinculoCobrancaId`.
- [X] T007 [P] `VinculoCobrancaRequestValidator.cs`: rejeitar `TurmaId` preenchido quando `Modalidade` é Mensalidade ou Pacote ("Mensalidade e Pacote valem para todas as aulas do aluno e não têm turma.").
- [X] T008 `VinculoCobrancaService`: substituir `GarantirSemVinculoAtivoAsync` por `GarantirRegrasDeCombinacaoAsync(alunoId, modalidade, turmaId, ignorarId)` com as 4 recusas de contracts/vinculos-cobranca-api.md; chamar em `CadastrarAsync`, em `AtualizarAsync` (sempre, não só quando a turma muda) e em `ReativarAsync`.
- [X] T009 [P] `MensalidadeDispatcherService`: descrição "Mensalidade - {competencia:MM/yyyy}" (sem turma); `DashboardService` (pacotes em atenção): contexto "Todas as aulas do aluno".

**Checkpoint**: regra nova de cobrança e de combinação funcionando no backend.

---

## Phase 3: User Story 1 - Mensalidade cobre todas as aulas do aluno (Priority: P1) 🎯 MVP

- [X] T010 [P] [US1] Testes em `tests/SPI.Application.Tests/Aulas/AulaServiceCobrancaPorAlunoTests.cs` (mensalidade): presença em aula de turma e em aula individual de aluno com mensalidade ativa (sem turma) não gera nenhuma conta a receber e grava `VinculoCobrancaId` da mensalidade (FR-004 "MUST NOT"); aluno com mensalidade e pacote ativos herdados: mensalidade prevalece, saldo do pacote não muda (FR-022); `AulasDadasNoMes` da mensalidade conta presenças em turma e individual e não conta faltas (FR-006, achado do analyze).
- [X] T011 [P] [US1] Testes em `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServiceCombinacaoTests.cs`: mensalidade recusada com outro vínculo ativo (cadastro e reativação); pacote e avulsa recusados com mensalidade ativa; edição que troca Avulsa→Mensalidade com outros ativos recusada; validator recusa turma em Mensalidade/Pacote.
- [X] T012 [P] [US1] Teste em `tests/SPI.Application.Tests/Mensalidade/`: aluno com mensalidade gera exatamente 1 cobrança no mês, descrição "Mensalidade - MM/aaaa".
- [X] T013 [US1] `frontend/components/alunos/VinculoCobrancaFormModal.tsx`: campo Turma só aparece para Avulsa; ao trocar para Mensalidade/Pacote, limpa a turma e envia `turmaId: null`.
- [X] T014 [US1] `frontend/components/alunos/VinculosCobrancaSection.tsx`: Mensalidade/Pacote exibidos como "Todas as aulas do aluno"; Avulsa mantém turma/"Atendimento individual".

---

## Phase 4: User Story 2 - Pacote como saldo do aluno (Priority: P1)

- [X] T015 [P] [US2] Testes em `tests/SPI.Application.Tests/Aulas/RegraCobrancaPresencaTests.cs`: cada ramo da ordem de decisão (mensalidade; pacote com saldo; pacote saldo nulo; avulsa do contexto; avulsa de outro contexto não vale; pacote esgotado + avulsa ⇒ Avulsa com `PacoteEsgotado`; pacote esgotado sem avulsa ⇒ Bloqueada; sem vínculo; dois pacotes ⇒ mais antigo com saldo primeiro).
- [X] T016 [P] [US2] Testes em `AulaServiceCobrancaPorAlunoTests.cs` (pacote): presença em turma A, turma B e individual consome o mesmo pacote; falta não consome (FR-010 "MUST NOT"); pacote com saldo tem prioridade sobre avulsa do contexto; esgotado + avulsa gera conta a receber pelo valor da avulsa e o aviso novo; esgotado sem avulsa bloqueia.
- [X] T017 [US2] Testes de vínculo: segundo pacote ativo recusado (cadastro e reativação) em `VinculoCobrancaServiceCombinacaoTests.cs`.
- [X] T018 [US2] Ajustar os testes existentes que codificam o modelo antigo por contexto (ex.: `AulaServiceRegistrarSessaoPacoteEsgotadoTests.Vinculo_esgotado_em_outro_contexto_nao_bloqueia`, testes de unicidade por contexto de `VinculoCobrancaServiceCadastrarTests`/`ReativarTests`, testes de descrição de `MensalidadeDispatcherServiceGerarCobrancasTests`, testes de contexto do dashboard) — cada ajuste com comentário citando a spec 056 e listado no relatório.

---

## Phase 5: User Story 3 - Reposição (Priority: P2)

- [X] T019 [US3] `AulaService`: `AdicionarParticipanteAsync(aulaId, alunoId)` e `RemoverParticipanteAsync(aulaId, alunoId)` com as regras de contracts/aulas-participantes-api.md; declarar em `IAulaService`; `AlunoPresencaResponse.Reposicao` + `Mapear`; novo `AdicionarParticipanteRequest`.
- [X] T020 [US3] `AulaService.AtualizarAsync`: preservar participantes `Reposicao = true`; recriar só os matriculados ativos; aluno de reposição que virou matriculado passa a `Reposicao = false` sem duplicar; aula que vira individual remove os de reposição (research.md R7).
- [X] T021 [US3] `AulasController`: `POST /api/aulas/{id}/participantes` e `DELETE /api/aulas/{id}/participantes/{alunoId}` (404/409 conforme contrato).
- [X] T022 [P] [US3] Testes em `tests/SPI.Application.Tests/Aulas/AulaServiceReposicaoTests.cs`: adicionar aluno não matriculado; recusar aluno inativo/já participante/aula realizada/aula cancelada/aula individual (FR-017); remover reposição; recusar remover matriculado (FR-016 "MUST NOT"); edição preserva reposição; edição com troca de turma mantém reposição e troca matriculados; aluno de reposição que vira matriculado não duplica; presença do aluno de reposição consome o pacote dele (FR-019). (Casos de borda adicionados pelo analyze.)
- [X] T023 [US3] `frontend/lib/api/aulas.ts`: `reposicao` em `AlunoPresenca`; `adicionarParticipante`/`removerParticipante`.
- [X] T024 [US3] `frontend/app/(app)/aulas/[id]/page.tsx`: em aula agendada de turma, select de alunos ativos que ainda não participam + botão "Adicionar (reposição)"; marca "Reposição" e botão de remover nos participantes de reposição.

---

## Phase 6: User Story 4 - Vínculos existentes (Priority: P3)

- [X] T025 [US4] `VinculoCobrancaResponse.Conflito` + cálculo em `VinculoCobrancaService.ListarPorAlunoAsync` (research.md R6).
- [X] T026 [P] [US4] Testes: aluno com mensalidade + pacote ativos ⇒ ambos com `Conflito`; dois pacotes ⇒ os dois; aluno sem conflito ⇒ todos nulos; inativos nunca marcados.
- [X] T027 [US4] `VinculosCobrancaSection.tsx`: aviso no topo quando algum vínculo tem `conflito`, listando-os; `frontend/lib/api/vinculosCobranca.ts` com o campo novo.
- [X] T028 [US4] Verificação FR-020/FR-021 ("MUST NOT alterar vínculos"): ler diretamente o diff de `database/21_cobranca_por_aluno.sql` e confirmar que o único comando sobre dados é o `UPDATE ... SET turma_id = NULL` para Mensalidade/Pacote; contar vínculos ativos e somar `valor`/`saldo_aulas`/`total_aulas_pacote` antes e depois da migração no banco local e confirmar igualdade.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T029 Nota "Regra substituída em parte pela spec 056" no topo de `specs/037-vinculo-cobranca/spec.md`, `specs/038-vinculo-cobranca-gerar-contas/spec.md`, `specs/039-job-cobranca-mensalidade/spec.md`, `specs/041-pacote-esgotado/spec.md` e `specs/052-auditoria-vinculo-cobranca/spec.md`, dizendo exatamente o que mudou em cada uma (FR-023).
- [X] T030 Princípio II / "MUST NOT duplicar": ler o diff de `AulaService.cs` e confirmar que a ordem de prioridade de cobrança só existe em `RegraCobrancaPresenca` (nenhum `Modalidade ==` decidindo cobrança dentro de `AulaService` além do `switch` de efeito).
- [X] T031 `git status --porcelain -- src frontend database tests`: arquivos novos esperados são só a migração 21, `RegraCobrancaPresenca.cs`, `AdicionarParticipanteRequest.cs` e os arquivos de teste novos.
- [X] T032 `dotnet build` + `dotnet test` (suíte completa); `npx tsc --noEmit`, `npx eslint` nos arquivos tocados e `npm run build` no frontend.
- [X] T033 Validar quickstart.md §1-§4 com a API rodando localmente (chamadas HTTP reais com dados sintéticos; nenhum e-mail enviado a endereço real).

---

## Dependencies & Execution Order

- Phase 1 → Phase 2 → (US1, US2 em paralelo) → US3 e US4 (independentes) → Polish.
- T006 depende de T004/T005; T008 depende de T004; T018 depende de T006/T008/T009.
- Frontend (T013, T014, T023, T024, T027) depende só dos contratos.

## Implementation Strategy

MVP = Phases 1-4 (regra de cobrança por aluno). US3/US4 completam a feature; Polish fecha documentação e verificações.
