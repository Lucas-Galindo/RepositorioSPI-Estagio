# Data Model: Centro de Notificações (spec 060)

`database/23_centro_notificacoes.sql`:

## `notificacao` (alterada)

| Mudança | Detalhe |
|---|---|
| `aluno_id INT NULL`, FK `alunos(id)` | referência dos avisos de pagamento |
| `UNIQUE uq_notificacao_tipo_professor_aluno (tipo, professor_id, aluno_id)` | uma por aluno e professora no tipo pagamento |

`tipo` aceita `AulaPendente`, `AulaFutura` e `PagamentoPendente`.

## `regra_lembrete_pagamento` (nova, uma linha)

| Coluna | Tipo | Padrão |
|---|---|---|
| `id` | INT PK | 1 |
| `ativo` | TINYINT(1) NOT NULL | 1 |
| `dia_semana` | TINYINT NOT NULL, CHECK 0-6 (0 = domingo) | 1 (segunda) |
| `hora` | TIME NOT NULL | 08:00:00 |

A migração já insere a linha padrão.

## `envio_lembrete_pagamento` (nova)

| Coluna | Tipo | Regra |
|---|---|---|
| `id` | INT PK AUTO_INCREMENT | |
| `aluno_id` | INT NOT NULL, FK `alunos(id)` | |
| `data_envio` | DATE NOT NULL | dia de envio (o dia da regra) |
| `tentado_em` | DATETIME NOT NULL | última tentativa |
| `destinatarios` | VARCHAR(500) NOT NULL | e-mails separados por vírgula (vazio se sem destinatário) |
| `resultado` | VARCHAR(20) NOT NULL | `Enviado`, `Falha` ou `SemDestinatario` |
| `tentativas` | INT NOT NULL | 1 a 3 |
| `detalhe` | VARCHAR(500) NULL | motivo da falha |
| `UNIQUE (aluno_id, data_envio)` | | um registro por aluno por dia de envio |

## DTOs

- **Notificação**:
  - `NotificacaoResponse.Pagamento` = `{ alunoId, alunoNome, pendencias: [{ pagamentoId, descricao, valor, vencimento, diasAtraso }] }`;
  - a mensagem de cada tipo é montada no backend.
- **Regra**: `RegraLembretePagamentoResponse/Request` = `{ ativo, diaSemana, hora }`.
- **Envio**: `EnvioLembretePagamentoResponse` = `{ id, alunoId, alunoNome, dataEnvio, tentadoEm, destinatarios, resultado, tentativas, detalhe }`.
