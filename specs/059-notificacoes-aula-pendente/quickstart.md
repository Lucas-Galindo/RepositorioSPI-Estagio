# Quickstart: validar notificações de aula pendente (spec 059)

Contrato em [contracts/notificacoes-api.md](./contracts/notificacoes-api.md); regra em [research.md](./research.md) R3.

## Pré-requisitos

- Backup do banco local e `database/22_notificacao.sql` aplicado.
- API local rodando com o SMTP desligado (nenhum e-mail sai) e um token de Professor.
- Dados sintéticos "TESTE-059", sem e-mail.

## Cenários (API)

| # | Passo | Esperado |
|---|---|---|
| 1 | Aula de hoje que já terminou (Agendada) | `GET /api/notificacoes` traz 1 notificação, `lida: false`, contexto = turma |
| 2 | `GET` de novo, duas vezes | Continua 1 só (mesmo `id`) |
| 3 | Aula de hoje que termina no futuro | Não aparece |
| 4 | Aula individual que já terminou | Aparece com contexto "Atendimento individual · {aluno}" |
| 5 | Aula que terminou antes de `AulasPendentesDesde` | Não aparece |
| 6 | `PATCH /lida` na notificação do cenário 1 | 204; no `GET`, `lida: true` e ela continua na lista |
| 7 | `PATCH /lida` com o id de uma notificação de outra professora (token com outro `sub`) | 404 |
| 8 | Registrar a sessão da aula do cenário 1 | No `GET` seguinte ela não aparece mais |
| 9 | Cancelar a aula do cenário 4 | Some da lista |
| 10 | Conferir no banco | Exatamente 1 linha por aula; as resolvidas com `resolvida_em` preenchido |

## Frontend (manual, navegador)

- O sino mostra o contador; clicar lista os avisos; clicar num aviso abre o popup com data, matéria e turma, e "Ir para a aula" leva aos detalhes da aula.
