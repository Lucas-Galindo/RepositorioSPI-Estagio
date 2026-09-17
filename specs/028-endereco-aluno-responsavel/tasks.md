# Tasks: Endereço do Aluno e do Responsável

**Input**: Design documents from `/specs/028-endereco-aluno-responsavel/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required),
[research.md](./research.md), [data-model.md](./data-model.md),
[contracts/aluno-endereco.md](./contracts/aluno-endereco.md), [quickstart.md](./quickstart.md)

**Tests**: Não solicitados. O projeto não tem framework de teste automatizado configurado (ver
plan.md Technical Context) — validação via [quickstart.md](./quickstart.md).

**Organization**: Tasks agrupadas por user story. US1 (endereço do Aluno, P1) é a base — US2
(endereço do responsável, P2) e US3 (autopreenchimento por CEP, P3) dependem dela porque tocam
os mesmos arquivos (DTOs, validators, `AlunoService.Mapear`, `AlunoForm.tsx`,
`alunos/[id]/page.tsx`) nas mesmas regiões de código. Todas dependem da Fase 2 (Foundational —
migração de banco + campos na entidade `Aluno`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 (endereço do Aluno), US2 (endereço do responsável), US3 (autopreenchimento CEP)
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Projeto web existente (backend ASP.NET Core em `src/`, frontend Next.js em `frontend/`, MySQL em
`database/`). Nenhum arquivo/entidade/endpoint novo além de `frontend/lib/viacep.ts` (US3) e da
migração SQL (Foundational).

---

## Phase 1: Setup

Não aplicável — projeto já inicializado, nenhuma dependência nova (ver research.md Decisão 5:
ViaCEP é chamado com `fetch` nativo, sem biblioteca nova).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Criar as 15 colunas no banco e os 15 campos na entidade `Aluno` — pré-requisito
para qualquer DTO/validator/service/tela das 3 user stories.

**⚠️ CRITICAL**: Nenhuma task de US1/US2/US3 pode começar antes desta fase estar completa.

- [X] T001 Criar `database/13_endereco_aluno_responsavel.sql`: `ALTER TABLE alunos ADD COLUMN`
  para as 15 colunas (todas `NULL`-áveis, nenhuma `NOT NULL`, ver research.md Decisão 3): `cep
  VARCHAR(8)`, `rua VARCHAR(150)`, `numero VARCHAR(10)`, `complemento VARCHAR(100)`, `bairro
  VARCHAR(100)`, `cidade VARCHAR(100)`, `estado VARCHAR(2)`, `responsavel_mesmo_endereco BOOLEAN
  NOT NULL DEFAULT TRUE`, `responsavel_cep VARCHAR(8)`, `responsavel_rua VARCHAR(150)`,
  `responsavel_numero VARCHAR(10)`, `responsavel_complemento VARCHAR(100)`, `responsavel_bairro
  VARCHAR(100)`, `responsavel_cidade VARCHAR(100)`, `responsavel_estado VARCHAR(2)` — usar o SQL
  exato e os comentários de coluna já definidos em data-model.md (seção "Migração de banco"),
  seguindo o cabeçalho/formato de `database/12_anexo_comprovante_financeiro.sql`. Aplicada
  contra o banco local via `mysql` CLI e verificada com `DESCRIBE alunos;` — as 15 colunas
  existem com os tipos/nullability exatos do script.
- [X] T002 Em `src/SPI.Domain/Entities/Aluno.cs`, adicionar as 15 propriedades correspondentes
  (todas `string?` exceto `ResponsavelMesmoEndereco:bool`): `Cep`, `Rua`, `Numero`,
  `Complemento`, `Bairro`, `Cidade`, `Estado`, `ResponsavelMesmoEndereco`, `ResponsavelCep`,
  `ResponsavelRua`, `ResponsavelNumero`, `ResponsavelComplemento`, `ResponsavelBairro`,
  `ResponsavelCidade`, `ResponsavelEstado`. Depende de T001 (mesma estrutura de colunas).
  **Achado durante a implementação**: `src/SPI.Infrastructure/Persistence/Configurations/AlunoConfiguration.cs`
  mapeia cada propriedade explicitamente via Fluent API (`HasColumnName`/`HasMaxLength`) — não
  há convenção automática de snake_case. Faltou no plan.md/data-model.md; corrigido adicionando
  as 15 mesmas propriedades nesse arquivo também, senão o EF não encontraria as colunas.

**Checkpoint**: Entidade e banco prontos — US1, US2 e US3 podem prosseguir.

---

## Phase 3: User Story 1 - Registrar e consultar o endereço do Aluno (Priority: P1) 🎯 MVP

**Goal**: A professora consegue cadastrar/editar o endereço do próprio Aluno (CEP, Rua, Número
obrigatórios; Complemento, Bairro, Cidade, Estado opcionais) e vê-lo na tela de detalhe.

**Independent Test**: Cadastrar um Aluno preenchendo CEP, Rua e Número, salvar, e confirmar que
o endereço aparece corretamente no detalhe do Aluno (Cenário 1 do quickstart.md).

### Implementation for User Story 1

- [X] T003 [P] [US1] Em `src/SPI.Application/Alunos/Dtos/CadastrarAlunoRequest.cs`, adicionar os
  7 campos de endereço do Aluno: `Cep`, `Rua`, `Numero`, `Complemento`, `Bairro`, `Cidade`,
  `Estado` (todos `string?`).
- [X] T004 [P] [US1] Em `src/SPI.Application/Alunos/Dtos/AtualizarAlunoRequest.cs`, adicionar os
  mesmos 7 campos (mesmo padrão "campo não enviado = não altera" já usado pelos demais campos
  opcionais deste DTO).
- [X] T005 [P] [US1] Em `src/SPI.Application/Alunos/Dtos/AlunoResponse.cs`, adicionar os mesmos
  7 campos.
- [X] T006 [US1] Em
  `src/SPI.Application/Alunos/Validators/CadastrarAlunoRequestValidator.cs`, adicionar:
  `Cep`, `Rua`, `Numero` `NotEmpty` sempre (FR-002); `Cep` com `Matches(@"^\d{8}$")` — exatamente
  8 dígitos numéricos, sem hífen (data-model.md "Validação"). Depende de T003.
- [X] T007 [US1] Em
  `src/SPI.Application/Alunos/Validators/AtualizarAlunoRequestValidator.cs`, adicionar: bloco
  `When(x => x.Cep != null || x.Rua != null || x.Numero != null, ...)` exigindo os três juntos
  (`NotEmpty`) só quando pelo menos um vier preenchido — nunca gerar erro quando os três vierem
  vazios, para não bloquear a edição de Alunos antigos sem endereço (data-model.md "Validação",
  regra assimétrica). `Cep`, quando preenchido, com a mesma regra de 8 dígitos. Depende de T004.
- [X] T008 [US1] Em `src/SPI.Application/Alunos/Services/AlunoService.cs`: em `CadastrarAsync`,
  logo após obter o `id` do `CadastrarViaProcedureAsync` (sem alterar a assinatura da procedure —
  research.md Decisão 4), buscar o Aluno recém-criado, setar os 7 campos de endereço e chamar
  `SalvarAlteracoesAsync` antes do `Mapear`; em `AtualizarAsync`, adicionar as 7 atribuições com
  fallback `??` (`aluno.Cep = request.Cep ?? aluno.Cep`, etc.), mesmo padrão já usado por
  `TelefoneAluno`/`TelefoneResponsavel`; em `Mapear`, incluir os 7 campos no `AlunoResponse`.
  Depende de T005, T006, T007.
- [X] T009 [P] [US1] Em `frontend/lib/api/alunos.ts`, adicionar os mesmos 7 campos (camelCase)
  às interfaces `Aluno`, `CadastrarAlunoRequest` e `AtualizarAlunoRequest`, mantendo a convenção
  `/** Espelha ... */` já usada no arquivo.
- [X] T010 [US1] Em `frontend/components/alunos/AlunoForm.tsx`, adicionar uma nova
  `.form-section` "Endereço" com os 7 campos (CEP, Rua, Número, Complemento, Bairro, Cidade,
  Estado) num `.field-grid`, seguindo o mesmo padrão visual (`.fs-title`/`.fs-num`/`.field`) já
  usado nas seções existentes deste formulário (ex. seção "Responsável"). CEP, Rua e Número
  marcados com `<span className="req">*</span>` e `required`, os demais sem. Depende de T009.
- [X] T011 [US1] Em `frontend/app/(app)/alunos/[id]/page.tsx`, adicionar um novo `.mini-panel`
  "Endereço" exibindo os 7 campos do Aluno (formatação simples: `Rua, Número` na primeira linha,
  `Bairro — Cidade/Estado` na segunda, `CEP: ...` — omitir campos vazios sem quebrar o layout).
  Depende de T009.

**Checkpoint**: User Story 1 completa e testável de forma independente — endereço do Aluno
cadastrável, editável e visível no detalhe.

---

## Phase 4: User Story 2 - Registrar o endereço do responsável (Priority: P2)

**Goal**: Quando o Aluno é menor de idade, a professora vê e preenche uma seção de endereço do
responsável, com o checkbox "Mesmo endereço do aluno" (default marcado) controlando se os
campos são espelhados ou independentes.

**Independent Test**: Marcar um Aluno como menor de idade, desmarcar "Mesmo endereço do aluno",
preencher um endereço de responsável diferente, salvar, e confirmar que os dois endereços
aparecem corretos e independentes no detalhe (Cenários 2, 3, 4 e 5 do quickstart.md).

### Implementation for User Story 2

- [X] T012 [US2] Em `src/SPI.Application/Alunos/Dtos/CadastrarAlunoRequest.cs`, adicionar
  `ResponsavelMesmoEndereco:bool` (default `true`) e os 7 campos `Responsavel*` (`string?`).
  Depende de T003 (mesmo arquivo).
- [X] T013 [US2] Em `src/SPI.Application/Alunos/Dtos/AtualizarAlunoRequest.cs`, adicionar os
  mesmos 8 campos. Depende de T004 (mesmo arquivo).
- [X] T014 [US2] Em `src/SPI.Application/Alunos/Dtos/AlunoResponse.cs`, adicionar os mesmos 8
  campos. Depende de T005 (mesmo arquivo).
- [X] T015 [US2] Em
  `src/SPI.Application/Alunos/Validators/CadastrarAlunoRequestValidator.cs`, adicionar bloco
  `When(x => x.EhMenorDeIdade && !x.ResponsavelMesmoEndereco, ...)` exigindo
  `ResponsavelCep`/`ResponsavelRua`/`ResponsavelNumero` `NotEmpty`, com `ResponsavelCep` seguindo
  a mesma regra de 8 dígitos — mesmo padrão do bloco `When(x => x.EhMenorDeIdade, ...)` já
  existente para `TelefoneResponsavel`/`EmailResponsavel`. Depende de T006, T012 (mesmo arquivo).
- [X] T016 [US2] Em
  `src/SPI.Application/Alunos/Validators/AtualizarAlunoRequestValidator.cs`, adicionar a mesma
  regra assimétrica de T007 (só exige os 3 campos juntos se pelo menos um vier preenchido),
  condicionada a `x.EhMenorDeIdade && !x.ResponsavelMesmoEndereco`. Depende de T007, T013 (mesmo
  arquivo).
- [X] T017 [US2] Em `src/SPI.Application/Alunos/Services/AlunoService.cs`: em `CadastrarAsync` e
  `AtualizarAsync`, setar `ResponsavelMesmoEndereco` e os 7 campos `Responsavel*` (mesmo padrão
  de T008); em `Mapear`, implementar a regra de espelhamento (research.md Decisão 2,
  data-model.md "Regra de leitura"): quando `aluno.ResponsavelMesmoEndereco == true`, o
  `AlunoResponse` retorna `ResponsavelCep = aluno.Cep`, `ResponsavelRua = aluno.Rua`, etc.
  (espelho calculado na leitura, nunca lido das colunas `responsavel_*` do banco nesse estado);
  quando `false`, retorna os valores das colunas `responsavel_*` como estão. Depende de T008,
  T014, T015, T016 (mesmo arquivo que T008).
- [X] T018 [US2] Em `frontend/lib/api/alunos.ts`, adicionar `responsavelMesmoEndereco:boolean` e
  os 7 campos `responsavel*` às interfaces `Aluno`, `CadastrarAlunoRequest` e
  `AtualizarAlunoRequest`. Depende de T009 (mesmo arquivo).
- [X] T019 [US2] Em `frontend/components/alunos/AlunoForm.tsx`: (a) trocar a inicialização
  `useState(false)` de `ehMenorDeIdade` para inferir `true` quando `aluno?.telefoneResponsavel`,
  `aluno?.emailResponsavel` ou qualquer `aluno?.responsavel*` vier preenchido (research.md
  Decisão 1 — corrige a falha pré-existente de o checkbox não refletir o estado salvo ao editar);
  (b) adicionar, condicionada a `{ehMenorDeIdade && (...)}`, uma nova `.form-section` "Endereço
  do responsável" com o checkbox "Mesmo endereço do aluno" (estilo `.chk-pill`, igual ao já usado
  para `ehMenorDeIdade`), default marcado; (c) quando o checkbox estiver desmarcado, exibir os
  mesmos 7 campos de endereço (mesmo padrão de layout de T010), independentes dos campos do
  Aluno; quando marcado, ocultar os 7 campos (nenhuma entrada necessária); (d) ao marcar
  novamente o checkbox depois de tê-lo desmarcado e preenchido um endereço próprio do
  responsável, limpar/resetar o estado local dos 7 campos do responsável (eles serão
  sobrescritos pelo espelhamento do endereço do Aluno de qualquer forma — evita manter no estado
  do formulário um endereço "fantasma" que não será usado nem exibido). Depende de T010, T018
  (mesmo arquivo que T010).
- [X] T020 [US2] Em `frontend/app/(app)/alunos/[id]/page.tsx`, adicionar (condicionado à mesma
  inferência de "é menor de idade" de T019) um `.mini-panel` "Endereço do responsável" com os 7
  campos `responsavel*` retornados pela API (já espelhados pelo backend quando aplicável — não
  precisa de lógica de espelho no frontend). Depende de T011, T018 (mesmo arquivo que T011).

**Checkpoint**: User Story 2 completa e testável de forma independente — endereço do
responsável funcional (espelhado ou independente), visível corretamente ao reabrir a edição.

---

## Phase 5: User Story 3 - Preencher endereço automaticamente a partir do CEP (Priority: P3)

**Goal**: Ao digitar um CEP de 8 dígitos (Aluno ou responsável), Rua/Bairro/Cidade/Estado são
preenchidos automaticamente via ViaCEP, sem bloquear o cadastro se a busca falhar.

**Independent Test**: Digitar um CEP válido existente e confirmar preenchimento automático;
digitar um CEP inexistente (ou desconectar a internet) e confirmar que o cadastro continua
editável e pode ser concluído manualmente (Cenários 6 e 7 do quickstart.md).

### Implementation for User Story 3

- [X] T021 [P] [US3] Criar `frontend/lib/viacep.ts`: função
  `buscarEnderecoPorCep(cep: string): Promise<{ logradouro: string; bairro: string; localidade:
  string; uf: string } | null>` — chama `fetch(\`https://viacep.com.br/ws/${cep}/json/\`)`
  diretamente (sem passar pelo backend — research.md Decisão 5); retorna `null` (nunca lança
  exceção para quem chama) em qualquer falha: erro de rede, resposta não-ok, ou corpo com `{erro:
  true}` (CEP inexistente) — contrato exato em contracts/aluno-endereco.md.
- [X] T022 [US3] Em `frontend/components/alunos/AlunoForm.tsx`, no campo CEP do endereço do
  Aluno: ao atingir 8 dígitos digitados, chamar `buscarEnderecoPorCep`; em caso de sucesso,
  preencher Rua/Bairro/Cidade/Estado (mapeando `logradouro→Rua`, `bairro→Bairro`,
  `localidade→Cidade`, `uf→Estado`) sem sobrescrever se a usuária já os editou manualmente depois
  do preenchimento automático anterior; em caso de falha (retorno `null`), não fazer nada — sem
  toast de erro, sem bloquear o formulário. Não disparar busca antes de completar os 8 dígitos.
  Depende de T021, T019 (mesmo arquivo que T019).
- [X] T023 [US3] Em `frontend/components/alunos/AlunoForm.tsx`, aplicar a mesma lógica de T022
  ao campo CEP da seção "Endereço do responsável" (quando visível e com "mesmo endereço"
  desmarcado), de forma independente do CEP do Aluno. Depende de T022 (mesmo arquivo).

**Checkpoint**: As três user stories funcionam em conjunto — endereço do Aluno e do responsável
com autopreenchimento por CEP, sem bloquear o cadastro em caso de falha da busca.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verificação final cruzando as três user stories, conforme
[quickstart.md](./quickstart.md).

- [X] T024 [P] Rodar `dotnet build` no backend (`src/`) — sem erros.
- [X] T025 [P] Rodar `npx tsc --noEmit` e o lint do frontend (`frontend/`) — sem erros.
- [ ] T026 Executar a validação completa do [quickstart.md](./quickstart.md) (Cenários 1 a 8):
  endereço do Aluno obrigatório/opcional corretamente, endereço do responsável espelhado e
  independente, seção de responsável aparecendo/sumindo corretamente (inclusive ao reabrir
  edição), autopreenchimento por CEP funcionando e falhando graciosamente, e Alunos antigos sem
  endereço continuando editáveis (FR-001 a FR-013, SC-001 a SC-005).
- [X] T027 Confirmar, por `git diff --stat`, que somente os arquivos previstos no plan.md
  (Project Structure) foram alterados — `Aluno.cs`, 3 DTOs, 2 validators, `AlunoService.cs`,
  `database/13_...sql`, `lib/api/alunos.ts`, `lib/viacep.ts` (novo), `AlunoForm.tsx`,
  `alunos/[id]/page.tsx` — nenhum outro arquivo/entidade/endpoint tocado.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: N/A.
- **Foundational (Phase 2)**: Sem dependências externas — pode começar imediatamente. BLOQUEIA
  todas as tasks de US1/US2/US3.
- **User Story 1 (Phase 3)**: Depende da Fase 2 completa.
- **User Story 2 (Phase 4)**: Depende da Fase 2 completa e de US1 (T003-T011) — toca os mesmos
  arquivos (DTOs, validators, `AlunoService.cs`, `AlunoForm.tsx`, `alunos/[id]/page.tsx`) nas
  mesmas regiões de código, então é sequencial após US1, não paralelizável com ela.
- **User Story 3 (Phase 5)**: Depende de US1 e US2 completas (edita as mesmas seções de
  `AlunoForm.tsx` criadas por T010/T019).
- **Polish (Phase 6)**: Depende das Fases 3, 4 e 5 completas.

### Dentro de cada User Story

- US1: T003/T004/T005 (DTOs, arquivos diferentes) são paralelizáveis entre si; T006 depende de
  T003, T007 depende de T004; T008 depende de T005-T007 (mesmo arquivo, sequencial); T009 é
  paralelizável com o backend (arquivo diferente); T010 depende de T009; T011 depende de T009
  (paralelizável com T010, arquivos diferentes).
- US2: espelha a mesma ordem de US1, mas cada task depende da equivalente de US1 por tocar o
  mesmo arquivo (T012→T003, T013→T004, T014→T005, T015→T006+T012, T016→T007+T013, T017→T008+
  T014-T016, T018→T009, T019→T010+T018, T020→T011+T018).
- US3: T021 é independente (arquivo novo); T022 depende de T021 e T019; T023 depende de T022
  (mesmo arquivo, mesmo bloco de trabalho).

### Parallel Opportunities

- T003, T004, T005 (Foundational→US1, arquivos DTO diferentes) podem rodar em paralelo.
- T009 (frontend types) pode rodar em paralelo com o backend de US1 (T006-T008).
- T021 (novo helper `viacep.ts`) pode ser escrito a qualquer momento após a Fase 2, em paralelo
  com US1/US2, já que não depende de nenhum dos dois — só sua integração em `AlunoForm.tsx`
  (T022/T023) depende de US1/US2 estarem prontas.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Fase 2: Foundational (T001-T002) → banco e entidade prontos
2. Completar Fase 3: User Story 1 (T003-T011) → endereço do Aluno funcional →
   **entregável isoladamente como MVP**
3. Parar e validar Cenários 1 e 8 do quickstart.md

### Incremental Delivery

1. Foundational (T001-T002) → base pronta
2. User Story 1 (T003-T011) → endereço do Aluno → MVP entregável
3. User Story 2 (T012-T020) → endereço do responsável → entregável sobre o MVP
4. User Story 3 (T021-T023) → autopreenchimento por CEP → entregável sobre as duas anteriores
5. Polish (T024-T027) → verificação final cruzada

## Notes

- Tests: nenhuma automatizada nesta feature — validação inteiramente manual via quickstart.md,
  mesma limitação pré-existente do projeto já registrada em specs anteriores.
- Nenhuma task desta lista deve adicionar `NOT NULL` às colunas de endereço no banco, nem exigir
  CEP/Rua/Número em `AtualizarAlunoRequestValidator` quando os três vierem vazios — isso
  quebraria a edição de Alunos cadastrados antes desta feature (ver research.md Decisão 3,
  Cenário 8 do quickstart.md).
- Nenhuma task deve adicionar uma coluna `eh_menor_de_idade` ao banco — o estado é inferido no
  frontend a partir de dados já persistidos (research.md Decisão 1).
- Nenhuma task deve fazer a busca de CEP passar pelo backend do SPI — é uma chamada direta do
  frontend ao ViaCEP (research.md Decisão 5).
