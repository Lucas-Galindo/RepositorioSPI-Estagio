# Feature Specification: Tabela de Lançamentos em Indicadores Financeiros

**Feature Branch**: `022-tabela-lancamentos-indicadores`

**Created**: 2026-09-16

**Status**: Draft

**Input**: User description: "Na aba Relatórios → Relatório Financeiro → visão Indicadores, onde hoje existe o card 'Gargalo de caixa' (mostrando maior entrada e maior saída do período), substituir com uma tabela detalhada com todos os lançamentos individuais de entrada e saída do período filtrado. Requisitos: 1. Colunas da tabela: descrição/nome do lançamento, e data (de entrada ou saída, conforme o registro). 2. Botões de filtro no topo da tabela, no mesmo padrão visual/componente das abas do menu Financeiro (corrigido em specs/021-fix-hitbox-cliques): 'Entradas' e 'Saídas' — clicar filtra quais opções aparecem. 3. Os filtros de período/data já existentes na página de indicadores devem continuar afetando essa tabela também."

## Clarifications

### Session 2026-09-16

- Q: Quais lançamentos devem entrar na tabela: apenas os já pagos/recebidos, ou todo lançamento não cancelado no período (incluindo pendentes e atrasados), como hoje já acontece no card "Gargalo de caixa" e no "Fluxo de caixa" da mesma tela? → A: Todos não cancelados — mesma base de dados já usada em "Gargalo de caixa" e "Fluxo de caixa" (todo Pagamento/Conta a Pagar com status diferente de "Cancelado", por data de vencimento), mantendo a tabela consistente com os totais já exibidos na mesma tela.
- Q: O campo de descrição de um lançamento pode ficar vazio (é opcional no cadastro). O que a tabela deve mostrar nesse caso, para a coluna de descrição nunca ficar em branco? → A: Quando a descrição não foi preenchida, mostrar o nome do aluno vinculado (entradas) ou o nome do favorecido/categoria da despesa (saídas) como substituto.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver todos os lançamentos do período em uma tabela (Priority: P1)

Como professora consultando Relatórios → Relatório Financeiro → Indicadores, no lugar do card "Gargalo de caixa" (que hoje só mostra a maior entrada e a maior saída do período), eu quero ver uma tabela com todos os lançamentos individuais de entrada e saída do período filtrado, para entender de onde vêm os valores em vez de apenas os dois extremos.

**Why this priority**: É a mudança central pedida — sem essa tabela substituindo o card atual, nenhum dos outros requisitos (filtro Entradas/Saídas, respeito ao filtro de período) tem o que exibir. Entrega valor sozinha: mesmo sem o filtro de tipo, já é uma melhoria sobre o card resumido atual.

**Independent Test**: Acessar Relatórios → Relatório Financeiro → aba Indicadores com dados de entrada e saída no período selecionado, e verificar que no lugar do card "Gargalo de caixa" aparece uma tabela listando cada lançamento individual (descrição e data), não apenas o maior de cada tipo.

**Acceptance Scenarios**:

1. **Given** a aba Indicadores do Relatório Financeiro com lançamentos de entrada e saída no período selecionado, **When** a professora abre a tela, **Then** o card "Gargalo de caixa" não aparece mais, e em seu lugar há uma tabela com uma linha por lançamento individual do período (entradas e saídas juntas, sem filtro de tipo ainda aplicado).
2. **Given** a tabela de lançamentos exibida, **When** a professora observa uma linha, **Then** essa linha mostra a descrição/nome do lançamento (ou, se não houver descrição cadastrada, o nome do aluno ou do favorecido/categoria da despesa) e a data de vencimento correspondente.
3. **Given** o período filtrado não tem nenhum lançamento de entrada nem de saída, **When** a professora abre a aba Indicadores, **Then** a tabela exibe um estado vazio claro (ex.: "Nenhum lançamento no período"), sem erro.

---

### User Story 2 - Filtrar a tabela por Entradas ou Saídas (Priority: P2)

Como professora vendo a tabela de lançamentos, eu quero clicar em botões "Entradas" e "Saídas" no topo da tabela para restringir a lista apenas ao tipo de lançamento que me interessa no momento, no mesmo padrão visual dos botões de aba do menu do Financeiro.

**Why this priority**: Depende da tabela existir (User Story 1), mas sem ela a tabela mistura entradas e saídas indefinidamente em listas potencialmente longas, dificultando a leitura — é a segunda maior fonte de valor da mudança.

**Independent Test**: Com a tabela de lançamentos carregada e contendo tanto entradas quanto saídas, clicar no botão "Entradas" e verificar que somente lançamentos de entrada permanecem visíveis; clicar em "Saídas" e verificar a troca para somente saídas.

**Acceptance Scenarios**:

1. **Given** a tabela de lançamentos com entradas e saídas do período, **When** a professora clica no botão "Entradas", **Then** a tabela passa a exibir somente os lançamentos de entrada do período, e o botão "Entradas" fica com a aparência de item ativo (mesmo padrão visual/componente dos botões de aba do menu do Financeiro corrigidos em [021-fix-hitbox-cliques](../021-fix-hitbox-cliques/spec.md)).
2. **Given** a tabela já filtrada em "Entradas", **When** a professora clica no botão "Saídas", **Then** a tabela passa a exibir somente os lançamentos de saída do período, e "Saídas" passa a ser o botão ativo.
3. **Given** a tabela recém-carregada (nenhum filtro de tipo escolhido ainda), **When** a professora observa o topo da tabela, **Then** ambos os lançamentos (entradas e saídas) aparecem juntos por padrão, sem nenhum dos dois botões marcado como exclusivamente ativo — refletindo o estado "mostrar tudo".
4. **Given** a professora filtrou por "Entradas" e não há nenhum lançamento de entrada no período, **When** o filtro é aplicado, **Then** a tabela exibe um estado vazio claro específico para entradas (ex.: "Nenhuma entrada no período"), sem erro.
5. **Given** a tabela já filtrada por "Entradas" (ou "Saídas"), **When** a professora clica novamente no mesmo botão já ativo, **Then** o filtro de tipo é removido e a tabela volta ao estado "mostrar tudo" (entradas e saídas juntas), com nenhum dos dois botões marcado como exclusivamente ativo — mesmo resultado visual do carregamento inicial (cenário 3).

---

### User Story 3 - Filtros de período já existentes continuam valendo para a tabela (Priority: P1)

Como professora que já usa os filtros de período/data existentes na página de Indicadores, quando eu mudo esses filtros, eu quero que a tabela de lançamentos se atualize automaticamente para refletir apenas o novo período, do mesmo jeito que os outros indicadores da página já fazem.

**Why this priority**: Sem essa integração, a tabela mostraria dados desalinhados com o resto da tela (KPIs e fluxo de caixa continuariam refletindo o período filtrado, mas a tabela não) — isso quebraria a confiança nos dados exibidos, por isso tem a mesma prioridade máxima da User Story 1.

**Independent Test**: Na aba Indicadores, com a tabela de lançamentos visível, alterar o filtro de período (e, se aplicável, os filtros de turma/matéria/aluno que já afetam o lado de receita) e verificar que a tabela é recarregada mostrando somente lançamentos do novo período/filtro, de forma consistente com os demais indicadores da tela.

**Acceptance Scenarios**:

1. **Given** a tabela de lançamentos exibindo os dados do período atual, **When** a professora altera o filtro de período/data existente na página, **Then** a tabela é atualizada para mostrar somente os lançamentos do novo período, sem exigir nenhuma ação adicional da professora.
2. **Given** a tabela filtrada por "Entradas" ou "Saídas" (User Story 2), **When** a professora altera o filtro de período, **Then** o filtro de tipo (Entradas/Saídas) escolhido permanece aplicado, apenas os dados do novo período são carregados.
3. **Given** a página de Indicadores já aplica filtros de turma/matéria/aluno ao lado da receita (conforme nota existente na tela: despesas não têm ligação com aluno/turma/matéria), **When** esses filtros estão ativos, **Then** a tabela de lançamentos de entrada respeita a mesma restrição — exibindo somente entradas compatíveis com o filtro — enquanto as saídas continuam representando o total do negócio, sem filtro de turma/matéria/aluno aplicado a elas.

---

### Edge Cases

- O que acontece se o período filtrado tiver um volume muito grande de lançamentos (ex.: centenas de registros)? A tabela deve continuar utilizável (rolagem dentro do painel, sem travar a tela) — não é exigida paginação nesta versão, mas a lista completa do período deve ficar acessível por rolagem.
- Como a tabela ordena os lançamentos por padrão? Por data, da mais recente para a mais antiga, para que os lançamentos mais próximos do presente apareçam primeiro (mesmo padrão adotado nas demais tabelas de lançamentos do módulo Financeiro).
- O que acontece se dois lançamentos tiverem exatamente a mesma data? Ambos aparecem, sem critério adicional de desempate exigido nesta versão.
- Como o painel se comporta em telas estreitas (mobile), já que ele ocupa uma coluna de um layout de duas colunas junto com o card "Fluxo de caixa"? A tabela deve permanecer legível, com rolagem horizontal/vertical se necessário, seguindo o mesmo padrão responsivo já usado pelas demais tabelas do sistema.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST remover o card "Gargalo de caixa" da aba Indicadores do Relatório Financeiro e substituí-lo, na mesma posição do layout, por uma tabela de lançamentos individuais do período filtrado.
- **FR-002**: A tabela de lançamentos MUST exibir, para cada lançamento, ao menos duas colunas: a descrição/nome do lançamento, e a data correspondente (data de vencimento do lançamento, seja ele uma entrada ou uma saída — mesma data usada hoje pelos cálculos de "Gargalo de caixa" e "Fluxo de caixa" da mesma tela).
- **FR-003**: A tabela MUST incluir, por padrão (sem nenhum filtro de tipo aplicado), todo lançamento de entrada e de saída do período filtrado com status diferente de "Cancelado" — incluindo lançamentos pendentes e atrasados, não apenas os já pagos/recebidos — para que os valores da tabela permaneçam consistentes com os totais já exibidos em "Gargalo de caixa" (substituído) e "Fluxo de caixa" na mesma tela.
- **FR-004**: O sistema MUST exibir dois botões de filtro no topo da tabela, rotulados "Entradas" e "Saídas", usando o mesmo padrão visual de botão dos botões de aba do menu do Financeiro (classes `btn btn-sm btn-primary`/`btn-ghost`, conforme corrigido em [021-fix-hitbox-cliques](../021-fix-hitbox-cliques/spec.md)) — sem exigir o wrapper `financeiro-tabs` em si, já que essa classe é específica para navegação entre páginas por `<a>`, não para botões de filtro local.
- **FR-005**: Ao clicar no botão "Entradas", o sistema MUST restringir a tabela a exibir somente lançamentos de entrada do período; ao clicar em "Saídas", MUST restringir a exibir somente lançamentos de saída.
- **FR-006**: O sistema MUST indicar visualmente qual botão de filtro de tipo está ativo (mesmo tratamento visual de estado ativo usado nas abas do menu do Financeiro).
- **FR-007**: A tabela de lançamentos MUST responder aos filtros de período/data já existentes na página de Indicadores, exibindo somente lançamentos cuja data de vencimento esteja dentro do período selecionado.
- **FR-008**: Quando os filtros de turma/matéria/aluno já existentes na página de Indicadores estiverem ativos, a tabela MUST aplicar essa restrição apenas aos lançamentos de entrada (entradas), mantendo os lançamentos de saída (despesas) sem essa restrição — consistente com o comportamento já documentado na tela para os demais indicadores.
- **FR-009**: Ao mudar o filtro de período (ou os filtros de turma/matéria/aluno), o sistema MUST atualizar a tabela automaticamente, preservando o filtro de tipo (Entradas/Saídas) que já estava selecionado.
- **FR-010**: Quando não houver nenhum lançamento a exibir para a combinação atual de filtros (período, tipo, turma/matéria/aluno), o sistema MUST exibir uma mensagem de estado vazio, sem erro.
- **FR-011**: A tabela MUST ordenar os lançamentos por data, da mais recente para a mais antiga, por padrão.
- **FR-012**: Quando um lançamento não tiver descrição cadastrada, o sistema MUST exibir, no lugar dela: para uma entrada, o nome do aluno vinculado; para uma saída, o nome do favorecido e, se este também estiver vazio, a categoria da despesa — nessa ordem de precedência — para que a coluna de descrição nunca fique em branco.

### Key Entities *(include if feature involves data)*

- **Lançamento (entrada ou saída)**: representa um único registro financeiro do período, com status diferente de "Cancelado" — uma entrada (cobrança/pagamento de aluno, pendente, atrasada ou paga) ou uma saída (registro de Contas a Pagar, pendente, atrasado ou pago). Atributos relevantes para esta funcionalidade: descrição/nome (com nome do aluno ou do favorecido/categoria como substituto quando vazia), data de vencimento, e o tipo (entrada/saída) usado para o filtro.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Uma professora consegue identificar, em até 5 segundos após abrir a aba Indicadores, todos os lançamentos individuais de entrada e saída do período filtrado, sem precisar navegar para outra tela.
- **SC-002**: 100% dos lançamentos de entrada e saída presentes no período filtrado (conforme já calculado pelos demais indicadores da tela, como o fluxo de caixa) aparecem listados na tabela, sem nenhum lançamento ausente ou duplicado.
- **SC-003**: Ao alternar entre os filtros "Entradas" e "Saídas", a tabela reflete a lista correta em até 1 segundo, sem recarregar a página inteira.
- **SC-004**: Ao mudar o filtro de período existente na página, a tabela de lançamentos e os demais indicadores da tela (KPIs, fluxo de caixa) sempre refletem o mesmo período, sem divergência entre eles.

## Assumptions

- Esta versão não exige paginação nem busca textual dentro da tabela — apenas rolagem, seguindo o padrão das demais tabelas do sistema, já que o volume típico de lançamentos por período é gerenciável visualmente.
- O estado "mostrar tudo" (sem filtro de tipo) é o padrão ao carregar a página; não é necessário lembrar a escolha de filtro entre visitas — cada carregamento da aba Indicadores começa sem filtro de tipo aplicado.
- O layout de duas colunas (`grid-2b`) que hoje posiciona "Gargalo de caixa" ao lado de "Fluxo de caixa" é reaproveitado para a nova tabela, mantendo "Fluxo de caixa" no mesmo lugar.
- A restrição de turma/matéria/aluno ao lado de entradas (e sua ausência do lado de saídas) segue a mesma regra de negócio já implementada e documentada para os demais indicadores da página (ver aviso já existente na tela).
