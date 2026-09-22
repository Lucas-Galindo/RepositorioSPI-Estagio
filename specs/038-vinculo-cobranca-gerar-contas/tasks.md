---

description: "Lista de tarefas para Cobrança Automática por Modalidade do Vínculo"
---

# Tasks: Cobrança Automática por Modalidade do Vínculo

**Input**: Documentos de design em `/specs/038-vinculo-cobranca-gerar-contas/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/README.md](./contracts/README.md), [quickstart.md](./quickstart.md)

**Tests**: INCLUÍDOS — `plan.md` (Technical Context) planeja a primeira suíte para `AulaService`, e a regra do usuário exige cobertura explícita para requisitos negativos (FR-009, FR-010, FR-011). Não há frontend nesta feature (ver Assumptions do spec.md) — nenhuma validação de `tsc`/`lint` é necessária.

**Organization**: Tarefas agrupadas por user story (US1=P1 sem vínculo/não-regressão; US2=P2 Avulsa; US3=P2 Pacote; US4=P3 Mensalidade).

**Nota estrutural importante** (research.md R2): as 4 user stories decidem ramos do **mesmo** método privado `AulaService.GerarContasAReceberAsync` (um único `switch` por modalidade, não um Strategy Pattern por story — decisão explícita do plano). Não é possível implementar apenas o ramo de US1 e deixar os demais "não implementados" de forma compilável e correta ao mesmo tempo — os 4 ramos nascem juntos, na tarefa de implementação de US1 (T008), porque US1 é P1 e sua correção depende de os outros ramos existirem (senão o `switch` ficaria incompleto/incorreto). US2, US3 e US4 continuam **testáveis de forma independente**: cada uma tem seu próprio arquivo de teste, que falha antes de T008 e passa imediatamente depois — nenhuma delas precisa de uma tarefa de implementação própria. Isso está documentado em cada fase para não parecer um esquecimento.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1, US2, US3 ou US4 (só nas fases de user story)
- Caminhos relativos à raiz do repositório `C:\PROJETO - SPI`
- Mensagens/nomes em C# seguem o padrão já usado no projeto (pt-BR sem acentuação em identificadores, com acentuação em comentários)

## Path Conventions

Web app em camadas: `src/SPI.Domain`, `src/SPI.Application`, `src/SPI.Infrastructure`, `tests/SPI.Application.Tests`. Sem `src/SPI.Api` nem `frontend/` nesta feature (nenhum contrato de API muda — ver [contracts/README.md](./contracts/README.md)).

---

## Phase 1: Setup

**Purpose**: garantir base verde antes de qualquer alteração

- [X] T001 Rodar `dotnet test "tests/SPI.Application.Tests"` para registrar a linha de base (deve estar 100% verde — a árvore está limpa, sem alterações pendentes de outras specs)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: o método de busca do vínculo por contexto, a injeção da dependência em `AulaService`, e os fakes de teste de que TODAS as user stories dependem

**⚠️ CRITICAL**: nenhuma user story começa antes desta fase terminar

- [X] T002 [P] Editar `src/SPI.Domain/Repositories/IVinculoCobrancaRepository.cs`: acrescentar `Task<VinculoCobranca?> ObterAtivoPorAlunoEContextoAsync(int alunoId, int? turmaId, CancellationToken cancellationToken = default);` com comentário XML/inline explicando o propósito (busca o único vínculo **ativo** do aluno para o contexto — mesma Turma, ou nenhuma Turma para atendimento individual; nunca ambíguo, pela unicidade já garantida em specs/037). Não remover nem alterar nenhum método existente da interface.
- [X] T003 [P] Editar `src/SPI.Infrastructure/Repositories/VinculoCobrancaRepository.cs`: implementar `ObterAtivoPorAlunoEContextoAsync` (depende de T002) como `_dbContext.VinculosCobranca.FirstOrDefaultAsync(v => v.AlunoId == alunoId && v.TurmaId == turmaId && v.Ativo, cancellationToken)` — mesma forma de comparação de `TurmaId` (incluindo `null == null`) já usada em `ExisteAtivoAsync` neste arquivo (research.md R1). Sem `Include` (a modalidade e o saldo já estão na própria linha; o chamador não precisa da navegação `Turma`).
- [X] T004 [P] Editar `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaFakes.cs` (depende de T002): implementar `ObterAtivoPorAlunoEContextoAsync` em `FakeVinculoCobrancaRepositoryParaTeste` como `Task.FromResult(Vinculos.FirstOrDefault(v => v.AlunoId == alunoId && v.TurmaId == turmaId && v.Ativo))`. **Não alterar mais nada neste arquivo** — os testes de specs/037 que já usam este fake (`VinculoCobrancaService*Tests.cs`) devem continuar passando sem nenhuma outra mudança.
- [X] T005 [P] Editar `src/SPI.Application/Aulas/Services/AulaService.cs` (independente de T002 — só precisa do tipo `IVinculoCobrancaRepository`, já existente desde specs/037; não depende do método novo que T002 acrescenta à interface): acrescentar `private readonly IVinculoCobrancaRepository _vinculoCobrancaRepository;` e o parâmetro `IVinculoCobrancaRepository vinculoCobrancaRepository` **ao final** da lista de parâmetros do construtor (depois de `categoriaReceitaRepository`), atribuindo o campo no corpo. Acrescentar `using SPI.Domain.Enums;` aos usings (necessário para `ModalidadeCobranca`, usado a partir de T008). **Não alterar `GerarContasAReceberAsync` nem nenhum outro método nesta tarefa** — só a assinatura do construtor. Nenhuma mudança é necessária em `ApplicationServiceCollectionExtensions.cs`: `IVinculoCobrancaRepository` já está registrado no container desde specs/037 (`InfrastructureServiceCollectionExtensions.cs`), e `AddScoped<IAulaService, AulaService>()` resolve o parâmetro novo automaticamente.
- [X] T006 [P] Criar `tests/SPI.Application.Tests/Aulas/AulaServiceFakes.cs` (namespace `SPI.Application.Tests.Aulas`; fakes manuais, mesmo padrão de `VinculosCobranca/VinculoCobrancaFakes.cs` — biblioteca de mock não instalada neste projeto). Classes:
  - `FakeAulaRepositoryParaAula : IAulaRepository` — propriedade `public Aula? AulaSemeada { get; set; }`; `ObterPorIdAsync(id, ct)` devolve `AulaSemeada` quando `AulaSemeada?.Id == id`, senão `null`; `SalvarAlteracoesAsync` incrementa `public int Salvamentos { get; private set; }` e devolve `Task.CompletedTask`; `ListarAsync`, `ExisteConflitoAsync`, `AdicionarAsync`, `DefinirAlunosAsync`, `ObterAlunosAtivosDaTurmaAsync` lançam `NotImplementedException` (não usados por `RegistrarSessaoAsync`/`GerarContasAReceberAsync`).
  - `FakeCategoriaReceitaRepositoryParaAula : ICategoriaReceitaRepository` — propriedades `public CategoriaReceita? AulaEmTurma { get; set; }` e `public CategoriaReceita? AulaParticular { get; set; }`; `ObterPorNomeAsync(nome, ct)` devolve `AulaEmTurma` quando `nome == "Aula em turma"`, `AulaParticular` quando `nome == "Aula particular"`, senão `null`; `ObterPorIdAsync`, `ListarAtivasAsync` lançam `NotImplementedException`.
  - `FakePagamentoRepositoryParaAula : IPagamentoRepository` — `public List<Pagamento> Gerados { get; } = new();`, `public List<(int PagamentoId, int AulaId)> Vinculacoes { get; } = new();`, `public int Salvamentos { get; private set; }`; `AdicionarAsync` atribui `Id` sequencial (1, 2, 3...) e adiciona a `Gerados`; `VincularAulaAsync` adiciona a `Vinculacoes`; `SalvarAlteracoesAsync` incrementa `Salvamentos`; `ObterPorIdAsync`, `ListarAsync`, `AtualizarStatusViaProcedureAsync` lançam `NotImplementedException`.
  - `FakeVinculoCobrancaRepositoryParaAula : IVinculoCobrancaRepository` — `public List<VinculoCobranca> Vinculos { get; } = new();`, `public int Salvamentos { get; private set; }`; `ObterAtivoPorAlunoEContextoAsync(alunoId, turmaId, ct)` devolve `Vinculos.FirstOrDefault(v => v.AlunoId == alunoId && v.TurmaId == turmaId && v.Ativo)`; `SalvarAlteracoesAsync` incrementa `Salvamentos`; `ObterPorIdAsync`, `ListarPorAlunoAsync`, `ExisteAtivoAsync`, `AdicionarAsync` lançam `NotImplementedException` — **prova estrutural de FR-011**: se a implementação chamar qualquer método de escrita/cadastro do vínculo além de `SalvarAlteracoesAsync` (ex.: tentar criar um vínculo novo), o teste falha imediatamente.
  - `FakeTurmaRepositoryVazia : ITurmaRepository`, `FakeMateriaRepositoryVazia : IMateriaRepository`, `FakeAlunoRepositoryVazio : IAlunoRepository` — todos os métodos lançam `NotImplementedException` (não usados por `RegistrarSessaoAsync`/`GerarContasAReceberAsync`; `Aluno` e `Materia` chegam já carregados via `Aula.AulaAlunos[].Aluno` e `Aula.Materia`).
  - `FakeLembreteServiceVazio : ILembreteService` — `RecalcularAsync` devolve `Task.CompletedTask` (não é chamado por `RegistrarSessaoAsync`, mas não deve quebrar se algo mudar); os demais métodos lançam `NotImplementedException`.

**Checkpoint**: `dotnet build` da solução compila (com o novo parâmetro do construtor); fundação pronta.

---

## Phase 3: User Story 1 — Aluno sem vínculo continua cobrado como hoje (Priority: P1) 🎯 MVP

**Goal**: presenças de alunos sem `VinculoCobranca` correspondente ao contexto da aula continuam gerando a conta a receber exatamente como antes desta feature.

**Independent Test**: quickstart.md §2 cenário 1 — registrar sessão de aluno sem vínculo e conferir que a conta usa `Aluno.ValorAula`.

### Tests for User Story 1 (escrever primeiro; devem FALHAR antes da implementação)

- [X] T007 [P] [US1] Criar `tests/SPI.Application.Tests/Aulas/AulaServiceGerarContasAReceberSemVinculoTests.cs` (usa os fakes de T006; helpers privados `NovoAluno`, `NovaMateria`, `NovaAulaDeTurma`/`NovaAulaIndividual`, `CriarServico` no padrão de `RelatorioServiceHistoricoAlunoTests.cs`): (1) aula de turma, aluno sem nenhum `VinculoCobranca`, presença confirmada → `RegistrarSessaoAsync` gera exatamente 1 `Pagamento` em `FakePagamentoRepositoryParaAula.Gerados` com `ValorFinal == Aluno.ValorAula`, `Status == "Pendente"`, `CategoriaReceitaId` da categoria "Aula em turma", `DataVencimento == DataInicio.AddDays(5)`, `Competencia` = primeiro dia do mês/ano de `DataInicio`, `Descricao == $"{Materia.Nome} - {DataInicio:dd/MM/yyyy}"`, e uma entrada em `Vinculacoes` ligando o pagamento à aula (US1-1, FR-002); (2) mesmo cenário para aula individual (`TurmaId = null`) → categoria "Aula particular", mesmo `ValorFinal` (variação de FR-002); (3) aluno com um `VinculoCobranca` ativo cadastrado só para OUTRA turma (contexto diferente da aula) → mesmo resultado do teste 1, como se não houvesse vínculo (US1-2, FR-001: contexto tem que bater exatamente); (4) aluno com um `VinculoCobranca` **excluído** (`Ativo = false`) para o contexto exato da aula → também cai no comportamento padrão (edge case "vínculo excluído tratado como inexistente"); (5) presença **não confirmada** (falta) → nenhum `Pagamento` gerado e `FakeVinculoCobrancaRepositoryParaAula.Salvamentos == 0` (FR-008, começo da cobertura — reforçado em T012 item 6 e T014 item 3). **Nota (não é TDD "red" clássico para esta suíte específica)**: diferente de US2/US3/US4, estes 5 cenários testam exatamente os caminhos em que o comportamento correto é idêntico ao código **atual, não modificado** (nenhum vínculo aplicável ⇒ cobrança avulsa incondicional com `Aluno.ValorAula`, do jeito que `GerarContasAReceberAsync` já fazia antes desta feature). Por isso, T007 já passa mesmo antes de T008 ser implementada — não há erro de compilação nem falha esperada aqui; ela funciona como uma suíte de regressão que continua válida depois de T008, não como um "teste que falha primeiro". O "red" de verdade desta feature está nas suítes de US2/US3/US4 (T010/T012/T014), que exercitam ramos que o código atual nunca implementou e por isso falham de verdade até T008 existir.

### Implementation for User Story 1

- [X] T008 [US1] Editar `src/SPI.Application/Aulas/Services/AulaService.cs`, método privado `GerarContasAReceberAsync` (depende de T005, T006): renomear a variável do laço `foreach (var vinculo in aula.AulaAlunos.Where(...))` para `aulaAluno` (evita colisão de nome com `VinculoCobranca`, que passa a ser buscado dentro do laço). Para cada `aulaAluno`, antes de decidir o efeito: `var vinculoCobranca = await _vinculoCobrancaRepository.ObterAtivoPorAlunoEContextoAsync(aulaAluno.AlunoId, aula.TurmaId, cancellationToken);`. Substituir o corpo do laço por um `switch (vinculoCobranca?.Modalidade)`:
  - `case null:` e `case ModalidadeCobranca.Avulsa:` → gera o `Pagamento` exatamente como o código atual gerava (mesma `Descricao`/`CategoriaReceitaId`/`DataVencimento`/`Competencia`), mas com `ValorFinal = vinculoCobranca?.Valor ?? aulaAluno.Aluno.ValorAula` (FR-002 quando `null`; FR-003 quando `Avulsa` — implementados juntos por serem o mesmo caminho de código, só a fonte do valor muda).
  - `case ModalidadeCobranca.Mensalidade:` → nenhuma ação (`break`) — nenhum `Pagamento`, nenhuma escrita no vínculo (FR-004, FR-009).
  - `case ModalidadeCobranca.Pacote:` → se `vinculoCobranca.SaldoAulas is > 0`: `vinculoCobranca.SaldoAulas -= 1;` seguido de `await _vinculoCobrancaRepository.SalvarAlteracoesAsync(cancellationToken);`; senão (zero ou `null`): nenhuma ação — nunca decrementar abaixo de zero nem alterar um saldo nunca informado (FR-005, FR-006, FR-010).
  Manter, fora do `switch`, a chamada final `await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);` exatamente como hoje (só é relevante quando ao menos um `Pagamento` foi criado no laço). **Não alterar `RegistrarSessaoAsync` nem qualquer outro método público de `AulaService`.** Faz T007 passar.
- [X] T009 [US1] Validar US1: `dotnet test "tests/SPI.Application.Tests" --filter FullyQualifiedName~AulaServiceGerarContasAReceberSemVinculoTests` verde; `dotnet build` da solução sem erros

**Checkpoint**: US1 funcional e testável sozinha — MVP (comportamento não-regressivo garantido). A implementação de T008 já contém os 4 ramos (ver nota estrutural no topo); US2, US3 e US4 só precisam dos próprios testes.

---

## Phase 4: User Story 2 — Cobrança avulsa usa o valor específico do vínculo (Priority: P2)

**Goal**: presenças de alunos com `VinculoCobranca` Avulsa geram conta a receber com o Valor do vínculo, não `Aluno.ValorAula`.

**Independent Test**: quickstart.md §2 cenário 2 — vínculo Avulsa com Valor diferente de `ValorAula`, registrar sessão, conferir o `valorFinal` da conta gerada.

### Tests for User Story 2 (a implementação já existe desde T008 — este teste só precisa ser escrito e já deve passar)

- [X] T010 [P] [US2] Criar `tests/SPI.Application.Tests/Aulas/AulaServiceGerarContasAReceberAvulsaTests.cs` (mesmo padrão de helpers de T007): (1) `VinculoCobranca` `Modalidade = Avulsa`, `Valor` diferente de `Aluno.ValorAula`, ativo para a Turma da aula → `Pagamento` gerado com `ValorFinal == VinculoCobranca.Valor` (nunca `Aluno.ValorAula`), demais campos (`Descricao`/`CategoriaReceitaId`/`DataVencimento`/`Competencia`) idênticos ao padrão do fallback (US2-1, FR-003); (2) mesmo teste para `VinculoCobranca` de atendimento individual (`TurmaId = null`) numa aula individual (US2-2); (3) **[FR-011]** em ambos os casos, `FakeVinculoCobrancaRepositoryParaAula.Salvamentos == 0` — modalidade Avulsa só lê o vínculo, nunca escreve nele

### Implementation for User Story 2

*Nenhuma tarefa de implementação nova — o ramo Avulsa já foi entregue em T008 (ver nota estrutural no topo do arquivo). T010 valida esse ramo de forma isolada.*

- [X] T011 [US2] Validar US2: `dotnet test "tests/SPI.Application.Tests" --filter FullyQualifiedName~AulaServiceGerarContasAReceberAvulsaTests` verde (deve passar sem nenhuma mudança de código desde T008)

**Checkpoint**: US1 e US2 cobertas por teste e funcionando.

---

## Phase 5: User Story 3 — Pacote consome saldo em vez de gerar cobrança (Priority: P2)

**Goal**: presenças de alunos com `VinculoCobranca` Pacote não geram cobrança; `SaldoAulas` decresce em 1 por presença, nunca abaixo de zero.

**Independent Test**: quickstart.md §2 cenários 4–6 — `SaldoAulas` inicial 2, três presenças seguidas, conferir a sequência 2→1→0→0 e nenhuma conta gerada em nenhuma delas.

### Tests for User Story 3 (a implementação já existe desde T008 — este teste só precisa ser escrito e já deve passar)

- [X] T012 [P] [US3] Criar `tests/SPI.Application.Tests/Aulas/AulaServiceGerarContasAReceberPacoteTests.cs` (mesmo padrão de helpers de T007; para o cenário sequencial, semear 3 aulas distintas do mesmo aluno no mesmo contexto e chamar `RegistrarSessaoAsync` três vezes sobre o **mesmo objeto** `VinculoCobranca` em memória, para que o decremento acumule): (1) `Modalidade = Pacote`, `SaldoAulas = 5`, presença confirmada → nenhum `Pagamento` em `Gerados`, e `VinculoCobranca.SaldoAulas == 4` após a chamada, com `FakeVinculoCobrancaRepositoryParaAula.Salvamentos >= 1` (US3-1, FR-005); (2) `SaldoAulas = 0` → nenhum `Pagamento`, `SaldoAulas` permanece `0` (nunca negativo) (US3-2, FR-006); (3) `SaldoAulas = null` (nunca informado) → nenhum `Pagamento`, `SaldoAulas` permanece `null` (US3-3, FR-006, research.md R5); (4) sequência completa: `SaldoAulas = 2` → após a 1ª presença `SaldoAulas == 1`, após a 2ª `SaldoAulas == 0`, após a 3ª `SaldoAulas == 0` (nunca negativo), e **nenhuma** das três gera `Pagamento` (Independent Test da User Story 3, replica quickstart §2 cenários 4–6); (5) **[FR-010]** no cenário 2 (saldo já em 0), nenhuma exceção é lançada pela chamada e nenhum campo além de `SaldoAulas` é tocado — prova de que nenhum aviso/bloqueio foi implementado; (6) **[FR-008]** com `SaldoAulas = 5` e a presença marcada como **não confirmada** (falta), `RegistrarSessaoAsync` não gera nenhum `Pagamento` e `SaldoAulas` permanece `5` (falta nunca decrementa, mesmo com vínculo Pacote ativo)

### Implementation for User Story 3

*Nenhuma tarefa de implementação nova — o ramo Pacote já foi entregue em T008 (ver nota estrutural no topo do arquivo). T012 valida esse ramo de forma isolada.*

- [X] T013 [US3] Validar US3: `dotnet test "tests/SPI.Application.Tests" --filter FullyQualifiedName~AulaServiceGerarContasAReceberPacoteTests` verde (deve passar sem nenhuma mudança de código desde T008)

**Checkpoint**: US1, US2 e US3 cobertas por teste e funcionando.

---

## Phase 6: User Story 4 — Mensalidade não gera cobrança avulsa por presença (Priority: P3)

**Goal**: presenças de alunos com `VinculoCobranca` Mensalidade não geram nenhuma cobrança nem alteram o vínculo.

**Independent Test**: quickstart.md §2 cenário 3 — vínculo Mensalidade, registrar sessão, conferir que nenhuma conta é gerada.

### Tests for User Story 4 (a implementação já existe desde T008 — este teste só precisa ser escrito e já deve passar)

- [X] T014 [P] [US4] Criar `tests/SPI.Application.Tests/Aulas/AulaServiceGerarContasAReceberMensalidadeTests.cs` (mesmo padrão de helpers de T007): (1) `VinculoCobranca` `Modalidade = Mensalidade`, ativo para o contexto da aula, presença confirmada → nenhum `Pagamento` em `Gerados` (US4-1, FR-004); (2) **[FR-009]** `Valor` e `AulasIncluidas` do `VinculoCobranca` continuam exatamente iguais antes/depois da chamada (snapshot comparado), e `FakeVinculoCobrancaRepositoryParaAula.Salvamentos == 0` — modalidade Mensalidade não escreve nada no vínculo, só o lê; (3) **[FR-008]** com a presença marcada como **não confirmada** (falta), `RegistrarSessaoAsync` não gera nenhum `Pagamento` mesmo com o vínculo Mensalidade ativo configurado para o contexto

### Implementation for User Story 4

*Nenhuma tarefa de implementação nova — o ramo Mensalidade já foi entregue em T008 (ver nota estrutural no topo do arquivo). T014 valida esse ramo de forma isolada.*

- [X] T015 [US4] Validar US4: `dotnet test "tests/SPI.Application.Tests" --filter FullyQualifiedName~AulaServiceGerarContasAReceberMensalidadeTests` verde (deve passar sem nenhuma mudança de código desde T008)

**Checkpoint**: as 4 user stories cobertas por teste e funcionando — feature completa (ver nota estrutural: aqui as stories convergem, porque é um único método).

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: cenário de múltiplos alunos com modalidades diferentes na mesma aula (FR-007), atualização do registro de EX-001, e validação final

- [X] T016 [P] Criar `tests/SPI.Application.Tests/Aulas/AulaServiceGerarContasAReceberMultiplosAlunosTests.cs` (depende de T008; mesmo padrão de helpers de T007) — **cobertura de FR-007**: uma única aula de turma com 3 `AulaAluno`, cada um com um contexto diferente configurado no `FakeVinculoCobrancaRepositoryParaAula` (aluno A sem vínculo, aluno B com Avulsa de Valor próprio, aluno C com Pacote `SaldoAulas = 3`), todos marcados presentes numa única chamada a `RegistrarSessaoAsync`: confirma que exatamente 2 `Pagamento` são gerados (A com `Aluno.ValorAula`, B com o Valor do vínculo) e nenhum para C, e que `SaldoAulas` de C cai para `2` — tudo na mesma chamada, provando que a decisão é por aluno, não por aula (edge case "modalidades diferentes na mesma aula").
- [X] T017 Editar `specs/037-vinculo-cobranca/plan.md`, seção "Nota sobre EX-001 (não é uma nova exceção, é a atualização da existente)" de `specs/038-vinculo-cobranca-gerar-contas/plan.md` → copiar/adaptar para lá: atualizar o bloco "EX-001 — Registro da exceção ao Princípio III" em `specs/037-vinculo-cobranca/plan.md` (Complexity Tracking) trocando **Status: 🔓 ABERTA** por **Status: 🟡 PARCIALMENTE RESOLVIDA (specs/038)** e acrescentando uma linha explicando que Avulsa e Pacote já têm efeito real desde specs/038, mas Mensalidade continua sem efeito automático até o job futuro — a exceção só fecha por completo (🔒 RESOLVIDA) quando esse job existir. **Não alterar mais nada nesse arquivo.**
- [X] T018 Rodar a suíte completa `dotnet test "tests/SPI.Application.Tests"` (todos os testes verdes, incluindo os de specs/037 que usam `VinculoCobrancaFakes.cs`) e `dotnet build` da solução; depois `git -C "C:\PROJETO - SPI" diff --stat -- src/SPI.Application/VinculosCobranca src/SPI.Api/Controllers/VinculosCobrancaController.cs frontend/components/alunos` (esperado: vazio — nada do cadastro do vínculo nem da UI foi tocado, FR-011/quickstart §3)
- [X] T019 Executar quickstart.md completo (§1 já coberto por T018; §2, os 8 cenários via API, com o backend rodando; §3, a checagem de não-regressão dos testes de specs/037)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001)**: sem dependências.
- **Foundational (T002–T006)**: depende de T001; **bloqueia** as user stories. Ordem interna: T002 → (T003, T004); T005 é independente de T002 (só usa o tipo `IVinculoCobrancaRepository`, já existente) e pode rodar em paralelo com T002/T003/T004; T006 também não depende de T003/T004/T005, mas por clareza é listado por último.
- **US1 (T007–T009)**: depende da Foundational. **US2 (T010–T011), US3 (T012–T013), US4 (T014–T015)**: dependem de T008 (dentro de US1) ter sido concluída — ver nota estrutural no topo. Seus testes podem ser **escritos** em paralelo a US1 (não dependem do código, só da interface pública já existente e dos fakes de T006), mas só **passam** depois de T008.
- **Polish (T016–T019)**: T016 depende de T008; T017 é independente de código (só depende da decisão de negócio estar tomada, ou seja, depois de T008); T018–T019 dependem de todas as stories.

### Within Each Story

- Testes primeiro e falhando (T007/T008) → implementação (T008) → validação.
- US2/US3/US4: teste primeiro (falha por falta do arquivo/compila mas falha lógica antes de T008; passa imediatamente depois) → validação.

### User Story Dependencies

- **US1**: bloqueia US2/US3/US4 na prática (mesma implementação), mas cada uma é **testável de forma independente** por ter sua própria suíte, que pode ser escrita antes ou depois de US1 sem afetar as demais.

## Parallel Opportunities

- Foundational: T002 ‖ T005 ‖ T006 desde o início (T005 e T006 não dependem de T002); T003 ‖ T004 assim que T002 terminar (dependem só dele).
- T007 (US1), T010 (US2), T012 (US3), T014 (US4) e T016 (Polly) podem todos ser **escritos** em paralelo entre si (arquivos diferentes, todos dependem só de T006) — só a execução/validação de US2/US3/US4/T016 depende de T008 estar pronta.

### Parallel Example: escrever todos os testes antes da implementação

```text
# Podem ser escritos em paralelo (todos dependem só de T006):
T007  AulaServiceGerarContasAReceberSemVinculoTests.cs
T010  AulaServiceGerarContasAReceberAvulsaTests.cs
T012  AulaServiceGerarContasAReceberPacoteTests.cs
T014  AulaServiceGerarContasAReceberMensalidadeTests.cs
T016  AulaServiceGerarContasAReceberMultiplosAlunosTests.cs

# Só depois, em sequência: T008 (implementação) faz todos passarem de uma vez.
```

## Implementation Strategy

### MVP First (User Story 1)

1. T001 → Foundational (T002–T006) → US1 (T007–T009).
2. **PARAR e validar** com quickstart §2 cenário 1.
3. Nesse ponto, o MVP já contém tecnicamente os 4 ramos (nota estrutural) — mas só US1 tem teste e validação formal feitos.

### Incremental Delivery (validação, não implementação)

1. Base + Foundational + US1 → MVP com não-regressão garantida por teste.
2. + US2 → Avulsa coberto por teste (sem nova implementação).
3. + US3 → Pacote coberto por teste (sem nova implementação).
4. + US4 → Mensalidade coberto por teste (sem nova implementação).
5. + Polish → FR-007 coberto, EX-001 atualizada, validação final.

## Notes

- **Nota estrutural** (repetida do topo por importância): T008 implementa os 4 ramos de uma vez, porque é um único método com um único `switch` (research.md R2) — não é possível decompor a implementação por story sem deixar o método temporariamente incorreto. A organização por story aqui serve para **teste e rastreabilidade de requisito**, não para entrega incremental de código.
- **Requisitos negativos com cobertura explícita**: FR-009 (Mensalidade não altera o vínculo) → T014 item 2; FR-010 (Pacote esgotado não gera aviso/bloqueio) → T012 item 5; FR-011 (cadastro do vínculo intocado) → T010 item 3, T006 (fakes que lançam `NotImplementedException` para métodos de escrita não usados) e T018 (`git diff` vazio).
- **EX-001**: T017 é obrigatória — sem ela, o registro em `specs/037-vinculo-cobranca/plan.md` ficaria desatualizado (diria "ABERTA" mesmo depois de Avulsa/Pacote já terem efeito real).
- Sem migração de banco nesta feature (specs/038 não cria `database/NN_*.sql`) — `saldo_aulas` já existe desde specs/037.
- Commitar por tarefa ou grupo lógico.
