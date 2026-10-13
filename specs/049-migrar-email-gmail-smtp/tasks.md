---

description: "Task list for Migrar Envio de E-mail para Gmail SMTP"
---

# Tasks: Migrar Envio de E-mail para Gmail SMTP

**Input**: Design documents from `specs/049-migrar-email-gmail-smtp/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: incluídas para a lógica nova de orçamento por ciclo/retomada parcial (research.md R5) — mesmo padrão já usado para `MensalidadeDispatcherService` (spec 039): extrair a lógica testável como método `public static`, tomando as dependências por parâmetro, em vez de depender do `IServiceScopeFactory`/DI do `BackgroundService`.

**Organization**: tarefas agrupadas por user story (US1 P1 recuperação de senha/2FA continuam funcionando; US2 P1 lembretes divididos em lotes entre ciclos). A renomeação Brevo→Smtp é compartilhada pelas duas stories (ambas dependem de `IEmailSender`) e vive na Fase Foundational.

## Format: `[ID] [P?] [Story] Description`

- **(parte manual)**: tarefa que exige app rodando e credenciais reais da conta Gmail dedicada (criada pelo usuário, fora desta lista de tarefas).

## Path Conventions

Projeto backend existente (.NET). Esta feature toca `src/SPI.Application/Common/IEmailSender.cs`, `src/SPI.Domain/Entities/Lembrete.cs`, `src/SPI.Infrastructure/Email/*`, `src/SPI.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs`, `src/SPI.Infrastructure/BackgroundServices/LembreteDispatcherService.cs`, `src/SPI.Infrastructure/Repositories/LembreteRepository.cs`, `src/SPI.Api/appsettings.json`, `database/18_*.sql`, e `tests/SPI.Application.Tests/Lembretes/`. Nenhum arquivo de frontend.

---

## Phase 1: Setup

- [ ] T001 Criar `database/18_lembrete_destinatarios_tentados.sql`: `ALTER TABLE lembrete ADD COLUMN destinatarios_tentados TEXT NULL COMMENT 'E-mails ja tentados para este lembrete entre ciclos, separados por ;', ADD COLUMN envios_com_sucesso INT NOT NULL DEFAULT 0 COMMENT 'Contagem cumulativa de envios bem-sucedidos entre ciclos';` — mesmo estilo de comentário das migrações anteriores
- [ ] T002 (parte manual) Aplicar `database/18_lembrete_destinatarios_tentados.sql` no MySQL local via `mysql` CLI (connection string das User Secrets), conforme regra de memória de auto-apply de migrações

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: a renomeação Brevo→Smtp (FR-007) — bloqueia as duas user stories, já que ambas dependem de `IEmailSender`/a implementação concreta.

**⚠️ CRITICAL**: T003-T007 bloqueiam as Fases 3 e 4 (US1/US2).

- [ ] T003 [P] Renomear `src/SPI.Infrastructure/Email/BrevoOptions.cs` → `SmtpOptions.cs`: classe `BrevoOptions` → `SmtpOptions`, `SectionName = "Brevo"` → `"Smtp"`, `SmtpHost` padrão `"smtp-relay.brevo.com"` → `"smtp.gmail.com"` (demais campos/padrões inalterados: `SmtpPorta = 587`, `SmtpUsuario`/`SmtpChave`/`RemetenteEmail` vazios, `RemetenteNome` inalterado) — atualizar o comentário do arquivo para não citar mais "Brevo" especificamente
- [ ] T004 [P] Renomear `src/SPI.Infrastructure/Email/BrevoEmailSender.cs` → `SmtpEmailSender.cs`: classe `BrevoEmailSender` → `SmtpEmailSender`, usa `SmtpOptions` (T003) — corpo de `EnviarAsync` inalterado (mesma sequência MailKit: `ConnectAsync`/`AuthenticateAsync`/`SendAsync`/`DisconnectAsync`)
- [ ] T005 Em `src/SPI.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs`, trocar `services.Configure<BrevoOptions>(configuration.GetSection(BrevoOptions.SectionName))` → `services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName))` e `services.AddScoped<IEmailSender, BrevoEmailSender>()` → `services.AddScoped<IEmailSender, SmtpEmailSender>()` (depende de T003, T004)
- [ ] T006 Em `src/SPI.Api/appsettings.json`, renomear a seção `"Brevo"` para `"Smtp"`, e `SmtpHost` de `"smtp-relay.brevo.com"` para `"smtp.gmail.com"` — `SmtpPorta`/`RemetenteNome` inalterados (depende de T003)
- [ ] T007 [P] Em `src/SPI.Application/Common/IEmailSender.cs`, atualizar o comentário que cita "Brevo" como exemplo para algo neutro de provedor (ex.: "Gmail, etc") — nenhuma mudança de assinatura

**Checkpoint**: renomeação completa — Fases 3 e 4 podem começar.

---

## Phase 3: User Story 1 - Recuperação de senha e 2FA continuam funcionando (Priority: P1) 🎯 MVP

**Goal**: os e-mails de recuperação de senha e de código de 2FA (exclusão de professor) continuam sendo entregues, agora através da conta Gmail configurada, sem nenhuma mudança de conteúdo/formato.

**Independent Test**: solicitar recuperação de senha e o código de 2FA de exclusão de professor; confirmar que ambos chegam, com o remetente agora sendo a conta Gmail dedicada.

- [ ] T008 (parte manual) [US1] Validar o §1 de [quickstart.md](./quickstart.md) (exige User Secrets `Smtp:*` já reconfigurados com a conta Gmail dedicada, e a migração T002 aplicada): recuperação de senha entrega o código; 2FA de exclusão de professor (spec 044) entrega o código; ambos com o mesmo conteúdo/formato de antes, remetente é a conta Gmail (FR-001, FR-002, FR-006, SC-001)

**Checkpoint**: User Story 1 completa e testável de forma independente.

---

## Phase 4: User Story 2 - Lembretes divididos em lotes entre ciclos (Priority: P1)

**Goal**: um lembrete com mais destinatários que o limite configurado por ciclo tem seus e-mails divididos entre múltiplos ciclos do serviço periódico, retomando exatamente de onde parou, sem nenhuma pausa bloqueante.

**Independent Test**: lembrete vencido com destinatários acima do limite configurado; confirmar que os e-mails saem em lotes ao longo de ciclos sucessivos, nunca excedendo o limite num único ciclo, até que todos sejam tentados.

### Tests for User Story 2 ⚠️

> Escrever estes testes primeiro; devem falhar até a implementação abaixo existir.

- [ ] T009 [P] [US2] Teste: extrair a lógica de processamento como `public static async Task ProcessarLembretesVencidosAsync(ILembreteRepository, IEmailSender, int maxEmailsPorCiclo, ILogger, DateTime agora, CancellationToken)` (mesmo padrão de `MensalidadeDispatcherService.GerarCobrancasDoMesAsync`, spec 039) em `src/SPI.Infrastructure/BackgroundServices/LembreteDispatcherService.cs`; criar `tests/SPI.Application.Tests/Lembretes/LembreteFakes.cs` com fakes de `ILembreteRepository` e `IEmailSender` (capturando destinatário de cada envio, para as asserções de orçamento)
- [ ] T010 [P] [US2] Teste: um lembrete com destinatários acima do `maxEmailsPorCiclo` configurado — uma primeira chamada envia só até o limite, `Status` permanece `"Pendente"`, `DestinatariosTentados` contém exatamente os endereços tentados nessa chamada em `tests/SPI.Application.Tests/Lembretes/LembreteDispatcherOrcamentoTests.cs`
- [ ] T011 [P] [US2] Teste: uma segunda chamada (simulando o próximo ciclo) sobre o mesmo lembrete envia só os destinatários que faltavam (nunca repete os já tentados), e quando o último destinatário é tentado, `Status` vira `"Enviado"` (se pelo menos um envio teve sucesso) ou `"Falha"` (se nenhum) em `tests/SPI.Application.Tests/Lembretes/LembreteDispatcherOrcamentoTests.cs`
- [ ] T012 [P] [US2] Teste negativo (FR-005): confirmar, por inspeção do código/assinatura, que `ProcessarLembretesVencidosAsync` não contém nenhum `Task.Delay` — o teste chama o método duas vezes em sequência (simulando dois ciclos) sem nenhuma espera entre as chamadas e confirma que ambas completam imediatamente (sem travar) em `tests/SPI.Application.Tests/Lembretes/LembreteDispatcherOrcamentoTests.cs`
- [ ] T013 [P] [US2] Teste (FR-004): dois lembretes vencidos com `HoraProgramada` diferentes, destinatários somados acima do `maxEmailsPorCiclo` — confirmar que o lembrete com `HoraProgramada` mais antiga consome o orçamento primeiro em `tests/SPI.Application.Tests/Lembretes/LembreteDispatcherOrcamentoTests.cs`
- [ ] T014 [P] [US2] Teste negativo (FR-006): confirmar que `ObterEmailsDestinatarios` e os critérios de elegibilidade (`Status == "Pendente"`, `HoraProgramada <= agora`, `Ativo`) não foram alterados — mesmo resultado de antes desta feature para os mesmos dados de entrada, em `tests/SPI.Application.Tests/Lembretes/LembreteDispatcherOrcamentoTests.cs`

### Implementation for User Story 2

- [ ] T015 [US2] Em `src/SPI.Domain/Entities/Lembrete.cs`, adicionar `public string? DestinatariosTentados { get; set; }` e `public int EnviosComSucesso { get; set; }` (padrão `0`)
- [ ] T016 [US2] Em `src/SPI.Infrastructure/Persistence/Configurations/LembreteConfiguration.cs`, mapear as 2 colunas novas (`destinatarios_tentados` nullable, `envios_com_sucesso` com `HasDefaultValue(0)`) (depende de T015)
- [ ] T017 [US2] Em `src/SPI.Infrastructure/Repositories/LembreteRepository.cs`, adicionar `.OrderBy(l => l.HoraProgramada)` a `ListarPendentesVencidosAsync` (research.md R3)
- [ ] T018 [US2] Em `src/SPI.Infrastructure/BackgroundServices/LembreteDispatcherService.cs`, implementar `ProcessarLembretesVencidosAsync` (assinatura de T009): ler `Lembretes:MaxEmailsPorCiclo` da configuração (padrão `10` se ausente); para cada lembrete vencido (já ordenado por T017), calcular `destinatarios = ObterEmailsDestinatarios(lembrete) - (DestinatariosTentados?.Split(';') ?? [])`; enviar até `min(destinatarios.Count, orçamentoRestante)`; acumular os tentados em `DestinatariosTentados` (`;`-separado) e os sucessos em `EnviosComSucesso`; decrementar o orçamento restante; parar de processar novos lembretes quando o orçamento chegar a 0 (os restantes continuam `"Pendente"` para o próximo ciclo); quando `DestinatariosTentados` cobrir 100% dos destinatários originais, definir `Status = EnviosComSucesso > 0 ? "Enviado" : "Falha"` — método privado existente (`ExecuteAsync`) passa a só resolver as dependências via `_scopeFactory` e chamar o método estático (depende de T009, T015, T017)

**Checkpoint**: User Story 2 completa e testável de forma independente.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão e verificação dos requisitos "MUST NOT" (regra do `CLAUDE.md`).

- [ ] T019 (parte manual) [US2] Validar os §2 e §3 de [quickstart.md](./quickstart.md): lembrete com destinatários acima do limite dividido entre ciclos reais (app rodando), retomada parcial confirmada via `SELECT` no MySQL, priorização por `HoraProgramada` confirmada com dois lembretes (FR-003/FR-004, SC-002)
- [ ] T020 Regra do `CLAUDE.md` para "não criar arquivo além do necessário": rodar `git status --porcelain -- src tests database` e confirmar que os únicos arquivos não rastreados são os intencionais desta feature (`SmtpOptions.cs`, `SmtpEmailSender.cs`, `database/18_*.sql`, `tests/SPI.Application.Tests/Lembretes/LembreteFakes.cs`, `LembreteDispatcherOrcamentoTests.cs`) — nenhum outro
- [ ] T021 Regra do `CLAUDE.md` para FR-007 ("MUST NOT deixar referência a Brevo"): rodar `grep -rln "Brevo" src/ --include=*.cs --include=*.json` (fora de `bin`/`obj`) e confirmar saída vazia (SC-004)
- [ ] T022 Regra do `CLAUDE.md` para FR-006 ("MUST NOT alterar regra de elegibilidade/conteúdo"): ler diretamente o diff de `LembreteDispatcherService.cs`/`LembreteRepository.cs` e confirmar que `ObterEmailsDestinatarios`, `MontarCorpoEmail`, e os critérios do `Where` de `ListarPendentesVencidosAsync` (além da nova `OrderBy`) não foram alterados (SC-003)
- [ ] T023 Rodar `dotnet test tests/SPI.Application.Tests` completo e confirmar 100% verde, incluindo os testes novos de T010-T014

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 bloqueia T002. Não bloqueia a Fase 2.
- **Foundational (Phase 2)**: T003/T004 em paralelo; T005/T006 dependem deles; T007 independente. T003-T007 bloqueiam as Fases 3 e 4.
- **User Story 1 (Phase 3)**: depende só da Fase 2.
- **User Story 2 (Phase 4)**: depende da Fase 2 (renomeação) e da Fase 1 (migração aplicada, T002) para a validação manual (T019); os testes (T009-T014) e a implementação (T015-T018) não dependem da migração estar aplicada no banco real (fakes em memória), só do schema do `Lembrete.cs` (T015).
- **Polish (Phase 5)**: depende das Fases 3 e 4 completas.

### Parallel Opportunities

- T003/T004/T007 em paralelo (arquivos distintos).
- T009-T014 (testes de US2) em paralelo entre si, antes de T015+.
- T008 (US1) e toda a Fase 4 (US2) podem ser trabalhadas em paralelo — stories independentes.

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (T001-T002) e Phase 2 (T003-T007 — renomeação)
2. Completar Phase 3 (T008) — recuperação de senha/2FA via Gmail, entregável como MVP
3. Phase 4 (US2, orçamento de lembretes) e Phase 5 (Polish) são a entrega incremental seguinte

### Incremental Delivery

Setup + Foundational (T001-T007) → US1 (T008, MVP) → US2 (T009-T019) → Polish (T020-T023).

## Notes

- Nenhuma tarefa desta lista deve alterar o conteúdo/formato de nenhum e-mail, a regra de elegibilidade de lembretes, ou introduzir `Task.Delay` bloqueante (FR-005/FR-006).
- A lógica testável de `LembreteDispatcherService` é extraída como método estático (T009), seguindo exatamente o precedente de `MensalidadeDispatcherService.GerarCobrancasDoMesAsync` (spec 039) — não é uma abstração nova para este projeto.
