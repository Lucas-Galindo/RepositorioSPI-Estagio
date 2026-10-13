# Implementation Plan: Backup Automático do Banco de Dados

**Branch**: `050-backup-automatico-banco` | **Date**: 2026-10-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/050-backup-automatico-banco/spec.md`

## Summary

Entrega um script PowerShell (`database/backup/backup-database.ps1`) que gera um dump completo do MySQL via `mysqldump` (já disponível no ambiente), nomeado com data/hora, salvo em `database/backup/` (fora do Git), descartando automaticamente o backup mais antigo quando o nono arquivo é criado (retenção de 8). As credenciais são lidas do `secrets.json` do .NET User Secrets já existente (`ConnectionStrings:SpiDB`), sem duplicar segredo em nenhum lugar novo. Um segundo script (`registrar-tarefa-agendada.ps1`), rodado uma única vez pelo usuário, registra o primeiro como uma Tarefa Agendada do Windows semanal — nenhum `BackgroundService`/mudança no código da aplicação .NET.

## Technical Context

**Language/Version**: PowerShell (Windows) — mesma linguagem já usada pelos scripts do Spec Kit e do hook do Trello (`.specify/scripts/powershell/*`, `.specify/hooks/trello/sync-card.ps1`); nenhuma linguagem nova no projeto.

**Primary Dependencies**: `mysqldump.exe` (já instalado junto com o MySQL Server, confirmado em `C:\Program Files\MySQL\MySQL Server 8.0\bin\`) e o cmdlet nativo `Register-ScheduledTask` do PowerShell (Windows, sem instalação adicional).

**Storage**: Arquivos `.sql` em `database/backup/` (fora do Git) — nenhuma tabela ou mudança de schema na aplicação.

**Testing**: Nenhum teste automatizado (xUnit) aplicável — é um script operacional fora da aplicação .NET; validação é manual, via `quickstart.md` (mesma decisão já aceita para mudanças de infraestrutura puramente operacional neste projeto).

**Target Platform**: Windows (máquina atual do usuário) — Agendador de Tarefas do Windows é Windows-specific (documentado como limitação a evoluir, ver spec.md).

**Project Type**: Infraestrutura operacional — não é nem frontend nem backend da aplicação SPI; vive em `database/`, like os scripts `.sql` já existentes, mas em PowerShell.

**Performance Goals**: Sem meta nova — um dump semanal de um banco pequeno (single-tenant) não é um cenário de performance crítica.

**Constraints**:
- FR-002: o mecanismo MUST funcionar sem a API/`dotnet` estar em execução — por isso é uma Tarefa Agendada do Windows chamando um script PowerShell direto, nunca um `BackgroundService` do ASP.NET Core.
- FR-004/FR-005: a pasta `database/backup/` MUST existir fora do Git — `.gitignore` MUST cobrir os arquivos gerados (`.sql`, `backup.log`), mas MUST NOT ignorar os próprios scripts `.ps1`/`README.md` (esses ficam versionados, são código da feature).
- Princípio IV (segredos seguros por padrão): o script MUST ler a credencial de conexão do `secrets.json` do .NET User Secrets já configurado (`ConnectionStrings:SpiDB`), resolvendo o `UserSecretsId` a partir de `src/SPI.Api/SPI.Api.csproj` — MUST NOT introduzir um segundo lugar para guardar usuário/senha do banco.
- FR-006: a rotina de retenção MUST contar os arquivos de backup existentes na pasta (ordenados por nome/data) e remover o(s) mais antigo(s) sempre que o total exceder 8, após cada novo backup gerado com sucesso.
- FR-007: toda execução (sucesso ou falha) MUST deixar uma linha em `database/backup/backup.log` (timestamp + resultado); em caso de falha, o script MUST terminar com código de saída diferente de zero, para que o Histórico do Agendador de Tarefas também registre a falha — MUST NOT haver nenhuma notificação ativa (e-mail/alerta).

**Scale/Scope**: 2 scripts novos + 1 README + 1 entrada de `.gitignore`. Nenhuma mudança em `src/` (C#) nem em nenhuma tabela do banco da aplicação.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Backup não exclui nenhum registro da aplicação — a "exclusão" aqui é só de arquivos de backup antigos (fora do domínio da aplicação), prevista explicitamente pela própria feature (FR-006). |
| II. Validação de Negócio Única e Centralizada no Backend | Não | Nenhuma regra de negócio da aplicação envolvida — é um script operacional externo. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim | A "capacidade" (backup automático semanal) é implementada de ponta a ponta nesta mesma fatia — nenhuma opção exposta sem funcionar de fato. |
| IV. Autenticação e Segredos Seguros por Padrão | Sim | A credencial de conexão nunca é duplicada/versionada — lida do User Secrets já existente, no mesmo lugar onde a própria API já busca essa credencial. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Feature nova; não diverge de nenhuma spec retroativa. |

**Resultado**: Sem violação.

## Project Structure

### Documentation (this feature)

```text
specs/050-backup-automatico-banco/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (N/A — sem entidade de domínio)
├── quickstart.md        # Phase 1 output
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

Sem diretório `contracts/` — nenhuma API externa envolvida (script operacional, não endpoint).

### Source Code (repository root)

```text
database/
└── backup/
    ├── backup-database.ps1            # NOVO: gera o dump (mysqldump), nomeia com timestamp, aplica retencao de 8
    ├── registrar-tarefa-agendada.ps1  # NOVO: registra backup-database.ps1 como Tarefa Agendada semanal (rodado uma vez pelo usuario)
    └── README.md                      # NOVO: como funciona, como verificar que rodou, limitacoes/evolucao (linkado ao spec.md)

.gitignore                              # MODIFICADO: + database/backup/*.sql, database/backup/backup.log (scripts/README continuam rastreados)
```

**Structure Decision**: Novo subdiretório `database/backup/`, ao lado dos scripts `.sql` já existentes em `database/` — mesma pasta "casa" de infraestrutura de banco, sem criar um diretório de topo novo no repositório.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

- Princípio IV: `research.md` detalha exatamente como o script localiza e lê o `secrets.json` sem nunca escrever a senha em nenhum arquivo versionado.
- Nenhum dos outros princípios é tocado pelo design.

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
