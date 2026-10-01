# Contrato: Confirmação de mudança de status (Conta a Pagar/Receber)

Sem mudança de endpoint — `atualizarStatusContaPagar`/`atualizarStatusContaReceber` (`PUT`/`PATCH` já existente) continuam exatamente iguais, com o mesmo payload. O contrato aqui é de **comportamento de interface**, idêntico nas duas telas (`contas-a-pagar/[id]`, `contas-a-receber/[id]`).

## Estrutura visual (antes → depois)

| | Antes | Depois |
|---|---|---|
| Status atual | Um dos 4 botões, estilo `btn-primary`, desabilitado | `StatusPill` (componente já existente, só leitura) |
| Transições possíveis | Os outros 3 botões, estilo `btn-ghost`, rótulo = nome do status | Botões de ação com rótulo de verbo (ex.: "Marcar como Pago"), visualmente distintos de um filtro |
| Confirmação | Nenhuma — clique aplica na hora | `ConfirmModal` (componente já existente) com texto específico da transição (ver [data-model.md](../data-model.md)) |

## Fluxo

1. Professora clica num botão de ação de transição (não o status atual, que não é clicável).
2. `ConfirmModal` abre com `titulo`/`descricao` da transição específica.
3. **Cancelar** → modal fecha, `statusEmConfirmacao` volta a `null`, nenhuma chamada ao backend (FR-007).
4. **Confirmar** → chama `handleStatus(novoStatus)` exatamente como hoje (mesma função, mesmo endpoint), modal fecha ao receber a resposta.

## Não-regressão

- `atualizarStatusContaPagar`/`atualizarStatusContaReceber`: mesmo contrato de request/response.
- Preenchimento automático da data de pagamento ao marcar "Pago": continua sendo responsabilidade do backend (`sp_atualizar_status_pagamento`), apenas mencionado no texto da confirmação (FR-006).
- Nenhum outro campo da conta (anexo, observações) é tocado por este fluxo (FR-012).
- O botão do status atual continua desabilitado e nunca abre confirmação (FR-009).
