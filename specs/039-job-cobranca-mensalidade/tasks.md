---

description: "Lista de tarefas para Job de Cobrança Automática de Mensalidade"
---

# Tasks: Job de Cobrança Automática de Mensalidade

**Input**: Documentos de design em `/specs/039-job-cobranca-mensalidade/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/README.md](./contracts/README.md), [quickstart.md](./quickstart.md)

**Tests**: INCLUÍDOS — `plan.md` planeja a primeira suíte de teste para a lógica de um `BackgroundService`, e a regra do usuário exige cobertura explícita para requisitos negativos (FR-009, FR-010 já cobertos por specs/038; aqui FR-004/FR-005 são os "MUST NOT" próprios desta feature).

**Organization**: Tarefas agrupadas por user story (US1=P1 geração automática; US2=P1 idempotência; US3=P2 vínculo excluído não é cobrado).

**Nota estrutural importante** (mesmo padrão de specs/038, research.md R7): `DeveDispararNesteMomento` (a condição de disparo) e `GerarCobrancasDoMesAsync` (a geração em si — que já embute a checagem de idempotência de FR-004 e o filtro `Ativo` de FR-005, porque vêm do mesmo `ListarAtivosPorModalidadeAsync`/mesma checagem por vínculo) nascem juntos, na implementação de US1 (T014), por serem inseparáveis de um único fluxo. US2 e US3 continuam **testáveis de forma independente**: cada uma tem sua própria suíte, que já passa assim que T014 existe, sem exigir nenhuma implementação própria.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1, US2 ou US3 (só nas fases de user story)
- Caminhos relativos à raiz do repositório `C:\PROJETO - SPI`
- Identificadores em C# em pt-BR sem acentuação; comentários com acentuação (padrão do projeto)

## Path Conventions

Web app em camadas: `src/SPI.Domain`, `src/SPI.Application`, `src/SPI.Infrastructure`, `src/SPI.Api`, `frontend/`, `tests/SPI.Application.Tests`, `database/`.

---

## Phase 1: Setup

**Purpose**: garantir base verde antes de qualquer alteração

- [X] T001 Rodar `dotnet test "tests/SPI.Application.Tests"` para registrar a linha de base (deve estar 100% verde)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: schema novo (categoria + FK de idempotência), entidade, repositórios, configuração e fakes de teste de que TODAS as user stories dependem

**⚠️ CRITICAL**: nenhuma user story começa antes desta fase terminar

- [X] T002 [P] Criar `database/15_job_cobranca_mensalidade.sql` (padrão de `database/10_financeiro_contas.sql` e `database/14_vinculo_cobranca.sql`: `USE spi_db;`, cabeçalho de descrição, comentário por coluna). Duas partes: (1) `INSERT INTO categoria_receita (nome, ativo) VALUES ('Mensalidade', TRUE);` (research.md R6); (2) `ALTER TABLE pagamento ADD COLUMN vinculo_cobranca_id INT NULL COMMENT 'Vinculo de cobranca que originou esta cobranca automatica de mensalidade (specs/039); NULL para pagamentos manuais ou gerados por presenca (specs/038)', ADD CONSTRAINT fk_pagamento_vinculo_cobranca FOREIGN KEY (vinculo_cobranca_id) REFERENCES vinculo_cobranca(id), ADD CONSTRAINT uq_pagamento_vinculo_competencia UNIQUE (vinculo_cobranca_id, competencia);` — comentário explicando por que a unicidade sustenta FR-004 no banco e por que `NULL` nunca colide em `UNIQUE` do MySQL (research.md R1, mesmo raciocínio de `uq_vinculocobranca_chave_ativa` em specs/037).
- [X] T003 Aplicar `database/15_job_cobranca_mensalidade.sql` com o cliente `mysql` (connection string dos .NET User Secrets do `src/SPI.Api`; nunca de `appsettings.json`, nunca imprimir a senha no output; depende de T002). Validar no banco: `SELECT id FROM categoria_receita WHERE nome = 'Mensalidade'` devolve 1 linha; inserir 2 `Pagamento` de teste com o mesmo `vinculo_cobranca_id` e a mesma `competencia` → o segundo falha com "Duplicate entry"; com `vinculo_cobranca_id = NULL` em ambos, os dois passam (sem colisão). Apagar as linhas de teste ao final.
- [X] T004 [P] Editar `src/SPI.Domain/Entities/Pagamento.cs` (depende de T002/T003 apenas para consistência com o schema, não bloqueia compilação): acrescentar `public int? VinculoCobrancaId { get; set; }` e `public VinculoCobranca? VinculoCobranca { get; set; }` (navegação, sem coleção recíproca em `VinculoCobranca` — não é necessária nesta fatia, data-model.md). Não alterar mais nada na entidade.
- [X] T005 [P] Editar `src/SPI.Infrastructure/Persistence/Configurations/PagamentoConfiguration.cs`: `builder.Property(p => p.VinculoCobrancaId).HasColumnName("vinculo_cobranca_id");` e `builder.HasOne(p => p.VinculoCobranca).WithMany().HasForeignKey(p => p.VinculoCobrancaId).HasConstraintName("fk_pagamento_vinculo_cobranca");` (sem `WithMany(v => ...)`, pois `VinculoCobranca` não tem coleção recíproca). Não alterar mais nada no arquivo.
- [X] T006 [P] Editar `src/SPI.Domain/Repositories/IVinculoCobrancaRepository.cs`: acrescentar `Task<List<VinculoCobranca>> ListarAtivosPorModalidadeAsync(ModalidadeCobranca modalidade, CancellationToken cancellationToken = default);` com comentário explicando o propósito (todo vínculo `Ativo == true` com a modalidade informada, usado pelo job de mensalidade — specs/039). Não alterar nenhum método existente.
- [X] T007 [P] Editar `src/SPI.Infrastructure/Repositories/VinculoCobrancaRepository.cs` (depende de T006): implementar `ListarAtivosPorModalidadeAsync` como `_dbContext.VinculosCobranca.Include(v => v.Turma).Where(v => v.Ativo && v.Modalidade == modalidade).ToListAsync(cancellationToken)` — o `Include(v => v.Turma)` é necessário para montar a Descrição da cobrança (research.md R5) sem consulta adicional.
- [X] T008 [P] Editar `src/SPI.Domain/Repositories/IPagamentoRepository.cs`: acrescentar `Task<bool> ExisteMensalidadeGeradaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default);` com comentário explicando o propósito (idempotência de FR-004 — ver `uq_pagamento_vinculo_competencia`, T002). Não alterar nenhum método existente.
- [X] T009 [P] Editar `src/SPI.Infrastructure/Repositories/PagamentoRepository.cs` (depende de T008): implementar `ExisteMensalidadeGeradaAsync` como `_dbContext.Pagamentos.AnyAsync(p => p.VinculoCobrancaId == vinculoCobrancaId && p.Competencia == competencia, cancellationToken)`.
- [X] T010 [P] Editar `src/SPI.Api/appsettings.json`: acrescentar, ao lado de `"Lembretes": { "IntervaloVerificacaoSegundos": 60 }`, a seção `"Mensalidade": { "IntervaloVerificacaoSegundos": 3600 }` (research.md R3 — 3600s garante um tick por hora-do-dia todo dia, cobrindo a janela 23h–23h59 de forma confiável, diferente de um intervalo literal de 1 dia). Não alterar mais nada no arquivo.
- [X] T011 [P] Criar `tests/SPI.Application.Tests/Mensalidade/MensalidadeFakes.cs` (namespace `SPI.Application.Tests.Mensalidade`; fakes manuais, mesmo padrão de `Aulas/AulaServiceFakes.cs` e `VinculosCobranca/VinculoCobrancaFakes.cs`). Classes:
  - `FakeVinculoCobrancaRepositoryParaMensalidade : IVinculoCobrancaRepository` — `public List<VinculoCobranca> Vinculos { get; } = new();`; `ListarAtivosPorModalidadeAsync(modalidade, ct)` devolve `Vinculos.Where(v => v.Ativo && v.Modalidade == modalidade).ToList()`; `ObterPorIdAsync`, `ListarPorAlunoAsync`, `ExisteAtivoAsync`, `ObterAtivoPorAlunoEContextoAsync`, `AdicionarAsync`, `SalvarAlteracoesAsync` lançam `NotImplementedException` — **prova estrutural** de que o job nunca escreve nem consulta o cadastro do vínculo além da listagem por modalidade.
  - `FakePagamentoRepositoryParaMensalidade : IPagamentoRepository` — `public List<Pagamento> Gerados { get; } = new();`, `public int Salvamentos { get; private set; }`, `public HashSet<(int VinculoCobrancaId, DateOnly Competencia)> JaGerados { get; } = new();` (seedável pelo teste para simular idempotência pré-existente), `public Func<Pagamento, bool>? FalharAoAdicionarSe { get; set; }` (hook para T021, FR-011); `ExisteMensalidadeGeradaAsync(vinculoCobrancaId, competencia, ct)` devolve `JaGerados.Contains((vinculoCobrancaId, competencia)) || Gerados.Any(p => p.VinculoCobrancaId == vinculoCobrancaId && p.Competencia == competencia)`; `AdicionarAsync` lança `InvalidOperationException` se `FalharAoAdicionarSe?.Invoke(pagamento) == true`, senão atribui `Id` sequencial e adiciona a `Gerados`; `SalvarAlteracoesAsync` incrementa `Salvamentos`; `ObterPorIdAsync`, `ListarAsync`, `VincularAulaAsync`, `AtualizarStatusViaProcedureAsync` lançam `NotImplementedException`.
  - `FakeCategoriaReceitaRepositoryParaMensalidade : ICategoriaReceitaRepository` — `public CategoriaReceita? Mensalidade { get; set; }`; `ObterPorNomeAsync(nome, ct)` devolve `Mensalidade` quando `nome == "Mensalidade"`, senão `null`; `ObterPorIdAsync`, `ListarAtivasAsync` lançam `NotImplementedException`.

**Checkpoint**: `dotnet build` da solução compila (schema, entidade e repositórios novos); fundação pronta.

---

## Phase 3: User Story 1 — Gerar automaticamente a cobrança mensal no fim do mês (Priority: P1) 🎯 MVP

**Goal**: no último dia do mês, 23h ou depois, cada Vínculo de Cobrança Mensalidade ativo gera sua própria conta a receber pendente, pelo valor cheio do vínculo, independente de presença.

**Independent Test**: quickstart.md §3 cenário 1 — dois vínculos Mensalidade ativos (contextos diferentes) do mesmo aluno, acionar a geração do mês, confirmar duas contas novas, cada uma com o valor certo.

### Tests for User Story 1 (escrever primeiro; devem FALHAR antes da implementação — `MensalidadeDispatcherService` ainda não existe)

- [X] T012 [P] [US1] Criar `tests/SPI.Application.Tests/Mensalidade/MensalidadeDispatcherServiceDeveDispararTests.cs` (usa `MensalidadeDispatcherService.DeveDispararNesteMomento`, `Theory`/`InlineData`): dia comum a qualquer hora → falso; último dia do mês às 22:59 → falso; último dia às 23:00 → verdadeiro; último dia às 23:59 → verdadeiro; último dia às 00:00 do dia seguinte (já não é mais o último dia) → falso; casos de fevereiro com 28 e 29 dias (ano bissexto), e meses de 30 e 31 dias, todos no dia certo às 23h → verdadeiro (FR-001).
- [X] T013 [P] [US1] Criar `tests/SPI.Application.Tests/Mensalidade/MensalidadeDispatcherServiceGerarCobrancasTests.cs` (usa os fakes de T011; chama `MensalidadeDispatcherService.GerarCobrancasDoMesAsync(vinculoRepo, pagamentoRepo, categoriaRepo, competencia, logger: null, ct)` diretamente — sem `IServiceScopeFactory`, sem `BackgroundService`, research.md R7): (1) um vínculo Mensalidade ativo com Turma, Valor 300 → `Gerados` tem exatamente 1 `Pagamento` com `ValorFinal == 300`, `Status == "Pendente"`, `Competencia == competencia`, `CategoriaReceitaId ==` id da categoria "Mensalidade", `VinculoCobrancaId ==` id do vínculo, `Descricao` contendo o nome da turma e `{competencia:MM/yyyy}` (US1-1, FR-002, FR-006); (2) mesmo vínculo mas de atendimento individual (`TurmaId = null`) → `Descricao` contém "Atendimento individual" (research.md R5); (3) `DataVencimento` do `Pagamento` gerado é sempre o dia 1 do mês seguinte à `competencia` (FR-007); (4) dois vínculos Mensalidade ativos do MESMO aluno, em contextos diferentes (uma turma + individual) → `Gerados` tem exatamente 2 `Pagamento`s, um por vínculo, cada um com o `ValorFinal` do respectivo vínculo (US1-3, FR-003); (5) nenhum vínculo Mensalidade ativo (lista vazia ou só vínculos de outras modalidades) → `Gerados` continua vazio, sem lançar exceção.

### Implementation for User Story 1

- [X] T014 [US1] Criar `src/SPI.Infrastructure/BackgroundServices/MensalidadeDispatcherService.cs` (depende de T004–T009; mesmo padrão de `LembreteDispatcherService.cs`: `BackgroundService`, `IServiceScopeFactory`, `IConfiguration` lida no construtor com `configuration.GetValue<int?>("Mensalidade:IntervaloVerificacaoSegundos") ?? 3600`, `ILogger<MensalidadeDispatcherService>`):
  - `public static bool DeveDispararNesteMomento(DateTime agora) => agora.Day == DateTime.DaysInMonth(agora.Year, agora.Month) && agora.Hour >= 23;` (research.md R2/R7; faz T012 passar).
  - `public static async Task GerarCobrancasDoMesAsync(IVinculoCobrancaRepository vinculoCobrancaRepository, IPagamentoRepository pagamentoRepository, ICategoriaReceitaRepository categoriaReceitaRepository, DateOnly competencia, ILogger? logger, CancellationToken cancellationToken)` — método estático, testável sem `IServiceScopeFactory` (research.md R7, mesmo espírito). Corpo: obter a categoria "Mensalidade" via `ObterPorNomeAsync`; `var vinculos = await vinculoCobrancaRepository.ListarAtivosPorModalidadeAsync(ModalidadeCobranca.Mensalidade, cancellationToken);` (já aplica FR-005: só vínculos `Ativo == true` no momento da chamada); para cada `vinculo` em `vinculos`, dentro de um `try/catch` individual (FR-011: uma falha loga e continua, não interrompe os demais nem propaga): se `await pagamentoRepository.ExisteMensalidadeGeradaAsync(vinculo.Id, competencia, cancellationToken)` → `continue` (FR-004); senão monta `contexto = vinculo.TurmaId.HasValue ? vinculo.Turma!.Nome : "Atendimento individual"` (research.md R5) e cria `new Pagamento { AlunoId = vinculo.AlunoId, VinculoCobrancaId = vinculo.Id, Descricao = $"Mensalidade - {contexto} - {competencia:MM/yyyy}", CategoriaReceitaId = categoria?.Id, DataVencimento = competencia.AddMonths(1), Competencia = competencia, ValorFinal = vinculo.Valor, Status = "Pendente" }` (research.md R4/R6), chama `AdicionarAsync` + `SalvarAlteracoesAsync`. Loga (`logger?.LogInformation`) a quantidade de cobranças geradas ao final. Faz T013 passar.
  - `protected override async Task ExecuteAsync(CancellationToken stoppingToken)` — `PeriodicTimer` com o intervalo lido no construtor; a cada tick, dentro de um `try/catch` (`when (e is not OperationCanceledException)`, mesmo padrão de `LembreteDispatcherService`), chama `if (DeveDispararNesteMomento(DateTime.Now)) await ProcessarCobrancasMensaisAsync(stoppingToken);`.
  - `private async Task ProcessarCobrancasMensaisAsync(CancellationToken cancellationToken)` — cria escopo com `_scopeFactory.CreateScope()`, resolve `IVinculoCobrancaRepository`, `IPagamentoRepository`, `ICategoriaReceitaRepository`, calcula `competencia = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1)` e chama `GerarCobrancasDoMesAsync(...)`.
- [X] T015 [US1] Editar `src/SPI.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` (depende de T014): acrescentar `services.AddHostedService<MensalidadeDispatcherService>();` logo após `services.AddHostedService<LembreteDispatcherService>();`. Não alterar mais nada no arquivo.
- [X] T016 [US1] Validar US1: `dotnet test "tests/SPI.Application.Tests" --filter FullyQualifiedName~MensalidadeDispatcherServiceDeveDispararTests|FullyQualifiedName~MensalidadeDispatcherServiceGerarCobrancasTests` verde; `dotnet build` da solução sem erros

**Checkpoint**: US1 funcional e testável sozinha — MVP. `GerarCobrancasDoMesAsync` já contém a checagem de idempotência e o filtro de vínculos ativos (nota estrutural no topo); US2 e US3 só precisam dos próprios testes.

---

## Phase 4: User Story 2 — Nunca duplicar a cobrança do mesmo mês (Priority: P1)

**Goal**: gerar a cobrança do mesmo vínculo na mesma competência mais de uma vez nunca duplica a conta a receber.

**Independent Test**: quickstart.md §3 cenário 2 — acionar a geração do mesmo mês duas vezes seguidas, confirmar que só a primeira gerou conta.

### Tests for User Story 2 (a implementação já existe desde T014 — este teste só precisa ser escrito e já deve passar)

- [X] T017 [P] [US2] Criar `tests/SPI.Application.Tests/Mensalidade/MensalidadeDispatcherServiceIdempotenciaTests.cs` (mesmo padrão de T013): (1) chamar `GerarCobrancasDoMesAsync` duas vezes seguidas para o mesmo vínculo e a mesma competência (a segunda chamada usando o mesmo `FakePagamentoRepositoryParaMensalidade`, já populado pela primeira) → `Gerados` continua com exatamente 1 `Pagamento` após a segunda chamada (US2-1, FR-004); (2) semear `JaGerados` do fake com `(vinculo.Id, competencia)` **antes** de qualquer chamada (simulando que o sistema foi reiniciado e a cobrança já existia de uma execução anterior) → `GerarCobrancasDoMesAsync` não adiciona nada a `Gerados` (US2-2, simula reinício); (3) o mesmo vínculo em **competências diferentes** (mês atual e mês seguinte) → gera uma conta para cada competência (confirma que a idempotência é por competência, não global).

### Implementation for User Story 2

*Nenhuma tarefa de implementação nova — a checagem de idempotência já foi entregue em T014 (ver nota estrutural no topo do arquivo). T017 valida esse comportamento de forma isolada.*

- [X] T018 [US2] Validar US2: `dotnet test "tests/SPI.Application.Tests" --filter FullyQualifiedName~MensalidadeDispatcherServiceIdempotenciaTests` verde (deve passar sem nenhuma mudança de código desde T014)

**Checkpoint**: US1 e US2 cobertas por teste e funcionando.

---

## Phase 5: User Story 3 — Vínculo excluído antes do disparo não é cobrado (Priority: P2)

**Goal**: um Vínculo de Cobrança Mensalidade excluído (inativo) no momento do disparo não gera cobrança naquele mês.

**Independent Test**: quickstart.md §3 cenário 3 — excluir um vínculo antes de acionar a geração, confirmar que ele não recebe cobrança, mas o outro vínculo ativo do mesmo aluno recebe normalmente.

### Tests for User Story 3 (a implementação já existe desde T014 — este teste só precisa ser escrito e já deve passar)

- [X] T019 [P] [US3] Criar `tests/SPI.Application.Tests/Mensalidade/MensalidadeDispatcherServiceExclusaoTests.cs` (mesmo padrão de T013): (1) vínculo Mensalidade com `Ativo = false` → `GerarCobrancasDoMesAsync` não gera nenhum `Pagamento` para ele (US3-1, FR-005) — `ListarAtivosPorModalidadeAsync` do fake já filtra por `Ativo`, então basta semear o vínculo inativo e confirmar `Gerados` vazio; (2) dois vínculos do mesmo aluno, um `Ativo = true` e outro `Ativo = false` → só o ativo gera conta (edge case "múltiplos vínculos, um excluído"); (3) vínculo reativado (`Ativo = true` no momento da chamada, mesmo tendo sido excluído e reativado antes) → gera a cobrança normalmente, pois só o estado `Ativo` no momento importa (US3-2).

### Implementation for User Story 3

*Nenhuma tarefa de implementação nova — o filtro por `Ativo` já vem de `ListarAtivosPorModalidadeAsync` (T007), usado por `GerarCobrancasDoMesAsync` desde T014 (ver nota estrutural no topo do arquivo). T019 valida esse comportamento de forma isolada.*

- [X] T020 [US3] Validar US3: `dotnet test "tests/SPI.Application.Tests" --filter FullyQualifiedName~MensalidadeDispatcherServiceExclusaoTests` verde (deve passar sem nenhuma mudança de código desde T014)

**Checkpoint**: as 3 user stories cobertas por teste e funcionando — feature completa (nota estrutural: aqui as stories convergem, porque é um único fluxo de geração).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: isolamento de falha por vínculo (FR-011), fechamento da exceção EX-001 (UI + registro), e validação final

- [X] T021 [P] Criar `tests/SPI.Application.Tests/Mensalidade/MensalidadeDispatcherServiceIsolamentoFalhaTests.cs` (depende de T014) — **cobertura de FR-011**: três vínculos Mensalidade ativos do mesmo aluno (ou de alunos diferentes); configurar `FakePagamentoRepositoryParaMensalidade.FalharAoAdicionarSe` para lançar ao processar o vínculo do meio (ex.: `p => p.VinculoCobrancaId == vinculoB.Id`); chamar `GerarCobrancasDoMesAsync` passando um `ILogger` fake/nulo e confirmar: (a) a chamada **não lança exceção** para quem a invocou (o `try/catch` interno absorve); (b) `Gerados` contém as cobranças dos vínculos A e C (os que não falharam); (c) nenhuma cobrança para o vínculo B (o que falhou), sem deixar o restante da lista de ser processado.
- [X] T022 [P] Editar `frontend/components/alunos/VinculosCobrancaSection.tsx` (FR-013, fecha EX-001 por completo): remover o bloco inteiro do aviso "Este vínculo é apenas um cadastro..." (identificado pelo comentário `{/* EX-001: remover este aviso quando a cobranca automatica passar a usar o vinculo (proxima fatia). */}` e a `<div>` logo abaixo) — depois de specs/038 (Avulsa/Pacote) e specs/039 (Mensalidade), não sobra nenhuma modalidade sem efeito automático real, então o aviso deixa de ter qualquer caso válido. Não alterar mais nada no arquivo (o restante da seção — listagem, modal, "Mostrar excluídos" — continua igual).
- [X] T023 Editar `specs/037-vinculo-cobranca/plan.md`, bloco "EX-001 — Registro da exceção ao Princípio III" (Complexity Tracking): trocar **Status: 🟡 PARCIALMENTE RESOLVIDA (specs/038)** por **Status: 🔒 RESOLVIDA (specs/039)**, acrescentando uma linha final confirmando que, com a entrega do job de Mensalidade (specs/039), as três modalidades (Avulsa, Pacote, Mensalidade) têm efeito automático real e o aviso da UI foi removido (T022) — a exceção está encerrada, conforme o compromisso original. Não alterar mais nada nesse arquivo.
- [X] T024 Rodar a suíte completa `dotnet test "tests/SPI.Application.Tests"` (todos os testes verdes, incluindo specs/037/038) e `dotnet build` da solução; depois `git -C "C:\PROJETO - SPI" diff --stat -- src/SPI.Application/Aulas src/SPI.Application/VinculosCobranca src/SPI.Api/Controllers` (esperado: vazio — nenhum controller, nenhum endpoint, nem a lógica de presença (specs/038) ou o cadastro do vínculo (specs/037) tocados por esta feature, contracts/README.md).
- [X] T025 Executar quickstart.md completo (§1 e parte do §2 já cobertos por T003/T024; §3, os 5 cenários via chamada direta de `GerarCobrancasDoMesAsync`, com o backend rodando e um pequeno harness/console referenciando `SPI.Infrastructure` — mesmo padrão usado para validar o repositório em specs/037/038; §4, checagem de não-regressão e confirmação visual de que o aviso não aparece mais na seção "Vínculos de Cobrança")

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001)**: sem dependências.
- **Foundational (T002–T011)**: depende de T001; **bloqueia** as user stories. Ordem interna: T002 → T003; (T004, T006, T008, T010, T011) podem rodar em paralelo entre si (arquivos diferentes, sem dependência um do outro); T005 depende de T004; T007 depende de T006; T009 depende de T008.
- **US1 (T012–T016)**: depende da Foundational. **US2 (T017–T018), US3 (T019–T020)**: dependem de T014 (dentro de US1) ter sido concluída — ver nota estrutural no topo. Seus testes podem ser **escritos** em paralelo a US1 (dependem só dos fakes de T011), mas só **passam** depois de T014.
- **Polish (T021–T025)**: T021 depende de T014; T022 é independente de código backend (só depende da decisão de negócio, ou seja, pode ser feito a qualquer momento, mas faz mais sentido depois de US1-3 confirmarem o job funcional); T023 depende de T022 (o registro de EX-001 cita a remoção do aviso); T024–T025 dependem de todas as stories.

### Within Each Story

- Testes primeiro e falhando (T012, T013) → implementação (T014) → validação.
- US2/US3: teste primeiro (falha por falta da classe; passa imediatamente depois de T014) → validação.

### User Story Dependencies

- **US1**: bloqueia US2/US3 na prática (mesma implementação), mas cada uma é **testável de forma independente** por ter sua própria suíte.

## Parallel Opportunities

- Foundational: T002 sozinho até T003; em paralelo, T004 ‖ T006 ‖ T008 ‖ T010 ‖ T011; depois T005 (após T004) ‖ T007 (após T006) ‖ T009 (após T008).
- T012 (US1) ‖ T013 (US1) ‖ T017 (US2) ‖ T019 (US3) ‖ T021 (Polish) podem todos ser **escritos** em paralelo entre si (arquivos diferentes, todos dependem só de T011) — só a execução/validação de US2/US3/T021 depende de T014 estar pronta.

### Parallel Example: escrever todos os testes antes da implementação

```text
# Podem ser escritos em paralelo (todos dependem só de T011):
T012  MensalidadeDispatcherServiceDeveDispararTests.cs
T013  MensalidadeDispatcherServiceGerarCobrancasTests.cs
T017  MensalidadeDispatcherServiceIdempotenciaTests.cs
T019  MensalidadeDispatcherServiceExclusaoTests.cs
T021  MensalidadeDispatcherServiceIsolamentoFalhaTests.cs

# Só depois, em sequência: T014 (implementação) faz todos passarem de uma vez.
```

## Implementation Strategy

### MVP First (User Story 1)

1. T001 → Foundational (T002–T011) → US1 (T012–T016).
2. **PARAR e validar** com quickstart §3 cenário 1.
3. Nesse ponto, idempotência e respeito à exclusão já estão embutidos (nota estrutural) — só US2/US3 ainda não têm teste formal.

### Incremental Delivery (validação, não implementação)

1. Base + Foundational + US1 → MVP com geração automática garantida por teste.
2. + US2 → idempotência coberta por teste (sem nova implementação).
3. + US3 → exclusão coberta por teste (sem nova implementação).
4. + Polish → isolamento de falha (FR-011), EX-001 fechada (UI + registro), validação final.

## Notes

- **Nota estrutural** (repetida do topo por importância): T014 implementa `DeveDispararNesteMomento` e `GerarCobrancasDoMesAsync` de uma vez, porque idempotência (FR-004) e respeito à exclusão (FR-005) nascem do mesmo fluxo de listagem/checagem — não é possível decompor por story sem duplicar a mesma lógica. A organização por story serve para **teste e rastreabilidade de requisito**, não para entrega incremental de código.
- **Requisitos negativos com cobertura explícita**: FR-004 (nunca duplicar) → T017; FR-005 (excluído não gera cobrança) → T019; FR-009/FR-010 (nenhuma sobreposição com Pacote/Avulsa/pacote esgotado) → já cobertos por specs/038, reforçados pelo `git diff` vazio de T024; FR-011 (falha isolada) → T021.
- **EX-001**: T022 (remover o aviso) e T023 (atualizar o registro) são obrigatórias juntas — a exceção só fecha de verdade quando as duas acontecerem; fazer só uma das duas deixaria a UI ou a documentação inconsistente com a outra.
- Migração aplicada na implementação via `mysql` CLI (T003), conforme preferência já registrada do usuário; connection string sempre dos User Secrets.
- Commitar por tarefa ou grupo lógico.
