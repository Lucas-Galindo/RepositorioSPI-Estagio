# Feature Specification: Backup Automático do Banco de Dados

**Feature Branch**: `050-backup-automatico-banco`

**Created**: 2026-10-13

**Status**: Draft

**Input**: User description: "Card de Backlog 'Backup de banco': procurar alternativas para fazer backup do banco de dados, pensando em algo autônomo. Decisões da conversa: MySQL roda na própria máquina do usuário (nada em nuvem por enquanto); autônomo = um job agendado que roda sozinho na mesma máquina; cadência semanal por agora (diária prevista para quando o projeto for finalizado); retenção de 8 backups (rolling window simples, sem camadas diferentes por ora); arquivo salvo numa pasta separada dentro do próprio projeto (mesmo disco — sem recurso para disco externo ou nuvem agora); isso é uma solução de desenvolvimento, marcada explicitamente para ser revista quando o projeto for finalizado; usuário pediu para detalhar todas as limitações e o que precisa evoluir depois."

## Nota de investigação prévia

Confirmado por leitura do ambiente atual (grounding antes de especificar):

- **Nenhum mecanismo de backup existe hoje** — nenhum script, nenhuma rotina, nenhuma pasta dedicada no projeto. É uma feature nova do zero.
- **`mysqldump` já está disponível no ambiente** (`C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe`) — é a ferramenta padrão de linha de comando do próprio MySQL para gerar um dump completo do banco; não exige nenhuma instalação nova.
- **"Autônomo, agendado, na mesma máquina" aponta para o Agendador de Tarefas do Windows (Task Scheduler), não para dentro da aplicação .NET**: o projeto já tem um precedente de "job periódico dentro do código" (`LembreteDispatcherService`/`MensalidadeDispatcherService`, `BackgroundService` do ASP.NET Core) — mas esses só rodam **enquanto a API estiver em execução**. Numa máquina de desenvolvimento, a API não fica necessariamente rodando 24/7 (diferente de um servidor de produção sempre ligado); se o backup dependesse da API estar de pé no momento exato, backups semanais poderiam falhar silenciosamente sem ninguém notar. Por isso, esta feature usa um mecanismo **fora da aplicação**: um script autônomo (PowerShell, chamando `mysqldump`) registrado como uma Tarefa Agendada do Windows — roda sozinho, independente de a API/`dotnet` estar aberta ou não.
- **Achado crítico de segurança, não levantado explicitamente pelo usuário, mas inevitável dado "pasta dentro do próprio projeto"**: a pasta de backups MUST ficar fora do controle de versão (`.gitignore`) — um dump de banco contém dados reais (nomes, e-mails, telefones de alunos/responsáveis, valores financeiros) e nunca pode ser commitado/enviado ao repositório Git. Esta é uma regra de segurança não-negociável desta feature (Princípio IV da constituição, mesma categoria de "segredo que nunca vai para o controle de versão"), não uma preferência.

## Clarifications

_Nenhuma pendente — todas as decisões de produto (cadência, retenção, local de armazenamento, caráter temporário da solução) já foram tomadas na conversa que precedeu esta spec. O único ponto técnico (mecanismo de agendamento) foi resolvido pela investigação prévia acima, não por suposição._

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Backup semanal automático, sem precisar lembrar (Priority: P1)

Como usuário responsável pelo sistema, quero que um backup completo do banco de dados seja gerado automaticamente uma vez por semana, sem que eu precise lembrar de rodar nada manualmente, para não correr o risco de perder dados por falta de backup.

**Why this priority**: é o núcleo do pedido original ("algo autônomo") — sem isso, a feature não entrega nenhum valor novo sobre simplesmente lembrar de rodar `mysqldump` manualmente de vez em quando.

**Independent Test**: configurar a tarefa agendada, esperar (ou forçar manualmente) a execução programada, e confirmar que um novo arquivo de backup aparece na pasta configurada, com os dados atuais do banco.

**Acceptance Scenarios**:

1. **Given** a tarefa agendada configurada, **When** chega o horário/dia programado, **Then** um novo arquivo de backup do banco é criado automaticamente, sem nenhuma ação manual.
2. **Given** a máquina está ligada e a tarefa chegou no horário programado, **When** a aplicação (API/frontend) não está em execução no momento, **Then** o backup ainda é gerado normalmente (o mecanismo não depende da aplicação estar rodando).
3. **Given** um backup recém-gerado, **When** o usuário abre o arquivo (ex.: restaurando num banco de teste), **Then** o conteúdo corresponde ao estado do banco no momento em que o backup foi feito.

---

### User Story 2 - Backups antigos são descartados automaticamente (Priority: P2)

Como usuário, quero que backups muito antigos sejam removidos automaticamente, mantendo só os mais recentes, para não acumular espaço em disco indefinidamente.

**Why this priority**: é um complemento necessário à US1 para a solução ser de fato "autônoma" de ponta a ponta (sem exigir limpeza manual periódica), mas o valor central já está entregue pela US1 mesmo antes desta vir — por isso P2.

**Independent Test**: gerar backups além do limite de retenção configurado (ex.: simulando múltiplas execuções) e confirmar que só os mais recentes permanecem na pasta, com os mais antigos removidos automaticamente.

**Acceptance Scenarios**:

1. **Given** já existem 8 backups na pasta (o limite de retenção) e um novo backup é gerado, **When** o nono backup é criado, **Then** o backup mais antigo dos 8 anteriores é removido automaticamente, mantendo sempre 8 no total.
2. **Given** menos de 8 backups existem, **When** um novo é gerado, **Then** nenhum backup existente é removido — só acumula até atingir o limite.

---

### Edge Cases

- **A tarefa agendada roda, mas o MySQL está indisponível no momento** (ex.: serviço parado): o backup daquela execução falha; isso MUST ser registrado de forma identificável (ex.: um arquivo de log, ou o próprio histórico de execução do Agendador de Tarefas do Windows) para que o usuário possa notar que um backup falhou — esta feature não inclui notificação ativa (e-mail/alerta), só o registro passivo do que aconteceu.
- **Espaço em disco insuficiente no momento do backup**: o `mysqldump` falha ao escrever o arquivo; mesmo tratamento do edge case anterior (falha registrada, sem notificação ativa).
- **A pasta de backups ainda não existe** (primeira execução): o mecanismo MUST criar a pasta automaticamente se ela não existir, em vez de falhar.
- **Dois backups gerados no mesmo dia/hora** (execução manual de teste + execução agendada no mesmo momento): o nome do arquivo MUST incluir data e hora completas (não só a data) para nunca sobrescrever um backup anterior silenciosamente.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST gerar um backup completo do banco de dados MySQL automaticamente, uma vez por semana, sem exigir nenhuma ação manual do usuário.
- **FR-002**: O mecanismo de agendamento MUST funcionar de forma independente de a aplicação (API/frontend) estar em execução no momento — rodar mesmo com a aplicação fechada.
- **FR-003**: Cada arquivo de backup gerado MUST ter um nome que identifique univocamente a data e hora em que foi criado, para nunca sobrescrever um backup anterior.
- **FR-004**: Os arquivos de backup MUST ser salvos numa pasta dedicada dentro do próprio projeto, no mesmo disco da máquina atual.
- **FR-005**: A pasta de backups MUST NOT ser rastreada pelo controle de versão (Git) — nenhum arquivo de backup pode ser commitado ou enviado ao repositório remoto, por conter dados reais de alunos/responsáveis/financeiro.
- **FR-006**: O sistema MUST manter, no máximo, os 8 backups mais recentes — ao gerar um backup além desse limite, o mais antigo MUST ser removido automaticamente.
- **FR-007**: Uma falha ao gerar um backup (ex.: banco indisponível, falta de espaço em disco) MUST deixar um registro identificável do que aconteceu — esta feature MUST NOT falhar silenciosamente, mas também MUST NOT incluir notificação ativa (e-mail/alerta) ao usuário.
- **FR-008**: Esta feature MUST ser documentada explicitamente como uma solução de estágio de desenvolvimento — ver Assumptions para as limitações aceitas e o que precisa ser revisto antes de o projeto ser considerado finalizado/produção.

### Key Entities

Não aplicável — feature de infraestrutura operacional (script + agendamento do sistema operacional), sem entidade de domínio ou mudança de schema do banco da aplicação.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um backup novo e íntegro do banco aparece na pasta dedicada a cada semana, sem nenhuma intervenção manual, de forma sustentada por múltiplas semanas consecutivas.
- **SC-002**: Em nenhum momento existem mais de 8 arquivos de backup na pasta — o nono (ou além) sempre resulta na remoção do mais antigo.
- **SC-003**: 0 arquivos de backup aparecem rastreados pelo Git (`git status`/`git log` nunca lista um arquivo da pasta de backups).
- **SC-004**: Um backup gerado pode ser restaurado num banco separado e reproduzir fielmente os dados do momento em que foi criado.

## Assumptions

- **Mecanismo de agendamento**: Tarefa Agendada do Windows (Task Scheduler) executando um script (PowerShell) que chama `mysqldump` — não um `BackgroundService` dentro da aplicação .NET, porque esse só rodaria com a API aberta (ver Nota de investigação prévia).
- **Retenção**: janela corrida simples de 8 backups (sem camadas diferentes por período, ex.: sem distinção entre "backups semanais" e "backups mensais") — decisão explícita do usuário, adequada ao volume atual (cadência semanal, sistema pequeno).
- **Local de armazenamento**: pasta dedicada dentro do próprio repositório do projeto, no mesmo disco da máquina — **nenhuma cópia fora da máquina atual** (sem disco externo, sem nuvem).

### ⚠️ Limitações aceitas conscientemente nesta fase (documentadas a pedido explícito do usuário)

- **Ponto único de falha**: o banco de dados **e** todos os seus backups vivem no mesmo disco, na mesma máquina. Se esse disco falhar, for roubado, ou a máquina for danificada, **tanto os dados atuais quanto todo o histórico de backups são perdidos juntos**. O padrão de mercado para evitar isso é a "regra 3-2-1" (3 cópias dos dados, em 2 tipos de mídia diferentes, com 1 cópia fora do local físico) — esta feature não implementa nenhuma parte dessa regra além da existência do backup em si. Isso é aceito por ora porque não há recurso para disco externo/nuvem neste momento.
- **Sem verificação automática de que um backup é restaurável**: um backup "existe" (o arquivo foi criado), mas esta feature não testa automaticamente se ele de fato restaura corretamente — um arquivo corrompido ou um dump incompleto só seria percebido se alguém tentasse restaurá-lo manualmente. É uma prática recomendada de mercado (testar restauração periodicamente) que esta feature não cobre.
- **Sem notificação ativa de falha**: se um backup falhar (banco indisponível, disco cheio), ninguém é avisado proativamente — é preciso checar manualmente (pasta de backups, log, ou histórico do Agendador de Tarefas) para notar que algo falhou.
- **Específico de Windows**: a solução usa o Agendador de Tarefas do Windows — se o ambiente de produção futuro rodar em Linux (comum para hospedar APIs .NET), este mecanismo precisará ser recriado com `cron` ou equivalente; não é portável como está.
- **Retenção simples (sem camadas) não escala para cadência diária**: quando a cadência mudar de semanal para diária (planejado para quando o projeto for finalizado), uma janela corrida de "últimos 8" passaria a cobrir só pouco mais de uma semana de histórico — valeria introduzir uma retenção em camadas (ex.: diários recentes + um semanal/mensal de mais longo prazo, padrão "Grandfather-Father-Son" já usado por grandes empresas) nesse momento, não antes.

### 🔜 O que precisa evoluir antes de o projeto ser considerado finalizado/produção (registrado a pedido do usuário — "até segunda ordem")

1. **Tirar o backup do mesmo disco/máquina do banco** — mover para um disco externo e/ou um destino fora do local físico (nuvem ou outro servidor), resolvendo o ponto único de falha.
2. **Revisar a cadência e a retenção** para diária + uma política em camadas, quando o volume de dados e a frequência de uso justificarem.
3. **Considerar verificação periódica de restauração** (restaurar um backup num ambiente de teste de tempos em tempos, para confirmar que os backups são de fato utilizáveis, não só "existem").
4. **Considerar notificação ativa de falha** (ex.: reaproveitando a mesma infraestrutura de e-mail da spec 049, já que backup falho e silencioso é pior que não ter backup — ninguém percebe até precisar dele).
5. **Se o ambiente de hospedagem mudar para Linux**, recriar o agendamento com `cron` (ou equivalente do provedor de hospedagem) em vez do Agendador de Tarefas do Windows.
6. Fora de escopo nesta fatia (mas candidatos a specs futuras separadas, não detalhados aqui): compressão dos arquivos de backup, criptografia em repouso, e backup incremental em vez de completo a cada execução.
