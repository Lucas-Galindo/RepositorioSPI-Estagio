# Tasks: Reativar Registros Desativados (Turma, Aluno, Matéria)

**Input**: Design documents from `/specs/026-reativar-registros-desativados/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required),
[research.md](./research.md), [contracts/reativar-endpoints.md](./contracts/reativar-endpoints.md),
[quickstart.md](./quickstart.md)

**Tests**: Não solicitados. O projeto não tem framework de teste automatizado configurado (ver
plan.md Technical Context) — validação via [quickstart.md](./quickstart.md).

**Organization**: Tasks agrupadas por user story. US1 (Turma), US2 (Aluno) e US3 (Matéria) são
todas P1 e tocam arquivos completamente distintos entre si — podem ser implementadas em
paralelo por pessoas/sessões diferentes. Dentro de cada story, backend e frontend dependem do
mesmo padrão: interface → implementação do serviço → controller → função do cliente de API →
botão na tela. Todas as três stories dependem da Fase 2 (Foundational — o helper `apiPatch`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 (Turma), US2 (Aluno), US3 (Matéria)
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Projeto web existente (backend ASP.NET Core em `src/`, frontend Next.js em `frontend/`, MySQL).
Nenhum arquivo novo de infraestrutura — só os arquivos já existentes de Turma, Aluno e Matéria
em ambas as camadas, mais o helper `apiPatch` no cliente HTTP do frontend.

---

## Phase 1: Setup

Não aplicável — projeto já inicializado, nenhuma dependência nova, nenhuma migração de banco
(o campo `Ativo` já existe nas 3 tabelas).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Adicionar o helper HTTP que falta no cliente de API do frontend — usado pelas 3
novas funções `reativarTurma`/`reativarAluno`/`reativarMateria` (uma por story).

**⚠️ CRITICAL**: Nenhuma task de frontend das 3 user stories pode começar antes desta fase
estar completa. As tasks de backend das 3 stories NÃO dependem desta fase e podem começar em
paralelo com ela.

- [X] T001 Adicionar `apiPatch` a `frontend/lib/api/client.ts`, espelhando `apiPut` (linhas
  109-129 do arquivo): mesma assinatura de `fetch` com `Authorization: Bearer`, método
  `"PATCH"`, sem `Content-Type`/corpo de requisição (a reativação não recebe payload — ver
  contracts/reativar-endpoints.md). Assinatura:
  `apiPatch<TResponse>(path: string, accessToken: string): Promise<TResponse>`.

**Checkpoint**: `apiPatch` pronto — as 3 user stories podem prosseguir (frontend e backend, em
paralelo entre si).

---

## Phase 3: User Story 1 - Reativar uma Turma desativada (Priority: P1) 🎯 MVP

**Goal**: Uma Turma Inativa pode ser reativada pela tela de detalhe, sem alterar nenhum outro
dado (nome, alunos vinculados, histórico de aulas).

**Independent Test**: Desativar uma Turma, reabrir sua tela de detalhe, clicar em "Reativar" e
confirmar que ela volta a Ativa com todos os dados inalterados (Cenário 1 do quickstart.md).

### Implementation for User Story 1

- [X] T002 [P] [US1] Adicionar `Task<TurmaResponse> ReativarAsync(int id, CancellationToken cancellationToken = default);`
  a `src/SPI.Application/Turmas/Services/ITurmaService.cs` (mesmo estilo das assinaturas já
  existentes no arquivo, ex. `DesvincularAlunoAsync`).
- [X] T003 [US1] Implementar `ReativarAsync` em
  `src/SPI.Application/Turmas/Services/TurmaService.cs`, espelhando `ExcluirAsync` (linhas
  60-67): buscar via `_turmaRepository.ObterPorIdAsync`, lançar `NaoEncontradoException` se
  nulo ("Turma nao encontrada."), setar `turma.Ativo = true`, chamar
  `_turmaRepository.SalvarAlteracoesAsync`, e devolver `Mapear(turma)` (helper já existente na
  linha 108) em vez de `Task` void — diferente de `ExcluirAsync`, esta operação devolve o
  recurso atualizado (ver research.md Decisão 3). Depende de T002.
- [X] T004 [US1] Adicionar ação `[HttpPatch("{id}/reativar")]` `Reativar` a
  `src/SPI.Api/Controllers/TurmasController.cs`, logo após `Excluir` (linhas 138-163): mesmo
  padrão de `[ProducesResponseType]` (200/404/500) e try/catch
  (`NaoEncontradoException` → `NotFound`, `Exception` → `Problem` 500), chamando
  `_turmaService.ReativarAsync(id)`, logando `"Turma {Id} reativada"`, retornando
  `Ok(response)` com o `TurmaResponse` devolvido pelo serviço. Sem `[Authorize]` próprio — herda
  o `[Authorize(Roles = nameof(PerfilUsuario.Professor))]` já aplicado a nível de classe (linha
  14). Depende de T003.
- [X] T005 [P] [US1] Adicionar `reativarTurma(id: number, accessToken: string): Promise<Turma>`
  a `frontend/lib/api/turmas.ts`, logo após `excluirTurma` (linha 40-42), chamando
  `apiPatch<Turma>(\`/api/turmas/${id}/reativar\`, accessToken)`. Depende de T001
  (Foundational).
- [X] T006 [US1] Em `frontend/app/(app)/turmas/[id]/page.tsx`: importar `reativarTurma`; criar
  `handleReativar` (mesmo formato de `handleExcluir`, linhas 84-97, mas sem `ConfirmModal` —
  chama `reativarTurma(turma.id, sessao.accessToken)` direto ao clicar, e no sucesso faz
  `setTurma(resposta)` e `mostrarToast("Turma reativada com sucesso.")` em vez de
  `router.push`); no bloco `.actions` (linhas 118-125), envolver o botão "Excluir" existente em
  `{turma.ativo && (...)}` e adicionar, ao lado, `{!turma.ativo && (<button ... onClick={handleReativar}>Reativar</button>)}`
  (mesma classe `btn btn-sm`, ícone `check` do `Icon` — `<Icon name="check" size={13} />`, ver
  `components/shared/Icon.tsx`). Depende de T004 e T005.

**Checkpoint**: User Story 1 completa e testável de forma independente — Turma reativável pela
tela, sem regressão nos demais dados.

---

## Phase 4: User Story 2 - Reativar um Aluno desativado (Priority: P1)

**Goal**: Um Aluno Inativo pode ser reativado pela tela de detalhe, sem alterar histórico de
aulas/pagamentos.

**Independent Test**: Desativar um Aluno, reabrir sua tela de detalhe, clicar em "Reativar" e
confirmar que ele volta Ativo com histórico inalterado (Cenário 2 do quickstart.md).

### Implementation for User Story 2

- [X] T007 [P] [US2] Adicionar `Task<AlunoResponse> ReativarAsync(int id, CancellationToken cancellationToken = default);`
  a `src/SPI.Application/Alunos/Services/IAlunoService.cs`.
- [X] T008 [US2] Implementar `ReativarAsync` em
  `src/SPI.Application/Alunos/Services/AlunoService.cs`, espelhando `ExcluirAsync` (linhas
  99-106): buscar via `ObterPorIdAsync`, `NaoEncontradoException` se nulo ("Aluno nao
  encontrado."), `aluno.Ativo = true`, `SalvarAlteracoesAsync`, devolver `Mapear(aluno)` (helper
  na linha 108). Depende de T007.
- [X] T009 [US2] Adicionar ação `[HttpPatch("{id}/reativar")]` `Reativar` a
  `src/SPI.Api/Controllers/AlunosController.cs`, logo após `Excluir` (linhas 169-185), mesmo
  padrão de T004 (200/404/500, log `"Aluno {Id} reativado"`, `Ok(response)`). Depende de T008.
- [X] T010 [P] [US2] Adicionar `reativarAluno(id: number, accessToken: string): Promise<Aluno>`
  a `frontend/lib/api/alunos.ts`, logo após `excluirAluno` (linhas 70-72), usando `apiPatch`.
  Depende de T001 (Foundational).
- [X] T011 [US2] Em `frontend/app/(app)/alunos/[id]/page.tsx`: mesmo padrão de T006 — importar
  `reativarAluno`, criar `handleReativar` (mesmo formato de `handleExcluir`, linhas 50-63, sem
  confirmação, `setAluno(resposta)` no sucesso), e no bloco de ações (próximo ao `StatusPill` na
  linha 93 e ao botão "Excluir" nas linhas 100-102) envolver "Excluir" em
  `{aluno.ativo && (...)}` e adicionar "Reativar" em `{!aluno.ativo && (...)}`. Depende de T009 e
  T010.

**Checkpoint**: User Story 2 completa e testável de forma independente.

---

## Phase 5: User Story 3 - Reativar uma Matéria desativada (Priority: P1)

**Goal**: Uma Matéria Inativa pode ser reativada pela tela de detalhe, sem alterar vínculos
existentes (turmas, aulas).

**Independent Test**: Desativar uma Matéria, reabrir sua tela de detalhe, clicar em "Reativar" e
confirmar que ela volta Ativa com vínculos inalterados (Cenário 3 do quickstart.md).

### Implementation for User Story 3

- [X] T012 [P] [US3] Adicionar `Task<MateriaResponse> ReativarAsync(int id, CancellationToken cancellationToken = default);`
  a `src/SPI.Application/Materias/Services/IMateriaService.cs`.
- [X] T013 [US3] Implementar `ReativarAsync` em
  `src/SPI.Application/Materias/Services/MateriaService.cs`, espelhando `ExcluirAsync` (linhas
  73-80): buscar via `ObterPorIdAsync`, `NaoEncontradoException` se nulo ("Materia nao
  encontrada."), `materia.Ativo = true`, `SalvarAlteracoesAsync`, devolver `Mapear(materia)`
  (helper na linha 82). Depende de T012.
- [X] T014 [US3] Adicionar ação `[HttpPatch("{id}/reativar")]` `Reativar` a
  `src/SPI.Api/Controllers/MateriasController.cs`, logo após `Excluir` (linhas 157-173), mesmo
  padrão de T004/T009 (200/404/500, log `"Materia {Id} reativada"`, `Ok(response)`). Depende de
  T013.
- [X] T015 [P] [US3] Adicionar `reativarMateria(id: number, accessToken: string): Promise<Materia>`
  a `frontend/lib/api/materias.ts`, logo após `excluirMateria` (linhas 38-40), usando
  `apiPatch`. Depende de T001 (Foundational).
- [X] T016 [US3] Em `frontend/app/(app)/materias/[id]/page.tsx`: mesmo padrão de T006/T011 —
  importar `reativarMateria`, criar `handleReativar` (mesmo formato de `handleExcluir`, linhas
  44-57, sem confirmação, `setMateria(resposta)` no sucesso), e no bloco de ações (próximo ao
  `StatusPill` na linha 77 e ao botão "Excluir" nas linhas 84-86) envolver "Excluir" em
  `{materia.ativo && (...)}` e adicionar "Reativar" em `{!materia.ativo && (...)}`. Depende de
  T014 e T015.

**Checkpoint**: As três user stories funcionam em conjunto — Turma, Aluno e Matéria seguem o
mesmo padrão simétrico de reativação, sem regressão em nenhuma das três.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verificação final cruzando as três user stories, conforme
[quickstart.md](./quickstart.md).

- [X] T017 [P] Rodar `dotnet build` no backend (`src/`) — sem erros.
- [X] T018 [P] Rodar `npx tsc --noEmit` e o lint do frontend (`frontend/`) — sem erros.
- [ ] T019 Executar a validação completa do [quickstart.md](./quickstart.md) (Cenários 1 a 7):
  reativação de Turma/Aluno/Matéria, ausência de "Reativar" em registro já Ativo, permissão
  espelhando a de desativação, idempotência, e simetria estrutural entre as 3 entidades
  (FR-001 a FR-008, SC-001 a SC-004).
- [X] T020 Confirmar, por `git diff --stat`, que somente os 16 arquivos previstos no plan.md
  (Project Structure) foram alterados — 3 controllers, 3 pares interface/serviço, 1
  `client.ts`, 3 arquivos `lib/api/*.ts` e 3 telas de detalhe — nenhum arquivo de banco de dados
  ou infraestrutura tocado.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: N/A.
- **Foundational (Phase 2)**: Sem dependências externas — pode começar imediatamente. BLOQUEIA
  as tasks de *frontend* de US1/US2/US3 (T005/T006, T010/T011, T015/T016); as tasks de
  *backend* das 3 stories não dependem dela.
- **User Story 1, 2, 3 (Phases 3-5)**: Cada uma depende só da Fase 2 (para a parte de
  frontend) — são independentes entre si (arquivos completamente distintos) e podem ser
  implementadas em paralelo.
- **Polish (Phase 6)**: Depende das Fases 3, 4 e 5 completas.

### Dentro de cada User Story

- Backend: interface (T002/T007/T012) → implementação do serviço (T003/T008/T013) → controller
  (T004/T009/T014), sequencial.
- Frontend: função do cliente de API (T005/T010/T015, depende só da Fase 2) → botão na tela
  (T006/T011/T016, depende do controller da própria story E da função do cliente de API).

### Parallel Opportunities

- T001 (Foundational) pode rodar em paralelo com o início do backend de todas as 3 stories
  (T002, T007, T012), já que o backend não depende do `apiPatch`.
- US1, US2 e US3 são inteiramente paralelizáveis entre si — nenhuma toca um arquivo que outra
  também toca.
- Dentro de cada story, T002/T007/T012 (interfaces) são independentes entre stories e
  paralelizáveis com T005/T010/T015 (funções de API, cada uma num arquivo diferente).

---

## Parallel Example: As Três User Stories

```bash
# Apos a Fase 2 (Foundational) completa, uma pessoa/sessao por story:
Task: "US1 — ITurmaService + TurmaService + TurmasController + reativarTurma + botao"
Task: "US2 — IAlunoService + AlunoService + AlunosController + reativarAluno + botao"
Task: "US3 — IMateriaService + MateriaService + MateriasController + reativarMateria + botao"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Fase 2: Foundational (T001) → `apiPatch` pronto
2. Completar Fase 3: User Story 1 (T002-T006) → Turma reativável → **entregável isoladamente
   como MVP** (prova o padrão de ponta a ponta antes de replicar para Aluno/Matéria)
3. Parar e validar Cenário 1 do quickstart.md

### Incremental Delivery

1. Foundational (T001) → base pronta
2. User Story 1 — Turma (T002-T006) → MVP entregável
3. User Story 2 — Aluno (T007-T011) → entregável isoladamente, mesmo padrão replicado
4. User Story 3 — Matéria (T012-T016) → entregável isoladamente, mesmo padrão replicado
5. Polish (T017-T020) → verificação final cruzada

## Notes

- Tests: nenhuma automatizada nesta feature (ver seção "Tests" acima) — validação inteiramente
  manual via quickstart.md, mesma limitação pré-existente do projeto já registrada em specs
  anteriores.
- Nenhuma task desta lista deve adicionar validação de negócio nova (unicidade, checagem de
  vínculos) à reativação — confirmado desnecessário em research.md Decisão 7 e spec.md FR-008.
- Nenhuma task deve alterar o comportamento do botão "Excluir" quando o registro já está Ativo
  — ele continua exatamente como hoje; só passa a ficar oculto quando o registro está Inativo
  (ver research.md Decisão 6).
