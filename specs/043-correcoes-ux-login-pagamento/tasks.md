---

description: "Lista de tarefas para Três Correções de UX (Login, Recuperação de Senha, Status de Pagamento)"
---

# Tasks: Três Correções de UX (Login, Recuperação de Senha, Status de Pagamento)

**Input**: Documentos de design em `/specs/043-correcoes-ux-login-pagamento/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/login-submit-contract.md](./contracts/login-submit-contract.md), [contracts/payment-status-confirm-contract.md](./contracts/payment-status-confirm-contract.md), [contracts/email-delivery-verification.md](./contracts/email-delivery-verification.md), [quickstart.md](./quickstart.md)

**Tests**: SEM suíte automatizada (research.md R4) — o projeto não tem testes de frontend hoje, e a mudança de configuração (US1) só é verificável com um envio real. Validação via `quickstart.md`, contra o app rodando de verdade. Tarefas de validação manual carregam `(parte manual)`.

**Organization**: Tarefas agrupadas por user story (US1=P1 e-mail de recuperação realmente entregue; US2=P1 confirmação antes de mudar status de pagamento; US3=P2 Enter submete o login).

**Nota estrutural importante**: as 3 correções são, por desenho do próprio pedido original, **pequenas e independentes** — não compartilham nenhum código, componente ou configuração entre si. Por isso **não há Fase 2 (Foundational)**: nada bloqueia nada; as 3 user stories podem ser implementadas em qualquer ordem, inclusive em paralelo por pessoas diferentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1, US2 ou US3 (só nas fases de user story)
- Caminhos relativos à raiz do repositório `C:\PROJETO - SPI`

## Path Conventions

Web app em camadas já existente: `frontend/` (Next.js App Router) para US2/US3; `src/SPI.Api` (User Secrets, sem código) para US1. Nenhum arquivo novo de componente — US2 reaproveita `ConfirmModal`/`StatusPill` já existentes.

---

## Phase 1: Setup

**Purpose**: confirmar a linha de base antes de qualquer mudança.

- [ ] T001 (parte manual) Com o backend e o frontend no ar, confirmar que o login atual (clique no botão "Entrar") e a tela de detalhe de Contas a Pagar/Receber carregam normalmente — linha de base antes de qualquer edição.

---

## Phase 2: User Story 1 - Recuperação de senha realmente chega no e-mail (Priority: P1) 🎯 MVP

**Goal**: o e-mail de recuperação de senha chega de verdade numa caixa de entrada real, uma vez configuradas as credenciais SMTP da Brevo via User Secrets.

**Independent Test**: com credenciais reais configuradas, pedir a recuperação de senha para uma conta de teste cadastrada e confirmar o recebimento real do e-mail (quickstart.md §2 / contracts/email-delivery-verification.md).

### Implementation for User Story 1

- [X] T002 [US1] (parte manual) Pedir à pessoa usuária as credenciais reais da Brevo (SMTP login, SMTP key, e-mail remetente — regra de não persistir, descartar ao final) e configurar via `dotnet user-secrets set "Brevo:SmtpUsuario" "<valor>" --project src/SPI.Api`, idem `Brevo:SmtpChave` e `Brevo:RemetenteEmail` (nunca em `appsettings.json`/`appsettings.Development.json` — Princípio IV).
- [X] T003 [US1] (parte manual) Com o backend rodando e as 3 chaves configuradas, executar os 5 passos de [contracts/email-delivery-verification.md](./contracts/email-delivery-verification.md): `POST /api/auth/esqueci-senha` para uma conta de teste cadastrada e ativa, confirmar o e-mail recebido de verdade; repetir para um e-mail inexistente e confirmar resposta idêntica (FR-003); se algo falhar, checar o log do backend (FR-004).
- [X] T004 [US1] **Resultado de T003**: `POST /api/auth/esqueci-senha` devolveu `200` com a mensagem genérica correta (FR-003), mas o e-mail não chegou — o log do backend registrou `System.TimeoutException: Operation timed out after 120000 milliseconds` dentro de `MailKit.Net.Smtp.SmtpClient.ConnectAsync` (`BrevoEmailSender.cs:27`), ou seja, FR-004 (log identificável, sem mudar a resposta) já se confirmou funcionando. **Investigação ampliada a pedido do usuário** (testar 2525/465 antes de mudar código): o mesmo sintoma (TCP conecta, banner SMTP nunca chega) se repete nas 3 portas contra a Brevo, mas **SMTP para `smtp.gmail.com:587` funciona normalmente** e **HTTPS para o próprio host da Brevo funciona** — descartando bloqueio de porta ou de host/IP em geral. O padrão isolado é tráfego SMTP especificamente para a Brevo sendo descartado, típico de antivírus/firewall local com heurística de reputação contra relays de e-mail em massa. **Nenhuma mudança de código feita** (trocar porta/modo SSL não ajudaria, já testado) — `BrevoEmailSender.cs`/`RecuperacaoSenhaService.cs` continuam corretos. Decisão do usuário: documentar como limitação de ambiente conhecida e seguir em frente — a entrega real (SC-001) fica pendente de confirmação num ambiente sem esse bloqueio.

**Checkpoint**: User Story 1 funcional e testável de forma independente — MVP entregue (e-mail de recuperação funcionando de ponta a ponta).

---

## Phase 3: User Story 2 - Botões de status de pagamento deixam claro que é uma ação, não um filtro (Priority: P1)

**Goal**: nas telas de detalhe de Conta a Pagar/Receber, o status atual aparece como selo (não clicável) e as transições aparecem como botões de ação com rótulo de verbo, exigindo confirmação explícita antes de aplicar.

**Independent Test**: abrir o detalhe de uma conta, clicar numa transição de status, confirmar que aparece um passo de confirmação; cancelar e confirmar que nada muda; confirmar e ver o status mudar como hoje (quickstart.md §3).

### Implementation for User Story 2

- [X] T005 [P] [US2] Em `frontend/app/(app)/financeiro/contas-a-pagar/[id]/page.tsx`: importar `ConfirmModal` de `@/components/shared/ConfirmModal`; adicionar o estado `const [statusEmConfirmacao, setStatusEmConfirmacao] = useState<(typeof STATUS_OPCOES)[number] | null>(null);`; substituir o bloco atual que mapeia `STATUS_OPCOES` em 4 botões (`btn-primary`/`btn-ghost`) por: (a) `<StatusPill status={conta.status} />` mostrando o status atual (não clicável — satisfaz FR-009 estruturalmente, sem precisar de `disabled`); (b) só os **3 outros** valores de `STATUS_OPCOES` (`STATUS_OPCOES.filter((s) => s !== conta.status)`) como botões de ação (`btn btn-sm`), com rótulo de verbo fixo — `Pendente` → `"Marcar como Pendente"`, `Pago` → `"Marcar como Pago"`, `Atrasado` → `"Marcar como Atrasado"`, `Cancelado` → `"Cancelar conta"` — cujo `onClick` chama `setStatusEmConfirmacao(s)` em vez de `handleStatus(s)` diretamente. Logo depois do `mini-panel`, renderizar `{statusEmConfirmacao && <ConfirmModal .../>}` com `titulo` = o rótulo de verbo escolhido, `descricao` = o texto exato da tabela "Textos de confirmação por transição" em [data-model.md](./data-model.md) para `statusEmConfirmacao`, `onCancelar` = `() => setStatusEmConfirmacao(null)` (sem nenhuma chamada de rede — FR-007), `onConfirmar` = chama `handleStatus(statusEmConfirmacao)` e depois `setStatusEmConfirmacao(null)`, `confirmando={processando}`, `confirmLabel` = o mesmo rótulo de verbo, `confirmVariant="btn-danger"` só para `"Cancelado"`, `"btn-primary"` para os demais.
- [X] T006 [P] [US2] Mesma mudança, ponto a ponto, em `frontend/app/(app)/financeiro/contas-a-receber/[id]/page.tsx` (arquivo irmão, mesma estrutura hoje — `pagamento.status` no lugar de `conta.status`).
- [ ] T007 [US2] (parte manual) Validar User Story 2 rodando [quickstart.md](./quickstart.md) §3 nas duas telas: status atual aparece como selo (não parece mais filtro), cada transição pede confirmação com o texto certo, cancelar não chama a rede (conferir na aba Network do navegador), confirmar aplica a mudança exatamente como antes (incluindo a data de pagamento automática ao marcar Pago). **Inclui a verificação de FR-012**: antes de confirmar uma transição, anotar o anexo de comprovante e o texto de observações da conta (quando houver); depois de confirmar, conferir que os dois continuam idênticos — só o `status` (e a data de pagamento, quando aplicável) mudou.

**Checkpoint**: User Stories 1 e 2 completas e independentes entre si.

---

## Phase 4: User Story 3 - Enter nos campos de login submete o formulário (Priority: P2)

**Goal**: confirmar que Enter já submete o login (comportamento esperado do HTML padrão, já implementado) e fechar a única lacuna real encontrada — ausência de proteção contra duplo-envio.

**Independent Test**: preencher login e apertar Enter em vez de clicar — resultado idêntico ao clique; apertar Enter várias vezes seguidas durante um envio em andamento não dispara requisições extras (quickstart.md §1).

### Implementation for User Story 3

- [X] T008 [US3] Em `frontend/components/auth/LoginForm.tsx`, função `handleSubmit`: logo após `event.preventDefault();` (antes de qualquer outra linha, inclusive a validação de campos vazios), adicionar `if (carregando) return;` — garante que nenhum segundo envio (via Enter repetido ou clique repetido) dispare enquanto `carregando` já é `true` (FR-011).
- [ ] T009 [US3] (parte manual) Validar User Story 3 rodando [quickstart.md](./quickstart.md) §1: Enter em cada um dos 2 campos submete como o clique; campos vazios mostram a mesma mensagem de validação; Enter repetido durante um envio lento (DevTools → Network → Slow 3G) dispara só 1 requisição (conferir na aba Network).

**Checkpoint**: As 3 user stories completas e validadas de forma independente.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão final, comum às 3 correções.

- [X] T010 [P] Rodar `npx tsc --noEmit` em `frontend/` e confirmar 0 erros após as mudanças de US2/US3.
- [ ] T011 [P] (parte manual) Rodar [quickstart.md](./quickstart.md) §4 (não-regressão): `git diff --stat -- database` vazio (nenhuma migração nesta feature); confirmar por leitura que `PagamentoService.cs`/`AtualizarStatusPagamentoRequestValidator.cs`/stored procedures não foram tocados; login com credencial inválida continua mostrando a mesma mensagem de erro, por clique e por Enter.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências — roda primeiro.
- **US1 (Phase 2)**, **US2 (Phase 3)** e **US3 (Phase 4)**: todas dependem só do Setup — **sem Fase Foundational**, pois as 3 user stories não compartilham nenhum código (ver Nota estrutural). Podem ser feitas em qualquer ordem, inclusive simultaneamente.
- **Polish (Phase 5)**: depende das 3 user stories completas.

### Parallel Opportunities

- US1, US2 e US3 inteiras podem ser feitas em paralelo entre si (arquivos e sistemas completamente disjuntos: User Secrets/backend de e-mail vs. duas páginas de Financeiro vs. `LoginForm.tsx`).
- Dentro de US2: T005 e T006 em paralelo (arquivos irmãos, mesma mudança).
- Polish: T010 e T011 em paralelo.

---

## Implementation Strategy

### MVP First (User Story 1 apenas)

1. Setup (T001).
2. US1 (T002-T004) → **PARAR e VALIDAR**: e-mail de recuperação funcionando de ponta a ponta — já é o item de maior risco resolvido.

### Incremental Delivery

Como as 3 stories são independentes, a ordem abaixo é só uma sugestão (por prioridade P1/P1/P2), não uma dependência real:

1. Setup → linha de base.
2. US1 → recuperação de senha funcionando de verdade (MVP, maior risco).
3. US2 → confirmação antes de mudar status de pagamento (maior risco de dado alterado sem querer).
4. US3 → Enter no login (menor risco, já era esperado funcionar).
5. Polish → checagem final de tipo e não-regressão.
