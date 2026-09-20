---

description: "Lista de tarefas para Vínculo de Cobrança do Aluno"
---

# Tasks: Vínculo de Cobrança do Aluno

**Input**: Documentos de design em `/specs/037-vinculo-cobranca/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/vinculos-cobranca-api.md](./contracts/vinculos-cobranca-api.md), [quickstart.md](./quickstart.md)

**Tests**: INCLUÍDOS — o plano define testes xUnit (validator, serviço, não-regressão de FR-012) e a regra do usuário exige cobertura explícita para requisitos negativos ("MUST NOT": FR-012, FR-013 parte final, FR-014 parte final). O frontend não tem suíte de testes; é validado por `tsc`, `lint` e pelo quickstart.

**Organization**: Tarefas agrupadas por user story (US1 = P1 cadastrar/listar; US2 = P2 editar/excluir/reativar).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1 ou US2 (somente nas fases de user story)
- Caminhos relativos à raiz do repositório `C:\PROJETO - SPI`
- Mensagens de erro do backend em pt-BR **sem acentuação** (padrão do projeto), exatamente as do contrato

## Path Conventions

Web app em camadas: `src/SPI.Domain`, `src/SPI.Application`, `src/SPI.Infrastructure`, `src/SPI.Api`, `frontend/`, `tests/SPI.Application.Tests`, `database/`.

---

## Phase 1: Setup

**Purpose**: garantir base verde antes de qualquer alteração

- [X] T001 Rodar `dotnet test "tests/SPI.Application.Tests"` e, em `frontend/`, `npx tsc --noEmit` para registrar a linha de base (a árvore já tem alterações não commitadas das specs 035/036 — não tocar nelas nem reverter; se a base estiver vermelha por causa delas, anotar e seguir)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: banco, entidade, repositório e DTOs de que TODAS as user stories dependem

**⚠️ CRITICAL**: nenhuma user story começa antes desta fase terminar

- [X] T002 [P] Criar `database/14_vinculo_cobranca.sql` seguindo o cabeçalho/estilo de `database/13_endereco_aluno_responsavel.sql` (`USE spi_db;`, comentário por coluna, `ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci` como em `02_criacao_tabelas.sql`). Tabela `vinculo_cobranca` com colunas, verbatim de data-model.md: `id INT AUTO_INCREMENT PRIMARY KEY`; `aluno_id INT NOT NULL` com `CONSTRAINT fk_vinculocobranca_aluno FOREIGN KEY (aluno_id) REFERENCES alunos(id)`; `turma_id INT NULL` com `CONSTRAINT fk_vinculocobranca_turma FOREIGN KEY (turma_id) REFERENCES turma(id)` (NULL = atendimento individual); `modalidade VARCHAR(20) NOT NULL` com `CONSTRAINT chk_vinculocobranca_modalidade CHECK (modalidade IN ('Avulsa','Mensalidade','Pacote'))`; `valor DECIMAL(10,2) NOT NULL` com `CONSTRAINT chk_vinculocobranca_valor CHECK (valor > 0)`; `aulas_incluidas INT NULL` com `CHECK (aulas_incluidas IS NULL OR (modalidade = 'Mensalidade' AND aulas_incluidas >= 1))`; `saldo_aulas INT NULL` com `CHECK (saldo_aulas IS NULL OR (modalidade = 'Pacote' AND saldo_aulas >= 0))`; `ativo BOOLEAN NOT NULL DEFAULT TRUE`; coluna gerada `chave_ativa VARCHAR(30) GENERATED ALWAYS AS (IF(ativo, CONCAT(aluno_id, ':', IFNULL(turma_id, 0)), NULL)) VIRTUAL` com `UNIQUE INDEX uq_vinculocobranca_chave_ativa (chave_ativa)`. Nomear os dois CHECKs de aplicabilidade (`chk_vinculocobranca_aulas_incluidas`, `chk_vinculocobranca_saldo_aulas`). Comentário no arquivo explicando por que a unicidade usa coluna gerada (NULL não colide em UNIQUE do MySQL e inativos devem ficar fora — ver research.md R1). Nenhuma stored procedure.
- [X] T003 Aplicar `database/14_vinculo_cobranca.sql` com o cliente `mysql` (connection string vem dos .NET User Secrets do `src/SPI.Api` — `dotnet user-secrets list --project src/SPI.Api`; nunca de `appsettings.json`, nunca imprimir a senha no output). Depois validar no banco (depende de T002): (a) dois INSERTs ativos com o mesmo `(aluno_id, turma_id)` → o segundo falha com "Duplicate entry"; (b) dois ativos com `turma_id NULL` para o mesmo aluno → o segundo falha; (c) com o primeiro `ativo = 0`, o segundo passa; (d) `valor = 0` e `aulas_incluidas` com `modalidade = 'Pacote'` são rejeitados pelos CHECKs. Apagar as linhas de teste ao final.
- [X] T004 [P] Criar `src/SPI.Domain/Enums/ModalidadeCobranca.cs` — `enum ModalidadeCobranca { Avulsa = 1, Mensalidade = 2, Pacote = 3 }` (namespace `SPI.Domain.Enums`, estilo de `PerfilUsuario.cs`)
- [X] T005 [P] Criar `src/SPI.Domain/Entities/VinculoCobranca.cs` (depende de T004) — propriedades `int Id`, `int AlunoId`, `int? TurmaId`, `ModalidadeCobranca Modalidade`, `decimal Valor`, `int? AulasIncluidas`, `int? SaldoAulas`, `bool Ativo`; navegações `Aluno Aluno { get; set; } = null!` e `Turma? Turma { get; set; }` (namespace `SPI.Domain.Entities`, estilo de `AlunoTurma.cs`/`Turma.cs`)
- [X] T006 [P] Editar `src/SPI.Domain/Entities/Aluno.cs` (depende de T005): adicionar SOMENTE `public ICollection<VinculoCobranca> VinculosCobranca { get; set; } = new List<VinculoCobranca>();` — NÃO alterar `ValorAula` nem nenhuma outra propriedade (FR-012)
- [X] T007 [P] Editar `src/SPI.Domain/Entities/Turma.cs` (depende de T005): adicionar SOMENTE `public ICollection<VinculoCobranca> VinculosCobranca { get; set; } = new List<VinculoCobranca>();`
- [X] T008 [P] Criar `src/SPI.Domain/Repositories/IVinculoCobrancaRepository.cs` (depende de T005) com: `Task<VinculoCobranca?> ObterPorIdAsync(int id, CancellationToken)` (inclui `Turma`); `Task<List<VinculoCobranca>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken)` (inclui `Turma`; ordena ativos primeiro, depois por nome da turma com atendimento individual por último); `Task<bool> ExisteAtivoAsync(int alunoId, int? turmaId, int? ignorarId, CancellationToken)`; `Task AdicionarAsync(VinculoCobranca, CancellationToken)`; `Task SalvarAlteracoesAsync(CancellationToken)`. **Nenhum método de remoção física** (Constituição I).
- [X] T009 [P] Criar `src/SPI.Infrastructure/Persistence/Configurations/VinculoCobrancaConfiguration.cs` (depende de T005; estilo de `AulaConfiguration.cs`): `ToTable("vinculo_cobranca")`; colunas `id`, `aluno_id`, `turma_id`, `modalidade` (`HasMaxLength(20)`, `IsRequired()`, `HasConversion<string>()`), `valor` (`HasColumnType("decimal(10,2)")`), `aulas_incluidas`, `saldo_aulas`, `ativo` (`HasDefaultValue(true)`); `HasOne(v => v.Aluno).WithMany(a => a.VinculosCobranca).HasForeignKey(v => v.AlunoId).HasConstraintName("fk_vinculocobranca_aluno")` e `HasOne(v => v.Turma).WithMany(t => t.VinculosCobranca).HasForeignKey(v => v.TurmaId).HasConstraintName("fk_vinculocobranca_turma")`. **NÃO mapear** `chave_ativa` (coluna gerada, gerida só pelo banco).
- [X] T010 Editar `src/SPI.Infrastructure/Persistence/SpiDbContext.cs` (depende de T005): `public DbSet<VinculoCobranca> VinculosCobranca => Set<VinculoCobranca>();`
- [X] T011 Criar `src/SPI.Infrastructure/Repositories/VinculoCobrancaRepository.cs` (depende de T008, T009, T010; estilo de `TurmaRepository.cs`) implementando `IVinculoCobrancaRepository`. `ExisteAtivoAsync` compara `AlunoId`, `TurmaId` (incluindo `null == null`), `Ativo == true` e exclui `ignorarId`. `SalvarAlteracoesAsync` envolve `SaveChangesAsync` e, ao capturar `DbUpdateException` cuja `InnerException` é `MySqlConnector.MySqlException` com `ErrorCode == MySqlErrorCode.DuplicateKeyEntry`, lança `SPI.Domain.Exceptions.ConflitoException("Ja existe um vinculo de cobranca ativo para esta combinacao.")` (proteção contra corrida, research.md R1); qualquer outra exceção propaga.
- [X] T012 Registrar o repositório em `src/SPI.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs`: `services.AddScoped<IVinculoCobrancaRepository, VinculoCobrancaRepository>();` (junto de `ITurmaRepository`; depende de T011)
- [X] T013 [P] Criar `src/SPI.Application/VinculosCobranca/Dtos/VinculoCobrancaRequest.cs` (depende de T004; `public record` com propriedades `{ get; set; }` como `TurmaRequest.cs`): `int? TurmaId`, `ModalidadeCobranca Modalidade`, `decimal Valor`, `int? AulasIncluidas`, `int? SaldoAulas`
- [X] T014 [P] Criar `src/SPI.Application/VinculosCobranca/Dtos/VinculoCobrancaResponse.cs` (depende de T004): `int Id`, `int AlunoId`, `int? TurmaId`, `string? TurmaNome`, `ModalidadeCobranca Modalidade`, `decimal Valor`, `int? AulasIncluidas`, `int? SaldoAulas`, `bool Ativo`
- [X] T015 [P] Criar `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaFakes.cs` (depende de T005, T008): fakes manuais (sem biblioteca de mock, padrão de `RelatorioServiceHistoricoAlunoTests.cs`) — `FakeVinculoCobrancaRepositoryParaTeste` (lista em memória, implementa a interface inteira, contador de `SalvarAlteracoesAsync`), `FakeTurmaRepositoryParaVinculo` (implementa `ITurmaRepository`; **contador de chamadas a `VincularAlunoAsync`/`DesvincularAlunoAsync`** para provar que nada escreve `AlunoTurma`) e `FakeAlunoRepositoryParaVinculo` (implementa `IAlunoRepository`; só `ObterPorIdAsync` funcional, demais métodos `throw new NotImplementedException()`). Nomes com sufixo distinto para não colidir com os fakes de `Relatorios/`.

**Checkpoint**: `dotnet build` da solução compila; banco migrado; fundação pronta.

---

## Phase 3: User Story 1 — Cadastrar e listar vínculos de cobrança (Priority: P1) 🎯 MVP

**Goal**: a professora cadastra um vínculo (turma do aluno ou atendimento individual, modalidade, valor, campo específico) na seção "Vínculos de Cobrança" da tela do aluno e o vê listado; conflitos e regras de modalidade são rejeitados com mensagem clara.

**Independent Test**: quickstart.md §3 cenários 1–6 — cadastrar Mensalidade com Aulas Incluídas em turma do aluno e ver na seção; cadastrar individual Pacote com saldo; tentar duplicar turma/individual (rejeitado); seletor só oferece turmas ativas do aluno; aviso EX-001 visível.

### Tests for User Story 1 (escrever primeiro; devem FALHAR antes da implementação)

- [X] T016 [P] [US1] Criar `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaRequestValidatorTests.cs` (`FluentValidation.TestHelper`, estilo de `LembreteRequestValidatorTests.cs`; instancia `new VinculoCobrancaRequestValidator()`): `Valor` 0 e negativo rejeitados com "O Valor deve ser maior que zero." e `Valor` 100000000 rejeitado (teto `99999999.99`); `Modalidade` fora do enum (ex.: `(ModalidadeCobranca)0`, `(ModalidadeCobranca)99`) rejeitada (FR-002: exatamente Avulsa/Mensalidade/Pacote); `AulasIncluidas` informado com Avulsa e com Pacote → "Aulas incluidas so se aplica a modalidade Mensalidade." (FR-003); `AulasIncluidas` 0 com Mensalidade rejeitado (≥ 1); `SaldoAulas` informado com Avulsa e com Mensalidade → "Saldo de aulas so se aplica a modalidade Pacote." (FR-004); `SaldoAulas` negativo com Pacote rejeitado, `SaldoAulas = 0` com Pacote aceito; combinações válidas aceitas (Avulsa sem extras, Mensalidade com e sem `AulasIncluidas`, Pacote com e sem `SaldoAulas`); cenário de troca de modalidade (Mensalidade→Pacote com `AulasIncluidas` ainda preenchido → rejeitado — US2-2)
- [X] T017 [P] [US1] Criar `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServiceCadastrarTests.cs` usando os fakes de T015: cadastro com turma do aluno cria vínculo `Ativo = true` com os dados corretos (US1-1) e `TurmaNome` preenchido; cadastro com `TurmaId = null` cria vínculo individual (US1-2); Pacote guarda `SaldoAulas` e deixa `AulasIncluidas` nulo (US1-3); segundo vínculo ativo na mesma turma → `ConflitoException` "Ja existe um vinculo de cobranca ativo para este aluno nesta turma." (FR-005); segundo individual ativo → `ConflitoException` "Ja existe um vinculo de cobranca ativo para este aluno em atendimento individual." (FR-006); com o vínculo existente `Ativo = false`, novo cadastro para a mesma combinação é permitido (FR-007, US2-4); aluno inexistente → `NaoEncontradoException`; turma inexistente → `NaoEncontradoException`; **FR-013**: turma inativa → `ConflitoException` "A turma informada esta inativa ou o aluno nao participa dela." e turma ativa da qual o aluno NÃO participa → mesma exceção; **FR-013 (MUST NOT)**: em todos os cenários de sucesso e de falha, o contador de `VincularAlunoAsync`/`DesvincularAlunoAsync` do `FakeTurmaRepositoryParaVinculo` é **0** e `turma.AlunosTurma` permanece inalterada (o vínculo nunca cria nem altera participação em turma)
- [X] T018 [P] [US1] Criar `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServiceListarTests.cs`: `ListarPorAlunoAsync` devolve só vínculos do aluno pedido, mesmo com vínculos de outro aluno no fake (US1-5, FR-009); vínculo individual sai com `TurmaNome == null`; filtro `ativo = true` omite inativos e `ativo = null` devolve ativos e inativos (base de FR-015); aluno sem vínculos → lista vazia (US1-6); aluno inexistente → `NaoEncontradoException`

### Implementation for User Story 1

- [X] T019 [US1] Criar `src/SPI.Application/VinculosCobranca/Validators/VinculoCobrancaRequestValidator.cs` (`AbstractValidator<VinculoCobrancaRequest>`, estilo de `TurmaRequestValidator.cs`; todas as regras de data-model.md, mensagens de contracts/): `Modalidade` `IsInEnum()` (FR-002); `Valor` `GreaterThan(0)` com "O Valor deve ser maior que zero." e `LessThanOrEqualTo(99999999.99m)`; `AulasIncluidas` — quando informado: `Modalidade == Mensalidade` (senão "Aulas incluidas so se aplica a modalidade Mensalidade.") e `>= 1`; `SaldoAulas` — quando informado: `Modalidade == Pacote` (senão "Saldo de aulas so se aplica a modalidade Pacote.") e `>= 0`. Depende de T013. Faz os testes de T016 passarem.
- [X] T020 [US1] Criar `src/SPI.Application/VinculosCobranca/Services/IVinculoCobrancaService.cs` (depende de T013, T014; estilo de `ITurmaService.cs`) com `ListarPorAlunoAsync(int alunoId, bool? ativo, ct)`, `ObterPorIdAsync(int alunoId, int id, ct)` e `CadastrarAsync(int alunoId, VinculoCobrancaRequest request, ct)` (os métodos de US2 entram na Phase 4)
- [X] T021 [US1] Criar `src/SPI.Application/VinculosCobranca/Services/VinculoCobrancaService.cs` (depende de T008, T020; estilo de `TurmaService.cs`; injeta `IVinculoCobrancaRepository`, `IAlunoRepository`, `ITurmaRepository`). `CadastrarAsync`: aluno via `IAlunoRepository.ObterPorIdAsync` (senão `NaoEncontradoException("Aluno nao encontrado.")`); se `TurmaId` informado, método privado `ValidarTurmaAsync` (turma via `ITurmaRepository.ObterPorIdAsync` → `NaoEncontradoException("Turma nao encontrada.")`; `!turma.Ativo` ou `!turma.AlunosTurma.Any(at => at.AlunoId == alunoId)` → `ConflitoException("A turma informada esta inativa ou o aluno nao participa dela.")`); `ExisteAtivoAsync(alunoId, turmaId, null)` verdadeiro → `ConflitoException` com a mensagem de turma ou de atendimento individual (ver T017); cria com `Ativo = true`, `AdicionarAsync` + `SalvarAlteracoesAsync`, recarrega via `ObterPorIdAsync` para trazer `Turma` e mapeia para `VinculoCobrancaResponse`. **Nunca chamar** `VincularAlunoAsync`/`DesvincularAlunoAsync` (FR-013). `ListarPorAlunoAsync`/`ObterPorIdAsync` (vínculo de outro aluno → `NaoEncontradoException("Vinculo de cobranca nao encontrado.")`). Faz T017 e T018 passarem.
- [X] T022 [US1] Registrar `services.AddScoped<IVinculoCobrancaService, VinculoCobrancaService>();` em `src/SPI.Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs` (+ `using SPI.Application.VinculosCobranca.Services;`; o validator é registrado automaticamente por `AddValidatorsFromAssembly`)
- [X] T023 [P] [US1] Editar `src/SPI.Application/Common/Dtos/TurmaResumoResponse.cs`: acrescentar `public bool Ativo { get; set; }` (aditivo; não alterar `Id`/`Nome`)
- [X] T024 [US1] Editar `src/SPI.Application/Alunos/Services/AlunoService.cs` (depende de T023): no mapeamento `Turmas = aluno.AlunosTurma.Select(at => new TurmaResumoResponse { ... })` (~linha 164) acrescentar `Ativo = at.Turma.Ativo`. Não alterar mais nada no arquivo (em especial nada de `ValorAula`).
- [X] T025 [US1] Criar `src/SPI.Api/Controllers/VinculosCobrancaController.cs` (depende de T019, T022; estilo exato de `TurmasController.cs`): `[Route("api/alunos/{alunoId}/vinculos-cobranca")] [ApiController] [Authorize(Roles = nameof(PerfilUsuario.Professor))]`, injeta `IVinculoCobrancaService`, `IValidator<VinculoCobrancaRequest>`, `ILogger`. Endpoints: `GET` (query `bool? ativo`) → 200 lista; `GET {id}` → 200 (`NaoEncontradoException`→404); `POST` → valida com o validator (400 com `validacao.Errors.Select(e => e.ErrorMessage)`), chama `CadastrarAsync`, `LogInformation`, devolve `CreatedAtAction(nameof(ObterPorId), new { alunoId, id = response.Id }, response)`; `NaoEncontradoException`→404, `ConflitoException`→409, `Exception`→`Problem(...)` 500. Comentários XML `///` e `ProducesResponseType` como nos demais controllers.
- [X] T026 [P] [US1] Editar `frontend/lib/api/alunos.ts`: em `TurmaResumo` acrescentar `ativo: boolean;` (espelha `TurmaResumoResponse`)
- [X] T027 [P] [US1] Criar `frontend/lib/api/vinculosCobranca.ts` (estilo de `turmas.ts`, comentários `/** Espelha ... */`): `type ModalidadeCobranca = "Avulsa" | "Mensalidade" | "Pacote"`; interfaces `VinculoCobranca` (espelha `VinculoCobrancaResponse`) e `VinculoCobrancaRequest`; `listarVinculosCobranca(alunoId, accessToken, filtros?: { ativo?: boolean })` e `cadastrarVinculoCobranca(alunoId, request, accessToken)` sobre `/api/alunos/${alunoId}/vinculos-cobranca` com `apiGet`/`apiPost`
- [X] T028 [US1] Criar `frontend/components/alunos/VinculoCobrancaFormModal.tsx` (depende de T026, T027): modal no padrão do popup de aula da Agenda (`modal-overlay` + `modal modal-aula-popup`, fechar ao clicar fora com `stopPropagation`, ver `frontend/app/(app)/agenda/page.tsx` ~linha 353) com formulário nas classes de `AlunoForm.tsx` (`form-wrap`, `field-grid`, `field`, `err-banner show`, `btn btn-primary`/`btn-ghost`). Props: `alunoId`, `turmasAtivas: TurmaResumo[]`, `onSalvo`, `onCancelar` (modo edição entra na Phase 4). Campos: select de turma (`""` = "Atendimento individual" + apenas `turmasAtivas`), select de Modalidade (Avulsa/Mensalidade/Pacote), input `type="number" step="0.01" min="0.01"` de Valor, e — só quando Modalidade = Mensalidade — "Aulas incluídas" (opcional, `min="1"`), só quando Pacote — "Saldo de aulas" (opcional, `min="0"`); ao trocar a modalidade, limpar o campo que deixou de se aplicar (conveniência de UI; **sem** replicar regra de negócio — Constituição II). Enviar `cadastrarVinculoCobranca`; erros da API (`ApiError.message`/`details`) exibidos no `err-banner`; `mostrarToast` no sucesso. Se necessário ajustar largura, acrescentar uma classe `.modal.modal-vinculo` em `frontend/styles/dashboard.css` (arquivo tocado só por esta tarefa).
- [X] T029 [US1] Criar `frontend/components/alunos/VinculosCobrancaSection.tsx` (depende de T027, T028): painel `mini-panel` com `<h4><Icon .../> Vínculos de Cobrança</h4>` (mesmo estilo dos painéis "Turmas"/"Contas a receber recentes" em `alunos/[id]/page.tsx`); props `aluno: Aluno` (usa `aluno.id` e `aluno.turmas.filter(t => t.ativo)` como `turmasAtivas`); carrega com `listarVinculosCobranca(aluno.id, token, { ativo: true })` (default só ativos, FR-015); cada item (`lesson-item`): turma ou "Atendimento individual", Modalidade, `currency(valor)` (de `@/lib/format`) e o campo específico (`{aulasIncluidas} aula(s) incluída(s)` ou `Saldo: {saldoAulas} aula(s)`, quando houver); estado vazio com `EmptyState` (US1-6); botão "Adicionar vínculo" que abre o modal; recarrega a lista após salvar, sem recarregar a página (SC-004). **Aviso EX-001 (obrigatório, plan.md → Complexity Tracking):** bloco de texto visível na seção — "Este vínculo é apenas um cadastro. Ele ainda não altera a cobrança automática das aulas." — sempre exibido, com comentário `// EX-001: remover este aviso quando a cobrança automática passar a usar o vínculo (próxima fatia)`.
- [X] T030 [US1] Editar `frontend/app/(app)/alunos/[id]/page.tsx` (depende de T029): importar e renderizar `<VinculosCobrancaSection aluno={aluno} />` em um bloco próprio após o grid "Turmas"/"Contas a receber recentes" (`<div style={{ marginTop: 18 }}>`), sem alterar nenhuma outra seção da página
- [ ] T031 [US1] Validar US1: `dotnet test "tests/SPI.Application.Tests"` (T016–T018 verdes), `dotnet build`, em `frontend/` `npx tsc --noEmit` e `npm run lint`; executar quickstart.md §3 cenários 1–6 no app rodando (`run-dev.ps1`) e §3 cenário 4/5 também via requisição direta à API (409 confirmado) **PENDENTE (parte manual):** os passos automatizados (testes, build por projeto, `tsc`, `eslint` nos arquivos da feature) estão verdes; falta rodar os cenários 1–6 do quickstart na interface, que exigem login e o `SPI.Api` reiniciado com o build novo (o processo em execução ainda é o antigo).

**Checkpoint**: US1 funcional e testável sozinha — MVP.

---

## Phase 4: User Story 2 — Editar, excluir e reativar vínculos (Priority: P2)

**Goal**: a professora corrige um vínculo (valor, modalidade, saldo, turma), exclui logicamente e reativa (via "Mostrar excluídos"), com as mesmas regras de conflito.

**Independent Test**: quickstart.md §3 cenários 7–13 — editar Valor; trocar Mensalidade→Pacote; excluir (linha permanece no banco com `ativo = 0`); recriar na mesma combinação; "Mostrar excluídos"; reativar com conflito (rejeitado, ativo intacto) e sem conflito.

### Tests for User Story 2 (escrever primeiro; devem FALHAR antes da implementação)

- [X] T032 [P] [US2] Criar `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServiceAtualizarTests.cs` (fakes de T015): edição de `Valor` persiste e devolve resposta atualizada (US2-1); troca Mensalidade→Pacote com `AulasIncluidas = null` e `SaldoAulas` informado persiste (US2-2); mudar `TurmaId` para turma em que já existe outro vínculo ativo do aluno → `ConflitoException` (FR-008/FR-005); mudar para individual quando já existe individual ativo → `ConflitoException` (FR-006); mudar para turma inativa ou da qual o aluno não participa → `ConflitoException` (FR-013 na edição); **`TurmaId` inalterado NÃO é revalidado**: com a turma do vínculo agora inativa (ou aluno removido da turma), editar só o `Valor` tem sucesso (plan.md R2); a checagem de unicidade na edição ignora o próprio vínculo (`ignorarId`); editar vínculo com `Ativo = false` → `ConflitoException` "Reative o vinculo antes de edita-lo."; vínculo inexistente ou de outro aluno → `NaoEncontradoException`; **FR-013 (MUST NOT)**: contador de `VincularAlunoAsync`/`DesvincularAlunoAsync` continua **0**
- [X] T033 [P] [US2] Criar `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServiceExcluirReativarTests.cs`: `ExcluirAsync` define `Ativo = false` e o registro **permanece** no repositório fake (nunca removido — FR-010/SC-003; a interface de repositório não tem remoção); excluir duas vezes é idempotente (sem erro); vínculo inexistente/de outro aluno → `NaoEncontradoException`; `ReativarAsync` sem conflito volta `Ativo = true` e devolve a resposta (US2-7); `ReativarAsync` com outro vínculo ativo na mesma turma → `ConflitoException` e — **FR-014 (MUST NOT)** — o vínculo ativo existente permanece `Ativo = true` e **inalterado** (nenhum campo mudou) e o vínculo excluído continua `Ativo = false` (US2-5); idem para conflito entre dois vínculos individuais; `ReativarAsync` NÃO reaplica FR-013 (turma desativada depois não bloqueia reativação — research.md R2); reativar vínculo já ativo é idempotente

### Implementation for User Story 2

- [X] T034 [US2] Editar `src/SPI.Application/VinculosCobranca/Services/IVinculoCobrancaService.cs` (depois de T020): acrescentar `AtualizarAsync(int alunoId, int id, VinculoCobrancaRequest request, ct)`, `ExcluirAsync(int alunoId, int id, ct)` e `ReativarAsync(int alunoId, int id, ct)` (devolve `VinculoCobrancaResponse`; `ExcluirAsync` devolve `Task`)
- [X] T035 [US2] Editar `src/SPI.Application/VinculosCobranca/Services/VinculoCobrancaService.cs` (depois de T021; depende de T034): `AtualizarAsync` — obter o vínculo do aluno (404 se ausente/de outro aluno); `!Ativo` → `ConflitoException("Reative o vinculo antes de edita-lo.")`; se `request.TurmaId != vinculo.TurmaId` → `ValidarTurmaAsync` (FR-013, só quando a turma muda) e `ExisteAtivoAsync(alunoId, request.TurmaId, vinculo.Id)` → `ConflitoException` com as mesmas mensagens do cadastro; atualizar `TurmaId`, `Modalidade`, `Valor`, `AulasIncluidas`, `SaldoAulas`; salvar; devolver resposta recarregada. `ExcluirAsync` — `Ativo = false` + salvar (nunca remover). `ReativarAsync` — se já ativo devolve sem alterar; senão `ExisteAtivoAsync(alunoId, vinculo.TurmaId, vinculo.Id)` verdadeiro → `ConflitoException` (mensagem de turma ou de individual) **sem tocar em nenhum vínculo**; senão `Ativo = true` + salvar. Faz T032 e T033 passarem.
- [X] T036 [US2] Editar `src/SPI.Api/Controllers/VinculosCobrancaController.cs` (depois de T025; depende de T035): `PUT {id}` (valida com o validator → 400; `AtualizarAsync`; 200; 404/409), `DELETE {id}` (`ExcluirAsync`; `Ok()`; `LogInformation("Vinculo de cobranca {Id} excluido (logicamente)")`; 404) e `PATCH {id}/reativar` (`ReativarAsync`; 200; 404/409) — mesmos catch/`ProducesResponseType`/comentários XML dos demais métodos e do controller de Turmas
- [X] T037 [US2] Editar `frontend/lib/api/vinculosCobranca.ts` (depois de T027): `atualizarVinculoCobranca(alunoId, id, request, accessToken)` (`apiPut`), `excluirVinculoCobranca(alunoId, id, accessToken)` (`apiDelete`), `reativarVinculoCobranca(alunoId, id, accessToken)` (`apiPatch`, como `reativarTurma`)
- [X] T038 [US2] Editar `frontend/components/alunos/VinculoCobrancaFormModal.tsx` (depois de T028; depende de T037): nova prop opcional `vinculo?: VinculoCobranca` — quando presente, título "Editar vínculo de cobrança", campos pré-preenchidos (turma atual selecionada; se a turma atual do vínculo não estiver em `turmasAtivas`, incluí-la como opção para não forçar troca) e submissão via `atualizarVinculoCobranca`; ao trocar Modalidade continuar limpando o campo não aplicável
- [X] T039 [US2] Editar `frontend/components/alunos/VinculosCobrancaSection.tsx` (depois de T029; depende de T037, T038): por item ativo, botões "Editar" (abre o modal em modo edição) e "Excluir" (abre `ConfirmModal` de `@/components/shared/ConfirmModal` com "Excluir vínculo de cobrança" / "O vínculo deixa de valer para este aluno, mas o registro é preservado e pode ser reativado."; após excluir, `mostrarToast` e recarregar); controle "Mostrar excluídos" (checkbox/`chk-pill`) que, ligado, recarrega **sem** filtro `ativo` (ativos + inativos) e exibe os inativos marcados com `StatusPill status="Inativo"` e botão "Reativar" (sem "Editar"), que chama `reativarVinculoCobranca` e mostra `ApiError.message` num toast quando o backend devolve 409 (FR-014/FR-015); desligado, volta a listar só `ativo: true`
- [ ] T040 [US2] Validar US2: `dotnet test` (T032–T033 verdes), `npx tsc --noEmit`, `npm run lint`; quickstart.md §3 cenários 7–13, conferindo no banco que o excluído mantém a linha com `ativo = 0` e que a tentativa de reativar em conflito deixa o vínculo ativo idêntico **PENDENTE (parte manual):** testes e `tsc`/`eslint` verdes, e os cenários 7–13 foram verificados no nível de serviço + MySQL reais (linha excluída permanece com `ativo = 0`; reativação em conflito deixa o vínculo ativo idêntico); falta só percorrê-los na interface.

**Checkpoint**: US1 e US2 completas e independentes.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão (FR-012), verificações de constituição e validação final

- [X] T041 [P] Criar `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaNaoAfetaCobrancaAutomaticaTests.cs` — **cobertura do requisito negativo FR-012** (research.md R9): por reflexão, (a) nenhum parâmetro do construtor público de `SPI.Application.Aulas.Services.AulaService` é `IVinculoCobrancaRepository`, `IVinculoCobrancaService` ou qualquer tipo cujo nome contenha `VinculoCobranca`; (b) o mesmo para `AlunoService` e `PagamentoService`; (c) `SPI.Domain.Entities.Aluno.ValorAula` continua existindo com tipo `decimal` e `AlunoService`/`AulaService` continuam sem membro que referencie `VinculoCobranca`; (d) `IAulaRepository`/`IPagamentoRepository` não expõem métodos com parâmetro ou retorno `VinculoCobranca`. Falha do teste = alguém ligou o vínculo à cobrança automática sem passar pela próxima fatia (EX-001).
- [X] T042 Verificação estática de constituição/escopo (sem código novo; falhar a tarefa se algo aparecer): `git -C "C:\PROJETO - SPI" diff -- src/SPI.Application/Aulas` vazio; `git diff -- src/SPI.Domain/Entities/Aluno.cs` só com a coleção `VinculosCobranca`; busca por `Remove(` / `DELETE FROM` / `ExecuteDelete` em arquivos `VinculoCobranca*` sem resultados (Constituição I — exclusão sempre lógica); nenhum segredo/connection string adicionado a arquivos versionados (Constituição IV)
- [X] T043 Rodar a suíte completa `dotnet test "tests/SPI.Application.Tests"`, `dotnet build` da solução e, em `frontend/`, `npx tsc --noEmit` + `npm run lint`; tudo verde (ou, se algo vermelho já existia na T001, sem regressão nova)
- [ ] T044 Executar quickstart.md por completo (§1–§4), incluindo §4 (registrar a sessão de uma aula do aluno que tem vínculos e conferir que a conta a receber usa `Aluno.ValorAula` e que `SaldoAulas` do pacote não muda) e o aviso EX-001 visível na seção (cenário 1) **PENDENTE (parte manual):** §1–§2 e a checagem estática de §4 estão feitas; falta percorrer §3 na interface e registrar uma sessão de aula real para conferir que a conta a receber usa `Aluno.ValorAula` (§4.1).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001)**: sem dependências.
- **Foundational (T002–T015)**: depende de T001; **bloqueia** as user stories. Ordem interna: T002 → T003; T004 → T005 → (T006, T007, T008, T009, T010) → T011 → T012; T013/T014 dependem só de T004; T015 depende de T005 e T008.
- **US1 (T016–T031)**: depende da Foundational. **US2 (T032–T040)**: depende da Foundational e, por editar os mesmos arquivos (`IVinculoCobrancaService.cs`, `VinculoCobrancaService.cs`, `VinculosCobrancaController.cs`, `vinculosCobranca.ts`, modal e seção), deve rodar **depois** de US1 — os testes T032/T033 podem ser escritos em paralelo a US1.
- **Polish (T041–T044)**: T041 pode começar após T020 (precisa dos tipos existirem); T042–T044 dependem das stories concluídas.

### Within Each Story

- Testes primeiro e falhando → validator/serviço → controller → frontend (`lib/api` → modal → seção → página).
- Conflito de arquivo (mesmo arquivo) = sequencial, nunca [P].

### User Story Dependencies

- **US1**: independente após a Foundational.
- **US2**: reaproveita a mesma seção/serviço de US1; testável de forma independente usando vínculos criados via US1 ou via SQL/API.

## Parallel Opportunities

- Foundational: T002 ‖ T004; depois T006 ‖ T007 ‖ T008 ‖ T009 (T005 pronto); T013 ‖ T014 ‖ T015.
- US1: T016 ‖ T017 ‖ T018 (arquivos de teste distintos); T023 ‖ T026 ‖ T027 (backend DTO, `alunos.ts`, `vinculosCobranca.ts`).
- US2: T032 ‖ T033.
- Polish: T041 em paralelo com T042.

### Parallel Example: User Story 1

```text
# Testes de US1 juntos:
T016 VinculoCobrancaRequestValidatorTests.cs
T017 VinculoCobrancaServiceCadastrarTests.cs
T018 VinculoCobrancaServiceListarTests.cs

# Pré-requisitos de frontend/DTO juntos:
T023 TurmaResumoResponse.cs  (Ativo)
T026 frontend/lib/api/alunos.ts (ativo)
T027 frontend/lib/api/vinculosCobranca.ts
```

## Implementation Strategy

### MVP First (User Story 1)

1. T001 → Foundational (T002–T015) → US1 (T016–T031).
2. **PARAR e validar** com quickstart §3 cenários 1–6 (inclui aviso EX-001).
3. Entregar/demonstrar: já é possível cadastrar e listar vínculos.

### Incremental Delivery

1. Base + US1 → MVP (cadastro e listagem).
2. + US2 → edição, exclusão lógica, reativação.
3. + Polish → guarda de FR-012, verificações de constituição e quickstart completo.

## Notes

- **Exceção EX-001** (plan.md → Complexity Tracking): o aviso da UI (T029) é obrigatório nesta fatia; a próxima fatia (ligar o vínculo a `AulaService.GerarContasAReceberAsync`) deve removê-lo e fechar a exceção — o teste T041 existe justamente para que esse acoplamento só aconteça de forma consciente.
- **Requisitos negativos com cobertura explícita**: FR-012 → T041 (+ T042/T044); FR-013 (não criar/alterar `AlunoTurma`) → T017/T032; FR-014 (não alterar o vínculo ativo) → T033; FR-010/SC-003 (nunca remover fisicamente) → T033/T042.
- Migração aplicada na implementação via `mysql` CLI (T003), conforme preferência do usuário; connection string sempre dos User Secrets.
- Commitar por tarefa ou grupo lógico; não commitar as alterações pré-existentes das specs 035/036 junto.
