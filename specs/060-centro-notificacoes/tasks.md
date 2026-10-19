---

description: "Task list for Centro de Notificações e Aba Lembretes"
---

# Tasks: Centro de Notificações (Aulas e Pagamentos) e Aba Lembretes

**Input**: Design documents from `specs/060-centro-notificacoes/`

**Tests**: incluídos (xUnit com fakes). Cada "MUST NOT" tem verificação explícita, conforme o `CLAUDE.md`:
- FR-003: sem notificação duplicada;
- FR-007: sem e-mail duplicado nem à professora;
- FR-015: sem e-mail real em desenvolvimento.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup e segurança de e-mail (US6, bloqueante)

- [X] T001 Backup do banco local. Criar `database/23_centro_notificacoes.sql` conforme data-model.md:
  - `notificacao.aluno_id` + FK + índice único;
  - tabela `regra_lembrete_pagamento`, com o `INSERT` da linha padrão;
  - tabela `envio_lembrete_pagamento`, com o índice único.
  Aplicar no MySQL local e conferir com `SHOW CREATE TABLE`.
- [X] T002 [US6] `SmtpOptions.RedirecionarTodosPara` e `SmtpEmailSender`:
  - receber `IHostEnvironment`;
  - método estático e puro `ResolverEnvio(...)` (research.md R5), aplicado a todo envio.
  Gravar o User Secret `Smtp:RedirecionarTodosPara` local com o endereço de teste do usuário. Nunca gravar o endereço em `appsettings*.json`.
- [X] T003 [P] [US6] Testes em `tests/SPI.Application.Tests/Email/SmtpEmailSenderRedirecionamentoTests.cs`:
  - Development com endereço de teste → envia para ele, com o original no assunto;
  - Development sem endereço → exceção (FR-015, "MUST NOT");
  - Production → destinatário real.

## Phase 2: Foundational

- [X] T004 Domain:
  - `TipoNotificacao` + `AulaFutura`, `PagamentoPendente`;
  - `Notificacao.AlunoId`/`Aluno`;
  - entidades `RegraLembretePagamento` e `EnvioLembretePagamento`;
  - repositórios:
    - `IAulaRepository.ListarNaJanelaDeAvisoAsync(agora, diasAntes, hora, professorId?)`;
    - `IPagamentoRepository.ListarPendentesVencidosAsync(hoje)`, com Aluno e VinculoCobranca;
    - `INotificacaoRepository.ListarPorAlunosAsync(tipo, professorId, alunoIds)`;
    - `ILembretePagamentoRepository`: regra, envios do dia, envios recentes, adicionar e salvar.
- [X] T005 Infrastructure:
  - configurações EF (snake_case, enum como string);
  - `DbSet`s;
  - implementações dos repositórios: a janela de aula futura segue research.md R3; `ListarAtivasPorProfessorAsync` inclui o Aluno;
  - registrar em `AddInfrastructure`;
  - atualizar os fakes de teste.

## Phase 3: US1 + US3 + US4 — Notificações de pagamento e aula futura (P1/P2)

- [X] T006 [US1] `NotificacaoService`:
  - `SincronizarAulasPendentesAsync` → `SincronizarAsync`, com os três tipos (research.md R2);
  - extrair o laço criar/reativar/resolver por aula num método privado, para não duplicar entre `AulaPendente` e `AulaFutura`;
  - laço de pagamento por aluno, com a professora do token ou a única cadastrada;
  - `NotificacoesOptions` + `AulaFuturaDiasAntes`/`AulaFuturaHora`;
  - DTO de pagamento com pendências (dias de atraso = hoje − vencimento);
  - mensagens por tipo;
  - atualizar `NotificacaoDispatcherService` e o `appsettings.json`.
- [X] T007 [P] [US1] Testes de pagamento em `tests/SPI.Application.Tests/Notificacoes/NotificacaoServicePagamentoTests.cs`:
  - pendência vencida gera 1 por aluno, com as pendências e os dias de atraso;
  - vence hoje não conta (FR-001);
  - FR-003 ("MUST NOT duplicar"): sincronizar 3 vezes continua 1;
  - pagar tudo resolve;
  - nova pendência reativa a mesma;
  - dois alunos geram duas.
- [X] T008 [P] [US3] Testes de aula futura em `NotificacaoServiceAulaFuturaTests.cs`:
  - antes das 08:00 da véspera não aparece; às 08:00 aparece;
  - cancelada ou terminada resolve;
  - remarcada para fora da janela resolve e volta na nova véspera (mesmo id);
  - não duplica com o tipo `AulaPendente` da mesma aula.
- [X] T009 [US4] Atualizar `NotificacaoServiceTests.cs` (059) para `SincronizarAsync`. Os testes de aula pendente continuam válidos, mas passam a olhar só o tipo `AulaPendente`: uma aula de hoje que ainda não terminou agora também tem aviso de aula futura.

## Phase 4: US2 + US5 — E-mail semanal e aba Lembretes (P1/P2)

- [X] T010 [US2] `Lembretes/Services/LembretePagamentoService.cs` (research.md R4):
  - `ProcessarAsync(agora)`;
  - `ObterRegraAsync`, `AtualizarRegraAsync` (validação: `diaSemana` entre 0 e 6);
  - `ListarEnviosAsync(limite)`;
  - corpo do e-mail com as pendências.
  Registrar no `AddApplication`. A rotina `LembretePagamentoDispatcherService` usa `LembretePagamento:IntervaloVerificacaoSegundos`.
- [X] T011 [P] [US2] Testes em `tests/SPI.Application.Tests/Lembretes/LembretePagamentoServiceTests.cs`:
  - fora do dia ou antes da hora: nada;
  - no dia e hora: um envio por aluno, para aluno e responsável, sem repetir endereço;
  - vence no próprio dia: não entra;
  - FR-007 ("MUST NOT duplicar"): a segunda execução no mesmo dia não reenvia; nenhum destinatário é e-mail de professora;
  - falha registra e tenta de novo até 3 vezes;
  - sem e-mail → `SemDestinatario`;
  - regra desligada → nada;
  - validação de `diaSemana`.
- [X] T012 [US5] `LembretesController`: `GET`/`PUT pagamento-pendente/regra` e `GET pagamento-pendente/envios`.

## Phase 5: Frontend (US4/US5)

- [X] T013 [US4]
  - `frontend/lib/api/notificacoes.ts`: tipos novos e `pagamento`;
  - `NotificacoesSino.tsx`:
    - abas Todas / Aulas / Pagamentos;
    - rótulo por tipo;
    - popup de pagamento com tabela de pendências e link `/financeiro/contas-a-receber/{id}`;
    - popup de aula futura com "Abrir aula";
    - o contador soma todas as não lidas.
  - Estilos só com tokens.
- [X] T014 [US5]
  - `frontend/lib/api/lembretes.ts`: regra e envios;
  - `frontend/components/lembretes/LembretePagamentoSection.tsx`: ligado/desligado, dia da semana, horário, botão salvar e a explicação "repete toda semana até a situação ser atualizada", mais o histórico (data e hora, aluno, destinatários, resultado, tentativas);
  - incluir a seção na aba Lembretes.

## Phase 6: Polish

- [X] T015 FR-003/FR-007 ("MUST NOT"): ler o diff de `NotificacaoService`/`LembretePagamentoService` e confirmar:
  - criação de notificação só depois de checar a existência pela chave;
  - envio só depois de checar o registro do dia;
  - destinatários só `Aluno.Email`/`Aluno.EmailResponsavel`.
- [X] T016 FR-015 ("MUST NOT"): `grep` nos `appsettings*.json` confirmando que nenhum endereço de teste foi gravado; ler o diff de `SmtpEmailSender` e confirmar que todo caminho de envio passa por `ResolverEnvio`.
- [X] T017 Nota na spec 059: o sino virou centro de notificações com tipos (060).
- [X] T018 Quickstart, cenários 1-11, com a API local (SMTP inválido, dados "TESTE-060" apagados e regra padrão restaurada ao final).
- [X] T019 `git status --porcelain -- src tests frontend database`: só os arquivos novos do plano. `dotnet build`, `dotnet test`, `tsc`, `eslint` nos arquivos tocados, `npm run build` e `npm run verificar-contraste`.
- [ ] T020 (parte manual) Conferir o sino e a aba Lembretes num navegador real. Opcionalmente, com credenciais SMTP reais, confirmar que o e-mail chega só ao endereço de teste.
