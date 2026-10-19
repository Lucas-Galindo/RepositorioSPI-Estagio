---

description: "Task list for Registrar Presença na Criação da Aula"
---

# Tasks: Registrar Presença na Criação da Aula

**Input**: Design documents from `specs/055-presenca-criacao-aula/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/aulas-api.md, quickstart.md

**Tests**: incluídos — a feature introduz um novo ponto de disparo (criação) para efeitos financeiros já existentes (cobrança/saldo), risco real o suficiente para justificar cobertura automatizada, no mesmo padrão já usado neste projeto para `AulaService` (specs/038, specs/041, specs/052).

**Organization**: tarefas agrupadas por user story (US1 P1 criar aula retroativa com presença; US2 P2 editar Descrição após "Realizada"). As duas stories são independentes entre si — tocam métodos diferentes de `AulaService` (`CadastrarAsync`/`RegistrarSessaoAsync` vs. `AtualizarAsync`) — por isso não há fase "Foundational" compartilhada; a implementação de cada uma vive na sua própria fase.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode ser feita em paralelo com outras tarefas marcadas [P] (arquivos diferentes, sem dependência pendente)
- **[Story]**: US1 ou US2

## Path Conventions

Backend em `src/SPI.Application/Aulas/` (DTO e service); frontend em `frontend/components/aulas/` e `frontend/lib/api/aulas.ts`; testes em `tests/SPI.Application.Tests/Aulas/`. Nenhuma migração de banco (`database/`), nenhuma mudança em `src/SPI.Api/Controllers/AulasController.cs` (os dois endpoints afetados não mudam de assinatura — só o comportamento interno do service muda, ver contracts/aulas-api.md).

---

## Phase 1: Setup

Não aplicável — nenhuma dependência nova, nenhuma configuração a alterar.

---

## Phase 2: Foundational (Blocking Prerequisites)

Não aplicável — US1 e US2 tocam métodos independentes de `AulaService` (`CadastrarAsync`/`RegistrarSessaoAsync` vs. `AtualizarAsync`); nenhuma delas bloqueia a outra.

---

## Phase 3: User Story 1 - Lançar uma aula que já aconteceu em um único passo (Priority: P1) 🎯 MVP

**Goal**: ao criar uma aula cuja Data de Início é hoje ou passada, a professora marca a presença de cada aluno no mesmo formulário, e a aula já nasce "Realizada" com os mesmos efeitos financeiros que "Registrar Sessão" já produz hoje.

**Independent Test**: criar uma aula de turma com data de ontem, marcar presença de parte dos alunos, salvar, e confirmar que a aula já aparece "Realizada" com as presenças corretas e os mesmos efeitos financeiros de sempre (quickstart.md §1).

### Implementation for User Story 1

- [ ] T001 [US1] Em `src/SPI.Application/Aulas/Dtos/AulaRequest.cs`, adicionar `public Dictionary<int, bool>? Presencas { get; set; }` — campo opcional (nulo por padrão), com o comentário XML descrito em data-model.md (obrigatório apenas quando `DataInicio` é hoje ou passada; ignorado quando futura).
- [ ] T002 [US1] Em `src/SPI.Application/Aulas/Services/AulaService.cs`, extrair o corpo de `RegistrarSessaoAsync` (a partir do loop de validação de presença/bloqueio por pacote esgotado/incremento de frequência, incluindo a troca de `Status` para `"Realizada"`, a chamada a `_lembreteService.RecalcularAsync` e a chamada a `GerarContasAReceberAsync`) para um novo método privado `private async Task<List<string>> AplicarPresencasAsync(Aula aula, Dictionary<int, bool> presencas, CancellationToken cancellationToken)`, retornando a lista de avisos (research.md R1). `RegistrarSessaoAsync` passa a só validar `aula.Status != "Agendada"` e delegar: `var avisos = await AplicarPresencasAsync(aula, request.Presencas, cancellationToken); var resposta = Mapear(aula); resposta.Avisos = avisos; return resposta;`. Comportamento observável de `RegistrarSessaoAsync` deve permanecer idêntico.
- [ ] T003 [US1] Em `AulaService.CadastrarAsync` (depende de T001), logo após `ValidarConflitoAsync`: calcular `var retroativa = request.DataInicio <= DateOnly.FromDateTime(DateTime.Now);` (research.md R2 — comparação só por data, sem hora); se `retroativa && request.Presencas is null`, lançar `throw new ConflitoException("Informe a presenca de cada aluno para criar uma aula retroativa.");` antes de criar a entidade `Aula`.
- [ ] T004 [US1] Em `AulaService.CadastrarAsync` (depende de T002, T003), após `DefinirAlunosAsync`/`SalvarAlteracoesAsync`: recarregar a aula via `ObterPorIdAsync` (já existe no método); se `retroativa`, chamar `var avisos = await AplicarPresencasAsync(aulaRecarregada, request.Presencas!, cancellationToken);` em vez da chamada atual a `_lembreteService.RecalcularAsync` (que passa a acontecer só no ramo `else`/futuro, já que `AplicarPresencasAsync` já cuida do recálculo quando retroativa); usar `avisos` para preencher `AulaResponse.Avisos` no retorno. Quando não retroativa, manter exatamente o comportamento atual (`RecalcularAsync` quando `TurmaId.HasValue`, `Avisos` vazio).
- [ ] T005 [P] [US1] Em `frontend/lib/api/aulas.ts`, adicionar `presencas?: Record<number, boolean>;` à interface `AulaRequest` (espelha T001).
- [ ] T006 [P] [US1] Criar `frontend/components/aulas/PresencaChecklist.tsx`: componente de apresentação que recebe `alunos: { id: number; nome: string; ra: string }[]`, `presencas: Record<number, boolean>` e `onChange: (alunoId: number, presente: boolean) => void`; renderiza um item por aluno (nome + RA) com um checkbox rotulado "Presente" (classe `chk-pill`, mesmo padrão visual já usado em `frontend/app/(app)/aulas/[id]/page.tsx`), marcado por padrão quando a chave correspondente não existe em `presencas` (research.md R3/FR-003).
- [ ] T007 [US1] Em `frontend/components/aulas/AulaForm.tsx` (depende de T005, T006): calcular `const hoje = new Date().toISOString().slice(0, 10); const retroativa = !editando && dataInicio !== "" && dataInicio <= hoje;` (mesma regra de T003, só por data); quando `retroativa`, montar o roster — para turma selecionada: `turmas.find(t => t.id === Number(turmaId))?.alunos.filter(a => alunosAtivos.some(ativo => ativo.id === a.id)) ?? []` (interseção com a lista `alunos` já carregada por `listarAlunos({ ativo: true })`, research.md R4); para aula individual: só o aluno selecionado em `alunoId`. Adicionar estado `const [presencas, setPresencas] = useState<Record<number, boolean>>({});`, renderizar `<PresencaChecklist>` com esse roster quando `retroativa && roster.length > 0`, e incluir `presencas` no objeto `AulaRequest` enviado por `cadastrarAula` somente quando `retroativa`.
- [ ] T008 [US1] Verificação de não-regressão (regra do `CLAUDE.md` para FR-009/SC-003, "MUST NOT" de comportamento): rodar `git diff -- "frontend/app/(app)/aulas/[id]/page.tsx"` ao final desta fase e confirmar saída vazia — a tela de "Registrar Sessão" não deve ter sido tocada (research.md R6).
- [ ] T009 [P] [US1] Testes em `tests/SPI.Application.Tests/Aulas/AulaServiceCadastrarRetroativoTests.cs`: (a) `DataInicio` passada com `Presencas` nulo lança `ConflitoException`; (b) `DataInicio` passada com `Presencas` completo cria a aula com `Status == "Realizada"` e `AulaAluno.Presente` correto para cada aluno; (c) `DataInicio` passada com `Presencas` faltando algum aluno da turma lança `ConflitoException` com a mesma mensagem já usada por `RegistrarSessaoAsync` ("Presenca do aluno {id} nao foi informada."); (d) `DataInicio` futura ignora `Presencas` mesmo se informado, aula permanece `"Agendada"`, `AulaAluno.Presente` continua `null`; (e) `_lembreteService.RecalcularAsync` (via `FakeLembreteServiceVazio.Chamadas`) é chamado exatamente uma vez para a turma, tanto no caminho retroativo (dentro de `AplicarPresencasAsync`) quanto no futuro — nunca duas vezes, nunca zero.
- [ ] T010 [US1] Rodar `dotnet test` na suíte completa (`tests/SPI.Application.Tests/`) e confirmar que todos os testes já existentes de `RegistrarSessaoAsync`/`GerarContasAReceberAsync` (specs/038, specs/041 — ex.: `AulaServiceGerarContasAReceberAvulsaTests.cs`, `...PacoteTests.cs`, `...MensalidadeTests.cs`, `...SemVinculoTests.cs`, `...MultiplosAlunosTests.cs`, `AulaServiceRegistrarSessaoPacoteEsgotadoTests.cs`, `...PacoteRenovacaoTests.cs`) continuam passando sem nenhuma asserção alterada — confirma que a extração de `AplicarPresencasAsync` (T002) preservou o comportamento existente byte a byte.

**Checkpoint**: User Story 1 completa e testável de forma independente.

---

## Phase 4: User Story 2 - Atualizar as observações de uma aula depois dela já ter acontecido (Priority: P2)

**Goal**: a professora consegue editar o campo Descrição de uma aula mesmo com `Status == "Realizada"`; todos os demais campos continuam bloqueados.

**Independent Test**: abrir uma aula "Realizada", editar só a Descrição, salvar com sucesso; tentar alterar qualquer outro campo da mesma aula e ser rejeitado (quickstart.md §6).

### Implementation for User Story 2

- [ ] T011 [US2] Em `AulaService.AtualizarAsync`, substituir o bloqueio incondicional (`if (aula.Status == "Realizada") throw new ConflitoException(...)`, antes de qualquer outra validação) por: se `aula.Status == "Realizada"`, verificar se o request tenta mudar algum destes campos em relação ao valor atual da aula — `request.MateriaId != aula.MateriaId`, `request.TurmaId != aula.TurmaId`, (quando `aula.TurmaId` é nulo) `request.AlunoId != aula.AulaAlunos.FirstOrDefault()?.AlunoId`, `request.DataInicio != aula.DataInicio`, `request.HoraInicio != aula.HoraInicio`, `request.HoraFim != aula.HoraFim`; se algum mudou, lançar `ConflitoException` (mensagem equivalente à atual, ex.: "Uma aula ja realizada so permite editar a descricao."); se nenhum mudou, executar só `aula.Descricao = request.Descricao; await _aulaRepository.SalvarAlteracoesAsync(cancellationToken); return Mapear(aula);` — sem chamar `ValidarReferenciasAsync`, `ValidarConflitoAsync`, a redefinição de `AulaAlunos`, nem `_lembreteService.RecalcularAsync` nesse caminho (research.md R5). Quando `aula.Status != "Realizada"`, manter o comportamento atual sem nenhuma mudança.
- [ ] T012 [P] [US2] Em `frontend/components/aulas/AulaForm.tsx`: quando `editando && aula?.status === "Realizada"`, desabilitar (`disabled`) os campos Matéria, Turma, Aluno, Data de Início, Horário início e Horário fim, mantendo o campo Descrição editável normalmente — decisão de UI que só evita uma submissão que o backend (T011) já vai rejeitar; nenhuma validação de negócio nova no frontend (Princípio II).
- [ ] T013 [P] [US2] Testes em `tests/SPI.Application.Tests/Aulas/AulaServiceAtualizarRealizadaTests.cs`: (a) aula `"Realizada"` com request alterando só `Descricao` salva com sucesso e o novo valor persiste em `aula.Descricao`; (b) aula `"Realizada"` com request alterando, um por vez, `DataInicio`, `HoraInicio`, `HoraFim`, `MateriaId`, `TurmaId` e `AlunoId` (aula individual) lança `ConflitoException` e não chama `SalvarAlteracoesAsync`; (c) aula com `Status != "Realizada"` continua aceitando edição completa de todos os campos, sem nenhuma mudança de comportamento (não-regressão).

**Checkpoint**: User Stories 1 e 2 completas e testáveis de forma independente.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: não-duplicação de regra de negócio (regra do `CLAUDE.md` para requisitos "MUST NOT"), não-regressão, e validação manual final.

- [ ] T014 Regra do `CLAUDE.md` para FR-005/FR-010 ("MUST NOT duplicar/reimplementar a regra de cobrança e saldo"): ler diretamente o diff de `AulaService.cs` e confirmar que `CadastrarAsync` nunca contém uma cópia da lógica de validação de presença/bloqueio por pacote esgotado/`GerarContasAReceberAsync` — todo esse comportamento só é alcançado através da chamada a `AplicarPresencasAsync` (T002/T004), nunca reimplementado inline.
- [ ] T015 Regra do `CLAUDE.md` para "MUST NOT criar arquivo além do necessário": rodar `git status --porcelain -- src frontend` e confirmar que o único arquivo novo não rastreado é `frontend/components/aulas/PresencaChecklist.tsx` (T006, criação deliberada e documentada em research.md R6) — nenhum outro arquivo novo inesperado.
- [ ] T016 [P] Validar os §1 a §5 de [quickstart.md](./quickstart.md) (US1: aula retroativa de turma com presença; aluno com pacote esgotado; aula futura inalterada; troca de data dinâmica antes de salvar; aula individual retroativa) com o app rodando de verdade.
- [ ] T017 [P] Validar o §6 de [quickstart.md](./quickstart.md) (US2: editar Descrição de uma aula "Realizada"; confirmar bloqueio dos demais campos).
- [ ] T018 Validar o §7 de [quickstart.md](./quickstart.md) (não-regressão: "Registrar Sessão" continua funcionando normalmente para uma aula "Agendada" cuja data já chegou).
- [ ] T019 Rodar `dotnet build` e `dotnet test` (suíte completa) no backend, e `npx tsc --noEmit`, `npx eslint` e `npm run build` no frontend — confirmar 0 erros antes de considerar a feature concluída.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: não aplicável.
- **Foundational (Phase 2)**: não aplicável.
- **User Story 1 (Phase 3)**: T002 depende de T001 (ambos preparam o terreno para T003/T004); T003 depende de T001; T004 depende de T002 e T003; T007 depende de T005 e T006; T008/T009/T010 dependem da implementação (T001-T004) estar pronta. Nenhuma dependência na Phase 4.
- **User Story 2 (Phase 4)**: T011 é independente de toda a Phase 3; T012 depende de T011 (precisa saber que o backend já aceita o fluxo); T013 depende de T011.
- **Polish (Phase 5)**: depende das Phases 3 e 4 completas.

### Parallel Opportunities

- T005 e T006 (frontend) podem ser feitas em paralelo entre si e em paralelo com T002/T003 (backend) — arquivos diferentes.
- T009 (testes US1) pode ser escrita em paralelo com T005-T007 (frontend), já que ambas dependem só de T001-T004 (backend) estarem prontos, não uma da outra.
- T012 e T013 (US2) podem ser feitas em paralelo entre si, ambas dependendo só de T011.
- T016 e T017 (validação manual) são independentes entre si.

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (Setup, N/A) e Phase 2 (Foundational, N/A).
2. Completar Phase 3 (T001-T010) — aula retroativa com presença funcionando ponta a ponta, entregável como MVP.
3. Phase 4 (T011-T013, edição de Descrição) é a entrega incremental seguinte — independente, menor risco.
4. Phase 5 (Polish) fecha com a verificação de não-duplicação e a validação manual completa.

### Incremental Delivery

Foundational (N/A) → US1 (T001-T010, MVP) → US2 (T011-T013) → Polish (T014-T019).
