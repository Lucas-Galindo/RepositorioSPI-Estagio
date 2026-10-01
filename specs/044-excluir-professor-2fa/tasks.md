---

description: "Task list for Excluir Professor com Confirmação por 2FA via E-mail"
---

# Tasks: Excluir Professor com Confirmação por 2FA via E-mail

**Input**: Design documents from `specs/044-excluir-professor-2fa/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: incluídas — `research.md` R7 e a regra de memória "FR MUST NOT precisa de cobertura de teste" exigem cobertura explícita das três User Stories, inclusive dos requisitos negativos (FR-002, FR-007, FR-010).

**Organization**: tarefas agrupadas por user story (US1 P1 exclusão com 2FA; US2 P1 aviso single-tenant; US3 P2 reativação), seguindo o mesmo padrão de `AlunosController`/`AlunoService` (specs 026/anteriores) e o token (`SenhaResetToken`) como molde estrutural.

## Format: `[ID] [P?] [Story] Description`

- **(parte manual)**: tarefa que exige app rodando, e-mail real da Brevo, ou confirmação visual/navegador — excluída da contagem de bloqueio do hook `revisao-codigo` (ver `.specify/hooks/trello/sync-card.ps1`).

## Path Conventions

Projeto web: `src/SPI.{Domain,Application,Infrastructure,Api}/`, `frontend/`, `database/`, `tests/SPI.Application.Tests/` — conforme `plan.md`.

---

## Phase 1: Setup

**Purpose**: migração de banco e scaffolding mínimo compartilhado por todas as stories.

- [X] T001 Criar `database/17_exclusao_professor_token.sql` com a tabela `exclusao_professor_token` (`id` BIGINT PK AUTO_INCREMENT, `admin_id` INT NOT NULL FK → `admin(id)`, `professor_id` INT NOT NULL FK → `professor(id)`, `token_hash` VARCHAR(255) NOT NULL UNIQUE, `criado_em` DATETIME NOT NULL, `expira_em` DATETIME NOT NULL, `usado` TINYINT(1) NOT NULL DEFAULT 0, índices em `admin_id` e `professor_id`), mesmo estilo de comentários de `database/07_auth.sql` linhas 57-75
- [X] T002 (parte manual) Aplicar `database/17_exclusao_professor_token.sql` no MySQL local via `mysql` CLI (connection string das User Secrets), conforme regra de memória de auto-apply de migrações

**Checkpoint**: tabela pronta — Foundational phase pode começar.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: entidade, repositório e configuração EF que tanto US1 (exclusão) quanto US3 (reativação) vão usar.

**⚠️ CRITICAL**: nenhuma story pode ser implementada antes desta fase.

- [X] T003 [P] Criar entidade `ExclusaoProfessorToken` em `src/SPI.Domain/Entities/ExclusaoProfessorToken.cs` com propriedades `Id (long)`, `AdminId (int)`, `ProfessorId (int)`, `TokenHash (string)`, `CriadoEm (DateTime)`, `ExpiraEm (DateTime)`, `Usado (bool)` — mesmo estilo de `src/SPI.Domain/Entities/SenhaResetToken.cs`
- [X] T004 [P] Criar `src/SPI.Domain/Repositories/IExclusaoProfessorTokenRepository.cs` com `AdicionarAsync(ExclusaoProfessorToken)`, `ObterPorTokenHashAsync(string tokenHash)`, `ObterPendentePorAdminIdAsync(int adminId)` (retorna o(s) token(s) não-usados do Admin, para invalidação em R4), `SalvarAlteracoesAsync()` — mirror de `ISenhaResetTokenRepository`
- [X] T005 [US-ALL] Criar `src/SPI.Infrastructure/Persistence/Configurations/ExclusaoProfessorTokenConfiguration.cs` mapeando para a tabela `exclusao_professor_token`, `TokenHash` como `UNIQUE`, FKs para `Admin`/`Professor` sem cascade delete (dados de auditoria do token devem sobreviver mesmo que o professor seja reativado/excluído de novo)
- [X] T006 Criar `src/SPI.Infrastructure/Repositories/ExclusaoProfessorTokenRepository.cs` implementando `IExclusaoProfessorTokenRepository` (depende de T003, T004, T005)
- [X] T007 Registrar `IExclusaoProfessorTokenRepository → ExclusaoProfessorTokenRepository` em `src/SPI.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` (mesmo ponto onde `ISenhaResetTokenRepository` é registrado, linha ~40)
- [X] T008 [P] Criar `tests/SPI.Application.Tests/Professores/ProfessorServiceFakes.cs` — primeiro arquivo do diretório (ainda não existe nenhum teste de `ProfessorService`): fake de `IProfessorRepository` (reaproveitando os métodos já existentes: `ObterUnicaAsync`, `SalvarAlteracoesAsync`), fake novo de `IExclusaoProfessorTokenRepository`, fake de `IAdminRepository` (`ObterPorIdAtivoAsync`), fake de `ITokenService` (`GerarTokenSeguro`/`CalcularHash` determinísticos para teste), fake de `IEmailSender` (capturando destinatário/assunto/corpo enviados, para as asserções negativas de FR-002)

**Checkpoint**: entidade/repositório/DI prontos — US1, US2 e US3 podem começar.

---

## Phase 3: User Story 1 - Exclusão com 2FA por e-mail (Priority: P1) 🎯 MVP

**Goal**: Admin autenticado solicita um código, recebe por e-mail (no endereço do Admin, nunca da professora), confirma, e a professora é desativada (`Ativo=false`) sem apagar nenhum dado vinculado.

**Independent Test**: logado como Admin, clicar em "Excluir Professor" → pedir código → receber e-mail na própria caixa do Admin → colar o código → professora inativa, Turmas/Alunos/Aulas/Pagamentos continuam intactos e consultáveis → login da professora recusado.

### Tests for User Story 1 ⚠️

> Escrever estes testes primeiro; devem falhar até a implementação abaixo existir.

- [X] T009 [P] [US1] Teste: `SolicitarExclusaoAsync` gera um novo token, salva hash (nunca o valor em claro) e envia e-mail para o endereço do **Admin autenticado** (nunca da professora) em `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs`
- [X] T010 [P] [US1] Teste negativo (FR-002/R3): `SolicitarExclusaoAsync` nunca usa `professor.Email` como destinatário, mesmo que o e-mail do Admin falhe ao resolver — cenário com dois Admins diferentes solicitando confirma que cada e-mail vai ao Admin correspondente (R4, edge case de dois Admins simultâneos) em `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs`
- [X] T011 [P] [US1] Teste: `SolicitarExclusaoAsync` invalida (`Usado=true`) qualquer token anterior não-usado do **mesmo** `AdminId` antes de criar o novo; um token pendente de **outro** Admin não é afetado (R4) em `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs`
- [X] T012 [P] [US1] Teste: `ConfirmarExclusaoAsync` com código correto, dentro da validade e não-usado define `Usado=true` no token e `Professor.Ativo=false`, numa única operação atômica em `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs`
- [X] T013 [P] [US1] Teste negativo (FR-007): `ConfirmarExclusaoAsync` bem-sucedido não remove a linha do Professor nem toca em nenhum repositório de Turma/Aluno/Aula/Pagamento — fake desses repositórios não deve registrar nenhuma chamada em `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs`
- [X] T014 [P] [US1] Teste negativo: `ConfirmarExclusaoAsync` com código errado, expirado, ou já usado lança exceção/retorna falha sem alterar `Professor.Ativo` nem o token (incluindo o caso do código antigo invalidado pelo T011) em `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs`
- [X] T015 [P] [US1] Teste negativo (FR-010): simular "cancelar" (nunca chamar `ConfirmarExclusaoAsync`) e então deixar o token expirar — `Professor.Ativo` permanece `true` e o token nunca foi consumido artificialmente, só pela expiração natural em `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs`
- [X] T016 [P] [US1] Teste: `ConfirmarExclusaoProfessorRequestValidator` rejeita `Codigo` vazio/nulo (400, FR contract) em `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs`
- [X] T016a [P] [US1] Teste negativo (FR-011): `SolicitarExclusaoAsync` com `IEmailSender` fake lançando falha de envio ainda assim salva o token no repositório (mesmo padrão "salvar primeiro, depois tentar enviar" de `RecuperacaoSenhaService`) e propaga a falha como erro ao chamador (ao contrário da recuperação de senha, aqui a falha de envio é reportada, nunca mascarada) em `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs`

### Implementation for User Story 1

- [X] T017 [US1] Criar `src/SPI.Application/Professores/Dtos/SolicitarExclusaoResponse.cs` (corpo vazio/200 — conforme contrato) e `src/SPI.Application/Professores/Dtos/ConfirmarExclusaoProfessorRequest.cs` com `Codigo: string` (obrigatório, não vazio — per `data-model.md`)
- [X] T018 [US1] Criar `src/SPI.Application/Professores/Validators/ConfirmarExclusaoProfessorRequestValidator.cs` (`RuleFor(x => x.Codigo).NotEmpty()`), mesmo estilo de `EsqueciSenhaRequestValidator`
- [X] T019 [US1] Adicionar `SolicitarExclusaoAsync(int adminId)` e `ConfirmarExclusaoAsync(int adminId, string codigo)` a `src/SPI.Application/Professores/Services/IProfessorService.cs`
- [X] T020 [US1] Implementar `SolicitarExclusaoAsync` em `src/SPI.Application/Professores/Services/ProfessorService.cs`: obter a professora via `ObterUnicaAsync` (404 se não houver), invalidar tokens pendentes do mesmo `adminId` (`IExclusaoProfessorTokenRepository`), gerar código via `ITokenService.GerarTokenSeguro()`, salvar hash com `ExpiraEm = agora + 15min`, resolver e-mail do Admin via `IAdminRepository.ObterPorIdAtivoAsync(adminId)`, enviar e-mail com `IEmailSender` usando o template de `contracts/email-codigo-verificacao.md` (assunto `"SPI - Código de confirmação para excluir professora"`), propagando falha de envio como erro (diferente de `RecuperacaoSenhaService`, per contrato) (depende de T003-T008, T017-T018)
- [X] T021 [US1] Implementar `ConfirmarExclusaoAsync` em `src/SPI.Application/Professores/Services/ProfessorService.cs`: buscar token por hash do código recebido, validar pertencimento ao `adminId`, não-expirado, não-usado (409 se qualquer uma falhar, sem revelar qual), marcar `Usado=true` e `Professor.Ativo=false` numa mesma chamada de `SalvarAlteracoesAsync` (depende de T020)
- [X] T022 [US1] Adicionar `POST api/professor/admin/excluir/solicitar-codigo` e `POST api/professor/admin/excluir/confirmar` em `src/SPI.Api/Controllers/ProfessorController.cs`, ambos `[Authorize(Roles = nameof(PerfilUsuario.Admin))]`, sem `{id}` na rota, resolvendo `adminId` via `User.ObterUsuarioId()`, seguindo o padrão try/catch → `NotFound`/`Conflict`/`Problem` de `AlunosController.Excluir` (depende de T019-T021)
- [X] T023 [P] [US1] Adicionar `solicitarExclusaoProfessor()` e `confirmarExclusaoProfessor(codigo: string)` em `frontend/lib/api/professor.ts`, chamando os 2 novos endpoints
- [X] T024 [US1] Adicionar seção "Excluir Professor" em `frontend/components/admin/CadastrarProfessoraForm.tsx`: botão visível só quando o usuário logado é Admin, state machine de etapas (`"inicial" | "aviso" | "codigo" | "concluido"`) igual ao padrão de `EsqueciSenhaForm.tsx`, campo de texto para colar o código recebido (depende de T023)
- [ ] T025 (parte manual) [US1] Validar ponta a ponta o §1 de `quickstart.md` (passos 1-8: botão visível, e-mail chega na caixa do Admin, código certo desativa a professora com dados vinculados intactos, login da professora excluída recusado, código errado/antigo/expirado recusados)

**Checkpoint**: US1 completa e testável de forma independente.

---

## Phase 4: User Story 2 - Aviso explícito de inacessibilidade single-tenant (Priority: P1)

**Goal**: antes (ou junto) do pedido de código, o Admin vê um aviso forte e específico — distinto de um aviso genérico de "ação irreversível" — de que excluir a única professora torna o sistema inacessível para uso normal até uma nova ser cadastrada.

**Independent Test**: ao clicar em "Excluir Professor", o aviso single-tenant aparece antes de qualquer código ser solicitado, com texto específico (não genérico).

### Implementation for User Story 2

- [X] T026 [US2] Adicionar o texto do aviso single-tenant (explícito: login da professora, alunos e aulas ficam inacessíveis até nova professora ser cadastrada) na etapa `"aviso"` de `frontend/components/admin/CadastrarProfessoraForm.tsx`, exibido antes do botão que efetivamente chama `solicitarExclusaoProfessor()` (depende de T024)
- [ ] T027 (parte manual) [US2] Validar o §2 de `quickstart.md`: confirmar visualmente que o aviso aparece antes do pedido de código e que seu texto é especificamente sobre a inacessibilidade single-tenant, não um aviso genérico de "esta ação não pode ser desfeita"

**Checkpoint**: US1 + US2 funcionando juntas (US2 depende apenas da UI de US1, nenhuma mudança de backend).

---

## Phase 5: User Story 3 - Reativação do Professor (Priority: P2)

**Goal**: um Professor previamente excluído pode ser reativado pelo Admin sem precisar de um novo 2FA, mesma consistência de `Aluno`/`Turma`/`Matéria`.

**Independent Test**: com a professora excluída (resultado de US1), acionar "Reativar" sem nenhum código → login volta a funcionar, dados intactos.

### Tests for User Story 3 ⚠️

- [X] T028 [P] [US3] Teste: `ReativarAsync` define `Professor.Ativo=true` sem exigir nenhum código/token em `tests/SPI.Application.Tests/Professores/ProfessorServiceReativacaoTests.cs`
- [X] T029 [P] [US3] Teste negativo: `ReativarAsync` numa professora já ativa retorna conflito informativo, sem alterar nada em `tests/SPI.Application.Tests/Professores/ProfessorServiceReativacaoTests.cs`
- [X] T030 [P] [US3] Teste: `ReativarAsync` sem nenhuma professora cadastrada retorna 404 em `tests/SPI.Application.Tests/Professores/ProfessorServiceReativacaoTests.cs`

### Implementation for User Story 3

- [X] T031 [US3] Adicionar `ReativarAsync(int adminId)` a `src/SPI.Application/Professores/Services/IProfessorService.cs` e implementar em `ProfessorService.cs`: obter a professora via `ObterUnicaAsync` (404 se ausente), 409 se já `Ativo=true`, senão `Ativo=true` + `SalvarAlteracoesAsync` — sem exigir token (depende de T003-T008)
- [X] T032 [US3] Adicionar `PATCH api/professor/admin/reativar` em `src/SPI.Api/Controllers/ProfessorController.cs`, `[Authorize(Roles = nameof(PerfilUsuario.Admin))]`, retornando `ProfessorResponse` atualizado (200), mesmo formato de `AlunosController.Reativar` (depende de T031)
- [X] T033 [P] [US3] Adicionar `reativarProfessor()` em `frontend/lib/api/professor.ts`
- [X] T034 [US3] Adicionar botão "Reativar" em `frontend/components/admin/CadastrarProfessoraForm.tsx`, visível quando a professora está inativa, chamando `reativarProfessor()` sem pedir nenhum código (depende de T024, T033)
- [ ] T035 (parte manual) [US3] Validar o §3 de `quickstart.md`: reativar sem código, confirmar login funcionando normalmente com dados intactos

**Checkpoint**: todas as 3 user stories funcionando de forma independente.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão e validação final de toda a feature.

- [X] T036 [P] Conferir que `GET/PUT /api/professor/admin`, `GET/PUT /api/professor/me`, `PUT /api/professor/me/senha`, `POST /api/professor/cadastro-inicial` permanecem com contrato inalterado (nenhuma assinatura de método tocada fora das novas adições)
- [X] T037 (parte manual) Rodar `dotnet test tests/SPI.Application.Tests` completo e confirmar 100% verde, incluindo os testes novos de T009-T016 e T028-T030
- [X] T038 (parte manual) Validar §4 de `quickstart.md` (não-regressão): `git diff --stat -- frontend/app frontend/components` fora da seção de exclusão, `database/` só com `17_exclusao_professor_token.sql`, nenhuma mudança em `AlunosController`/`TurmasController`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências — pode começar imediatamente
- **Foundational (Phase 2)**: depende de Setup (T002, migração aplicada) — bloqueia todas as stories
- **US1 (Phase 3)**: depende de Foundational
- **US2 (Phase 4)**: depende da UI de US1 (T024) — mesma tela, etapa anterior ao pedido de código
- **US3 (Phase 5)**: depende de Foundational apenas (independente de US1/US2 no backend; na UI depende de T024 existir como ponto de inserção)
- **Polish (Phase 6)**: depende de todas as stories completas

### Parallel Opportunities

- T003, T004, T008 em paralelo (arquivos distintos)
- T009-T016 (testes de US1) em paralelo entre si, mas todos antes de T017+
- T023 em paralelo com o backend de US1 (T017-T022), já que só consome o contrato já fechado
- T028-T030 (testes de US3) em paralelo entre si
- T033 em paralelo com T031-T032

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 2)

1. Completar Phase 1 (Setup) e Phase 2 (Foundational)
2. Completar Phase 3 (US1) — fluxo de exclusão com 2FA completo
3. Completar Phase 4 (US2) — aviso single-tenant (poucas linhas de UI sobre o que US1 já construiu)
4. **Parar e validar**: US1+US2 juntas já cobrem o pedido original mais crítico (exclusão seletiva com 2FA + aviso)
5. Phase 5 (US3, reativação) é a entrega incremental seguinte — menor risco, sem 2FA

### Incremental Delivery

Setup + Foundational → US1 (MVP de exclusão) → US2 (aviso, mesma tela) → US3 (reativação) → Polish.
