# Relatório: Notificações de Status de Aula Pendente (spec 059)

**Card Trello**: "Notificações de status de aula pendente" (Backlog → Design → A Fazer → Em andamento → Revisão)
**Spec**: `specs/059-notificacoes-aula-pendente/` (specify, plan, tasks, analyze e implement em 2026-10-19)
**Status final**: ✅ Implementado. Falta conferir o sino num navegador (T017, parte manual).

## O que foi entendido do pedido

Quando uma aula termina e continua "Agendada", a professora recebe um aviso no sino do cabeçalho:
- **uma notificação por aula**, que fica ativa nos dias seguintes;
- **abrir** marca como lida;
- registrar, cancelar ou excluir a aula **resolve** o aviso;
- o popup mostra data, matéria, turma e o botão **"Ir para a aula"**.

Decisões que você já tinha tomado:
- só aulas que terminarem depois da entrada no ar geram aviso;
- aula individual aparece como "Atendimento individual".

## O que foi feito

**Banco**: `database/22_notificacao.sql`, aplicado localmente com backup antes. Cria a tabela `notificacao`, com o índice único `(tipo, aula_id)`, que garante uma por aula mesmo com execuções simultâneas. Nunca se apaga linha: a notificação é resolvida.

**Backend**
- **`NotificacaoService.SincronizarAulasPendentesAsync`**: é o único lugar da regra.
  - Cria o aviso de cada aula pendente.
  - Reativa o mesmo aviso se a aula voltar a ficar pendente; não cria outro.
  - Resolve os avisos cujas aulas deixaram de estar pendentes.
  - Por isso não foi preciso mexer no `AulaService`.
- **"Pendente"** = aula ativa, "Agendada", com término (data + hora fim) entre o início de vigência e agora. A consulta fica em `AulaRepository.ListarPendentesDeStatusAsync`.
- **Rotina `NotificacaoDispatcherService`**: roda a cada 5 minutos, para todas as professoras.
- **`GET /api/notificacoes`**: sincroniza a professora logada antes de listar, para cobrir o "ao carregar o sistema".
- **`PATCH /api/notificacoes/{id}/lida`**: marca como lida; responde 404 para notificação de outra professora.
- **Configuração** (`appsettings`, seção `Notificacoes`):
  - `AulasPendentesDesde` = 2026-10-19 00:00;
  - `IntervaloVerificacaoSegundos` = 300.
- **Preparação para a 060**: a notificação já tem um tipo (`TipoNotificacao`), então a 060 só acrescenta os seus.

**Frontend**
- O sino decorativo do cabeçalho (só ícone e um ponto fixo, sem função) virou `NotificacoesSino`:
  - contador de não lidas;
  - painel com a lista (mais recentes primeiro, não lidas em negrito e com um ponto);
  - popup com mensagem, data, horário, matéria e turma ("Atendimento individual · aluno" na aula individual), mais o botão "Ir para a aula".
- Consulta a API ao abrir cada tela e a cada 60 segundos.
- Só aparece para a Professora; o Admin não tem aulas.
- Os estilos usam só os tokens da spec 057. O CSS do ponto decorativo, agora sem uso, foi removido.

## Decisões tomadas e por quê

- **Início de vigência configurável.** O padrão é a data de hoje; dá para ajustar na implantação, se a entrada no ar em produção for outro dia.
- **Atualização do sino.** O aviso sai na próxima consulta: ao trocar de tela, ou em até 1 minuto. Isso evita duplicar a regra no `AulaService`.
- **Duas sincronizações ao mesmo tempo.** Se a rotina e uma abertura do sistema gravarem juntas, o índice único barra a segunda e o serviço ignora o conflito. A próxima execução completa o que faltar.

## Como foi testado

- `dotnet test`: **344/344**. São 15 testes novos:
  - criação;
  - sem duplicar (FR-003), inclusive com inserção concorrente;
  - sem aviso antes da vigência (FR-004);
  - resolvida não volta sozinha (FR-012);
  - reativação da mesma notificação;
  - escopo por professora;
  - lida idempotente e 404 para notificação alheia;
  - ordem da lista e contexto de aula individual.
- **Verificação MUST NOT por leitura do diff**:
  - um único ponto de criação, depois de checar a existência;
  - nenhum `Remove`;
  - o frontend não calcula "pendente".
- `git status --porcelain`: os arquivos novos são só os previstos no plano.
- **API real no localhost**:
  - backup antes;
  - SMTP apontado para um host inexistente;
  - token de Professor gerado localmente, sem senha;
  - dados "TESTE-059", inseridos direto no banco (pela API, uma aula passada já nasce Realizada) e apagados ao final.
  - **10/10 cenários do quickstart OK.**
  - O cenário 4 acusou falha no script por causa da leitura do caractere "·" pelo PowerShell; conferido à parte, a API devolve U+00B7, como esperado.
- `dotnet build`, `tsc`, `eslint` nos arquivos tocados e `npm run build` OK.

## Pendências e riscos

- **Navegador (T017).** Falta conferir o sino, o painel e o popup num navegador real; não havia navegador na sessão.
- **Produção.** Antes da implantação, aplicar `22_notificacao.sql` (depois da 21) e confirmar `Notificacoes:AulasPendentesDesde`.
