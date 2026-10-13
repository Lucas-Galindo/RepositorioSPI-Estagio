# Data Model: Backup Automático do Banco de Dados

Não aplicável — esta feature não envolve nenhuma entidade de domínio, nenhum campo novo, nenhuma mudança de schema na aplicação. O único "dado" gerado são arquivos `.sql` de backup e um arquivo de log de texto simples, ambos fora do banco de dados e fora do controle de versão (ver `spec.md` FR-004/FR-005).

## Formato do nome do arquivo de backup (não é uma entidade, é uma convenção)

`spi_db_<yyyyMMdd_HHmmss>.sql` — ex.: `spi_db_20261019_020000.sql` para um backup gerado em 19/10/2026 às 02:00.

## Formato de `backup.log` (não é uma entidade, é um arquivo de texto)

Uma linha por execução, append-only:

```text
2026-10-19 02:00:03 OK spi_db_20261019_020000.sql
2026-10-26 02:00:05 FALHA Nao foi possivel conectar ao MySQL (verificar se o servico esta rodando)
```
