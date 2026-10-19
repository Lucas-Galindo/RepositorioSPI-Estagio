# Relatório: Centro de Notificações (Aulas e Pagamentos) e Aba Lembretes (spec 060)

**Card Trello**: "Centro de notificações (aulas e pagamentos) e aba 'Lembretes'" (Backlog → Design → A Fazer → Em andamento → Revisão)
**Spec**: `specs/060-centro-notificacoes/` (specify, plan, tasks, analyze e implement em 2026-10-19)
**Status final**: ✅ Implementado. Falta conferir num navegador (T020, parte manual) e, se quiser, um envio real com credenciais SMTP.

## O que foi entendido do pedido

O sino da spec 059 vira um centro de notificações com três tipos:
- aula sem status;
- **aula futura**: na véspera, a partir das 08:00;
- **pagamento pendente**: um aviso por aluno, com as pendências listadas.

Além disso:
- **e-mail semanal**: aluno e responsável recebem toda segunda, às 08:00, até a situação ser atualizada;
- **aba Lembretes**: mostra a regra e o histórico de envios.

Decisões que você já tinha tomado:
- o lembrete de mensalidade 5 dias antes continua;
- a regra de pagamento é totalmente editável;
- a antecedência do aviso de aula futura fica em configuração.

## O que foi feito

**Segurança de e-mail (feita primeiro)**
- `SmtpEmailSender` agora passa todo envio por `ResolverEnvio`.
  - **Em Development**: o e-mail vai para `Smtp:RedirecionarTodosPara`, com o destinatário original no assunto (`[DEV → original] ...`). Sem esse valor configurado, o envio é **recusado**.
  - **Fora de Development**: nada muda.
- Isso vale para **todos** os e-mails do sistema (lembrete de aula, mensalidade, recuperação de senha e o novo).
- O endereço de teste ficou só nos **User Secrets** locais, nunca em `appsettings`.

**Banco**: `database/23_centro_notificacoes.sql`, aplicado localmente com backup antes.
- `notificacao.aluno_id`, com índice único `(tipo, professora, aluno)`.
- Tabela `regra_lembrete_pagamento`: uma linha só; padrão ligada, segunda, 08:00.
- Tabela `envio_lembrete_pagamento`: histórico, com índice único `(aluno, dia de envio)` para nunca duplicar.

**Backend**
- **`NotificacaoService.SincronizarAsync`**: os três tipos usam a mesma lógica de criar, reativar ou resolver.
  - Os dois tipos de aula compartilham o mesmo método.
  - Pagamento pendente = status "Pendente" com vencimento **antes de hoje** (o mesmo critério do "Atrasado" que o sistema já calcula).
  - O aviso de pagamento vai para a professora única do sistema (single-tenant).
- **Aula futura**: aparece desde `N dias antes, às H` até o fim da aula. A configuração fica em `Notificacoes:AulaFuturaDiasAntes`/`AulaFuturaHora` (padrão 1 dia e 08:00).
- **`LembretePagamentoService.ProcessarAsync`** cuida do envio semanal:
  - só no dia e a partir da hora da regra;
  - um e-mail por aluno, com todas as pendências, para o e-mail do aluno e o do responsável, sem repetir endereço;
  - registra cada tentativa no histórico;
  - uma falha é tentada de novo, até 3 vezes no mesmo dia;
  - aluno sem e-mail fica registrado como "sem destinatário";
  - a professora nunca recebe e-mail.
- Nova rotina `LembretePagamentoDispatcherService`, que roda a cada 5 minutos.
- **Endpoints**:
  - `GET/PUT /api/lembretes/pagamento-pendente/regra`;
  - `GET /api/lembretes/pagamento-pendente/envios`.

**Frontend**
- **Sino**:
  - abas Todas / Aulas / Pagamentos;
  - rótulo por tipo ("Aula", "Próxima aula", "Pagamento");
  - o contador soma todas as não lidas;
  - o popup de pagamento traz a tabela de pendências (descrição, valor, vencimento, atraso), com link para o registro em contas a receber.
- **Aba Lembretes**: nova seção "Pagamento pendente" com:
  - ligar e desligar, dia da semana e horário;
  - explicação "repete toda semana até ser atualizado";
  - o histórico de envios.

**Documentação**: a spec 059 ganhou nota apontando para a 060.

## Decisões tomadas e por quê

- **Envio perdido**: se o sistema ficar fora do ar o dia inteiro da regra, aquela semana fica sem e-mail. A notificação no sistema continua valendo de qualquer forma.
- **Testes da 059**: passaram a olhar só o tipo "aula pendente". Uma aula de hoje que ainda não terminou agora também tem o aviso de "aula futura", que é o comportamento pedido.
- **Execuções simultâneas da rotina**: se o índice barrar um registro duplicado, o ciclo é encerrado e o próximo continua. Isso evita que o registro barrado faça falhar os outros salvamentos do mesmo ciclo.

## Achado fora do escopo

- Criar um aluno **com e-mail** pela API (`POST /api/alunos`) devolve 500, "Value cannot be null (input)". Parece exigir senha quando o e-mail é informado. Não mexi, porque é de outro fluxo; no teste, gravei o e-mail sintético direto no banco. Vale um card.

## Como foi testado

- `dotnet test`: **386/386**. Os testes novos cobrem:
  - redirecionamento de e-mail, incluindo a recusa sem endereço de teste (FR-015);
  - notificação de pagamento: uma por aluno, vence hoje não conta, sem duplicar (FR-003), baixa resolve, reativação, sem professora;
  - aula futura: janela das 08:00, cancelada, troca para "pendente" ao terminar, remarcada reativa a mesma, sem duplicar;
  - e-mail semanal: dia e hora, mesmo endereço uma vez, vence no dia não entra, sem duplicar no dia (FR-007), semanal, falha até 3 vezes, falha seguida de sucesso, sem destinatário, regra desligada, regra editada, histórico, validação.
- **Verificação MUST NOT**:
  - o endereço de teste não aparece em nenhum arquivo do repositório;
  - todo envio passa por `ResolverEnvio`, e só existe um `IEmailSender`;
  - a notificação é criada num único ponto, depois de checar a existência;
  - os destinatários são só os e-mails do aluno.
- **API real no localhost**:
  - backup antes;
  - **SMTP apontado para um host inexistente** e redirecionamento ativo, então nada podia sair;
  - alunos "TESTE-060" com e-mail `@exemplo.invalid`;
  - intervalo da rotina reduzido para o teste.
  - **11/11 cenários OK**:
    - aviso de pagamento com 2 pendências e atraso certo;
    - sem duplicar, e baixa resolve;
    - aula futura aparece e some ao cancelar;
    - regra padrão;
    - falha registrada com novas tentativas;
    - sem destinatário;
    - regra desligada não envia;
    - histórico;
    - 400 para dia inválido.
  - Ao fim:
    - dados de teste, notificações e histórico apagados;
    - regra de volta ao padrão;
    - pagamentos reais intactos.
  - O único aluno real com pendência não tem e-mail, então nem houve tentativa de envio para ele.
- `dotnet build`, `npm run build`, `tsc`, `eslint` e `npm run verificar-contraste` OK.

## Pendências e riscos

- **Navegador (T020).** Falta conferir o sino e a aba Lembretes num navegador real.
- **Envio real.** Os e-mails de verdade dependem das credenciais SMTP (a conta Google com senha de app ainda está pendente, conforme a memória do projeto). Com elas configuradas, em Development tudo vai só para o endereço de teste.
- **Produção:**
  - aplicar `23_centro_notificacoes.sql` (depois da 21 e da 22);
  - **não** configurar `RedirecionarTodosPara` em produção.
  - Ao entrar no ar, alunos com pendência e e-mail cadastrado passam a receber o e-mail na segunda seguinte, às 08:00.
