# Implementation Plan: Três Correções de UX (Login, Recuperação de Senha, Status de Pagamento)

**Branch**: `043-correcoes-ux-login-pagamento` | **Date**: 2026-10-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/043-correcoes-ux-login-pagamento/spec.md`

## Summary

Três correções pequenas e independentes, todas já com grounding de código feito na especificação:

1. **Login (US3/P2)**: `LoginForm.tsx` já submete com Enter (form/onSubmit/button type="submit" corretos, nenhum keydown interceptando) — nenhuma mudança de comportamento de Enter é esperada; a única lacuna real encontrada é a ausência de uma guarda contra duplo-envio (`handleSubmit` não verifica se já está `carregando`), que afeta tanto Enter quanto clique repetido.
2. **Recuperação de senha (US1/P1)**: fluxo de backend já correto (`RecuperacaoSenhaService` + `BrevoEmailSender` via MailKit/SMTP); a única lacuna é de configuração — `BrevoOptions.SmtpUsuario`/`SmtpChave`/`RemetenteEmail` nunca foram preenchidos neste ambiente. A correção é configurar via User Secrets (nunca `appsettings.json`) e provar o envio de ponta a ponta com um e-mail de teste real.
3. **Botões de status de pagamento (US2/P1)**: hoje os 4 valores (Pendente/Pago/Atrasado/Cancelado) são renderizados como um grupo `btn-primary`/`btn-ghost` lado a lado — visualmente um seletor de filtro. A correção separa a **exibição do status atual** (reaproveitando o componente `StatusPill` já usado em outras telas) das **ações de transição** (só os 3 outros valores, como botões de ação com rótulo de verbo — "Marcar como Pago", não "Pago") e exige confirmação explícita (reaproveitando o componente `ConfirmModal` já existente) antes de aplicar qualquer transição.

## Technical Context

**Language/Version**: TypeScript/Next.js App Router (frontend, itens 1 e 3); C#/.NET 10 + User Secrets (item 2, só configuração — sem mudança de código esperada a priori). Mesma stack já vigente, nenhuma dependência nova.

**Primary Dependencies**: Nenhuma nova. Reaproveita `ConfirmModal` e `StatusPill` (`frontend/components/shared/`), já usados em outras telas.

**Storage**: Nenhuma mudança de schema. `BrevoOptions` já existe como seção de configuração (`Brevo` em `appsettings.json` + User Secrets); nenhum campo novo.

**Testing**: Sem suíte automatizada de UI (o projeto não tem testes de frontend hoje). Item 1 e 3 validados manualmente no navegador (`quickstart.md`); item 2 validado com um envio real de e-mail usando credenciais reais da Brevo (fornecidas temporariamente durante a implementação, descartadas ao final — mesma regra já em uso nesta sessão).

**Target Platform**: Web (frontend Next.js já em produção/dev local) + backend ASP.NET Core já em execução.

**Project Type**: Web application (frontend + configuração de backend) — segue a estrutura já existente (`frontend/app/(app)/...`, `frontend/components/`, `src/SPI.Api` User Secrets).

**Performance Goals**: Sem meta nova.

**Constraints**:
- FR-003/FR-004: a correção do item 2 MUST NOT alterar a resposta observável do endpoint `POST /api/auth/esqueci-senha` (sempre a mesma, exista ou não o e-mail) — único "código" tocado é a configuração (User Secrets), não a lógica do `RecuperacaoSenhaService`.
- FR-012: a correção do item 3 MUST NOT introduzir nenhuma restrição nova de transição de status no backend (`PagamentoService.AtualizarStatusAsync`/`sp_atualizar_status_pagamento` já aceitam qualquer transição e não têm regra a duplicar) — a confirmação é 100% de interface, sem lógica de negócio nova.
- Princípio IV (segredos): `BrevoOptions.SmtpUsuario`/`SmtpChave`/`RemetenteEmail` MUST ir para User Secrets (`dotnet user-secrets set`), nunca para `appsettings.json`/`appsettings.Development.json`.

**Scale/Scope**: 1 guarda contra duplo-envio em `LoginForm.tsx`; 3 chaves novas em User Secrets (sem código novo, a priori); reestruturação de uma seção de UI em 2 páginas quase idênticas (`contas-a-pagar/[id]` e `contas-a-receber/[id]`), reaproveitando componentes já existentes. Nenhum arquivo novo de componente (a menos que a investigação do item 1 revele a necessidade, o que não é esperado).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhuma exclusão envolvida; "Cancelado" já existe como um status, não uma exclusão física do registro (comportamento preexistente, preservado). |
| II. Validação de Negócio Única e Centralizada no Backend | Sim | A confirmação de status (item 3) é só uma pausa de interface antes de reenviar a mesma chamada que já existe (`atualizarStatusContaPagar`) — nenhuma regra de "quais transições são permitidas" é adicionada no frontend; o backend continua sendo o único dono de qualquer regra futura sobre transições. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim | `BrevoOptions` já é uma capacidade real e implementada (SMTP de verdade) — esta feature só preenche a configuração que faltava; nenhuma opção nova é anunciada sem implementação. |
| IV. Autenticação e Segredos Seguros por Padrão | Sim — central | As 3 credenciais da Brevo MUST ir para User Secrets, nunca `appsettings.json`. Reforça o padrão já usado para `Jwt:Key` e `ConnectionStrings:SpiDB`. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Nenhum comportamento documentado anteriormente é superado — item 2 corrige uma lacuna de configuração de ambiente, não uma regra de negócio já especificada em outra spec. |

**Resultado**: Sem violação.

## Project Structure

### Documentation (this feature)

```text
specs/043-correcoes-ux-login-pagamento/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (confirma: nenhuma entidade nova)
├── quickstart.md        # Phase 1 output
├── contracts/            # Phase 1 output
│   ├── login-submit-contract.md
│   ├── payment-status-confirm-contract.md
│   └── email-delivery-verification.md
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
frontend/
├── components/auth/LoginForm.tsx                         # + guarda contra duplo-envio
├── app/(app)/financeiro/contas-a-pagar/[id]/page.tsx      # StatusPill + acoes + ConfirmModal
└── app/(app)/financeiro/contas-a-receber/[id]/page.tsx    # idem (mesmo padrao, arquivo irmao)

src/SPI.Api/
└── (User Secrets, fora do repositorio -- Brevo:SmtpUsuario/SmtpChave/RemetenteEmail)
```

**Structure Decision**: Nenhum diretório novo. Reaproveita componentes compartilhados já existentes (`ConfirmModal`, `StatusPill`) em vez de criar variações novas — segue o padrão já usado nas demais telas do sistema.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, contracts/, quickstart.md).*

- Princípio II: `contracts/payment-status-confirm-contract.md` confirma que nenhuma regra de transição é duplicada no frontend — só a interação de confirmação.
- Princípio IV: `research.md` documenta exatamente os 3 valores que vão para User Secrets, nenhum em `appsettings.json`.
- Nenhuma violação nova introduzida pelo design.

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
