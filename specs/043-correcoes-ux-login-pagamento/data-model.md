# Data Model: Três Correções de UX

**Nenhuma entidade nova, nenhum DTO novo, nenhuma alteração de schema.** Os 3 itens desta feature operam sobre comportamento de interface e configuração, reaproveitando entidades/DTOs/endpoints já existentes.

## Entidades reutilizadas (sem alteração de estrutura)

| Entidade/DTO | Uso nesta feature |
|---|---|
| `Professor`/`Aluno` (login) | Nenhuma mudança — `LoginForm.tsx` continua chamando o mesmo `login()` de `frontend/lib/api/auth.ts`. |
| `SenhaResetToken` (recuperação de senha) | Nenhuma mudança — `RecuperacaoSenhaService` continua gerando e validando o token exatamente como hoje. |
| `BrevoOptions` (configuração, não persistida em banco) | Preenchida via User Secrets: `SmtpUsuario`, `SmtpChave`, `RemetenteEmail` (hoje `string.Empty` por padrão, nunca configurados neste ambiente). `SmtpHost`/`SmtpPorta`/`RemetenteNome` já têm valor em `appsettings.json`, inalterados. |
| `Pagamento` / Conta a Pagar-Receber (status) | Nenhuma mudança de schema nem de regra — `AtualizarStatusAsync`/`sp_atualizar_status_pagamento` continuam aceitando qualquer transição entre os 4 valores, exatamente como hoje. Só a interface que leva até essa chamada muda. |

## Estado de UI novo (só no frontend, nada persistido)

| Campo (estado local do componente) | Tela | Descrição |
|---|---|---|
| `statusEmConfirmacao: string \| null` | `contas-a-pagar/[id]`, `contas-a-receber/[id]` | Qual dos 3 status de transição está com o `ConfirmModal` aberto; `null` = nenhum modal aberto. Nunca enviado ao backend — só controla a UI. |

## Textos de confirmação por transição (FR-006)

Reaproveita o texto de aviso já existente hoje (fora dos botões), agora contextualizado dentro do `ConfirmModal` no momento da decisão:

| Transição | Texto da confirmação |
|---|---|
| → Pago | "Marcar esta conta como Pago? A data de pagamento será preenchida automaticamente com a data de hoje." |
| → Pendente | "Marcar esta conta como Pendente novamente?" |
| → Atrasado | "Marcar esta conta como Atrasado?" |
| → Cancelado | "Cancelar esta conta? O registro não será excluído, só encerrado — você pode ver o histórico depois, mas ele deixa de contar como pendente ou pago." |

## Validações (dono único, sem duplicação)

| Regra | Onde |
|---|---|
| Quais transições de status são permitidas | `PagamentoService.AtualizarStatusAsync`/`sp_atualizar_status_pagamento` (backend, já existente, inalterado) — o frontend nunca decide isso, só exibe a confirmação e repassa a escolha. |
| Resposta idêntica para e-mail existente/inexistente na recuperação de senha | `RecuperacaoSenhaService.EsqueciSenhaAsync` (backend, já existente, inalterado — FR-003/FR-004). |
| Nenhum segundo envio de login enquanto um está em andamento | `LoginForm.handleSubmit` (frontend, guarda nova — FR-011). |
