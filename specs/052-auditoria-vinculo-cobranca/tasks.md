---

description: "Task list for Auditoria de Aulas Dadas por Vínculo de Cobrança"
---

# Tasks: Auditoria de Aulas Dadas por Vínculo de Cobrança

**Input**: Design documents from `specs/052-auditoria-vinculo-cobranca/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/vinculos-cobranca-api-delta.md](./contracts/vinculos-cobranca-api-delta.md), [quickstart.md](./quickstart.md)

**Tests**: INCLUÍDOS — mesmo padrão já usado em specs/038/039 (xUnit, fakes manuais); a regra do `CLAUDE.md` exige cobertura explícita para os FRs "MUST NOT" (FR-011 a FR-014), além da cobertura normal das 4 user stories.

**Organization**: tarefas agrupadas por user story (US1=P1 total do Pacote; US2=P1 associação de presença a vínculo; US3=P2 teto/contagem mensal da Mensalidade; US4=P2 bloqueio de edição — depende de US1+US3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1, US2, US3 ou US4 (só nas fases de user story)
- Caminhos relativos à raiz do repositório `C:\PROJETO - SPI`
- Identificadores em C# em pt-BR sem acentuação; comentários com acentuação (padrão do projeto)

## Path Conventions

Web app em camadas: `src/SPI.Domain`, `src/SPI.Application`, `src/SPI.Infrastructure`, `src/SPI.Api`, `tests/SPI.Application.Tests`, `database/`. Nenhum arquivo em `frontend/` é tocado (FR-013).

---

## Phase 1: Setup

- [X] T001 Rodar `dotnet test "tests/SPI.Application.Tests"` para registrar a linha de base (deve estar 100% verde antes de qualquer mudança)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: schema novo e as 2 entidades que TODAS as user stories dependem

**⚠️ CRITICAL**: nenhuma user story começa antes desta fase terminar

- [X] T002 [P] Criar `database/18_auditoria_vinculo_cobranca.sql` (padrão de `database/14_vinculo_cobranca.sql`/`database/15_job_cobranca_mensalidade.sql`: `USE spi_db;`, cabeçalho de descrição, comentário por coluna). Três partes: (1) `ALTER TABLE vinculo_cobranca ADD COLUMN total_aulas_pacote INT NULL COMMENT 'Total original de aulas do pacote, imutavel por consumo (specs/052); so se aplica a modalidade Pacote', ADD CONSTRAINT chk_vinculocobranca_total_aulas_pacote CHECK (total_aulas_pacote IS NULL OR (modalidade = 'Pacote' AND total_aulas_pacote >= 1));` (research.md R1); (2) `ALTER TABLE aula_aluno ADD COLUMN vinculo_cobranca_id INT NULL COMMENT 'Vinculo de cobranca ativo no momento desta presenca, para auditoria historica (specs/052); NULL quando nao havia vinculo correspondente', ADD CONSTRAINT fk_aulaaluno_vinculo_cobranca FOREIGN KEY (vinculo_cobranca_id) REFERENCES vinculo_cobranca(id);` — sem índice único, ao contrário de `pagamento.vinculo_cobranca_id` (research.md R1); (3) backfill `UPDATE vinculo_cobranca SET total_aulas_pacote = saldo_aulas WHERE modalidade = 'Pacote' AND saldo_aulas IS NOT NULL;` (research.md R2)
- [X] T003 Aplicar `database/18_auditoria_vinculo_cobranca.sql` com o cliente `mysql` (connection string dos .NET User Secrets do `src/SPI.Api`; nunca de `appsettings.json`, nunca imprimir a senha no output; depende de T002). Validar: `SELECT id, total_aulas_pacote, saldo_aulas FROM vinculo_cobranca WHERE modalidade = 'Pacote'` mostra `total_aulas_pacote` preenchido igual a `saldo_aulas` pré-existente; tentar `UPDATE vinculo_cobranca SET total_aulas_pacote = 5 WHERE modalidade = 'Mensalidade' LIMIT 1` falha por CHECK; tentar inserir `aula_aluno.vinculo_cobranca_id` com um id inexistente falha por FK
- [X] T004 [P] Editar `src/SPI.Domain/Entities/VinculoCobranca.cs`: acrescentar `public int? TotalAulasPacote { get; set; }` (comentário explicando: só Pacote, imutável por consumo automático, specs/052). Não alterar mais nada na entidade
- [X] T005 [P] Editar `src/SPI.Domain/Entities/AulaAluno.cs`: acrescentar `public int? VinculoCobrancaId { get; set; }` e `public VinculoCobranca? VinculoCobranca { get; set; }` (navegação, sem coleção recíproca em `VinculoCobranca` — mesmo padrão de `Pagamento.VinculoCobranca`, specs/039). Não alterar mais nada na entidade
- [X] T006 [P] Editar `src/SPI.Infrastructure/Persistence/Configurations/VinculoCobrancaConfiguration.cs` (depende de T004): `builder.Property(v => v.TotalAulasPacote).HasColumnName("total_aulas_pacote");`
- [X] T007 [P] Editar `src/SPI.Infrastructure/Persistence/Configurations/AulaAlunoConfiguration.cs` (depende de T005): `builder.Property(aa => aa.VinculoCobrancaId).HasColumnName("vinculo_cobranca_id");` e `builder.HasOne(aa => aa.VinculoCobranca).WithMany().HasForeignKey(aa => aa.VinculoCobrancaId).HasConstraintName("fk_aulaaluno_vinculo_cobranca");` (sem `WithMany(v => ...)`, pois `VinculoCobranca` não tem coleção recíproca)

**Checkpoint**: `dotnet build` da solução compila (schema, entidades e configurações novas); fundação pronta.

---

## Phase 3: User Story 1 — Saber quantas aulas de um Pacote já foram dadas, sempre (Priority: P1) 🎯 MVP

**Goal**: `TotalAulasPacote` é persistido na criação do vínculo, nunca decrementado automaticamente; "aulas dadas" (`AulasDadas`) é exposto na resposta, calculado a partir de `TotalAulasPacote - SaldoAulas`.

**Independent Test**: quickstart.md §1 — cadastrar Pacote com 10 aulas, registrar 3 presenças, confirmar `totalAulasPacote: 10` (inalterado) e `aulasDadas: 3`.

### Tests for User Story 1 (escrever primeiro; devem FALHAR antes da implementação)

- [X] T008 [P] [US1] Estender `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaRequestValidatorTests.cs`: `TotalAulasPacote` informado com `Modalidade = Mensalidade` → erro "Total de aulas do pacote so se aplica a modalidade Pacote."; `TotalAulasPacote = 0` com `Modalidade = Pacote` → erro "Total de aulas do pacote deve ser no minimo 1."; `TotalAulasPacote = null` → nenhum erro, para qualquer modalidade (FR-001)
- [X] T009 [P] [US1] Estender `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServiceCadastrarTests.cs`: cadastrar vínculo Pacote com `TotalAulasPacote = 10` → `VinculoCobrancaResponse.TotalAulasPacote == 10` e `AulasDadas == 0` (saldo igual ao total na criação, FR-001)
- [X] T010 [P] [US1] Criar `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServiceAuditoriaTests.cs` (novo arquivo — vai acumular também os testes de US3/US4 nas fases seguintes): (1) vínculo Pacote com `TotalAulasPacote = 10`, `SaldoAulas = 7` (simulando 3 presenças já consumidas por specs/038) → `AulasDadas == 3` (FR-003); (2) `TotalAulasPacote = null` (vínculo nunca migrado) → `AulasDadas == null`, sem lançar exceção (Edge Case); (3) vínculo Mensalidade ou Avulsa → `AulasDadas == null` sempre (campo exclusivo de Pacote)

### Implementation for User Story 1

- [X] T011 [US1] Editar `src/SPI.Application/VinculosCobranca/Validators/VinculoCobrancaRequestValidator.cs`: dentro de um novo bloco `When(x => x.TotalAulasPacote.HasValue, ...)`, acrescentar `RuleFor(x => x.TotalAulasPacote).Must((request, _) => request.Modalidade == ModalidadeCobranca.Pacote).WithMessage("Total de aulas do pacote so se aplica a modalidade Pacote.");` e `RuleFor(x => x.TotalAulasPacote).GreaterThanOrEqualTo(1).WithMessage("Total de aulas do pacote deve ser no minimo 1.");` (mesmo padrão dos blocos `AulasIncluidas`/`SaldoAulas` já existentes no mesmo arquivo). Faz T008 passar
- [X] T012 [US1] Editar `src/SPI.Application/VinculosCobranca/Dtos/VinculoCobrancaRequest.cs`: acrescentar `public int? TotalAulasPacote { get; set; }` com comentário `// Somente com Modalidade = Pacote.`
- [X] T013 [US1] Editar `src/SPI.Application/VinculosCobranca/Dtos/VinculoCobrancaResponse.cs`: acrescentar `public int? TotalAulasPacote { get; set; }` e `public int? AulasDadas { get; set; }` (somente-leitura, nunca aceito em request)
- [X] T014 [US1] Editar `src/SPI.Application/VinculosCobranca/Services/VinculoCobrancaService.cs` (depende de T011–T013): em `CadastrarAsync`, atribuir `TotalAulasPacote = request.TotalAulasPacote` ao criar a entidade; em `AtualizarAsync`, atribuir `vinculo.TotalAulasPacote = request.TotalAulasPacote;` junto dos demais campos (sem bloqueio ainda — isso é US4, fase seguinte); no método `Mapear`, calcular `AulasDadas = vinculo.Modalidade == ModalidadeCobranca.Pacote && vinculo.TotalAulasPacote.HasValue && vinculo.SaldoAulas.HasValue ? vinculo.TotalAulasPacote - vinculo.SaldoAulas : (int?)null;` e incluir no objeto retornado. Faz T009/T010 passarem
- [X] T015 [US1] Validar US1: `dotnet test "tests/SPI.Application.Tests" --filter FullyQualifiedName~VinculoCobrancaRequestValidatorTests|FullyQualifiedName~VinculoCobrancaServiceCadastrarTests|FullyQualifiedName~VinculoCobrancaServiceAuditoriaTests` verde; `dotnet build` sem erros

**Checkpoint**: US1 funcional e testável sozinha — MVP.

---

## Phase 4: User Story 2 — Rastrear exatamente quais presenças pertencem a qual vínculo (Priority: P1)

**Goal**: toda presença confirmada com vínculo correspondente fica associada ao `Id` desse vínculo em `AulaAluno.VinculoCobrancaId`, para Avulsa, Mensalidade e Pacote.

**Independent Test**: quickstart.md §2 — registrar presença confirmada com vínculo ativo, confirmar `aula_aluno.vinculo_cobranca_id` gravado; sem vínculo ou em falta, confirmar que permanece `NULL`.

### Tests for User Story 2 (escrever primeiro; devem FALHAR antes da implementação)

- [X] T016 [P] [US2] Criar `tests/SPI.Application.Tests/Aulas/AulaServiceAuditoriaTests.cs` (mesmo padrão de `AulaServiceGerarContasAReceberPacoteTests.cs`, reaproveitando os fakes de `AulaServiceFakes.cs`): (1) presença confirmada com vínculo Avulsa ativo no contexto → `aulaAluno.VinculoCobrancaId == vinculo.Id` (FR-004); (2) mesmo cenário com vínculo Mensalidade → `aulaAluno.VinculoCobrancaId == vinculo.Id` (FR-004); (3) mesmo cenário com vínculo Pacote → `aulaAluno.VinculoCobrancaId == vinculo.Id` (FR-004); (4) presença confirmada sem nenhum vínculo correspondente ao contexto → `aulaAluno.VinculoCobrancaId == null` (FR-005); (5) presença confirmada com vínculo cadastrado para outro contexto (turma diferente) → `aulaAluno.VinculoCobrancaId == null` (FR-005, contexto não bate); (6) presença **não confirmada** (falta), com vínculo ativo no contexto → `aulaAluno.VinculoCobrancaId == null` (FR-006, falta nunca dispara associação); (7) aluno troca de vínculo Pacote (vínculo A esgotado e desativado, vínculo B novo ativo) → presenças anteriores à troca referenciam A, posteriores referenciam B (Acceptance Scenario 3 da US2 do spec.md); (8) **cobertura explícita de FR-002** (`/speckit-analyze`, achado E1): vínculo Pacote com `TotalAulasPacote = 10`, `SaldoAulas = 10`, registrar uma presença confirmada via o fluxo real (`GerarContasAReceberAsync`) → `vinculo.TotalAulasPacote` continua `10` (inalterado) e `vinculo.SaldoAulas` cai para `9` — prova, por execução, que a presença nunca decrementa `TotalAulasPacote`, só `SaldoAulas`

### Implementation for User Story 2

- [X] T017 [US2] Editar `src/SPI.Application/Aulas/Services/AulaService.cs`, dentro de `GerarContasAReceberAsync` (linha ~230, logo após `var vinculoCobranca = await _vinculoCobrancaRepository.ObterAtivoPorAlunoEContextoAsync(...)` e antes do `switch (vinculoCobranca?.Modalidade)`): acrescentar `aulaAluno.VinculoCobrancaId = vinculoCobranca?.Id;` — incondicional, reaproveitando o vínculo já resolvido ali mesmo (research.md R3); persistido pela chamada já existente a `_pagamentoRepository.SalvarAlteracoesAsync(cancellationToken)` ao final do método (mesmo `DbContext` escopado). Não alterar nenhuma outra linha do método (FR-012). Faz T016 passar
- [X] T018 [US2] Validar US2: `dotnet test "tests/SPI.Application.Tests" --filter FullyQualifiedName~AulaServiceAuditoriaTests` verde; rodar também as suítes já existentes de specs/038 (`FullyQualifiedName~AulaServiceGerarContasAReceberSemVinculoTests|FullyQualifiedName~AulaServiceGerarContasAReceberAvulsaTests|FullyQualifiedName~AulaServiceGerarContasAReceberMensalidadeTests|FullyQualifiedName~AulaServiceGerarContasAReceberPacoteTests|FullyQualifiedName~AulaServiceGerarContasAReceberMultiplosAlunosTests`) e confirmar que continuam 100% verdes sem nenhuma asserção alterada (FR-012)

**Checkpoint**: US1 e US2 completas e testáveis de forma independente.

---

## Phase 5: User Story 3 — Mensalidade com teto mensal opcional e contagem automática por mês (Priority: P2)

**Goal**: `AulasIncluidas` da Mensalidade continua opcional; quando presente, é comparável contra `AulasDadasNoMes`, derivado por contagem das presenças associadas ao vínculo (T017) filtradas pela competência corrente — sem job novo, reseta sozinho a cada mês.

**Independent Test**: quickstart.md §3 — Mensalidade com teto 10, 4 presenças no mês → `aulasDadasNoMes: 4`; mês seguinte reinicia em 0.

**Depende de US2** (T017) — a contagem só funciona se `AulaAluno.VinculoCobrancaId` já estiver sendo populado.

### Tests for User Story 3 (escrever primeiro; devem FALHAR antes da implementação)

- [X] T019 [P] [US3] Editar `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaFakes.cs`: acrescentar `FakeAulaRepositoryParaVinculo : IAulaRepository` (mesma prova estrutural dos demais fakes do arquivo — só o método novo tem lógica, todo o resto lança `NotImplementedException`, provando que `VinculoCobrancaService` nunca lê/escreve `Aula` além de contar presenças): `public List<(int VinculoCobrancaId, int Ano, int Mes)> Presencas { get; } = new();`; `ContarPresencasPorVinculoECompetenciaAsync(vinculoCobrancaId, competencia, ct)` devolve `Presencas.Count(p => p.VinculoCobrancaId == vinculoCobrancaId && p.Ano == competencia.Year && p.Mes == competencia.Month)`
- [X] T020 [P] [US3] Estender `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServiceAuditoriaTests.cs` (criado em T010): (1) vínculo Mensalidade com `AulasIncluidas = 10`, fake semeado com 4 presenças no mês corrente desse vínculo → `AulasDadasNoMes == 4` (FR-008); (2) mesmo vínculo, fake semeado com presenças do mês corrente **e** do mês anterior → `AulasDadasNoMes` conta só as do mês corrente (FR-008, reset automático); (3) vínculo Mensalidade **sem** `AulasIncluidas` definido, com presenças no mês → `AulasDadasNoMes` ainda calculado normalmente, sem teto para comparar (FR-009); (4) vínculo Pacote ou Avulsa → `AulasDadasNoMes == null` sempre (campo exclusivo de Mensalidade)

### Implementation for User Story 3

- [X] T021 [US3] Editar `src/SPI.Domain/Repositories/IAulaRepository.cs`: acrescentar `Task<int> ContarPresencasPorVinculoECompetenciaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default);` com comentário explicando o propósito (contagem de presenças confirmadas associadas a um vínculo, filtrada por mês/ano da aula — specs/052). Não alterar nenhum método existente
- [X] T022 [US3] Editar `src/SPI.Infrastructure/Repositories/AulaRepository.cs` (depende de T021): implementar `ContarPresencasPorVinculoECompetenciaAsync` como `_dbContext.AulaAlunos.Where(aa => aa.VinculoCobrancaId == vinculoCobrancaId && aa.Aula.DataInicio.Year == competencia.Year && aa.Aula.DataInicio.Month == competencia.Month).CountAsync(cancellationToken)`
- [X] T023 [US3] Editar `src/SPI.Application/VinculosCobranca/Services/VinculoCobrancaService.cs` (depende de T022): acrescentar `IAulaRepository _aulaRepository` ao construtor; trocar o método `Mapear` (hoje `static`/síncrono) por `MapearAsync` (instância, assíncrono): para vínculos `Modalidade == Mensalidade`, calcular `AulasDadasNoMes = await _aulaRepository.ContarPresencasPorVinculoECompetenciaAsync(vinculo.Id, new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1), cancellationToken)` (mesmo cálculo de competência atual já usado em `MensalidadeDispatcherService`); para as demais modalidades, `AulasDadasNoMes = null`. Atualizar todos os call sites existentes (`ListarPorAlunoAsync`, `ObterPorIdAsync`, `CadastrarAsync`, `AtualizarAsync`, `ReativarAsync`) para `await MapearAsync(...)` (em listas, via loop ou `Select` + `await Task.WhenAll`)
- [X] T024 [US3] Atualizar os 4 arquivos de teste que instanciam `VinculoCobrancaService` diretamente — `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServiceExcluirReativarTests.cs`, `VinculoCobrancaServiceAtualizarTests.cs`, `VinculoCobrancaServiceListarTests.cs`, `VinculoCobrancaServiceCadastrarTests.cs` — para passar um novo `FakeAulaRepositoryParaVinculo` (T019, sem nenhuma presença semeada) no construtor. Nenhuma asserção existente nesses arquivos deve mudar de comportamento (o fake vazio sempre conta 0)
- [X] T025 [US3] Validar US3: `dotnet test "tests/SPI.Application.Tests" --filter FullyQualifiedName~VinculoCobrancaServiceAuditoriaTests` verde; rodar as 4 suítes de T024 e confirmar que continuam 100% verdes

**Checkpoint**: US1, US2 e US3 completas e testáveis de forma independente.

---

## Phase 6: User Story 4 — Impedir reduzir o total/teto abaixo do que já foi consumido (Priority: P2)

**Goal**: editar um vínculo Pacote ou Mensalidade reduzindo o total/teto abaixo do já consumido é rejeitado com `ConflitoException` explicando o motivo.

**Independent Test**: quickstart.md §4 — Pacote 10/6 dadas, editar para 5 → 409; editar para 6 → aceito. Mensalidade 10/7 dadas no mês, editar para 5 → 409.

**Depende de US1** (cálculo de `AulasDadas` do Pacote) **e US3** (cálculo de `AulasDadasNoMes` da Mensalidade, via `IAulaRepository` já injetado em T023).

### Tests for User Story 4 (escrever primeiro; devem FALHAR antes da implementação)

- [X] T026 [P] [US4] Estender `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServiceAuditoriaTests.cs`: (1) vínculo Pacote `TotalAulasPacote = 10`, `SaldoAulas = 4` (dado = 6), `PUT` com `TotalAulasPacote = 5` → `ConflitoException` com mensagem `"O novo total de aulas do pacote nao pode ser menor que as aulas ja dadas (6)."` (FR-007, Acceptance Scenario 1 da US4); (2) mesmo vínculo, `PUT` com `TotalAulasPacote = 6` e `SaldoAulas = 4` (inalterado no mesmo request) → aceito, sem exceção, e `SaldoAulas` permanece `4` — prova explícita de que o sistema não recalcula `SaldoAulas` automaticamente a partir do novo total (Acceptance Scenario 2, `/speckit-analyze` achado F1); (3) vínculo Mensalidade `AulasIncluidas = 10`, fake (T019) semeado com 7 presenças no mês corrente, `PUT` com `AulasIncluidas = 5` → `ConflitoException` com mensagem `"O novo teto de aulas da mensalidade nao pode ser menor que as aulas ja dadas neste mes (7)."` (FR-010, Acceptance Scenario 3); (4) vínculo Mensalidade sem `AulasIncluidas`, `PUT` sem definir nenhum teto → aceito, sem exceção (Acceptance Scenario 4); (5) **edge case** (`/speckit-analyze` achado C1): vínculo Mensalidade **sem** `AulasIncluidas` definido (nunca teve teto), fake semeado com 7 presenças no mês corrente, `PUT` definindo `AulasIncluidas = 5` pela primeira vez → mesma `ConflitoException` de (3) — a regra vale tanto para reduzir um teto existente quanto para definir um teto novo abaixo do já dado

### Implementation for User Story 4

- [X] T027 [US4] Editar `src/SPI.Application/VinculosCobranca/Services/VinculoCobrancaService.cs`, em `AtualizarAsync`, logo após carregar `vinculo` e verificar `vinculo.Ativo` (antes de atribuir os novos valores do `request`, depende de T014/T023): se `vinculo.Modalidade == ModalidadeCobranca.Pacote && request.TotalAulasPacote.HasValue`, calcular `var dado = (vinculo.TotalAulasPacote ?? 0) - (vinculo.SaldoAulas ?? 0);` e, se `request.TotalAulasPacote.Value < dado`, `throw new ConflitoException($"O novo total de aulas do pacote nao pode ser menor que as aulas ja dadas ({dado}).");`; se `vinculo.Modalidade == ModalidadeCobranca.Mensalidade && request.AulasIncluidas.HasValue`, obter `var dadoNoMes = await _aulaRepository.ContarPresencasPorVinculoECompetenciaAsync(vinculo.Id, new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1), cancellationToken);` e, se `request.AulasIncluidas.Value < dadoNoMes`, `throw new ConflitoException($"O novo teto de aulas da mensalidade nao pode ser menor que as aulas ja dadas neste mes ({dadoNoMes}).");`. **Importante (FR-007, `/speckit-analyze` achado F1)**: fora dessas duas condições de bloqueio, não acrescentar nenhuma lógica que recalcule `SaldoAulas` a partir do novo `TotalAulasPacote` — `vinculo.SaldoAulas` continua sendo atribuído só a partir de `request.SaldoAulas`, exatamente como o código já faz hoje, sem nenhuma derivação automática. Faz T026 passar
- [X] T028 [US4] Validar US4: `dotnet test "tests/SPI.Application.Tests" --filter FullyQualifiedName~VinculoCobrancaServiceAuditoriaTests` verde; rodar `VinculoCobrancaServiceAtualizarTests.cs` e confirmar que os cenários de edição já existentes (não relacionados a Pacote/Mensalidade) continuam passando

**Checkpoint**: as 4 user stories completas e testáveis — feature completa.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: verificação explícita dos requisitos "MUST NOT" (regra do `CLAUDE.md`) e validação final

- [X] T029 Regra do `CLAUDE.md` para FR-011 ("MUST NOT alterar o job de cobrança mensal, specs/039"): rodar `git diff --stat -- src/SPI.Infrastructure/BackgroundServices/MensalidadeDispatcherService.cs` e confirmar saída vazia (nenhuma linha alterada nesse arquivo)
- [X] T030 Regra do `CLAUDE.md` para FR-012 ("MUST NOT alterar o comportamento de cobrança por presença além do campo de auditoria"): ler diretamente `git diff -- src/SPI.Application/Aulas/Services/AulaService.cs` e confirmar que a única linha nova dentro de `GerarContasAReceberAsync` é `aulaAluno.VinculoCobrancaId = vinculoCobranca?.Id;` — nenhuma mudança na lógica do `switch` por modalidade, nos valores gerados, ou na condição de decremento de `SaldoAulas`
- [X] T031 Regra do `CLAUDE.md` para FR-013 ("MUST NOT criar nem tocar nenhuma tela"): rodar `git status --porcelain -- frontend` e confirmar que não aparece nenhum arquivo, nem novo (não rastreado) nem modificado
- [X] T032 Regra do `CLAUDE.md` para FR-014 ("MUST NOT implementar lembrete por e-mail"): rodar `git status --porcelain -- src/SPI.Infrastructure/Email src/SPI.Infrastructure/BackgroundServices` e confirmar que só os arquivos já esperados por esta feature (`MensalidadeDispatcherService.cs` sem alteração, por T029) aparecem — nenhum arquivo novo de dispatcher/lembrete de vencimento
- [X] T033 Rodar a suíte completa `dotnet test "tests/SPI.Application.Tests"` (100% verde, incluindo specs/037/038/039 sem regressão) e `dotnet build` da solução
- [X] T034 Executar `quickstart.md` completo (§1 a §5), com a migração (T003) já aplicada e o backend rodando

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001)**: sem dependências.
- **Foundational (T002–T007)**: depende de T001; **bloqueia** todas as user stories. Ordem interna: T002 → T003; (T004, T005) podem rodar em paralelo entre si; T006 depende de T004; T007 depende de T005.
- **US1 (T008–T015)**: depende da Foundational. Independente de US2/US3/US4.
- **US2 (T016–T018)**: depende da Foundational. Independente de US1.
- **US3 (T019–T025)**: depende da Foundational **e de US2** (T017) — a contagem só reflete dado real se `AulaAluno.VinculoCobrancaId` já estiver sendo populado.
- **US4 (T026–T028)**: depende de **US1** (T014, cálculo de `AulasDadas`) e **US3** (T023, `IAulaRepository` já injetado e `AulasDadasNoMes` calculado).
- **Polish (T029–T034)**: depende de todas as 4 user stories completas.

### Within Each Story

- Testes primeiro e falhando → implementação → validação (mesmo padrão de specs/038/039).

### Parallel Opportunities

- Foundational: T002 sozinho até T003; T004 ‖ T005; depois T006 (após T004) ‖ T007 (após T005).
- T008 (US1) ‖ T016 (US2) podem ser **escritos** em paralelo (arquivos diferentes, ambos só dependem da Foundational) — mas a validação de US3 só é possível depois de US2 implementada (T017).
- T019/T020 (US3) podem ser escritos em paralelo entre si.
- T026 (US4) só pode ser escrito depois que T010/T020 (estruturas de teste de US1/US3) já existirem no mesmo arquivo `VinculoCobrancaServiceAuditoriaTests.cs`.
- T029/T030/T031/T032 (Polish, verificação de git) são independentes entre si e podem rodar em paralelo.

### Parallel Example: Foundational

```text
# Em paralelo, depois de T003:
T004  VinculoCobranca.cs (+TotalAulasPacote)
T005  AulaAluno.cs (+VinculoCobrancaId)

# Depois, em paralelo:
T006  VinculoCobrancaConfiguration.cs (depende de T004)
T007  AulaAlunoConfiguration.cs (depende de T005)
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (T001) e Phase 2 (T002–T007).
2. Completar Phase 3 (T008–T015) — `TotalAulasPacote` persistido e `AulasDadas` calculável, entregável como MVP.
3. US2, US3, US4 e Polish são a entrega incremental seguinte, nessa ordem (US3 depende de US2; US4 depende de US1+US3).

### Incremental Delivery

Foundational (T002–T007) → US1 (T008–T015, MVP) → US2 (T016–T018) → US3 (T019–T025, depende de US2) → US4 (T026–T028, depende de US1+US3) → Polish (T029–T034).

## Notes

- **Requisitos negativos com cobertura explícita** (regra do `CLAUDE.md`): FR-011 → T029; FR-012 → T030; FR-013 → T031; FR-014 → T032; FR-002 → T016(8) (achado E1 do `/speckit-analyze`, corrigido).
- **Achados do `/speckit-analyze` já corrigidos**: F1 (spec.md/quickstart.md ajustados — `SaldoAulas` nunca é recalculado automaticamente ao editar `TotalAulasPacote`, T026(2)/T027 refletem isso) e C1 (T026(5) cobre definir `AulasIncluidas` pela primeira vez abaixo do já dado, não só reduzir um teto existente).
- **Mudança de assinatura com ripple conhecido**: T023 troca `Mapear` por `MapearAsync` em `VinculoCobrancaService` — T024 é obrigatória junto (atualiza os 4 arquivos de teste que instanciam o serviço diretamente), senão a suíte não compila.
- Migração aplicada na implementação via `mysql` CLI (T003), conforme preferência já registrada do usuário; connection string sempre dos User Secrets.
- Commitar por tarefa ou grupo lógico.
