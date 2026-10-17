---

description: "Task list for Código de Exclusão de Professora via E-mail da Própria Professora"
---

# Tasks: Código de Exclusão de Professora via E-mail da Própria Professora

**Input**: Design documents from `specs/054-codigo-exclusao-email-professora/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md (N/A), contracts/email-codigo-verificacao-delta.md, quickstart.md

**Tests**: incluídas — ajuste dos 2 testes existentes que afirmavam o destino antigo + 1 teste novo pro bloqueio defensivo (FR-004), mesmo padrão xUnit/fakes já usado no restante do projeto.

**Organization**: tudo numa única user story (US1 P1) — o pedido inteiro cabe nela.

## Format: `[ID] [P?] [Story] Description`

## Path Conventions

Esta feature toca **somente** `src/SPI.Application/Professores/Services/ProfessorService.cs` e `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs` — nenhum outro arquivo, nenhuma mudança de schema/endpoint/frontend.

---

## Phase 1: Setup

Não aplicável — nenhuma dependência nova, nenhuma configuração a alterar.

---

## Phase 2: Foundational

Não aplicável — feature pequena o suficiente pra viver inteira na única user story.

---

## Phase 3: User Story 1 - Código de exclusão vai para o e-mail da professora, não do Admin (Priority: P1) 🎯 MVP

**Goal**: ao clicar em "Excluir Professor", o código de confirmação é enviado para o e-mail cadastrado da professora-alvo, não mais para o e-mail do Admin autenticado — o Admin continua digitando o código na tela normalmente.

**Independent Test**: logado como Admin, clicar em "Excluir Professor"; confirmar que o código chega no e-mail da professora (não no do Admin), e que digitar esse código efetiva a exclusão normalmente.

### Tests for User Story 1 (escrever/ajustar primeiro; devem FALHAR antes da implementação)

- [X] T001 [P] [US1] Em `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs`: renomear `SolicitarExclusao_EnviaCodigoApenasParaOEmailDoAdminAutenticado` para `SolicitarExclusao_EnviaCodigoParaOEmailDaProfessora` e inverter as asserções — `Assert.Equal(professora.Email, emailSender.EnviosRealizados[0].Destinatario)` e `Assert.DoesNotContain(emailSender.EnviosRealizados, e => e.Destinatario == admin.Email)` (research.md R4)
- [X] T002 [P] [US1] No mesmo arquivo: renomear `SolicitarExclusao_DoisAdminsDiferentes_CadaUmRecebeSeuProprioCodigoNoProprioEmail` para `SolicitarExclusao_DoisAdminsDiferentes_AmbosEnviamParaOEmailDaProfessora` e trocar as asserções de destinatário por `Assert.All(emailSender.EnviosRealizados, e => Assert.Equal(professora.Email, e.Destinatario))` — os outros `Assert` (2 tokens, nenhum usado) continuam (research.md R4)
- [X] T003 [P] [US1] No mesmo arquivo: criar `SolicitarExclusao_ProfessoraSemEmail_RecusaSemCriarToken` — professora semeada com `Email = ""`, `await Assert.ThrowsAsync<ConflitoException>(() => service.SolicitarExclusaoAsync(adminId: 1))`, e depois `Assert.Empty(tokens.Tokens)` / `Assert.Empty(emailSender.EnviosRealizados)` (FR-004, research.md R3)

### Implementation for User Story 1

- [X] T004 [US1] Em `src/SPI.Application/Professores/Services/ProfessorService.cs`, método `SolicitarExclusaoAsync` (depende de T001-T003 existirem como testes falhando): logo após `var professor = await _professorRepository.ObterUnicaAsync(...)` (linha ~148) e antes de qualquer outra coisa, acrescentar: `if (string.IsNullOrWhiteSpace(professor.Email)) { throw new ConflitoException("A professora nao tem e-mail cadastrado; nao e possivel enviar o codigo de confirmacao."); }` (FR-004, research.md R3) — nenhum token deve ser criado quando essa exceção é lançada
- [X] T005 [US1] No mesmo método (depende de T004): trocar `await _emailSender.EnviarAsync(admin.Email, ...)` por `await _emailSender.EnviarAsync(professor.Email, ...)`, e reescrever a string `corpo` (linhas ~179-184) para o texto de research.md R2: saudação `"Olá, {professor.Nome}."`, segundo parágrafo citando `{admin.Nome}` como quem solicitou e instruindo repassar o código a ele, e o aviso final orientando não compartilhar o código se o e-mail não era esperado — manter o mesmo código/token/expiração interpolados, só o texto ao redor muda. Não tocar em `admin = await _adminRepository.ObterPorIdAtivoAsync(...)` nem na gravação de `AdminId` no `ExclusaoProfessorToken` (FR-003 — rastreabilidade continua). Faz T001-T003 passarem

**Checkpoint**: User Story 1 completa e testável de forma independente — já entrega o pedido inteiro.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão e verificação dos requisitos "MUST NOT" (regra do `CLAUDE.md`)

- [X] T006 Regra do `CLAUDE.md` para FR-003 ("MUST NOT remover a rastreabilidade de qual Admin solicitou") e "não criar arquivo além do necessário": rodar `git status --porcelain -- src tests` e confirmar que não aparece nenhum arquivo não rastreado; rodar `git diff --stat -- src tests` e confirmar que só os 2 arquivos esperados (`ProfessorService.cs`, `ProfessorServiceExclusaoTests.cs`) foram alterados; ler o diff e confirmar que a linha `AdminId = adminId` na criação do `ExclusaoProfessorToken` continua presente e inalterada
- [X] T007 Regra do `CLAUDE.md` para FR-005 ("MUST NOT alterar nenhuma outra regra já especificada em specs/044"): ler diretamente o diff de `ProfessorService.cs` e confirmar que `ConfirmarExclusaoAsync`, `ReativarAsync`, `ValidadeTokenExclusao`, e a lógica de invalidar tokens pendentes do mesmo Admin (R4 de specs/044) permanecem byte a byte inalterados — a única função tocada é `SolicitarExclusaoAsync`
- [X] T008 Rodar `dotnet test tests/SPI.Application.Tests --filter FullyQualifiedName~ProfessorServiceExclusaoTests` e depois a suíte completa `dotnet test tests/SPI.Application.Tests` — 100% verde nos dois
- [X] T009 (parte manual) Executar os §1 e §2 de [quickstart.md](./quickstart.md) com o backend rodando e o Gmail SMTP configurado (specs/049): confirmar que o código chega no e-mail da professora (não do Admin) e que o Admin ainda consegue confirmar a exclusão digitando esse código

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: não aplicável.
- **Foundational (Phase 2)**: não aplicável.
- **User Story 1 (Phase 3)**: T001/T002/T003 podem ser escritos em paralelo (mesmo arquivo, mas blocos de teste independentes — ajustar com cuidado pra não conflitar na mesma edição); T004 depende dos 3 testes já escritos; T005 depende de T004 (mesmo método, ordem de edição).
- **Polish (Phase 4)**: depende da Phase 3 completa.

### Parallel Opportunities

- T001/T002/T003 são no mesmo arquivo — tecnicamente não paralelizáveis por edição simultânea, mas independentes em conteúdo (podem ser feitos em sequência rápida, sem depender um do resultado do outro).
- T006 e T007 (verificação de diff) são independentes entre si.

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (N/A) e Phase 2 (N/A).
2. Completar Phase 3 (T001-T005) — mudança de destino do e-mail funcionando de ponta a ponta, entregável como MVP (e como entrega completa, já que é a única story).
3. Phase 4 (Polish) é a validação final de não-regressão.

### Incremental Delivery

Foundational (N/A) → US1 (T001-T005, MVP e entrega completa) → Polish (T006-T009).

## Notes

- Nenhuma tarefa desta lista deve tocar `ConfirmarExclusaoAsync`, `ReativarAsync`, o schema do banco, ou criar nenhum arquivo novo — tudo vive dentro dos 2 arquivos já existentes (FR-003/FR-005).
- O texto do e-mail (T005) é a única parte "criativa" desta lista — seguir exatamente o modelo de research.md R2, não inventar variação.
