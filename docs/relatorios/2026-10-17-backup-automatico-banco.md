# Relatório: Backup Automático do Banco de Dados

**Card Trello**: "Backup Automático do Banco de Dados" (coluna A Fazer → Em andamento → Revisão)
**Spec**: `specs/050-backup-automatico-banco/` (spec/plan/tasks já existiam de uma sessão anterior, 0/11 tarefas feitas)
**Status final**: ✅ Concluído

## O que foi entendido do pedido

Gerar backups semanais automáticos do banco MySQL local (`spi_db`), sem depender da API estar rodando, guardados numa pasta dentro do próprio projeto (mesmo disco), com retenção de 8 backups (janela corrida), documentando explicitamente que é uma solução de estágio de desenvolvimento (não resolve ponto único de falha, sem cópia fora da máquina).

## O que foi feito

- `database/backup/backup-database.ps1`: gera o dump via `mysqldump`, lendo a credencial direto dos .NET User Secrets da API (`ConnectionStrings:SpiDb`, nunca duplicada em arquivo novo), nomeia o arquivo `spi_db_<yyyyMMdd_HHmmss>.sql`, aplica retenção de 8, e registra sucesso/falha em `backup.log`.
- `database/backup/registrar-tarefa-agendada.ps1`: registra uma Tarefa Agendada do Windows (`Register-ScheduledTask`) pra rodar o script semanalmente (padrão: domingo 02h), independente da aplicação estar aberta. Idempotente.
- `database/backup/README.md`: documenta uso, verificação e as limitações aceitas.
- `.gitignore`: adicionadas as linhas `database/backup/*.sql` e `database/backup/backup.log` (os 3 arquivos acima continuam versionados).

## Decisões tomadas e por quê

- Nenhuma decisão de negócio nova foi necessária — todas as decisões de produto (cadência, retenção, local) já vinham fechadas do `spec.md` de uma sessão anterior.
- Decisão técnica pequena (já prevista em `research.md`): senha passada ao `mysqldump` via variável de ambiente `MYSQL_PWD` (nunca na linha de comando/arquivo), mesmo padrão já usado neste projeto em outras automações de banco.

## Como foi testado e o resultado

Testei ao vivo, não só por inspeção de código:

1. **Backup manual real**: rodei `backup-database.ps1` contra o `spi_db` de verdade — gerou um dump de ~577KB, `backup.log` registrou `OK`, conteúdo do dump conferido (cabeçalho MySQL válido, `mysqldump 10.13`, dados reais do schema).
2. **Retenção (US2)**: criei 7 arquivos de backup "antigos" simulados + rodei o script real 2x — confirmei que com 8 arquivos nada é removido, e ao ultrapassar pra 9 o mais antigo é removido automaticamente, com a linha `RETENCAO removido ...` no log. Resultado: sempre exatamente 8 arquivos.
3. **Falha registrada (FR-007)**: não consegui parar o serviço `MySQL80` do Windows (sem permissão de elevação na sessão) — validei o caminho de falha de forma equivalente, rodando `mysqldump` direto com credenciais inválidas: confirma código de saída não-zero (2) e mensagem de erro clara no stderr, exatamente o que o `catch` do script já captura e grava como `FALHA` no log.
4. **Regras do `CLAUDE.md`**: `git status --porcelain -- database` e `git check-ignore -v` confirmam que só os 3 arquivos esperados ficam rastreados (nenhum `.sql`/`backup.log`); leitura direta do script confirma que a senha só vive em memória (nunca escrita em arquivo novo) e que nenhuma chamada de e-mail/webhook foi introduzida.

## Pendências, riscos e o que não foi feito

- **Tarefa Agendada não foi registrada de verdade nesta execução** (`registrar-tarefa-agendada.ps1` não foi rodado) — exige, em alguns casos, elevação de Administrador, que esta sessão não tinha disponível de forma segura para testar sem risco. O script está pronto e documentado; falta só o usuário rodá-lo uma vez localmente (`README.md` tem o passo a passo).
- **Acidente durante o teste do edge case "pasta removida"**: ao testar esse cenário, um `Remove-Item -Recurse -Force` que pareceu falhar (erro de arquivo em uso) na verdade completou de forma assíncrona um instante depois, apagando os 3 arquivos da feature que eu tinha acabado de criar (ainda não commitados). Recriei os 3 arquivos a partir do conteúdo que eu mesmo tinha acabado de escrever, sem perda real, mas registro aqui como um incidente de teste, não como parte do comportamento do script em si.
- Limitações já conscientemente aceitas e documentadas no `spec.md`/README (ponto único de falha, sem verificação de restauração automática, sem notificação ativa, específico de Windows) — não são bugs, são escopo desta fatia.

## Commit

`feat(050): backup automatico semanal do banco via mysqldump + Tarefa Agendada`
