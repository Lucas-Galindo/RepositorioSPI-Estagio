# Feature Specification: Lançamentos Clicáveis na Tabela de Indicadores

**Feature Branch**: `045-lancamentos-clicaveis`

**Created**: 2026-10-03

**Status**: Draft

**Input**: User description: "Na tabela de lançamentos da aba Relatórios → Relatório Financeiro → visão Indicadores (spec 022), tornar cada linha clicável, levando para a tela de detalhe que já existe do registro de origem: linha de entrada (Pagamento) abre o detalhe em Contas a Receber; linha de saída (Conta a Pagar) abre o detalhe em Contas a Pagar. Reaproveitar as telas de detalhe existentes, sem criar tela nova. Linha com sinal visual de clicável (cursor e hover, classe row-link). Nenhuma mudança de backend se a API já trouxer identificador e tipo. Ao voltar do detalhe, preservar os filtros do relatório quando viável sem complexidade extra, senão registrar como limitação em research.md. Filtros Entradas/Saídas e cálculo da tabela continuam idênticos ao comportamento da spec 022. Fora de escopo: tela nova de detalhe, painel lateral, modal, ou mudança nos indicadores."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar):

- **A API já traz tudo que esta feature precisa, sem nenhuma mudança de backend**: `LancamentoIndicadorItem` (`src/SPI.Application/Relatorios/Dtos/LancamentoIndicadorItem.cs`) já tem `Id` e `Tipo` (`"Entrada"`/`"Saida"`), e `RelatorioService` (linhas ~270-292) já popula `Id` com o `Pagamento.Id` real (para entradas) ou o `ContaPagar.Id` real (para saídas) — exatamente o identificador que as telas de detalhe existentes esperam na rota. O Requisito 3 do pedido ("só mudar backend se a API não trouxer id/tipo") não se aplica: a API já traz os dois campos, então esta feature é só frontend.
- **As telas de detalhe já existem e usam o padrão certo de rota**: `frontend/app/(app)/financeiro/contas-a-pagar/[id]/page.tsx` e `frontend/app/(app)/financeiro/contas-a-receber/[id]/page.tsx` já existem e exibem o detalhe completo do registro, por `id` na URL.
- **O padrão `row-link` já está em uso e é exatamente o pedido pelo usuário**: em `frontend/app/(app)/financeiro/contas-a-pagar/page.tsx` (linha ~196), cada linha da tabela usa `<tr className="row-link" onClick={() => router.push(...)}>` — classe CSS já existente (`cursor: pointer` + `hover` já estilizados globalmente), reaproveitável sem nenhuma mudança de CSS.
- **Achado relevante para o Requisito 4 (preservar filtros ao voltar)**: a tabela de lançamentos vive em `frontend/app/(app)/relatorios/financeiro/page.tsx`, e todos os filtros da aba Indicadores (período, turma, matéria, aluno, semestre, e o filtro de tipo Entradas/Saídas da própria tabela) são `useState` local — **nenhum é refletido na URL** (não há query params). Isso significa que uma navegação de rota completa para a tela de detalhe e, depois, para a lista de origem (`/financeiro/contas-a-pagar` ou `/financeiro/contas-a-receber`, via o breadcrumb que a própria tela de detalhe já tem) **não volta para o relatório** — o breadcrumb da tela de detalhe leva para a listagem bruta da entidade, não para `relatorios/financeiro`. O único caminho que efetivamente "volta" para o relatório é o botão/gesto de voltar do navegador (ou um botão "voltar" equivalente que esta feature precisaria adicionar). Com `useState` local (sem URL), o React desmonta o componente da página ao navegar para o detalhe; se o retorno for feito por um link/botão que recarregue a rota do relatório do zero, os filtros voltam ao estado padrão. Se for feito por navegação "voltar" do navegador preservada pelo cache de rota do Next.js (App Router), o estado pode ou não ser preservado, dependendo de comportamento de cache que precisa ser verificado na prática — por isso o Requisito 4 do pedido já previu esse caso como possivelmente inviável "sem complexidade extra", com uma saída explícita (documentar como limitação em `research.md`).
- **Nenhuma tela nova, nenhum painel lateral, nenhum modal**: confirmado — a mudança é inteiramente dentro da tabela já existente em `relatorios/financeiro/page.tsx`, usando rotas e componentes 100% já existentes.

## Clarifications

_Nenhuma pendente — o pedido original já é preciso o suficiente (reaproveitar telas existentes, usar o padrão `row-link`, backend só se necessário) e a investigação prévia confirmou que nenhuma decisão de produto ficou em aberto; a única incerteza real (preservação de filtro ao voltar) já tem uma saída definida pelo próprio pedido (documentar limitação, se inviável) e será resolvida tecnicamente no `/speckit-plan`._

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Abrir o detalhe de um lançamento de entrada (Priority: P1)

Como professora olhando a tabela de lançamentos do período (Relatórios → Relatório Financeiro → Indicadores), eu quero clicar numa linha de entrada e ir direto para o detalhe completo daquele pagamento em Contas a Receber, para não precisar procurar manualmente o mesmo registro na lista de Contas a Receber.

**Why this priority**: é a metade mais comum do uso real (entradas tendem a ser mais numerosas e mais investigadas no dia a dia do que saídas) e sozinha já entrega o valor central do pedido.

**Independent Test**: Na aba Indicadores, com ao menos um lançamento de entrada na tabela, clicar na linha e confirmar que a tela de detalhe de Contas a Receber abre mostrando exatamente aquele pagamento (mesmo nome/valor/data vistos na linha clicada).

**Acceptance Scenarios**:

1. **Given** a tabela de lançamentos com uma linha de entrada visível, **When** a professora clica em qualquer ponto da linha, **Then** o sistema navega para a tela de detalhe de Contas a Receber do pagamento correspondente.
2. **Given** o cursor sobre uma linha de entrada da tabela, **When** a professora passa o mouse por cima, **Then** a linha mostra sinal visual de que é clicável (cursor de ponteiro e destaque visual de hover), igual ao já usado na lista de Contas a Pagar.

---

### User Story 2 - Abrir o detalhe de um lançamento de saída (Priority: P1)

Como professora olhando a mesma tabela, eu quero clicar numa linha de saída e ir direto para o detalhe completo daquela conta em Contas a Pagar, pelo mesmo motivo da User Story 1.

**Why this priority**: mesma prioridade da US1 — é a outra metade do mesmo comportamento, sem a qual a feature ficaria incompleta (só entradas clicáveis seria uma meia-entrega confusa para quem usa a tabela).

**Independent Test**: Na aba Indicadores, com ao menos um lançamento de saída na tabela, clicar na linha e confirmar que a tela de detalhe de Contas a Pagar abre mostrando exatamente aquela conta.

**Acceptance Scenarios**:

1. **Given** a tabela de lançamentos com uma linha de saída visível, **When** a professora clica em qualquer ponto da linha, **Then** o sistema navega para a tela de detalhe de Contas a Pagar da conta correspondente.
2. **Given** o cursor sobre uma linha de saída da tabela, **When** a professora passa o mouse por cima, **Then** a linha mostra o mesmo sinal visual de clicável da User Story 1.

---

### User Story 3 - Voltar ao relatório sem perder os filtros aplicados (Priority: P3)

Como professora que já filtrou o relatório por período (e, se aplicável, turma/matéria/aluno) antes de clicar num lançamento, eu quero voltar para a tela de Indicadores e encontrar os mesmos filtros ainda aplicados, para não ter que refazer a filtragem do zero a cada lançamento que eu for investigar.

**Why this priority**: é uma melhoria de conforto sobre as User Stories 1/2, não um bloqueio — mesmo sem ela, clicar e ver o detalhe (o núcleo do pedido) já funciona; por isso fica em P3, e o próprio pedido original já previu que pode não ser viável sem complexidade extra.

**Independent Test**: Aplicar um filtro de período (e turma/matéria/aluno, se aplicável) na aba Indicadores, clicar num lançamento, voltar para o relatório (pelo mecanismo de navegação que a implementação definir) e confirmar se os mesmos filtros ainda estão selecionados.

**Acceptance Scenarios**:

1. **Given** a aba Indicadores com um filtro de período (e opcionalmente turma/matéria/aluno) já aplicado, **When** a professora clica num lançamento e depois volta para o relatório, **Then** os mesmos filtros continuam aplicados, **se a solução técnica escolhida permitir isso sem complexidade extra** (ver Assumptions).
2. **Given** a mesma situação, **When** a preservação do filtro não for viável sem complexidade extra, **Then** o sistema MAY voltar ao relatório com os filtros no estado padrão — desde que essa limitação esteja documentada e a professora não veja nenhum erro, apenas filtros resetados.

---

### Edge Cases

- **Lançamento cujo registro de origem foi excluído logicamente (`Ativo`/`Status` alterado) entre a listagem e o clique**: a tela de detalhe já existente é reaproveitada como está — seu comportamento atual para esse cenário (ex.: exibir o registro mesmo inativo, ou mensagem de não encontrado) não muda; esta feature não introduz tratamento novo para isso.
- **Clique em um elemento interativo aninhado dentro da linha** (não há nenhum hoje — a linha só tem texto/descrição e data): não se aplica no estado atual da tabela; se um botão/link for adicionado dentro da linha no futuro, ele precisa impedir a propagação do clique para a navegação da linha (fora do escopo desta feature, que não adiciona nenhum elemento interativo dentro da linha).
- **Usuário sem permissão para ver Contas a Pagar ou Contas a Receber** (não existe hoje — ambas as telas já são acessíveis a qualquer usuário autenticado que vê o relatório): não se aplica; não há um perfil que veja o relatório mas não as telas de detalhe.
- **Tabela filtrada por "Entradas" ou "Saídas" (spec 022) e a professora clica numa linha**: o clique continua funcionando normalmente, independente do filtro de tipo ativo — a navegação não depende de qual filtro de tipo está selecionado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST tornar cada linha da tabela de lançamentos (aba Indicadores, spec 022) clicável.
- **FR-002**: Ao clicar numa linha de entrada, o sistema MUST navegar para a tela de detalhe já existente de Contas a Receber do pagamento correspondente àquela linha.
- **FR-003**: Ao clicar numa linha de saída, o sistema MUST navegar para a tela de detalhe já existente de Contas a Pagar da conta correspondente àquela linha.
- **FR-004**: O sistema MUST NOT criar nenhuma tela de detalhe nova, painel lateral, ou modal — a navegação MUST usar exclusivamente as telas de detalhe de Contas a Pagar/Receber já existentes no sistema.
- **FR-005**: Cada linha da tabela MUST exibir sinal visual de que é clicável (cursor de ponteiro ao passar o mouse, e destaque visual de hover), seguindo o mesmo padrão (classe `row-link`) já usado nas demais tabelas clicáveis do sistema (ex.: lista de Contas a Pagar).
- **FR-006**: O sistema MUST NOT alterar o contrato de API da tabela de lançamentos além do estritamente necessário — e, como a resposta já traz o identificador (`Id`) e o tipo (`Tipo`) de cada lançamento, esta feature MUST NOT exigir nenhuma mudança de backend.
- **FR-007**: O sistema MUST NOT alterar o comportamento dos filtros "Entradas"/"Saídas" nem o cálculo/conteúdo da tabela de lançamentos (descrição, data, ordenação, estados vazios) definidos pela spec 022 — esta feature só adiciona a navegação ao clicar.
- **FR-008**: O sistema MUST NOT alterar nenhum dos demais indicadores da aba (KPIs, fluxo de caixa, prazo médio de atraso) — a mudança é restrita à tabela de lançamentos.
- **FR-009**: Quando tecnicamente viável sem complexidade adicional relevante, o sistema SHOULD preservar os filtros de período/turma/matéria/aluno da aba Indicadores ao a professora voltar da tela de detalhe para o relatório; quando não for viável, essa limitação MUST ser registrada explicitamente em `research.md` durante o planejamento, e o comportamento de fallback (filtros resetados, sem erro) é aceitável.

### Key Entities

- **Lançamento (entrada ou saída)** — já existente (spec 022), sem nenhum campo novo: usa o `Id` e o `Tipo` já presentes na resposta da API para determinar o destino da navegação (`Tipo = "Entrada"` → Contas a Receber; `Tipo = "Saida"` → Contas a Pagar).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos cliques numa linha de entrada da tabela de lançamentos abrem o detalhe correto (mesmo registro) em Contas a Receber.
- **SC-002**: 100% dos cliques numa linha de saída da tabela de lançamentos abrem o detalhe correto (mesmo registro) em Contas a Pagar.
- **SC-003**: Uma professora identifica visualmente, sem precisar clicar, que uma linha da tabela é clicável (cursor e hover), sem precisar de instrução adicional.
- **SC-004**: 0 regressões no comportamento já existente dos filtros Entradas/Saídas, do cálculo/estados vazios da tabela, e dos demais indicadores da aba — confirmável comparando o comportamento antes/depois desta feature.
- **SC-005**: 0 mudanças no contrato de API da tabela de lançamentos (nenhum campo novo exigido, nenhuma rota de backend alterada).

## Assumptions

- A API (`LancamentoIndicadorItem`) já expõe `Id` e `Tipo` com os valores corretos (Id real do `Pagamento`/`ContaPagar` de origem) — confirmado por leitura do código (`RelatorioService.cs`); por isso FR-006 proíbe qualquer mudança de backend nesta feature.
- As rotas de detalhe existentes são `/financeiro/contas-a-receber/[id]` e `/financeiro/contas-a-pagar/[id]`, já funcionais e inalteradas por esta feature.
- A classe CSS `row-link` já cobre o requisito visual (cursor + hover) sem nenhum CSS novo — reaproveitada tal como está, nenhuma variação visual nova é necessária.
- Preservar os filtros ao voltar (User Story 3 / FR-009) depende de uma decisão técnica a ser tomada em `/speckit-plan` (ex.: refletir os filtros na URL, ou confiar no comportamento de navegação "voltar" do navegador) — se a opção mais simples não preservar os filtros de forma confiável, a spec aceita o fallback (filtros resetados) como resultado válido, desde que documentado.
- O breadcrumb "voltar" já existente dentro das telas de detalhe de Contas a Pagar/Receber (que leva para a listagem bruta da entidade, não para o relatório) não é alterado por esta feature — ele continua levando para onde já leva hoje; a navegação "voltar para o relatório" referida na User Story 3 é feita pelo gesto de voltar do navegador (ou equivalente), não por aquele breadcrumb.
- Fora de escopo: qualquer tela de detalhe nova, painel lateral, modal, paginação da tabela de lançamentos, ou mudança nos demais indicadores/KPIs da aba (conforme delimitado explicitamente no pedido original).
