# Quickstart: Centro de Notificações (spec 060)

Contrato em [contracts/api.md](./contracts/api.md); regras em [research.md](./research.md).

## Pré-requisitos

- **Banco**: backup local e `database/23_centro_notificacoes.sql` aplicado.
- **Endereço de teste**: User Secret `Smtp:RedirecionarTodosPara` = endereço de teste do usuário.
- **Durante o teste**: SMTP apontado para um host inexistente. Assim o envio falha antes de sair e exercita o caminho de falha; mesmo que saísse, iria só para o endereço de teste.
- **Dados**: sintéticos "TESTE-060".

## Cenários

| # | Passo | Esperado |
|---|---|---|
| 1 | Aluno com 2 pagamentos "Pendente" vencidos e 1 que vence hoje | `GET /api/notificacoes`: 1 `PagamentoPendente` com 2 pendências e dias de atraso corretos |
| 2 | `GET` de novo | Continua 1 (mesmo id) |
| 3 | Baixa (status Pago) nos 2 vencidos | A notificação some |
| 4 | Aula agendada para amanhã | `AulaFutura` presente (após 08:00); cancelar a aula → some |
| 5 | `GET /regra` | `{ativo: true, diaSemana: 1, hora: 08:00}` |
| 6 | `PUT /regra` com o dia e a hora de agora; aluno com pendência e e-mail | A rotina registra `Falha` (SMTP inválido), tentativas 1; a execução seguinte, 2. Nenhum e-mail chega a endereço real |
| 7 | Aluno sem e-mail com pendência | Registro `SemDestinatario`, sem nova tentativa |
| 8 | `PUT /regra` com `ativo: false` | Nenhum envio novo |
| 9 | `GET /envios` | Lista os registros acima, mais recentes primeiro |
| 10 | `PUT /regra` com `diaSemana: 9` | 400 |
| 11 | Ao fim, restaurar a regra padrão (segunda, 08:00, ligada) e apagar os dados de teste | |

## Frontend (manual)

- **Sino**: as abas Todas / Aulas / Pagamentos funcionam, e o popup de pagamento traz as pendências com link.
- **Aba Lembretes**: a seção "Pagamento pendente" mostra a regra editável e o histórico.
