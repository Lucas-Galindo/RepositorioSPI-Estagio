# Feature Specification: Remover Indicador "Margem de Segurança %"

**Feature Branch**: `031-remove-margem-seguranca`

**Created**: 2026-09-18

**Status**: Draft

**Input**: User description: "Remover completamente o indicador 'Margem de Segurança %' do sistema (backend e frontend), seguindo o mesmo padrão da remoção de Cobertura de Custos (spec 030): remover o campo do DTO de resposta, remover o cálculo interno (fluxoCaixaOperacional / valorFaturado), remover a exibição do card no Dashboard e na visão Indicadores, e atualizar as specs retroativas afetadas (015 e outras que mencionem o indicador) conforme Princípio V da constituição. Motivo da remoção: o nome 'Margem de Segurança' sugere uma métrica de mercado (folga entre receita e ponto de equilíbrio/break-even), mas o cálculo real não corresponde a essa definição — risco de confundir quem usa o sistema com uma leitura errada do próprio negócio."

## Nota de investigação prévia

Confirmado por leitura do código atual:

- O indicador é calculado em dois pontos — `DashboardService.ObterIndicadoresFinanceirosAsync` (endpoint `GET /api/dashboard`, `DashboardService.cs:75`) e `RelatorioService.ObterIndicadoresFinanceirosAsync` (endpoint `GET /api/relatorios/indicadores-financeiros`, `RelatorioService.cs:206`) — como `FluxoCaixaOperacional ÷ ValorFaturado × 100`, e exposto como `MargemSegurancaPercentual` no mesmo DTO compartilhado pelos dois endpoints (`IndicadoresFinanceirosResponse`, que já perdeu o campo irmão `IndiceCoberturaCustosFixos` na spec 030).
- **Correção ao pedido original**: o card de "Margem de segurança" **não é exibido na tela Dashboard (`/dashboard`)** — essa tela nunca renderizou o objeto `indicadores` retornado por `GET /api/dashboard` (mesmo comportamento já documentado em `specs/011-dashboard/spec.md`: "a API calcula mais indicadores do que a Home exibe"). A única exibição visual encontrada é o card "Margem de segurança" na aba **"Visão de Indicadores"** da tela **Relatórios → Relatório Financeiro** (`frontend/app/(app)/relatorios/financeiro/page.tsx`, dentro da mesma fileira de 3 cards — Inadimplência, Prazo médio de atraso, Margem de segurança — de onde o card "Cobertura de custos" foi removido pela spec 024). Esta spec trata a remoção desse único card real, não de um card inexistente na Home.
- Especificações retroativas que citam "Margem de segurança" nominalmente: `specs/015-relatorio-financeiro/spec.md` (User Story 3, Acceptance Scenario 1, FR-005). `specs/024-remover-card-cobertura-custos/spec.md` também a cita, mas apenas como contexto histórico dos "3 cards restantes" daquela mudança já concluída — seu conteúdo continua factualmente correto sobre o que aconteceu *naquela* mudança, então não precisa ser reescrito (mesmo raciocínio já aplicado pela spec 030 a specs históricas neutras). `specs/022-tabela-lancamentos-indicadores/contracts/indicadores-financeiros.md` inclui o campo `margemSegurancaPercentual` num exemplo de payload JSON — não é uma spec retroativa de comportamento, mas fica desatualizado como documentação de referência se não for ajustado.
- Nenhum outro consumidor do campo foi encontrado (frontend ou backend) além dos já listados.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Não ver mais um indicador com nome enganoso (Priority: P1)

Como professora consultando Relatórios → Relatório Financeiro → Visão de Indicadores, eu não quero mais ver um card chamado "Margem de Segurança %" cujo cálculo não corresponde ao que esse nome significa no mercado (folga entre receita e ponto de equilíbrio), para que eu não tire uma conclusão errada sobre a saúde financeira do meu negócio a partir de um número mal rotulado.

**Why this priority**: É o motivo central da mudança — o risco de leitura equivocada existe desde que a professora vê esse card, então removê-lo é o que efetivamente elimina o risco.

**Independent Test**: Abrir Relatórios → Relatório Financeiro → aba Indicadores e verificar que o card "Margem de segurança" não aparece em nenhuma combinação de filtros (período, turma, matéria, aluno) nem em nenhum estado de dados (com dados, sem dados, carregando, erro).

**Acceptance Scenarios**:

1. **Given** a professora está na aba Indicadores do Relatório Financeiro, **When** a tela carrega com dados do período selecionado, **Then** o card "Margem de segurança" não é exibido em nenhum lugar da tela.
2. **Given** a professora está na aba Indicadores, **When** ela altera os filtros (período, turma, matéria ou aluno), **Then** o card "Margem de segurança" continua ausente, independentemente do resultado retornado.
3. **Given** o card foi removido, **When** a professora visualiza a fileira de indicadores restante (Inadimplência, Prazo médio de atraso), **Then** os dois cards ocupam o espaço da fileira de forma equilibrada, sem um vão vazio no lugar do card removido — mesmo padrão de reorganização já usado quando "Cobertura de custos" foi removida (spec 024).
4. **Given** os dois cards restantes, **When** consultados em qualquer combinação de filtros, **Then** eles continuam exibindo exatamente o mesmo conteúdo, cálculo e comportamento que já tinham antes desta mudança.

---

### User Story 2 - Contrato de API sem o indicador removido (Priority: P2)

Como mantenedora do sistema, eu quero que as respostas de `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros` parem de calcular e retornar o indicador removido, para que o contrato da API não continue carregando um valor cujo nome induz a uma leitura errada, mesmo que nenhuma tela mais o exiba.

**Why this priority**: Segue o mesmo padrão de completude já estabelecido pela remoção do indicador de Cobertura de Custos (spec 030) — remover só a exibição (User Story 1) resolveria o risco imediato para a professora, mas deixaria o mesmo tipo de dado morto/mal nomeado no contrato de API para quem investigar o sistema no futuro.

**Independent Test**: Chamar `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros` (autenticado) e confirmar que nenhum dos dois retorna mais o campo `margemSegurancaPercentual` em nenhuma combinação de filtros ou período.

**Acceptance Scenarios**:

1. **Given** qualquer período/filtro válido, **When** consultado `GET /api/dashboard`, **Then** a resposta não contém o campo `margemSegurancaPercentual` em nenhum lugar da estrutura de indicadores.
2. **Given** qualquer período/filtro válido (incluindo turma, matéria e aluno), **When** consultado `GET /api/relatorios/indicadores-financeiros`, **Then** a resposta também não contém esse campo.
3. **Given** a remoção do campo, **When** os demais campos de indicadores financeiros (inadimplência, prazo médio de atraso, fluxo de caixa operacional, gargalo de caixa, fluxo de caixa mensal) são consultados nos dois endpoints, **Then** eles continuam presentes e com exatamente os mesmos valores de antes da mudança.

---

### Edge Cases

- O card "Margem de segurança" aparece em algum outro lugar do sistema além da aba Indicadores do Relatório Financeiro (por exemplo, a tela Dashboard/`/dashboard`)? Não — confirmado na investigação prévia: a tela Dashboard nunca exibiu esse indicador, apesar de a API que ela consome (`GET /api/dashboard`) sempre tê-lo calculado internamente.
- `FluxoCaixaOperacional` (usado como numerador do cálculo removido) é um indicador próprio e continua existindo? Sim — `FluxoCaixaOperacional` é um campo independente do DTO, usado também na Visão Geral/Relatório Financeiro (Saldo Realizado); esta remoção não o afeta.
- Existe algum outro relatório, exportação ou tela que dependa desse campo além dos dois endpoints já identificados? Não foi encontrado nenhum.
- E se, no futuro, alguém quiser reintroduzir um indicador de margem de segurança com a definição correta de mercado (folga sobre o ponto de equilíbrio)? Isso é tratado como uma feature nova e independente, não uma reversão desta remoção — o sistema hoje não tem os conceitos de custo fixo/variável nem ponto de equilíbrio no domínio, então essa reintrodução exigiria modelagem nova, fora do escopo aqui.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST deixar de exibir o card "Margem de segurança" na aba Indicadores do Relatório Financeiro, em qualquer combinação de filtros (período, turma, matéria, aluno) e em qualquer estado de dados (com dados, sem dados, carregando, erro).
- **FR-002**: O sistema MUST distribuir os cards restantes da fileira (Inadimplência, Prazo médio de atraso) de forma equilibrada, sem deixar um vão vazio no lugar do card removido, e MUST continuar exibindo-os com exatamente o mesmo conteúdo, cálculo e comportamento que já têm hoje.
- **FR-003**: O sistema MUST deixar de incluir o campo do indicador de margem de segurança na resposta de `GET /api/dashboard`.
- **FR-004**: O sistema MUST deixar de incluir esse mesmo campo na resposta de `GET /api/relatorios/indicadores-financeiros`.
- **FR-005**: O sistema MUST deixar de executar, em ambos os pontos que montam essas respostas, qualquer cálculo cujo único propósito seja preencher esse campo.
- **FR-006**: O sistema MUST manter inalterados todos os demais campos e valores de indicadores financeiros retornados pelos dois endpoints (inadimplência, prazo médio de atraso, fluxo de caixa operacional, gargalo de caixa, fluxo de caixa mensal, e quaisquer outros já existentes) — esta mudança MUST se limitar exclusivamente ao indicador removido.
- **FR-007**: Qualquer representação do contrato da API mantida fora do backend (como um espelhamento de tipos usado pelo frontend deste sistema) MUST ser atualizada na mesma mudança, para que nenhuma parte do código continue declarando um campo que a API não envia mais.

### Key Entities

- Não introduz nem remove entidades de domínio — afeta apenas (a) a estrutura de resposta (DTO) compartilhada pelos dois endpoints de indicadores financeiros, removendo um campo calculado que não corresponde a nenhuma entidade persistida, e (b) a apresentação visual de um card na tela de Relatório Financeiro.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das visitas à aba Indicadores do Relatório Financeiro, em qualquer filtro ou estado de dados, não exibem o card "Margem de segurança".
- **SC-002**: Os dois cards restantes preenchem visualmente toda a largura da fileira de indicadores, sem espaço vazio perceptível, em telas desktop e mobile.
- **SC-003**: 100% das respostas de `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros`, em qualquer combinação de filtros testada, deixam de conter o campo removido.
- **SC-004**: 100% dos demais campos/cards de indicadores financeiros permanecem com o mesmo valor e comportamento, para o mesmo conjunto de dados, antes e depois da mudança.
- **SC-005**: Nenhuma referência ao indicador removido (cálculo, propriedade de resposta, card visual, ou tipo espelhado) permanece em nenhuma parte do código do sistema (backend ou frontend) após a mudança.

## Assumptions

- "Remover completamente... backend e frontend" inclui tanto a remoção visual (o único card real, na aba Indicadores) quanto a remoção do campo do contrato de API e do cálculo interno — não há, ao contrário da spec 030, uma remoção visual anterior já feita; esta spec cobre as duas partes numa única mudança.
- O pedido original menciona um card no "Dashboard" que a investigação não confirmou existir — tratado como um engano de premissa do pedido, não como um requisito adicional a criar. Esta spec documenta e corrige essa premissa (ver Nota de investigação prévia) em vez de adicionar um card que nunca existiu.
- `specs/015-relatorio-financeiro/spec.md` MUST ser atualizada (Princípio V), pelo mesmo padrão já usado pela spec 030 para o mesmo arquivo. `specs/024-remover-card-cobertura-custos/spec.md` não precisa de edição — seu conteúdo é um registro histórico correto do que aconteceu naquela mudança, não uma afirmação sobre o estado atual do sistema.
- O exemplo de payload JSON em `specs/022-tabela-lancamentos-indicadores/contracts/indicadores-financeiros.md` é tratado como documentação de referência técnica (não uma spec retroativa de comportamento) — deve ser atualizado por consistência, mas essa atualização é uma decisão de qualidade de documentação, não uma exigência do Princípio V da constituição.
- Nenhuma migração de banco de dados é necessária — o indicador é 100% calculado em memória, sem coluna ou entidade persistida associada.
