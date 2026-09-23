# Sincronização Spec Kit → Trello

Ferramenta de processo de desenvolvimento (não faz parte do domínio de negócio do SPI — spec [040-trello-sync-hooks](../../../specs/040-trello-sync-hooks/spec.md)). Sincroniza automaticamente o progresso das features do Spec Kit com o quadro Kanban "Estágio SPI" no Trello.

## O que faz

A cada fase do ciclo Spec Kit (`/speckit-specify`, `/speckit-plan`, `/speckit-tasks`, `/speckit-implement`), um hook registrado em `.specify/extensions.yml` invoca a skill `trello-sync-card`, que chama `sync-card.ps1` para criar ou mover o card da feature no quadro. Via de mão única: Spec Kit → Trello, nunca o contrário.

Uma falha do Trello (fora do ar, credencial inválida, card não encontrado) nunca interrompe o Spec Kit — é só registrada em `sync.log` (gitignored).

## Pré-requisitos

- Um quadro Trello chamado **"Estágio SPI"** com exatamente estas 7 listas: Backlog, Design, A Fazer, Em andamento, Revisão de código, Fase de teste, Concluído. Este script não cria o quadro nem as listas — só interage com eles.
- PowerShell 5.1+ (já disponível neste ambiente).

## Como obter API Key e Token do Trello

1. Acesse https://trello.com/power-ups/admin (ou https://trello.com/app-key, dependendo da conta) logado com a conta que administra o quadro "Estágio SPI".
2. Copie a **API Key** exibida na página.
3. Gere um **Token** de acesso pessoal (a própria página tem um link "Token" que abre um fluxo de autorização; após autorizar, copie o token gerado).
4. Guarde os dois valores com segurança — nunca em texto plano em nenhum arquivo versionado (Constitution, Princípio IV).

## Como obter o Board ID

Com a API Key e o Token em mãos, rode (substituindo `{key}`/`{token}`):

```powershell
Invoke-RestMethod "https://api.trello.com/1/members/me/boards?key={key}&token={token}&fields=name,id" |
  Where-Object { $_.name -eq "Estágio SPI" } |
  Select-Object name, id
```

O campo `id` retornado é o `TRELLO_BOARD_ID`.

## Configuração

Duas formas (o script tenta variável de ambiente primeiro, depois cai para o `.env` local):

**Opção A — variáveis de ambiente** (sessão atual do PowerShell):

```powershell
$env:TRELLO_API_KEY = "sua-api-key"
$env:TRELLO_TOKEN = "seu-token"
$env:TRELLO_BOARD_ID = "id-do-quadro"
```

**Opção B — arquivo `.env` local** (persiste entre sessões, nunca commitado):

```powershell
Copy-Item .specify/hooks/trello/.env.example .specify/hooks/trello/.env
# edite .specify/hooks/trello/.env preenchendo os 3 valores
```

## Uso manual (fora dos hooks)

```powershell
.specify/hooks/trello/sync-card.ps1 -Fase <backlog|design|a-fazer|em-andamento|revisao-codigo> [-DryRun]
```

Ver contrato completo em [../../../specs/040-trello-sync-hooks/contracts/sync-card-cli.md](../../../specs/040-trello-sync-hooks/contracts/sync-card-cli.md).

## Logs

Toda execução grava uma entrada em `.specify/hooks/trello/sync.log` (gitignored) — é o único lugar com detalhe suficiente para diagnosticar uma falha de sincronização, já que o script sempre sai com código `0` independentemente do resultado (para nunca travar o Spec Kit).

## Fora de escopo

- Mover cards automaticamente para "Fase de teste" ou "Concluído" — permanece manual.
- Qualquer leitura do Trello de volta para o Spec Kit.
- Criar cards retroativos para features especificadas antes desta integração existir (`specs/001` a `specs/039`).
