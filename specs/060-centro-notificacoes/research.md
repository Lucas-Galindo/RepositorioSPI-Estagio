# Research: Centro de Notificações (spec 060)

## R1 — Mesma tabela, chaves únicas por tipo

**Decision**:
- **Coluna nova**: `notificacao.aluno_id` (NULL nos tipos de aula).
- **Índice novo**: `UNIQUE uq_notificacao_tipo_professor_aluno (tipo, professor_id, aluno_id)`. Os tipos de aula têm `aluno_id` NULL, e o MySQL permite vários NULL num índice único, então não conflitam.
- **Índice da 059**: `uq_notificacao_tipo_aula (tipo, aula_id)` continua garantindo uma por aula e tipo. A aula pendente e a aula futura da mesma aula são tipos diferentes.

**Rationale**: as mesmas garantias da 059 (FR-003), sem tabela nova para notificação.

## R2 — Sincronização genérica por chave

**Decision**: `NotificacaoService.SincronizarAsync(agora, professorId?)` roda três sincronizações. Todas seguem a regra da 059:
- referência que precisa de aviso e não tem → cria;
- tem aviso resolvido → reativa como não lido;
- aviso ativo que não está mais no conjunto → resolve.

| Tipo | Chave | Conjunto que precisa de aviso |
|---|---|---|
| `AulaPendente` | aula | `ListarPendentesDeStatusAsync` (059) |
| `AulaFutura` | aula | `ListarNaJanelaDeAvisoAsync(agora, diasAntes, hora, professorId)`: aulas ativas, Agendadas, com término > agora e `DataInicio <= DateOnly(agora − hora) + diasAntes` |
| `PagamentoPendente` | (professora, aluno) | alunos com `Pagamento.Status == "Pendente"` e `DataVencimento < hoje` (`IPagamentoRepository.ListarPendentesVencidosAsync`) |

- **Laço por aula**: o laço criar/reativar/resolver fica num método privado, usado pelos dois tipos de aula.
- **Laço por aluno**: o de pagamento é análogo, com chave aluno. A professora é a do token (listagem) ou a única cadastrada (`IProfessorRepository.ObterUnicaAsync`, na rotina).

O método público da 059 (`SincronizarAulasPendentesAsync`) passa a se chamar `SincronizarAsync`, já que sincroniza todos os tipos.

## R3 — Janela da aula futura

**Decision**:
- **Configuração**: `Notificacoes:AulaFuturaDiasAntes` (padrão 1) e `Notificacoes:AulaFuturaHora` (padrão "08:00").
- **Quando aparece**: o aviso fica visível de `(DataInicio − diasAntes) + hora` até o término da aula.
- **Consulta**: `limite = DateOnly.FromDateTime(agora − hora) + diasAntes`, filtrando `DataInicio <= limite` e término > agora.
  - Exemplo: com 1 dia e 08:00, às 07:59 de hoje o limite é hoje (só aulas de hoje); às 08:00 passa a ser amanhã.

## R4 — E-mail semanal

**Decision**: `LembretePagamentoService.ProcessarAsync(agora)` faz os passos abaixo.

1. Lê a regra. Se estiver desligada, ou se hoje não for o dia, ou se a hora ainda não chegou, termina.
2. Fixa `diaEnvio = hoje` e lê as pendências vencidas antes de hoje, agrupadas por aluno.
3. Para cada aluno:
   - Lê o envio do dia. Pula se já foi `Enviado` ou `SemDestinatario`. Também pula se foi `Falha` com 3 tentativas.
   - Monta os destinatários: e-mail do aluno e do responsável, sem repetir.
   - **Sem nenhum**: registra `SemDestinatario`.
   - **Com destinatários**: envia o mesmo e-mail a cada um.
     - Todos certos: grava `Enviado`.
     - Algum falhou: grava `Falha`, com o motivo.
   - Atualiza `tentado_em` e `tentativas`, e salva aluno a aluno.

O índice único `(aluno_id, data_envio)` impede dois registros do mesmo aluno no mesmo dia. Junto com a checagem de "Enviado", isso garante FR-007.

O e-mail traz as pendências (descrição, valor, vencimento, dias em atraso) e um texto para o aluno ou responsável. Nunca é enviado à professora.

Uma rotina nova, `LembretePagamentoDispatcherService`, roda a cada `LembretePagamento:IntervaloVerificacaoSegundos` (padrão 300).

**Alternatives considered**: guardar a regra no appsettings — rejeitado, porque o usuário decidiu "totalmente editável" pela tela.

## R5 — Redirecionamento de e-mail em desenvolvimento

**Decision**: `SmtpOptions.RedirecionarTodosPara` e um método estático e puro, `SmtpEmailSender.ResolverEnvio(destinatario, assunto, ehDesenvolvimento, redirecionarPara)`:

| Ambiente | `RedirecionarTodosPara` | Resultado |
|---|---|---|
| Development | preenchido | envia para ele; o assunto ganha `[DEV → destinatario-original]` |
| Development | vazio | lança `InvalidOperationException`, nada sai |
| Outros | — | sem mudança |

O endereço de teste vai para User Secrets do `SPI.Api` (`Smtp:RedirecionarTodosPara`), não para o repositório.

**Rationale**: torna permanente a regra de segurança do usuário para todos os e-mails do sistema (lembrete de aula, mensalidade, recuperação de senha e o novo).

## R6 — API

**Decision**:
- `GET /api/notificacoes`: o DTO ganha `pagamento: { alunoId, alunoNome, pendencias[] }` (pendência: `pagamentoId, descricao, valor, vencimento, diasAtraso`).
- `GET /api/lembretes/pagamento-pendente/regra` e `PUT .../regra` (`{ ativo, diaSemana 0-6, hora "HH:mm" }`, com validação).
- `GET /api/lembretes/pagamento-pendente/envios?limite=100`.

## R7 — Frontend

**Decision**:
- **Sino**:
  - abas Todas / Aulas / Pagamentos;
  - rótulos "Aula pendente", "Aula amanhã" (ou "Próxima aula") e "Pagamento";
  - popup de pagamento com a tabela de pendências e o link para `/financeiro/contas-a-receber/{id}`;
  - popup de aula futura com o botão "Abrir aula".
- **Aba Lembretes**: novo componente `LembretePagamentoSection`, com a regra editável e o histórico.
