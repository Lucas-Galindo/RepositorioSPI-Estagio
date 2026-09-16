# Tasks: Anexo de Arquivo em Contas a Pagar e a Receber

**Input**: Design documents from `/specs/023-anexo-arquivo-financeiro/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/anexo-comprovante.md](./contracts/anexo-comprovante.md), [quickstart.md](./quickstart.md)

**Tests**: Solicitados para a lógica de backend com maior risco de regressão silenciosa: o `AnexoValidator` compartilhado (rejeição por formato real do conteúdo, não extensão; rejeição por tamanho) — mesma decisão já tomada em specs/022 de testar a lógica de negócio de maior risco, não a UI. O projeto não tem biblioteca de mock instalada (`tests/SPI.Application.Tests` só usa `xunit` + `FluentValidation`) — os testes do validador não precisam de nenhum fake de repositório (o `AnexoValidator` não depende de repositório, só recebe bytes). Frontend continua sem framework de teste automatizado — validação manual via [quickstart.md](./quickstart.md).

**Organization**: Tasks agrupadas por user story. US1 (P1, MVP) e US2 (P1) têm a mesma prioridade máxima; US2 depende de US1 (a tabela só ganha o que baixar depois que algo pode ser anexado). US3 (P2) é puramente frontend (confirmação antes de substituir) e reaproveita o mesmo endpoint de upload de US1 — nenhuma mudança de backend adicional.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 (anexar), US2 (visualizar/baixar), US3 (substituir com confirmação)
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Projeto web existente (frontend Next.js + backend ASP.NET Core + MySQL). Esta feature toca:
- Banco de dados: `database/12_anexo_comprovante_financeiro.sql` (novo)
- Backend: `src/SPI.Domain/Entities/{ContaPagar,Pagamento}.cs`, `src/SPI.Infrastructure/Persistence/Configurations/{ContaPagarConfiguration,PagamentoConfiguration}.cs`, `src/SPI.Application/Anexos/` (novo), `src/SPI.Application/{ContasPagar,Pagamentos}/{Dtos,Services}/`, `src/SPI.Api/Controllers/{ContasPagarController,PagamentosController}.cs`, `src/SPI.Api/Program.cs` (registro de DI)
- Frontend: `frontend/lib/api/{client,contasPagar,pagamentos}.ts`, `frontend/components/financeiro/AnexoComprovante.tsx` (novo), `frontend/app/(app)/financeiro/{contas-a-pagar,contas-a-receber}/{[id]/page.tsx,novo/page.tsx}`
- Testes: `tests/SPI.Application.Tests/Anexos/` (novo diretório)

---

## Phase 1: Setup

Não aplicável — projeto já inicializado, nenhuma dependência nova (upload usa `IFormFile`, nativo do ASP.NET Core; ver [plan.md](./plan.md) Technical Context). Prosseguir direto para a Fase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Criar o schema de banco, as propriedades de entidade, o DTO de metadados e o validador de arquivo compartilhado que todas as três user stories (anexar, visualizar/baixar, substituir) vão consumir. Sem isso, nenhuma delas tem onde persistir ou o que validar.

**⚠️ CRITICAL**: Esta fase bloqueia toda a Fase 3 (US1). US2 e US3 dependem, por sua vez, de US1 estar completa (ver Dependencies).

- [ ] T001 [P] Criar `database/12_anexo_comprovante_financeiro.sql`, seguindo a convenção dos scripts `01` a `11` já existentes (cabeçalho de comentário, `USE spi_db;`, pré-requisito "executar 01 a 11 antes deste bloco"): `ALTER TABLE conta_pagar ADD COLUMN arquivo_conteudo MEDIUMBLOB NULL, ADD COLUMN arquivo_nome_original VARCHAR(255) NULL, ADD COLUMN arquivo_tipo_mime VARCHAR(100) NULL, ADD COLUMN arquivo_tamanho_bytes INT UNSIGNED NULL, ADD COLUMN arquivo_data_upload DATETIME NULL;` e o mesmo bloco `ALTER TABLE pagamento ADD COLUMN ...` idêntico (ver [data-model.md](./data-model.md) tabela de colunas) — cada `ADD COLUMN` com `COMMENT` explicando o propósito, mesmo padrão de `10_financeiro_contas.sql`.
- [ ] T002 [P] Em `src/SPI.Domain/Entities/ContaPagar.cs`, adicionar as 5 propriedades novas: `public byte[]? ArquivoConteudo { get; set; }`, `public string? ArquivoNomeOriginal { get; set; }`, `public string? ArquivoTipoMime { get; set; }`, `public int? ArquivoTamanhoBytes { get; set; }`, `public DateTime? ArquivoDataUpload { get; set; }`.
- [ ] T003 [P] Em `src/SPI.Domain/Entities/Pagamento.cs`, adicionar as mesmas 5 propriedades novas de T002.
- [ ] T004 [P] Em `src/SPI.Infrastructure/Persistence/Configurations/ContaPagarConfiguration.cs`, adicionar `HasColumnName` para as 5 colunas novas (`arquivo_conteudo`, `arquivo_nome_original`, `arquivo_tipo_mime`, `arquivo_tamanho_bytes`, `arquivo_data_upload`), mesmo padrão das demais propriedades já mapeadas no arquivo.
- [ ] T005 [P] Em `src/SPI.Infrastructure/Persistence/Configurations/PagamentoConfiguration.cs`, adicionar `HasColumnName` para as mesmas 5 colunas de T004.
- [ ] T006 [P] Criar `src/SPI.Application/Anexos/Dtos/AnexoResponse.cs` (namespace `SPI.Application.Anexos.Dtos`): DTO compartilhado com `NomeOriginal` (`string`), `TipoMime` (`string`), `TamanhoBytes` (`int`), `DataUpload` (`DateTime`) — nunca o conteúdo binário (ver [contracts/anexo-comprovante.md](./contracts/anexo-comprovante.md)).
- [ ] T007 Criar `src/SPI.Application/Anexos/Services/IAnexoValidator.cs` e `AnexoValidator.cs`: método `List<string> Validar(IFormFile arquivo)` devolvendo lista de mensagens de erro (vazia = válido). Regras: (a) tamanho do arquivo MUST ser ≤ 10.485.760 bytes (10MB, FR-003), com mensagem clara se exceder; (b) conteúdo real do arquivo MUST corresponder a JPG, PNG ou PDF por assinatura binária — `FF D8 FF` (JPEG), `89 50 4E 47 0D 0A 1A 0A` (PNG) ou `25 50 44 46 2D` / `%PDF-` (PDF) nos primeiros bytes do stream — nunca a extensão do nome nem o `Content-Type` declarado pelo navegador (FR-002, Edge Case de arquivo renomeado), com mensagem clara se não corresponder a nenhum. Um arquivo vazio (0 bytes) ou com menos bytes do que o necessário para comparar qualquer assinatura MUST ser tratado como formato inválido (mensagem clara de rejeição), nunca lançar exceção — spec.md Edge Case "arquivo vazio (0 bytes) ou corrompido". Sem biblioteca externa de detecção de tipo (ver [research.md](./research.md) #3). Chamado pelos controllers antes de invocar o service, no mesmo padrão dos `IValidator<TRequest>` do FluentValidation já injetados em `ContasPagarController`/`PagamentosController` — não dentro dos services (Princípio II: um único ponto de validação, reaproveitado pelos dois controllers).
- [ ] T008 Em `src/SPI.Api/Program.cs`, registrar `IAnexoValidator`/`AnexoValidator` no container de injeção de dependência (mesmo padrão de registro dos demais serviços/validators já existentes no arquivo).
- [ ] T009 [P] Em `frontend/lib/api/client.ts`, adicionar dois helpers novos: `apiPostFile<TResponse>(path: string, arquivo: File, accessToken: string): Promise<TResponse>` (envia `FormData` com o campo `arquivo`, sem definir `Content-Type` manualmente — o navegador define o boundary automaticamente — reaproveita `extrairErro` no caminho de erro, mesmo padrão de `apiPost`) e `apiGetBlob(path: string, accessToken: string): Promise<{ blob: Blob; nomeArquivo: string }>` (faz `fetch` com `Authorization`, lê a resposta como `Blob`, extrai o nome do arquivo do cabeçalho `Content-Disposition`, e reaproveita `extrairErro` se `!response.ok`).

**Checkpoint**: Schema, entidades, DTO de metadados e validador compartilhado prontos — a Fase 3 (US1) já pode implementar contra eles.

---

## Phase 3: User Story 1 - Anexar o comprovante a um registro financeiro (Priority: P1) 🎯 MVP

**Goal**: Um registro de Contas a Pagar ou de Contas a Receber já salvo ganha um botão "Anexar arquivo"; escolher um JPG/PNG/PDF válido de até 10MB salva o arquivo vinculado ao registro (persistido nas colunas de T001-T005); formatos/tamanhos inválidos são rejeitados com mensagem clara, sem alterar nada. Logo após salvar um NOVO registro, a tela oferece a opção de anexar em seguida, sem navegação adicional (FR-013 a FR-015); ignorar essa oferta não tem nenhum efeito sobre o registro.

**Independent Test**: Abrir um registro existente de Contas a Pagar (ou de Contas a Receber) sem anexo, usar "Anexar arquivo" com um PDF válido e confirmar que o registro passa a mostrar o arquivo como anexado; tentar um `.txt` e um arquivo > 10MB e confirmar rejeição com mensagem clara; criar um novo registro e confirmar que a oferta de anexar aparece logo após salvar.

### Implementation for User Story 1

- [ ] T010 [US1] Em `src/SPI.Application/ContasPagar/Services/IContaPagarService.cs` e `ContaPagarService.cs`, adicionar `Task<AnexoResponse> AnexarArquivoAsync(int id, IFormFile arquivo, CancellationToken cancellationToken = default)`: busca o registro (`NaoEncontradoException` se não existir, mesmo padrão de `AtualizarAsync`), lê o `IFormFile` em `byte[]`, grava as 5 propriedades de T002 (`ArquivoDataUpload = DateTime.UtcNow`), chama `SalvarAlteracoesAsync`, retorna `AnexoResponse` mapeado do resultado. **Não** revalida formato/tamanho aqui — a validação já ocorreu no controller via `IAnexoValidator` (T007).
- [ ] T011 [US1] Em `src/SPI.Application/Pagamentos/Services/IPagamentoService.cs` e `PagamentoService.cs`, adicionar o mesmo método `AnexarArquivoAsync` de T010, adaptado a `Pagamento`.
- [ ] T012 [US1] Em `src/SPI.Application/ContasPagar/Dtos/ContaPagarResponse.cs`, adicionar `public AnexoResponse? Anexo { get; set; }`; em `ContaPagarService.Mapear`, preencher `Anexo` com um `AnexoResponse` (a partir de `ArquivoNomeOriginal`/`ArquivoTipoMime`/`ArquivoTamanhoBytes`/`ArquivoDataUpload`) quando `ArquivoConteudo` não for `null`, ou `null` caso contrário — nunca incluir o conteúdo binário neste DTO (ver [contracts/anexo-comprovante.md](./contracts/anexo-comprovante.md)).
- [ ] T013 [US1] Mesma alteração de T012 em `src/SPI.Application/Pagamentos/Dtos/PagamentoResponse.cs` e `PagamentoService.Mapear`.
- [ ] T014 [US1] Em `src/SPI.Api/Controllers/ContasPagarController.cs`, adicionar `[HttpPost("{id}/anexo")]` (`[Consumes("multipart/form-data")]`, `200 OK`/`400 BadRequest`/`404 NotFound`/`500`): injeta `IAnexoValidator` no construtor, chama `Validar(arquivo)` — se a lista não estiver vazia, `return BadRequest(erros)` sem chamar o service (FR-004, FR-007); senão chama `_contaPagarService.AnexarArquivoAsync(id, arquivo)` e retorna `Ok(response)`; captura `NaoEncontradoException` como `404`, mesmo padrão dos demais actions do arquivo.
- [ ] T015 [US1] Mesma alteração de T014 em `src/SPI.Api/Controllers/PagamentosController.cs`, chamando `_pagamentoService.AnexarArquivoAsync`.
- [ ] T016 [P] [US1] Em `frontend/lib/api/contasPagar.ts`, adicionar `export interface Anexo { nomeOriginal: string; tipoMime: string; tamanhoBytes: number; dataUpload: string; }` e o campo `anexo?: Anexo | null;` (opcional, não `anexo: Anexo | null`) na interface `ContaPagar` — essa mesma interface é usada tanto por `listarContasPagar` (que nunca traz `anexo`, ficando `undefined` em runtime) quanto por `obterContaPagar` (que sempre traz `anexo`, `null` ou preenchido); o `?` evita que o tipo afirme uma garantia que a listagem não cumpre (ver [contracts/anexo-comprovante.md](./contracts/anexo-comprovante.md) "Compatibilidade" — listagem não ganha o campo). Adicionar também `export function anexarArquivoContaPagar(id: number, arquivo: File, accessToken: string): Promise<Anexo>` usando `apiPostFile` de T009 no caminho `/api/contas-pagar/${id}/anexo`.
- [ ] T017 [P] [US1] Mesma alteração de T016 em `frontend/lib/api/pagamentos.ts` (campo `anexo?: Anexo | null;` na interface `Pagamento`, mesmo motivo), caminho `/api/pagamentos/${id}/anexo`, função `anexarArquivoPagamento`.
- [ ] T018 [US1] Criar `frontend/components/financeiro/AnexoComprovante.tsx`: componente reaproveitado nas duas telas de detalhe, recebendo por props o `anexo: Anexo | null` atual e uma função `onAnexar(arquivo: File): Promise<void>` (chamada por quem o usa com `anexarArquivoContaPagar`/`anexarArquivoPagamento` já parcialmente aplicada ao `id`). Renderiza: se `anexo` for `null`, um botão "Anexar arquivo" (`<input type="file" accept=".jpg,.jpeg,.png,.pdf">` disparado por um botão estilizado, mesmo padrão `btn btn-sm` já usado no projeto) que chama `onAnexar` ao selecionar um arquivo e exibe a mensagem de erro (via `ApiError.message`, mesmo padrão de tratamento de erro já usado nas telas de formulário) se a chamada falhar (400); se `anexo` não for `null`, mostra um ícone, `anexo.nomeOriginal` e o tamanho formatado — as ações de visualizar/baixar (US2) e substituir (US3) são adicionadas em tasks posteriores neste mesmo arquivo.
- [ ] T019 [US1] Em `frontend/app/(app)/financeiro/contas-a-pagar/[id]/page.tsx`, renderizar `<AnexoComprovante anexo={contaPagar.anexo ?? null} onAnexar={(arquivo) => anexarArquivoContaPagar(id, arquivo, sessao.accessToken).then(setAnexoAtualizado)} />` (o `?? null` normaliza o campo opcional de T016 para o prop `anexo: Anexo | null` do componente — nesta tela de detalhe o campo sempre vem preenchido pelo backend, nunca `undefined` de fato, mas o tipo TS agora permite os dois) (ajustando ao estado local já usado na tela para refletir o novo `anexo` sem recarregar a página inteira).
- [ ] T020 [US1] Mesma alteração de T019 em `frontend/app/(app)/financeiro/contas-a-receber/[id]/page.tsx` (incluindo o `?? null`), usando `anexarArquivoPagamento`.
- [ ] T021 [US1] Em `frontend/app/(app)/financeiro/contas-a-pagar/[id]/page.tsx`, ler o parâmetro de busca `anexar` da URL (`useSearchParams`); quando `anexar === "1"`, exibir um banner dispensável acima/dentro de `<AnexoComprovante>` (ex.: "Registro salvo! Deseja anexar o comprovante agora?" com um botão que aciona o mesmo fluxo de anexar de T019, e uma opção de dispensar que apenas remove o banner, sem nenhum outro efeito — FR-014, FR-015, spec.md US1 Acceptance Scenarios 5 e 6).
- [ ] T022 [US1] Mesma alteração de T021 em `frontend/app/(app)/financeiro/contas-a-receber/[id]/page.tsx`.
- [ ] T023 [US1] Em `frontend/app/(app)/financeiro/contas-a-pagar/novo/page.tsx`, trocar `router.push(\`/financeiro/contas-a-pagar/${criada.id}\`)` (linha ~64) por `router.push(\`/financeiro/contas-a-pagar/${criada.id}?anexar=1\`)`, para acionar o banner de T021.
- [ ] T024 [US1] Mesma alteração de T023 em `frontend/app/(app)/financeiro/contas-a-receber/novo/page.tsx`, acionando o banner de T022.
- [ ] T025 [P] [US1] Criar `tests/SPI.Application.Tests/Anexos/AnexoValidatorTests.cs`: casos aceitando um JPG válido (bytes `FF D8 FF` + conteúdo qualquer, ≤ 10MB), um PNG válido, um PDF válido (`%PDF-` + conteúdo), rejeitando um arquivo com conteúdo de texto simples renomeado para `.pdf` (Edge Case de conteúdo real vs. extensão, FR-002), rejeitando um arquivo válido em formato mas maior que 10.485.760 bytes (FR-003), e rejeitando um arquivo de 0 bytes (`IFormFile` com `Length == 0`) sem lançar exceção (Edge Case de arquivo vazio/corrompido, ver nota em T007) — cada rejeição deve produzir ao menos uma mensagem de erro não vazia (FR-004).

**Checkpoint**: User Story 1 completa e testável de forma independente — anexar funciona, com validação e a oferta pós-salvamento.

---

## Phase 4: User Story 2 - Visualizar e baixar o comprovante anexado (Priority: P1)

**Goal**: Um registro com anexo mostra uma opção de visualizar/baixar que entrega exatamente o mesmo conteúdo enviado, sem nenhuma permissão adicional além do acesso já existente ao registro.

**Independent Test**: Num registro com anexo (resultado de US1), clicar em visualizar/baixar e confirmar que o arquivo entregue é idêntico ao originalmente enviado; confirmar que a listagem de registros não carrega o conteúdo binário.

### Implementation for User Story 2

- [ ] T026 [US2] Em `IContaPagarService`/`ContaPagarService`, adicionar `Task<(byte[] Conteudo, string TipoMime, string NomeOriginal)?> ObterArquivoAsync(int id, CancellationToken cancellationToken = default)`: busca o registro, devolve `null` se `ArquivoConteudo` for `null` (sem anexo) ou se o registro não existir, senão devolve a tupla com os 3 campos.
- [ ] T027 [US2] Mesma alteração de T026 em `IPagamentoService`/`PagamentoService`.
- [ ] T028 [US2] Em `ContasPagarController.cs`, adicionar `[HttpGet("{id}/anexo")]` (`200 OK`/`404 NotFound`/`500`): chama `ObterArquivoAsync`; se `null`, `return NotFound()`; senão `return File(conteudo, tipoMime, nomeOriginal)` com `Content-Disposition: inline` (ver [contracts/anexo-comprovante.md](./contracts/anexo-comprovante.md) — `File(bytes, contentType, fileDownloadName)` do ASP.NET Core já gera esse cabeçalho).
- [ ] T029 [US2] Mesma alteração de T028 em `PagamentosController.cs`.
- [ ] T030 [P] [US2] Em `frontend/lib/api/contasPagar.ts`, adicionar `export function obterAnexoContaPagar(id: number, accessToken: string): Promise<{ blob: Blob; nomeArquivo: string }>` usando `apiGetBlob` de T009 no caminho `/api/contas-pagar/${id}/anexo`.
- [ ] T031 [P] [US2] Mesma alteração de T030 em `frontend/lib/api/pagamentos.ts`, função `obterAnexoPagamento`.
- [ ] T032 [US2] Em `frontend/components/financeiro/AnexoComprovante.tsx` (T018), adicionar a ação "Visualizar/baixar" (visível quando `anexo` não é `null`): chama a função de obtenção passada por prop (`onVisualizar: () => Promise<{ blob: Blob; nomeArquivo: string }>`), cria uma URL de objeto (`URL.createObjectURL`) a partir do `Blob` e abre numa nova aba (`window.open`) — cobre visualizar (imagens/PDF renderizam inline no navegador) e baixar (controles nativos do navegador) com uma única ação, conforme [contracts/anexo-comprovante.md](./contracts/anexo-comprovante.md).
- [ ] T033 [US2] Passar `onVisualizar={() => obterAnexoContaPagar(id, sessao.accessToken)}` ao `<AnexoComprovante>` em `contas-a-pagar/[id]/page.tsx` (T019) e o equivalente `onVisualizar={() => obterAnexoPagamento(id, sessao.accessToken)}` em `contas-a-receber/[id]/page.tsx` (T020).

**Checkpoint**: User Stories 1 e 2 funcionam juntas — anexar e depois visualizar/baixar o mesmo conteúdo, sem regressão na listagem.

---

## Phase 5: User Story 3 - Substituir o comprovante anexado (Priority: P2)

**Goal**: A opção de substituir um anexo existente exige confirmação explícita (aviso de que o anexo atual será perdido) antes de qualquer novo arquivo ser solicitado/enviado; cancelar não altera nada; confirmar reaproveita o mesmo endpoint de upload de US1 (que já sobrescreve).

**Independent Test**: Num registro com anexo, clicar em "Substituir", confirmar que o aviso aparece antes de qualquer seleção de arquivo, cancelar e confirmar que nada muda, repetir confirmando e enviando um novo arquivo válido, e confirmar que o anexo anterior deixa de estar acessível.

### Implementation for User Story 3

- [ ] T034 [US3] Em `frontend/components/financeiro/AnexoComprovante.tsx` (T018/T032), adicionar o botão "Substituir" (visível quando `anexo` não é `null`, ao lado de "Visualizar/baixar") que, ao ser clicado, abre um diálogo de confirmação (mesmo componente de confirmação já usado em outras telas do projeto, se existir — senão um `<dialog>`/modal simples) com o texto "Isso vai substituir o anexo atual, que não poderá ser recuperado depois." e duas ações: "Cancelar" (fecha o diálogo, nenhuma mudança) e "Substituir" (fecha o diálogo e então abre o seletor de arquivo, reaproveitando a mesma função `onAnexar` de T018 — o mesmo endpoint de upload de US1 já sobrescreve o anexo existente, ver [contracts/anexo-comprovante.md](./contracts/anexo-comprovante.md) "mesmo endpoint sempre sobrescreve") — spec.md US3 Acceptance Scenarios 1-4, FR-016.

**Checkpoint**: As três user stories funcionam em conjunto — anexar, visualizar/baixar e substituir com confirmação, sem regressão nas telas de Contas a Pagar/Receber já existentes.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verificação final cruzando as três user stories, conforme [quickstart.md](./quickstart.md).

- [ ] T035 [P] Rodar `dotnet test tests/SPI.Application.Tests --filter "FullyQualifiedName~Anexos"` e confirmar que T025 passa.
- [ ] T036 [P] Rodar `npx tsc --noEmit` e o lint do frontend nos arquivos alterados (`frontend/lib/api/client.ts`, `frontend/lib/api/contasPagar.ts`, `frontend/lib/api/pagamentos.ts`, `frontend/components/financeiro/AnexoComprovante.tsx`, as 4 páginas de Contas a Pagar/Receber tocadas) — sem erros.
- [ ] T037 [P] Rodar `dotnet build` na solução (ou ao menos `src/SPI.Domain`, `src/SPI.Application`, `src/SPI.Infrastructure`, `src/SPI.Api`) para confirmar que as entidades/configurações/DTOs/endpoints novos compilam sem erro.
- [ ] T038 Executar a validação completa do [quickstart.md](./quickstart.md) (Cenários 1 a 4) nas telas de Contas a Pagar e Contas a Receber, incluindo a oferta pós-salvamento, a rejeição de formato/tamanho, o conteúdo idêntico no download, e a confirmação antes de substituir. Incluir explicitamente: criar e salvar um novo registro de Contas a Pagar e um de Contas a Receber **sem** anexar nenhum arquivo (ignorando a oferta pós-salvamento) e confirmar que ambos são salvos e permanecem acessíveis normalmente, sem nenhuma mudança no fluxo de cadastro já existente (FR-011, SC-005) — checagem de não-regressão explícita, já que nenhuma task desta feature altera `RegistrarAsync`/`AtualizarAsync` ou os formulários de criação.
- [ ] T039 Confirmar, por `git diff --stat`, que nenhum arquivo fora do escopo listado em "Path Conventions" foi alterado, e que a listagem de Contas a Pagar/Receber (`GET /api/contas-pagar`, `GET /api/pagamentos`) continua sem o campo `anexo` na resposta (só o detalhe individual ganha esse campo, conforme [contracts/anexo-comprovante.md](./contracts/anexo-comprovante.md) "Compatibilidade") — sem regressão de performance na listagem.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: N/A.
- **Foundational (Phase 2)**: Sem dependências — pode começar imediatamente. Bloqueia toda a Phase 3.
- **User Story 1 (Phase 3)**: Depende de Phase 2 completa (T001-T009).
- **User Story 2 (Phase 4)**: Depende de Phase 3 completa (`AnexoComprovante.tsx` e as telas de detalhe já precisam existir para receber a ação de visualizar/baixar).
- **User Story 3 (Phase 5)**: Depende de Phase 3 completa (reaproveita `onAnexar` de T018) e de Phase 4 estar concluída na prática (mesmo componente, para evitar conflito de edição simultânea no mesmo arquivo).
- **Polish (Phase 6)**: Depende de Phases 3, 4 e 5 completas.

### Dentro de cada User Story

- T010 e T011 (US1) são paralelizáveis entre si (services diferentes). T012 depende de T002 e T006 (Foundational) — o campo `Anexo` em `Mapear` é lido diretamente das propriedades da entidade, não do retorno de `AnexarArquivoAsync`, então T012 não depende de T010 em termos de código, só faz mais sentido implementá-lo logo depois por tocar o mesmo service. T013 espelha T012 para Pagamento (depende de T003 e T006). T014 depende de T007 (Foundational) e T010. T015 depende de T007 e T011. T016/T017 são paralelizáveis com o backend inteiro (arquivos de frontend, dependem só de T009 Foundational). T018 depende de T016/T017 (usa os tipos `Anexo`). T019/T020 dependem de T018 e T014/T015 (endpoint precisa existir para a chamada funcionar em teste manual, embora o componente compile sem o backend). T021/T022 dependem de T019/T020. T023/T024 dependem de T021/T022 (o parâmetro `anexar=1` só faz sentido depois que a tela de detalhe sabe lê-lo). T025 é paralelizável com todo o resto de US1 (só depende de T007 Foundational).
- T026/T027 (US2) são paralelizáveis entre si, dependem de Phase 2. T028/T029 dependem de T026/T027. T030/T031 são paralelizáveis entre si, dependem de T009 (Foundational). T032 depende de T018 (mesmo arquivo) e T030/T031. T033 depende de T032 e T019/T020.
- T034 (US3) depende de T018/T032 (mesmo arquivo, ações anteriores já devem existir).

### Parallel Opportunities

- T001-T007, T009 (Foundational) podem rodar em paralelo entre si (arquivos diferentes); T008 depende de T007.
- T010 e T011 (US1, backend) podem rodar em paralelo.
- T016 e T017 (US1, frontend clients) podem rodar em paralelo entre si e com o backend inteiro de US1.
- T025 (teste) pode rodar em paralelo com qualquer outra task de US1 depois de T007.
- T026/T027 e T030/T031 (US2) podem rodar em paralelo entre si.
- T035, T036 e T037 (Polish) podem rodar em paralelo.

---

## Parallel Example: Foundational

```bash
# T001-T007 e T009 em paralelo (arquivos completamente diferentes):
Task: "T001 Criar database/12_anexo_comprovante_financeiro.sql"
Task: "T002 Adicionar propriedades de anexo em src/SPI.Domain/Entities/ContaPagar.cs"
Task: "T003 Adicionar propriedades de anexo em src/SPI.Domain/Entities/Pagamento.cs"
Task: "T006 Criar src/SPI.Application/Anexos/Dtos/AnexoResponse.cs"
Task: "T007 Criar IAnexoValidator/AnexoValidator em src/SPI.Application/Anexos/Services/"
Task: "T009 Adicionar apiPostFile/apiGetBlob em frontend/lib/api/client.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 2: Foundational (T001-T009)
2. Completar Phase 3: User Story 1 (T010-T025) → anexar funciona, com validação e oferta pós-salvamento → **entregável isoladamente como MVP** (ainda sem visualizar/baixar pela UI, mas o arquivo já está persistido corretamente)
3. Parar e validar Cenários 1 e 3 do quickstart.md

### Incremental Delivery

1. Foundational (T001-T009) → schema e validador prontos
2. User Story 1 (T010-T025) → anexar + oferta pós-salvamento → MVP entregável
3. User Story 2 (T026-T033) → visualizar/baixar → entregável isoladamente por cima do MVP
4. User Story 3 (T034) → substituir com confirmação
5. Polish (T035-T039) → verificação final cruzada

## Notes

- Tests: apenas `AnexoValidator` (T025) é automatizado, pela mesma razão registrada em specs/022 — é a lógica de negócio de maior risco de regressão silenciosa; o restante (endpoints, UI) segue sem testes automatizados, mesma limitação pré-existente do projeto.
- Nenhuma task desta lista deve alterar `GET /api/contas-pagar` / `GET /api/pagamentos` (listagens) para incluir o campo `anexo` — só os endpoints de detalhe individual (`GET .../{id}`) ganham esse campo (T012/T013), conforme [contracts/anexo-comprovante.md](./contracts/anexo-comprovante.md) "Compatibilidade".
- FR-016 (confirmação antes de substituir) é responsabilidade exclusiva do frontend (T034) — o endpoint de upload (T014/T015) não tem nem precisa de nenhuma lógica de "é substituição ou é o primeiro anexo", sempre sobrescreve.
- A oferta pós-salvamento (T021-T024) usa um parâmetro de busca (`?anexar=1`) na própria URL de redirecionamento já existente, em vez de uma tela intermediária nova — menor mudança possível na navegação já existente, mantendo o "salvar → ver o registro" atual e apenas acrescentando um banner opcional.
