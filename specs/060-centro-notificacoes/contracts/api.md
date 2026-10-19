# Contrato: Centro de Notificações e Lembrete de Pagamento (spec 060)

Todos os endpoints exigem o perfil Professor.

## `GET /api/notificacoes` (alterado)

Sincroniza os três tipos e devolve as ativas, das mais recentes para as mais antigas. Cada item traz `tipo`, que pode ser `AulaPendente`, `AulaFutura` ou `PagamentoPendente`:

- **Tipos de aula**: `aula` preenchida (como na 059) e `pagamento` nulo.
- **Pagamento pendente**: `aula` nula e `pagamento` preenchido:

```json
{ "alunoId": 3, "alunoNome": "Ana", "pendencias": [ { "pagamentoId": 9, "descricao": "Matemática - 01/10/2026", "valor": 80.0, "vencimento": "2026-10-06", "diasAtraso": 13 } ] }
```

## `PATCH /api/notificacoes/{id}/lida`

Igual à 059.

## `GET /api/lembretes/pagamento-pendente/regra`

- **200**: `{ "ativo": true, "diaSemana": 1, "hora": "08:00:00" }`

## `PUT /api/lembretes/pagamento-pendente/regra`

- **Corpo**: `{ "ativo": bool, "diaSemana": 0-6, "hora": "HH:mm[:ss]" }`.
- **200**: a regra salva.
- **400**: `diaSemana` fora de 0-6.

## `GET /api/lembretes/pagamento-pendente/envios?limite=100`

- **200**: os envios, dos mais recentes para os mais antigos (`tentadoEm`), até `limite` (1-500, padrão 100).
