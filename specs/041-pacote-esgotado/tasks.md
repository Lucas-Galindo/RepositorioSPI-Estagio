---

description: "Lista de tarefas para Esgotamento de Pacote de Aulas (Alerta e Bloqueio)"
---

# Tasks: Esgotamento de Pacote de Aulas (Alerta e Bloqueio)

**Input**: Documentos de design em `/specs/041-pacote-esgotado/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/registrar-sessao.md](./contracts/registrar-sessao.md), [contracts/dashboard-pacotes-atencao.md](./contracts/dashboard-pacotes-atencao.md), [quickstart.md](./quickstart.md)

**Tests**: INCLUÍDOS — `plan.md` (Technical Context) define xUnit com fakes manuais, e a regra do projeto exige cobertura explícita para requisitos negativos (FR-007, FR-009, FR-010, e os filtros "nunca aparece" do painel). Há frontend nesta feature, mas a lógica de negócio vive só no backend (Princípio II); o frontend só renderiza o que recebe e é validado manualmente (quickstart).

**Organization**: Tarefas agrupadas por user story (US1=P1 painel na Home; US2=P1 bloqueio ao registrar a sessão; US3=P2 renovação libera o aluno).

**Ajuste de design registrado nesta etapa (refina research.md R5)**: o painel NÃO usa um método de repositório novo (`ListarPacotesComSaldoAteAsync`, previsto no plan). Em vez disso reaproveita `IVinculoCobrancaRepository.ListarAtivosPorModalidadeAsync(Pacote)` (já existente, specs/039) — que passa a incluir também o `Aluno` — e a regra de elegibilidade (saldo informado ≤ `LimiteAlertaPacote`, aluno ativo) fica **toda** no método estático puro `DashboardService.MontarPacotesEmAtencao`. Motivo: uma única dona da regra, e ela fica **testável sem banco** — assim os filtros negativos (saldo ≥ 3, saldo nulo, aluno inativo) têm teste automatizado em vez de depender só do quickstart. Custo: carrega todos os vínculos Pacote ativos (tabela pequena, single-tenant). `plan.md`/`research.md`/`data-model.md` foram atualizados para refletir isso.

**Nota estrutural**: US3 (renovação) **não tem código de produção próprio** — a edição do vínculo (specs/037) já existe e tanto o bloqueio (US2) quanto o painel (US1) leem sempre o saldo atual. Por isso US3 é só teste + validação, e seus testes passam assim que US1/US2 existirem (intencional, não descuido — mesmo padrão das specs 038/039).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1, US2 ou US3 (só nas fases de user story)
- Caminhos relativos à raiz do repositório `C:\PROJETO - SPI`
- Tarefas de validação manual carregam o marcador `(parte manual)` (convenção lida por `.specify/hooks/trello/sync-card.ps1` para não bloquear a passagem do card para "Revisão de código")

## Path Conventions

Web app em camadas: `src/SPI.Domain`, `src/SPI.Application`, `src/SPI.Infrastructure`, `tests/SPI.Application.Tests`, `frontend/`. Nenhum controller novo: `DashboardController` só devolve um DTO maior.

---

## Phase 1: Setup

**Purpose**: garantir base verde antes de qualquer alteração

- [X] T001 Rodar `dotnet test "tests/SPI.Application.Tests"` para registrar a linha de base (deve estar 100% verde, 159 testes hoje — a árvore só tem alterações de outras specs já concluídas).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: coluna de banco, propriedades e DTOs compartilhados que US1 e US2 usam. Nenhuma user story começa antes desta fase.

**⚠️ CRITICAL**: sem a coluna aplicada e o mapeamento EF, o registro da sessão quebra em runtime.

- [X] T002 Aplicar `database/16_aula_aluno_motivo_nao_registro.sql` (já criado no `/speckit-plan`) no MySQL local via `mysql` CLI — a connection string vem do .NET User Secrets (`dotnet user-secrets list --project src/SPI.Api`), nunca de `appsettings.json`. Depois confirmar: (a) `DESCRIBE aula_aluno` mostra `motivo_nao_registro varchar(30) YES NULL`; (b) `information_schema.CHECK_CONSTRAINTS` lista `ck_aulaaluno_motivo_nao_registro`; (c) `SELECT COUNT(*) FROM aula_aluno WHERE motivo_nao_registro IS NOT NULL` retorna 0 (nenhum backfill, todas as linhas existentes continuam `NULL`).
- [X] T003 [P] Em `src/SPI.Domain/Entities/AulaAluno.cs`: adicionar `public string? MotivoNaoRegistro { get; set; }` e a constante `public const string MotivoPacoteEsgotado = "PacoteEsgotado";` (data-model.md: valores permitidos `NULL` ou `'PacoteEsgotado'`; `NULL` = comportamento normal ou falta comum, **nunca** bloqueio). Comentário curto explicando o significado de `NULL`.
- [X] T004 [P] Em `src/SPI.Infrastructure/Persistence/Configurations/AulaAlunoConfiguration.cs`: mapear `builder.Property(aa => aa.MotivoNaoRegistro).HasColumnName("motivo_nao_registro").HasMaxLength(30);` (mesma coluna criada em T002; `VARCHAR(30) NULL`, sem default).
- [X] T005 [P] Em `src/SPI.Application/Aulas/Dtos/AlunoPresencaResponse.cs`: adicionar `public string? MotivoNaoRegistro { get; set; }` com comentário no padrão do arquivo (`NULL` = presente ou falta comum; `"PacoteEsgotado"` = presença barrada por pacote esgotado).
- [X] T006 [P] Em `src/SPI.Application/Aulas/Dtos/AulaResponse.cs`: adicionar `public List<string> Avisos { get; set; } = new();` (contrato registrar-sessao.md: aviso imediato, padrão vazio, aditivo).
- [X] T007 [P] Em `src/SPI.Domain/Entities/VinculoCobranca.cs`: adicionar `public const int LimiteAlertaPacote = 2;` com comentário de uma linha (dono único do limite de alerta; fixo, não configurável — Princípio III).
- [X] T008 Em `src/SPI.Application/Aulas/Services/AulaService.cs`, método privado `Mapear`: incluir `MotivoNaoRegistro = aa.MotivoNaoRegistro` no `AlunoPresencaResponse` (para que o motivo apareça também ao consultar a aula depois — FR-012 / cenário 3a). Depende de T003 e T005.
- [X] T009 [P] Em `src/SPI.Infrastructure/Repositories/VinculoCobrancaRepository.cs`, método `ListarAtivosPorModalidadeAsync`: adicionar `.Include(v => v.Aluno)` ao lado do `Include(v => v.Turma)` já existente (o painel precisa do nome e do `Ativo` do aluno). Não altera o contrato da interface nem o job da 039 (só carrega uma navegação a mais).
- [X] T010 Rodar `dotnet build` da solução e confirmar zero erros — checkpoint da fase.

**Checkpoint**: coluna aplicada, propriedades/DTOs compilando — US1 e US2 podem começar.

---

## Phase 3: User Story 1 - Ser avisada, na Home, de quem está acabando o pacote (Priority: P1) 🎯 MVP

**Goal**: painel na Home com os vínculos Pacote ativos de saldo 0, 1 ou 2, cada um com aluno, contexto, saldo e estado (Esgotado primeiro), sempre com o saldo atual.

**Independent Test**: cadastrar vínculos Pacote com saldos 5, 2, 1 e 0 e abrir a Home — só 2, 1 e 0 aparecem, esgotado primeiro (quickstart §1).

### Tests for User Story 1

> **NOTA: escrever estes testes PRIMEIRO e confirmar que FALHAM (o método estático ainda não existe) antes de implementar.**

- [X] T011 [P] [US1] Criar `tests/SPI.Application.Tests/Dashboard/DashboardPacotesEmAtencaoTests.cs`, chamando diretamente `DashboardService.MontarPacotesEmAtencao(IEnumerable<VinculoCobranca>)` (estático e puro, sem fakes de repositório, sem `IScopeFactory`). Cenários — **positivos**: (1) saldo 0 → item com `Estado == "Esgotado"`; (2) saldos 1 e 2 → `Estado == "Atencao"` (FR-001/FR-003); (3) vínculo com turma → `Contexto` = nome da turma; sem turma → `Contexto == "Atendimento individual"` (FR-002); (4) ordenação: `Esgotado` antes de `Atencao`, depois `SaldoAulas` crescente, depois `AlunoNome` (FR-003); (5) lista vazia de entrada → lista vazia de saída (FR-004). **Negativos (cobertura explícita dos "nunca aparece")**: (6) saldo 3 e saldo 5 **não** entram (FR-001); (7) `SaldoAulas == null` **não** entra (saldo nunca informado — FR-007); (8) `Ativo == false` **não** entra; (9) modalidade `Avulsa` e `Mensalidade` **não** entram mesmo com `SaldoAulas` de 0 a 2 (FR-007); (10) `Aluno.Ativo == false` **não** entra (Princípio I); (11) dois vínculos do mesmo aluno em turmas diferentes geram **duas** linhas (uma por vínculo, não por aluno).

### Implementation for User Story 1

- [X] T012 [P] [US1] Criar `src/SPI.Application/Dashboard/Dtos/PacoteEmAtencaoResponse.cs` com os campos de data-model.md: `VinculoId` (int), `AlunoId` (int), `AlunoNome` (string), `Contexto` (string), `SaldoAulas` (int), `Estado` (string: `"Esgotado"` quando saldo 0, `"Atencao"` quando 1 ou 2).
- [X] T013 [US1] Em `src/SPI.Application/Dashboard/Dtos/DashboardResponse.cs`: adicionar `public List<PacoteEmAtencaoResponse> PacotesEmAtencao { get; set; } = new();` (contracts/dashboard-pacotes-atencao.md: sempre presente, `[]` quando ninguém precisa de atenção; demais campos intactos). Depende de T012.
- [X] T014 [US1] Em `src/SPI.Application/Dashboard/Services/DashboardService.cs`: (a) injetar `IVinculoCobrancaRepository` no construtor (já registrado no DI); (b) criar `public static List<PacoteEmAtencaoResponse> MontarPacotesEmAtencao(IEnumerable<VinculoCobranca> vinculos)` que é a **única dona** da regra: mantém só `Ativo && Modalidade == Pacote && SaldoAulas != null && SaldoAulas <= VinculoCobranca.LimiteAlertaPacote && Aluno.Ativo`, calcula `Estado` (0 → `"Esgotado"`, senão `"Atencao"`), `Contexto` (`Turma!.Nome` se `TurmaId.HasValue`, senão `"Atendimento individual"`) e ordena (Esgotado primeiro, depois `SaldoAulas`, depois `AlunoNome`); (c) em `ObterAsync`, chamar `ListarAtivosPorModalidadeAsync(ModalidadeCobranca.Pacote, ct)` e preencher `PacotesEmAtencao`. Depende de T007, T009, T012, T013; faz T011 passar.
- [X] T015 [P] [US1] Em `frontend/lib/api/dashboard.ts`: adicionar a interface `PacoteEmAtencao` (com o comentário `/** Espelha SPI.Application.Dashboard.Dtos.PacoteEmAtencaoResponse. */`, campos `vinculoId`, `alunoId`, `alunoNome`, `contexto`, `saldoAulas`, `estado: "Esgotado" | "Atencao"`) e o campo `pacotesEmAtencao: PacoteEmAtencao[]` em `Dashboard`.
- [X] T016 [US1] Criar `frontend/components/dashboard/PacotesEmAtencaoPanel.tsx`: recebe `pacotes: PacoteEmAtencao[]` e renderiza **na ordem recebida** (sem reordenar nem recalcular estado — Princípio II), com estilo visual distinto para `estado === "Esgotado"` ("Esgotado/Bloqueado") e `"Atencao"` ("Atenção"), usando os tokens/estilos já existentes da Home; cada linha mostra aluno, contexto e saldo, e linka para `/alunos/{alunoId}` (onde a edição do vínculo, specs/037, já existe); lista vazia mostra mensagem clara de que ninguém precisa de atenção (FR-004). Depende de T015.
- [X] T017 [US1] Em `frontend/app/(app)/dashboard/page.tsx`: renderizar `<PacotesEmAtencaoPanel pacotes={dashboard.pacotesEmAtencao} />` na seção que já usa o `dashboard` carregado por `obterDashboard` (sem chamada nova — o painel fica dinâmico a cada carga da Home). Depende de T016.
- [ ] T018 [US1] (parte manual) Validar User Story 1 rodando [quickstart.md](./quickstart.md) §1 com o backend e o frontend no ar e a professora logada (exige ambiente rodando e login): saldos 5/2/1/0 → só 2, 1 e 0 aparecem, esgotado primeiro e visualmente distinto; sem nenhum em atenção, aparece a mensagem de vazio.

**Checkpoint**: US1 funcional e testável de forma independente — MVP entregue (o aviso já vale, mesmo sem o bloqueio).

---

## Phase 4: User Story 2 - Impedir presença de aluno com pacote esgotado (Priority: P1)

**Goal**: ao registrar a sessão, o aluno marcado presente com pacote esgotado (saldo 0) fica sem presença/frequência/débito, com motivo persistido e aviso na resposta; a aula fica `Realizada` para os demais.

**Independent Test**: aula com dois alunos presentes, um esgotado e outro com saldo — a aula fica `Realizada`, o esgotado é barrado com aviso e motivo, o outro é processado normalmente (quickstart §2).

### Tests for User Story 2

> **NOTA: escrever estes testes PRIMEIRO e confirmar que FALHAM antes de implementar T020.**

- [X] T019 [P] [US2] Criar `tests/SPI.Application.Tests/Aulas/AulaServiceRegistrarSessaoPacoteEsgotadoTests.cs` (reusar o construtor/fakes de `AulaServiceFakes.cs` e o padrão de `AulaServiceGerarContasAReceberPacoteTests.cs`; chamar `AulaService.RegistrarSessaoAsync` de verdade). Cenários — **positivos**: (1) um aluno com vínculo `Pacote` ativo e `SaldoAulas == 0` marcado presente → `Presente == false`, `MotivoNaoRegistro == "PacoteEsgotado"`, `Aluno.Frequencia` inalterada, `SaldoAulas` continua 0, resposta com `Avisos` contendo exatamente a mensagem `"Pacote esgotado: <Nome> não teve a presença registrada. Renove o pacote (edite o Vínculo de Cobrança) para liberar novos registros."` (FR-005/FR-012); (2) aula com um esgotado e outro com saldo positivo, ambos presentes → a aula fica `Realizada`, o outro aluno tem `Presente == true`, `Frequencia + 1`, saldo −1 e `MotivoNaoRegistro == null` (FR-006, SC-003); (3) **todos** os presentes esgotados → a aula ainda fica `Realizada` e a resposta traz um aviso por aluno (edge case); (4) o registro **nunca** lança `ConflitoException` por causa de pacote esgotado. **Negativos (FR-007/FR-010 — cobertura explícita)**: (5) aluno esgotado marcado **ausente** → aceito, `Presente == false` com `MotivoNaoRegistro == null` (falta comum, distinguível do bloqueio) e `Avisos` vazio; (6) `SaldoAulas == null` presente → **não** bloqueia (`Presente == true`, motivo `null`); (7) vínculo `Pacote` esgotado só em **outra turma** (contexto diferente) → **não** bloqueia; (8) vínculo `Pacote` com `Ativo == false` → **não** bloqueia; (9) vínculo `Avulsa` e `Mensalidade` (mesmo com `SaldoAulas == 0` residual) → **não** bloqueiam; (10) aluno sem vínculo no contexto → **não** bloqueia (cobrado por `Valor da aula` como hoje); (11) **FR-010**: o aluno barrado não gera nenhum `Pagamento` e o `SaldoAulas` do vínculo não é alterado nem renovado automaticamente (fica em 0). **Leitura posterior (FR-012, cenário 3a)**: (12) depois de registrar, `ObterPorIdAsync(aula.Id)` devolve `Alunos[].MotivoNaoRegistro == "PacoteEsgotado"` para o aluno barrado e `null` para os demais (o motivo persistido aparece na consulta, não só na resposta do registro; o fake `FakeAulaRepositoryParaAula.AulaSemeada` já atende essa leitura).

### Implementation for User Story 2

- [X] T020 [US2] Em `src/SPI.Application/Aulas/Services/AulaService.cs`, método `RegistrarSessaoAsync`: dentro do laço existente `foreach (var vinculo in aula.AulaAlunos)` — **atenção**: a variável desse laço é um `AulaAluno`, não um `VinculoCobranca`; renomeá-la para `aulaAluno` para não confundir com o vínculo de cobrança — **antes** de gravar `aulaAluno.Presente = presente`, quando `presente == true`: chamar `_vinculoCobrancaRepository.ObterAtivoPorAlunoEContextoAsync(aulaAluno.AlunoId, aula.TurmaId, ct)` e guardar o resultado em `vinculoCobranca`; se `vinculoCobranca` existir, for `ModalidadeCobranca.Pacote` e `SaldoAulas == 0`, gravar `aulaAluno.Presente = false`, `aulaAluno.MotivoNaoRegistro = AulaAluno.MotivoPacoteEsgotado`, **não** incrementar `aulaAluno.Aluno.Frequencia`, e acumular o aviso `"Pacote esgotado: {aulaAluno.Aluno.Nome} não teve a presença registrada. Renove o pacote (edite o Vínculo de Cobrança) para liberar novos registros."`; caso contrário, comportamento atual. Depois do `Mapear(aula)` final, atribuir os avisos a `AulaResponse.Avisos`. Nunca lançar exceção por pacote esgotado; a aula continua indo para `Realizada` e `GerarContasAReceberAsync` segue inalterado (já filtra `Presente == true`). Depende de T003, T006, T008; faz T019 passar.
- [X] T021 [US2] Em `tests/SPI.Application.Tests/Aulas/AulaServiceGerarContasAReceberPacoteTests.cs`, ajustar os 2 testes da 038 que assumiam presença aceita com saldo 0 (research.md R8): `Pacote_com_saldo_zero_nao_gera_conta_e_nao_fica_negativo` e a **terceira** presença de `Sequencia_de_tres_presencas_decresce_ate_zero_sem_gerar_conta`. As asserções "saldo nunca negativo" e "nenhuma conta gerada" continuam valendo — agora porque a presença é **barrada** (`Presente == false`, `MotivoNaoRegistro == "PacoteEsgotado"`); manter o restante da sequência (5→4, 2→1→0) intacto como prova de que o débito da 038 com saldo > 0 não regrediu (FR-009).
- [X] T022 [P] [US2] Em `specs/038-vinculo-cobranca-gerar-contas/spec.md`: acrescentar uma nota curta de superação (Princípio V) apontando que o comportamento "presença com saldo 0 aceita sem bloqueio" foi substituído por `specs/041-pacote-esgotado` (bloqueio por aluno + motivo persistido), sem reescrever o restante da spec.
- [X] T023 [P] [US2] Em `frontend/lib/api/aulas.ts`: adicionar `avisos: string[]` na interface `Aula` e `motivoNaoRegistro: string | null` na interface de presença do aluno (espelhando `AulaResponse.Avisos` e `AlunoPresencaResponse.MotivoNaoRegistro`, com o comentário `/** Espelha ... */` do padrão do arquivo).
- [X] T024 [US2] Em `frontend/app/(app)/aulas/[id]/page.tsx`: (a) em `handleRegistrarSessao`, quando `atualizada.avisos.length > 0`, exibir os avisos (além do toast de sucesso atual) para a professora ver que o pacote esgotou e precisa ser renovado; (b) na lista de presenças, mostrar um selo "Pacote esgotado" ao lado do aluno cujo `motivoNaoRegistro === "PacoteEsgotado"` — só exibe o rótulo do motivo recebido, sem inferir nada (também ao reabrir a aula depois, é o histórico). Depende de T023.
- [ ] T025 [US2] (parte manual) Validar User Story 2 rodando [quickstart.md](./quickstart.md) §2 (exige backend, frontend e MySQL rodando e login): aula com um aluno de saldo 0 e outro com saldo, ambos presentes → aula `Realizada`, aviso na tela, selo "Pacote esgotado" (e ao reabrir a aula depois, continua); esgotado marcado ausente → sem aviso e sem selo; conferir no MySQL `SELECT presente, motivo_nao_registro FROM aula_aluno` para os dois casos.

**Checkpoint**: US1 e US2 funcionam juntas — alerta antecipado + bloqueio real com histórico.

---

## Phase 5: User Story 3 - Renovar o pacote e liberar o aluno na hora (Priority: P2)

**Goal**: atualizar o saldo pela edição do vínculo (specs/037) libera o aluno e o tira do painel, sem nenhuma ação adicional.

**Independent Test**: com um aluno bloqueado (saldo 0), definir saldo 10 pela edição do vínculo — a presença passa a ser aceita e ele sai do painel (quickstart §3).

*(Sem código de produção — ver "Nota estrutural" no topo. Os testes abaixo passam assim que US1 e US2 existirem.)*

- [X] T026 [P] [US3] Em `tests/SPI.Application.Tests/Aulas/AulaServiceRegistrarSessaoPacoteEsgotadoTests.cs`: (1) mesmo `VinculoCobranca` com `SaldoAulas` alterado de 0 para 10 entre dois registros de sessão (simula a edição do vínculo) → a segunda presença é aceita (`Presente == true`, `MotivoNaoRegistro == null`, saldo 10 → 9, sem aviso) (FR-008); (2) `SaldoAulas` renovado para 1 ou 2 → presença aceita e o vínculo continua sendo listado pelo painel como `"Atencao"` (cruza com `DashboardService.MontarPacotesEmAtencao`); (3) `SaldoAulas` renovado para 3 ou mais → some do painel.
- [ ] T027 [US3] (parte manual) Validar User Story 3 rodando [quickstart.md](./quickstart.md) §3 pela tela do aluno (edição do vínculo, specs/037) e por uma nova aula: saldo 0 → 10, a presença é aceita sem aviso e o aluno sai do painel da Home.

**Checkpoint**: as 3 user stories completas e validadas.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão, requisitos negativos globais e fechamento.

- [X] T028 [P] Não-regressão da 038 (FR-009): rodar `dotnet test "tests/SPI.Application.Tests" --filter "FullyQualifiedName~Aulas"` e confirmar que **todos** os testes de `GerarContasAReceber*` (Avulsa, Mensalidade, MultiplosAlunos, SemVinculo e os demais de Pacote além dos 2 ajustados em T021) continuam verdes sem alteração — o débito de saldo com saldo > 0 e as cobranças de todas as modalidades permanecem intactos.
- [X] T029 [P] Verificação de FR-010/FR-011 por leitura: (a) `git diff` de `AulaService.cs` confirma que `GerarContasAReceberAsync` **não foi alterado** (nenhuma renovação/cobrança automática ao esgotar); (b) `Grep` confirma que `DashboardController` continua com `[Authorize(Roles = nameof(PerfilUsuario.Professor))]` e não ganhou nenhum endpoint novo (o painel herda as regras de acesso já existentes); (c) `Grep` em `src/` confirma que nenhum código envia notificação/lembrete ao aluno por causa do saldo (fora de escopo — alertar o aluno).
- [X] T030 Rodar a suíte completa `dotnet test "tests/SPI.Application.Tests"` e confirmar 100% verde (baseline de T001 + os testes novos, com T021 já ajustado).
- [ ] T031 (parte manual) Rodar [quickstart.md](./quickstart.md) §4 (não-regressão ponta a ponta com o sistema no ar): saldo positivo continua consumindo exatamente 1 por presença; saldo `null`, `Avulsa` e `Mensalidade` nunca bloqueados nem listados; nenhuma conta a receber gerada ao esgotar.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências — roda primeiro.
- **Foundational (Phase 2)**: depende do Setup — BLOQUEIA US1 e US2. T003–T007 e T009 são independentes entre si (arquivos diferentes); T008 depende de T003+T005; T010 fecha a fase.
- **US1 (Phase 3)** e **US2 (Phase 4)**: ambas dependem só da Foundational e podem andar em paralelo (arquivos disjuntos, exceto `AulaService.cs` que só US2 e o T008 tocam).
- **US3 (Phase 5)**: depende de US1 e US2 existirem (seu teste T026 cruza os dois).
- **Polish (Phase 6)**: depende de todas as user stories.

### Within Each User Story

- Testes (T011, T019) escritos e falhando **antes** da implementação.
- DTOs/propriedades → serviço → frontend → validação manual.

### Parallel Opportunities

- Foundational: T003, T004, T005, T006, T007 e T009 em paralelo.
- US1: T011 e T012 e T015 em paralelo; US2: T019, T022 e T023 em paralelo.
- Polish: T028 e T029 em paralelo.

---

## Implementation Strategy

### MVP First (User Story 1)

1. Setup (T001) → Foundational (T002–T010).
2. US1 (T011–T018) → **PARAR e VALIDAR**: o painel já avisa quem está acabando o pacote.

### Incremental Delivery

1. Foundational → base pronta.
2. US1 → alerta antecipado (MVP).
3. US2 → bloqueio real com motivo persistido e aviso.
4. US3 → prova de que a renovação libera o aluno, sem código novo.
5. Polish → não-regressão da 038, verificação dos requisitos negativos e suíte completa.

## Notes

- T002 é a migração de banco: aplicada via `mysql` CLI durante a implementação (preferência já estabelecida), com a connection string do User Secrets — nunca commitar credenciais.
- As tarefas `(parte manual)` (T018, T025, T027, T031) exigem sistema rodando + login e ficam pendentes para a professora/usuário validar; não bloqueiam a passagem do card para "Revisão de código".
