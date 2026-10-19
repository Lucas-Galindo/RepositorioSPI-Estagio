# Data Model: Notificações de Status de Aula Pendente (spec 059)

## Tabela `notificacao` (nova, `database/22_notificacao.sql`)

| Coluna | Tipo | Regra |
|---|---|---|
| `id` | INT PK AUTO_INCREMENT | |
| `professor_id` | INT NOT NULL, FK `professor(id)` | destinatária |
| `tipo` | VARCHAR(40) NOT NULL | `AulaPendente` (a spec 060 acrescenta outros) |
| `aula_id` | INT NULL, FK `aula(id)` | obrigatória para `AulaPendente` |
| `criada_em` | DATETIME NOT NULL | atualizada na reativação |
| `lida_em` | DATETIME NULL | NULL = não lida |
| `resolvida_em` | DATETIME NULL | NULL = ativa |

Índices:
- `UNIQUE uq_notificacao_tipo_aula (tipo, aula_id)`: uma por aula e tipo (FR-002/FR-003);
- `idx_notificacao_professor_ativa (professor_id, resolvida_em)`: listagem.

## Entidade `Notificacao` (Domain)

As propriedades espelham as colunas; `Tipo` é o enum `TipoNotificacao { AulaPendente }`, gravado como texto. A navegação `Aula` é opcional.

## `IAulaRepository.ListarPendentesDeStatusAsync`

Ver research.md R2. A query inclui `Materia`, `Turma` e `AulaAlunos.Aluno`.

## `NotificacaoResponse` (DTO)

| Campo | Conteúdo |
|---|---|
| `id` | |
| `tipo` | `"AulaPendente"` |
| `mensagem` | "Você ainda não atualizou o status desta aula." |
| `criadaEm` | |
| `lida` | |
| `aula` | `{ id, data, horaInicio, horaFim, materiaNome, contexto }` |

`contexto` vem preenchido assim:
- aula de turma: o nome da turma;
- aula individual: "Atendimento individual · {nome do aluno}".
