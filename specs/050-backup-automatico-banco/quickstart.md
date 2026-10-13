# Quickstart: validar o backup automático do banco

Guia de validação ponta a ponta. Decisões técnicas em [research.md](./research.md); formato de arquivos em [data-model.md](./data-model.md).

## Pré-requisitos

- MySQL rodando localmente, com `spi_db` populado.
- `mysqldump.exe` disponível (já confirmado no ambiente: `C:\Program Files\MySQL\MySQL Server 8.0\bin\`).
- User Secrets do projeto já configurados (`ConnectionStrings:SpiDB`).

## 1. Backup manual funciona e gera um arquivo correto (US1, FR-001/FR-003, SC-001/SC-004)

| # | Passo | Esperado |
|---|---|---|
| 1 | Rodar `powershell -File database/backup/backup-database.ps1` manualmente | Script termina sem erro (código de saída 0) |
| 2 | Olhar `database/backup/` | Um novo arquivo `spi_db_<data>_<hora>.sql` aparece, com conteúdo (tamanho > 0) |
| 3 | Olhar `database/backup/backup.log` | Uma linha nova `OK spi_db_...sql` foi acrescentada |
| 4 | Restaurar esse arquivo num banco MySQL separado/de teste (`mysql -u root -p banco_teste < arquivo.sql`) | O banco de teste reproduz os dados do momento do backup |

## 2. Retenção remove o backup mais antigo (US2, FR-006, SC-002)

| # | Passo | Esperado |
|---|---|---|
| 1 | Rodar o script manualmente 9 vezes em sequência (ou criar 8 arquivos de teste com nomes antigos + rodar 1 vez) | Só os 8 backups mais recentes permanecem em `database/backup/` |
| 2 | Conferir qual arquivo foi removido | É o de timestamp mais antigo entre os que existiam |

## 3. Falha é registrada sem travar nem notificar (Edge Cases do spec.md, FR-007)

| # | Passo | Esperado |
|---|---|---|
| 1 | Parar o serviço do MySQL temporariamente e rodar o script | Script termina com código de saída diferente de 0; `backup.log` ganha uma linha `FALHA ...` |
| 2 | Religar o MySQL e rodar de novo | Volta a funcionar normalmente, sem nenhuma configuração extra |

## 4. Tarefa Agendada roda sozinha, sem a aplicação aberta (US1, FR-002)

| # | Passo | Esperado |
|---|---|---|
| 1 | Rodar `powershell -File database/backup/registrar-tarefa-agendada.ps1` (uma vez, como administrador se exigido) | Tarefa aparece no Agendador de Tarefas do Windows ("Exibir Biblioteca do Agendador de Tarefas") |
| 2 | Fechar a API/frontend do projeto completamente (nenhum processo `dotnet`/`node` rodando) | — |
| 3 | No Agendador de Tarefas, clicar com o botão direito na tarefa e "Executar" (para forçar, sem esperar o agendamento real) | Um novo backup aparece em `database/backup/`, mesmo com a aplicação fechada |
| 4 | Olhar a aba "Histórico" da tarefa no Agendador | Mostra a execução com sucesso |

## 5. Não-regressão e verificação (regra do `CLAUDE.md`, FR-004/FR-005, SC-003)

- `git status --porcelain -- database` mostra os 2 scripts `.ps1` e o `README.md` como arquivos novos (esperado, serão comitados), mas **nenhum arquivo `.sql` de backup nem `backup.log`** aparece — confirma que o `.gitignore` está cobrindo os gerados corretamente.
- `git check-ignore -v database/backup/spi_db_teste.sql` (criar um arquivo de teste vazio com esse nome) confirma que a regra do `.gitignore` o cobre.
- Nenhuma mudança em `src/` (C#) nem em nenhum script `database/*.sql` já existente.
