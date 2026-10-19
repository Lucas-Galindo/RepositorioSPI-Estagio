---

description: "Task list for Notificações de Status de Aula Pendente"
---

# Tasks: Notificações de Status de Aula Pendente

**Input**: Design documents from `specs/059-notificacoes-aula-pendente/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: incluídos (xUnit com fakes). Há requisitos "MUST NOT" (FR-003 sem duplicar, FR-004 sem aviso para aula antiga, FR-012 resolvida não volta), e cada um tem verificação explícita, conforme o `CLAUDE.md`.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Backup do banco local (`database/backup/backup-database.ps1`). Criar `database/22_notificacao.sql` conforme data-model.md: tabela `notificacao`, FKs, `UNIQUE uq_notificacao_tipo_aula (tipo, aula_id)` e `idx_notificacao_professor_ativa`. Aplicar no MySQL local e conferir com `SHOW CREATE TABLE`.

---

## Phase 2: Foundational

- [X] T002 [P] Domain:
  - `Entities/Notificacao.cs`;
  - `Enums/TipoNotificacao.cs` (`AulaPendente`);
  - `Repositories/INotificacaoRepository.cs`, com `ListarAtivasPorProfessorAsync`, `ListarPorAulasAsync(tipo, aulaIds)`, `ListarAtivasPorTipoAsync(tipo, professorId?)`, `ObterPorIdAsync`, `AdicionarAsync` e `SalvarAlteracoesAsync`;
  - `IAulaRepository.ListarPendentesDeStatusAsync(agora, desde, professorId?)`.
- [X] T003 Infrastructure:
  - `NotificacaoConfiguration` (tipo como string, colunas snake_case);
  - `DbSet` no `SpiDbContext`;
  - `NotificacaoRepository`: `SalvarAlteracoesAsync` traduz chave duplicada em `ConflitoException`, como o `VinculoCobrancaRepository`; `ListarAtivasPorProfessorAsync` inclui `Aula.Materia`, `Aula.Turma` e `Aula.AulaAlunos.Aluno`;
  - `AulaRepository.ListarPendentesDeStatusAsync`, com a comparação data+hora em duas partes (research.md R2);
  - atualizar os fakes de `IAulaRepository` nos testes.
- [X] T004 Application:
  - `Notificacoes/Services/NotificacoesOptions.cs` (`AulasPendentesDesde`, `IntervaloVerificacaoSegundos`);
  - `Notificacoes/Dtos/NotificacaoResponse.cs`;
  - `INotificacaoService`/`NotificacaoService`: `SincronizarAulasPendentesAsync(agora, professorId?)` com a tabela de research.md R3, ignorando `ConflitoException` de inserção concorrente; `ListarAtivasAsync(professorId, agora)`, que sincroniza e depois lista; `MarcarComoLidaAsync(professorId, id)`, com `NaoEncontradoException` para notificação de outra professora;
  - mensagem e contexto montados no service (data-model.md);
  - registrar no `AddApplication`, e as opções em `AddInfrastructure` a partir da seção `Notificacoes`.

---

## Phase 3: User Story 1 - Aviso de aula que passou sem status (P1) 🎯 MVP

- [X] T005 [US1] `Infrastructure/BackgroundServices/NotificacaoDispatcherService.cs`: `PeriodicTimer` com `Notificacoes:IntervaloVerificacaoSegundos` (padrão 300), um escopo por execução, chamando `SincronizarAulasPendentesAsync(DateTime.Now, null)` e com try/catch por execução (padrão das rotinas existentes). Registrar com `AddHostedService`.
- [X] T006 [US1] `appsettings.json`: seção `Notificacoes` com `AulasPendentesDesde = "2026-10-19T00:00:00"` e `IntervaloVerificacaoSegundos = 300`.
- [X] T007 [P] [US1] Testes em `tests/SPI.Application.Tests/Notificacoes/NotificacaoServiceSincronizarTests.cs`:
  - aula pendente gera 1;
  - FR-003 (MUST NOT duplicar): sincronizar 3 vezes continua 1;
  - inserção concorrente (o fake lança `ConflitoException` no salvar) não propaga erro;
  - aula de professora diferente não aparece em `ListarAtivasAsync`;
  - mensagem e contexto de turma e de aula individual.

---

## Phase 4: User Story 2 - Abrir o aviso e ir à aula (P1)

- [X] T008 [US2] `Api/Controllers/NotificacoesController.cs`, conforme o contrato: `GET` e `PATCH {id}/lida` (204/404), com `[Authorize(Roles = "Professor")]` e `User.ObterUsuarioId()`.
- [X] T009 [P] [US2] Testes de `MarcarComoLidaAsync`: marca `LidaEm` sem resolver; é idempotente (não muda a data); notificação de outra professora → `NaoEncontradoException`.
- [X] T010 [US2] Frontend:
  - `frontend/lib/api/notificacoes.ts` (tipos do contrato, `listarNotificacoes`, `marcarNotificacaoComoLida`);
  - `frontend/components/layout/NotificacoesSino.tsx`: contador de não lidas (oculto se 0), painel com a lista (mais recentes primeiro, não lidas destacadas, vazio com mensagem) e popup com mensagem, data, horário, matéria, contexto e botão "Ir para a aula";
  - consulta ao montar, na troca de rota e a cada 60 s;
  - `Header.tsx` troca o sino decorativo pelo componente, renderizado só quando `sessao.perfil === "Professor"` (Assumption da spec; evita o 403 periódico para Admin);
  - estilos em `frontend/styles/dashboard.css` usando só tokens da spec 057, sem cor literal.

---

## Phase 5: User Story 3 - Aviso some quando a aula é atualizada (P1)

- [X] T011 [P] [US3] Testes:
  - aula que vira Realizada, Cancelada ou inativa resolve a notificação;
  - aula editada para terminar no futuro resolve a notificação;
  - FR-012: depois de resolvida e sem voltar a ficar pendente, a sincronização não a reativa;
  - aula que volta a ficar pendente reativa **a mesma** notificação (mesmo id, não lida);
  - FR-004 (MUST NOT): aula que terminou antes de `AulasPendentesDesde` nunca gera notificação. Este teste fica no repositório fake, aplicando o filtro da query; o filtro real é conferido em T014.

---

## Phase 6: Polish

- [X] T012 FR-003/FR-012 ("MUST NOT"): ler o diff de `NotificacaoService.cs` e confirmar que só há um ponto de criação (`AdicionarAsync`), sempre depois de checar a existência por `(tipo, aula)`, e que nenhum caminho apaga linha (`Remove`).
- [X] T013 Princípio II: ler o diff do frontend e confirmar que ele não calcula "pendente" nem filtra por horário. Ele só exibe o que a API devolve e conta `lida == false`.
- [X] T014 Validar quickstart.md, cenários 1-10, com a API local (SMTP desligado, dados "TESTE-059" apagados ao final), incluindo a query real de `ListarPendentesDeStatusAsync` (cenários 3 e 5).
- [X] T015 `git status --porcelain -- src tests frontend database`: os arquivos novos devem ser só os listados no plan.md.
- [X] T016 `dotnet build`, `dotnet test`, `npx tsc --noEmit`, `npx eslint` nos arquivos tocados e `npm run build`.
- [ ] T017 (parte manual) Conferir o sino, o painel e o popup num navegador real (quickstart.md "Frontend").

## Dependencies & Execution Order

T001 → T002 → T003/T004 → US1 (T005-T007) → US2 (T008-T010) → US3 (T011) → Polish. T007, T009 e T011 podem ser escritos em paralelo depois de T004.
