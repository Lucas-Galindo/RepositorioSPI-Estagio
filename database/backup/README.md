# Backup automático do banco (`spi_db`)

Solução de **estágio de desenvolvimento** (specs/050-backup-automatico-banco) — ver limitações e o que precisa evoluir antes de produção em [`../../specs/050-backup-automatico-banco/spec.md`](../../specs/050-backup-automatico-banco/spec.md#%EF%B8%8F-limitações-aceitas-conscientemente-nesta-fase-documentadas-a-pedido-explícito-do-usuário).

## O que os scripts fazem

- **`backup-database.ps1`**: gera um dump completo do banco (`mysqldump`), salva em `database/backup/spi_db_<yyyyMMdd_HHmmss>.sql`, mantém só os **8 backups mais recentes** (remove os mais antigos automaticamente) e registra cada execução (sucesso ou falha) em `database/backup/backup.log`.
- **`registrar-tarefa-agendada.ps1`**: registra (ou atualiza) uma Tarefa Agendada do Windows que roda `backup-database.ps1` toda semana, independente de a aplicação (API/frontend) estar aberta.

A credencial do banco é lida direto dos **.NET User Secrets** já usados pela API (`ConnectionStrings:SpiDb`) — nunca duplicada em nenhum arquivo novo.

## Rodar um backup manualmente

```powershell
cd "C:\PROJETO - SPI\database\backup"
.\backup-database.ps1
```

Gera o arquivo, aplica a retenção de 8, e grava uma linha em `backup.log`. Código de saída `0` em sucesso, `1` em falha.

## Registrar o agendamento semanal (rodar uma única vez)

```powershell
cd "C:\PROJETO - SPI\database\backup"
.\registrar-tarefa-agendada.ps1
# ou customizando dia/horário:
.\registrar-tarefa-agendada.ps1 -DiaDaSemana Monday -Horario 03:30
```

Padrão: domingo às 02:00. Pode exigir rodar o PowerShell como Administrador, dependendo da política local do Windows.

## Verificar/forçar a execução

- Abra o **Agendador de Tarefas do Windows** → procure **"SPI - Backup Semanal do Banco"** → aba **Histórico**, para ver execuções passadas (sucesso/falha).
- Pra forçar uma execução de teste agora, sem esperar o horário programado:
  ```powershell
  Start-ScheduledTask -TaskName 'SPI - Backup Semanal do Banco'
  ```

## Restaurar um backup (teste)

```powershell
mysql -u root -p novo_banco_de_teste < database\backup\spi_db_20261017_020000.sql
```

## O que NÃO está coberto por esta fatia

- Cópia fora do disco/máquina atual (sem disco externo, sem nuvem) — ponto único de falha aceito por ora.
- Verificação automática de que um backup é restaurável.
- Notificação ativa (e-mail/alerta) quando um backup falha — só o `backup.log` e o Histórico do Agendador.
- Portabilidade para Linux (`cron`) — fica para quando/se o ambiente de hospedagem mudar.

Lista completa em `spec.md`, seção "🔜 O que precisa evoluir antes de o projeto ser considerado finalizado/produção".
