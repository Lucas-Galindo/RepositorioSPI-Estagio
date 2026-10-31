# Contrato: notificação de aula sem frequência (spec 064)

A estrutura de `GET /api/notificacoes` **não muda** (`NotificacaoResponse` e `NotificacaoAulaResponse` da spec 059/060). Mudam o conjunto de aulas que geram `AulaPendente` e o texto.

## Conjunto de aulas com aviso

| Antes (spec 059) | Agora |
|---|---|
| Aula ativa, `"Agendada"`, **término ≥ 19/10/2026 00:00** e já passado | Aula ativa, `"Agendada"` e **término já passado** (sem corte de data) |

- "Término" = `dataInicio` + `horaFim`, comparado com o `agora` de **America/Sao_Paulo** enviado pelo chamador.
- Em andamento (começou, não terminou) **não** gera aviso. Cancelada ou excluída **não** gera e resolve o aviso existente.
- Uma notificação por aula; a rotina e a abertura das notificações continuam idempotentes (restrição `uq_notificacao_tipo_aula` da spec 059).
- O aviso é resolvido quando a frequência é registrada (a aula deixa de ser "Agendada"), a aula é cancelada ou excluída, ou o término passa a estar no futuro.

## Texto (`tipo = "AulaPendente"`)

| Campo | Antes | Agora |
|---|---|---|
| `mensagem` | Você ainda não atualizou o status desta aula. | Esta aula já terminou e está sem frequência registrada. Registre a chamada para corrigir. |
| Título no sino (frontend) | Aula sem status | Aula sem frequência |
| Botão | Ir para a aula | Ir para a aula (sem mudança; abre `/aulas/{id}`) |

`aula` continua trazendo `id`, `data`, `horaInicio`, `horaFim`, `materiaNome` e `contexto` (turma, ou "Atendimento individual · Aluno").

## Configuração

Removida a chave `Notificacoes:AulasPendentesDesde` (e a propriedade em `NotificacoesOptions`). Quem ainda a tiver no `appsettings` local não sofre erro (chave desconhecida é ignorada pelo binder), mas ela não tem mais efeito.
