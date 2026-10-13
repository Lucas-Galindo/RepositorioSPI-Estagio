---

description: "Task list for Backup Automático do Banco de Dados"
---

# Tasks: Backup Automático do Banco de Dados

**Input**: Design documents from `specs/050-backup-automatico-banco/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md (N/A), quickstart.md

**Tests**: não aplicável (script PowerShell operacional, fora da aplicação .NET testável — ver `research.md` R5). Toda validação é manual via `quickstart.md`.

**Organization**: tarefas agrupadas por user story (US1 P1 backup semanal automático; US2 P2 retenção de 8 backups). As duas vivem no mesmo script (`backup-database.ps1`), mas são aspectos distintos e independentemente testáveis.

## Format: `[ID] [P?] [Story] Description`

- **(parte manual)**: tarefa que exige MySQL real rodando, e opcionalmente parar o serviço (teste de falha) ou registrar a Tarefa Agendada de verdade no Windows.

## Path Conventions

Esta feature toca **somente** `database/backup/` (3 arquivos novos) e `.gitignore` — nenhum arquivo em `src/`, nenhuma mudança de schema da aplicação.

---

## Phase 1: Setup

- [ ] T001 Criar o diretório `database/backup/` (pode ficar vazio até os scripts da Fase 3) e adicionar ao `.gitignore` as linhas `database/backup/*.sql` e `database/backup/backup.log` — **sem** ignorar `database/backup/*.ps1` nem `database/backup/README.md` (esses ficam versionados; conferir que o padrão não os cobre por engano)

---

## Phase 2: Foundational

Não aplicável — não há pré-requisito compartilhado entre as duas stories além do Setup (Phase 1); a lógica de cada uma vive em pontos distintos do mesmo script, implementados dentro de cada fase de story.

---

## Phase 3: User Story 1 - Backup semanal automático, sem precisar lembrar (Priority: P1) 🎯 MVP

**Goal**: um script gera um dump completo do banco, nomeado por data/hora, sem exigir ação manual; uma Tarefa Agendada do Windows roda esse script semanalmente, de forma independente de a aplicação estar aberta.

**Independent Test**: rodar o script manualmente e confirmar o arquivo gerado; registrar a Tarefa Agendada e forçar sua execução com a aplicação fechada, confirmando que o backup acontece mesmo assim.

- [ ] T002 [US1] Criar `database/backup/backup-database.ps1`: (a) resolver o `UserSecretsId` lendo `src/SPI.Api/SPI.Api.csproj` (regex pela tag `<UserSecretsId>`); (b) ler `$env:APPDATA\Microsoft\UserSecrets\<id>\secrets.json`, extrair `ConnectionStrings:SpiDB`, e fazer parse de `Server`/`Port`/`Database`/`User`/`Password` dessa string; (c) criar o diretório `database/backup/` com `New-Item -ItemType Directory -Force` caso ele não exista (Edge Case do spec.md: a pasta pode ter sido removida ou ainda não existir na primeira execução — o script MUST NOT falhar por isso) e montar o nome do arquivo `spi_db_<yyyyMMdd_HHmmss>.sql` dentro dela; (d) chamar `mysqldump.exe` (caminho: `C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe`, ou resolvido via `Get-Command`/PATH) com os parâmetros extraídos, redirecionando a saída para o arquivo; (e) em caso de sucesso, acrescentar uma linha `<timestamp> OK <nome-do-arquivo>` a `database/backup/backup.log` e terminar com código de saída `0`; (f) em caso de qualquer erro (conexão, `mysqldump` ausente, falha de escrita), acrescentar `<timestamp> FALHA <mensagem>` ao log e terminar com código de saída `1` — nunca deixar a senha em texto claro em nenhum arquivo além da variável em memória durante a execução (ver `research.md` R1, R4)
- [ ] T003 [US1] Criar `database/backup/registrar-tarefa-agendada.ps1`: usa `Register-ScheduledTask` para criar uma tarefa que executa `powershell.exe -File <caminho-absoluto-de>\backup-database.ps1` semanalmente (padrão: domingo às 02:00; aceitar parâmetros opcionais `-DiaDaSemana`/`-Horario` para o usuário customizar) — script idempotente (se a tarefa já existir, atualizar em vez de duplicar) (depende de T002, referencia seu caminho)
- [ ] T004 [P] [US1] Criar `database/backup/README.md`: explica o que os 2 scripts fazem, como rodar o backup manualmente, como registrar/verificar a Tarefa Agendada (Agendador de Tarefas do Windows → Histórico), e um link/resumo para a seção "Limitações aceitas"/"O que precisa evoluir" do `spec.md`
- [ ] T005 (parte manual) [US1] Validar o §1 de [quickstart.md](./quickstart.md): rodar `backup-database.ps1` manualmente, confirmar o arquivo gerado (nome/conteúdo) e a linha `OK` em `backup.log`; restaurar o arquivo num banco de teste e confirmar que os dados correspondem (FR-001, FR-003, SC-001, SC-004). **Além disso** (Edge Case do spec.md): apagar a pasta `database/backup/` antes de uma execução e confirmar que o script a recria sozinho, sem falhar
- [ ] T006 (parte manual) [US1] Validar o §4 de [quickstart.md](./quickstart.md): rodar `registrar-tarefa-agendada.ps1`, confirmar a tarefa no Agendador de Tarefas do Windows, fechar toda a aplicação (API/frontend), forçar a execução da tarefa pelo Agendador, e confirmar que um novo backup é gerado mesmo sem a aplicação rodando (FR-002)

**Checkpoint**: User Story 1 completa e testável de forma independente — já entrega o backup automático semanal.

---

## Phase 4: User Story 2 - Backups antigos são descartados automaticamente (Priority: P2)

**Goal**: ao ultrapassar 8 backups na pasta, o(s) mais antigo(s) são removidos automaticamente, sem intervenção manual.

**Independent Test**: gerar mais de 8 backups (reais ou simulados) e confirmar que só os 8 mais recentes permanecem.

- [ ] T007 [US2] Em `database/backup/backup-database.ps1`, após um backup ser gerado com sucesso (T002), adicionar a lógica de retenção: listar todos os arquivos `spi_db_*.sql` em `database/backup/`, ordenar por nome (o formato `yyyyMMdd_HHmmss` já ordena cronologicamente como texto — ver `research.md` R3), e remover os mais antigos além dos **8** mais recentes (depende de T002)
- [ ] T008 (parte manual) [US2] Validar o §2 de [quickstart.md](./quickstart.md): gerar backups além do limite (9+ execuções, ou 8 arquivos de teste + 1 execução real) e confirmar que só os 8 mais recentes permanecem, com o mais antigo removido (FR-006, SC-002)

**Checkpoint**: ambas as user stories completas e testáveis de forma independente.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão e verificação dos requisitos "MUST NOT" (regra do `CLAUDE.md`).

- [ ] T009 (parte manual) Validar o §3 de [quickstart.md](./quickstart.md) (FR-007 — falha registrada, sem notificação ativa): parar o serviço do MySQL, rodar o script, confirmar código de saída não-zero e a linha `FALHA` em `backup.log`; religar o MySQL e confirmar que volta a funcionar normalmente
- [ ] T010 Regra do `CLAUDE.md` para FR-004/FR-005 ("MUST NOT rastrear backups pelo Git"): rodar `git status --porcelain -- database` e confirmar que só os 3 arquivos novos (`backup-database.ps1`, `registrar-tarefa-agendada.ps1`, `README.md`) aparecem como não rastreados — nenhum `.sql`/`backup.log`; rodar `git check-ignore -v database/backup/spi_db_teste.sql` (criar um arquivo vazio de teste com esse nome) e confirmar que a regra do `.gitignore` o cobre (SC-003)
- [ ] T011 Regra do `CLAUDE.md` para o Princípio IV ("MUST NOT duplicar segredo") e para FR-007 ("MUST NOT notificação ativa"): ler diretamente o diff/conteúdo de `backup-database.ps1` e confirmar que (a) a senha do banco só é lida do `secrets.json` já existente, nunca escrita em nenhum arquivo novo nem hardcoded no script; (b) nenhuma chamada de envio de e-mail/webhook/notificação foi adicionada em caso de falha — só o log e o código de saída

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 não bloqueia nada além de precisar existir antes dos scripts serem criados (a pasta/`.gitignore`).
- **User Story 1 (Phase 3)**: T002 bloqueia T003 (que referencia seu caminho) e T006; T004 é independente (só documentação). Depende só da Phase 1.
- **User Story 2 (Phase 4)**: depende de T002 (a lógica de retenção é adicionada ao mesmo script).
- **Polish (Phase 5)**: depende das Fases 3 e 4 completas.

### Parallel Opportunities

- T004 (README) pode ser escrito em paralelo com T002/T003.
- T005 (US1) e T008 (US2) podem ser validados em paralelo depois que T002+T007 estiverem prontos.
- T010/T011 (verificação de git/diff) são independentes das validações manuais.

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (T001)
2. Completar Phase 3 (T002-T006) — backup manual + agendado funcionando, entregável como MVP
3. Phase 4 (US2, retenção) e Phase 5 (Polish) são a entrega incremental seguinte

### Incremental Delivery

Setup (T001) → US1 (T002-T006, MVP) → US2 (T007-T008) → Polish (T009-T011).

## Notes

- Nenhuma tarefa desta lista deve introduzir um segundo lugar para guardar a credencial do banco, nem nenhuma notificação ativa de falha (e-mail/webhook) — ver FR-007 e Princípio IV.
- Este script é deliberadamente específico de Windows (Agendador de Tarefas) — ver spec.md "O que precisa evoluir" para quando o ambiente migrar para Linux.
