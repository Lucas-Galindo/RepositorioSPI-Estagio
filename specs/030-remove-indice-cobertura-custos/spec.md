# Feature Specification: Remover Índice de Cobertura de Custos Fixos do Backend

**Feature Branch**: `030-remove-indice-cobertura-custos`

**Created**: 2026-09-18

**Status**: Draft

**Input**: User description: "Remover completamente o cálculo de IndiceCoberturaCustosFixos do backend (DashboardService e RelatorioService), já que ele não é mais exibido em nenhuma tela desde a spec 024 e não é consumido por nenhum outro lugar do sistema. Remover o campo correspondente do contrato de resposta da API também, não só parar de calcular internamente."

## Nota de investigação prévia

Confirmado por leitura do código atual: o indicador "Cobertura de custos" (`IndiceCoberturaCustosFixos`, cálculo `Recebido ÷ Pago no período`) é calculado em dois pontos — `DashboardService.ObterAsync` (endpoint `GET /api/dashboard`) e `RelatorioService.ObterIndicadoresFinanceirosAsync` (endpoint `GET /api/relatorios/indicadores-financeiros`) — e exposto como campo do mesmo tipo de resposta compartilhado por ambos. A [spec 024](../024-remover-card-cobertura-custos/spec.md) já havia removido a exibição desse indicador da tela "Relatórios → Relatório Financeiro → Indicadores", deixando explícito que a remoção do cálculo no backend seria "uma decisão técnica a ser avaliada na fase de planejamento, não uma exigência funcional" daquela spec. Uma busca por qualquer outro consumidor do campo (frontend, outros relatórios, outros serviços) não encontrou nenhuma outra tela, componente ou serviço que leia esse valor — o único vestígio remanescente no frontend é o espelhamento do tipo TypeScript da resposta da API (que também nunca é lido/exibido).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Contrato de API sem campos mortos (Priority: P1)

Como mantenedora e única desenvolvedora do sistema, eu quero que as respostas de `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros` não incluam mais nenhum campo que não é calculado com propósito nem lido por nenhuma tela, para que o contrato da API reflita fielmente o que o sistema realmente oferece e eu não perca tempo, no futuro, investigando ou mantendo um valor que ninguém usa.

**Why this priority**: É o pedido central da mudança — sem a remoção efetiva do campo (não apenas da exibição), o contrato da API continua carregando um dado morto que pode confundir uma investigação futura (como já aconteceu: o valor foi encontrado numa investigação anterior e precisou ser explicado como "legado da spec 024").

**Independent Test**: Chamar `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros` (autenticado) e confirmar que nenhum dos dois retorna mais o campo `indiceCoberturaCustosFixos` (nem qualquer campo equivalente) em nenhuma combinação de filtros ou período.

**Acceptance Scenarios**:

1. **Given** qualquer período/filtro válido, **When** a professora (ou qualquer cliente autenticado) consulta `GET /api/dashboard`, **Then** a resposta não contém o campo `indiceCoberturaCustosFixos` em nenhum lugar da estrutura de indicadores.
2. **Given** qualquer período/filtro válido (incluindo turma, matéria e aluno), **When** consultado `GET /api/relatorios/indicadores-financeiros`, **Then** a resposta também não contém esse campo.
3. **Given** a remoção do campo, **When** os demais campos de indicadores financeiros (inadimplência, prazo médio de atraso, margem de segurança, gargalo de caixa, fluxo de caixa mensal) são consultados nos dois endpoints, **Then** eles continuam presentes e com exatamente os mesmos valores de antes da mudança — a remoção não MUST afetar nenhum outro indicador.

---

### User Story 2 - Nenhum trabalho de cálculo desperdiçado (Priority: P2)

Como mantenedora do sistema, eu quero que o backend pare de calcular um valor que nenhuma tela exibe, para que o código de geração de indicadores financeiros contenha apenas lógica que efetivamente sustenta algo visível ou consumido, reduzindo a superfície de manutenção.

**Why this priority**: É a limpeza interna que acompanha a remoção do contrato (User Story 1) — sem ela, o campo desapareceria da resposta da API mas o cálculo (divisão, arredondamento, etc.) continuaria rodando à toa a cada requisição, o que é um desperdício menor, porém evitável na mesma mudança.

**Independent Test**: Inspecionar (por revisão de código) que nenhum dos dois métodos que hoje montam a resposta de indicadores financeiros ainda calcula uma divisão de valor recebido por valor pago com o propósito de preencher esse indicador.

**Acceptance Scenarios**:

1. **Given** o código dos dois pontos que hoje geram a resposta de indicadores financeiros, **When** a mudança é aplicada, **Then** nenhum dos dois mais executa o cálculo que só existia para preencher o campo removido.

---

### Edge Cases

- Existe algum outro relatório, exportação ou tela do sistema que dependa desse campo além dos dois endpoints já identificados? Não foi encontrado nenhum — a investigação prévia cobriu todo o backend e o frontend.
- E se, no futuro, alguém quiser reintroduzir um indicador de cobertura de custos com um cálculo diferente ou mais útil? Isso é tratado como uma feature nova e independente, não uma reversão desta remoção — esta mudança não impede recriar um indicador com esse propósito depois, apenas remove o atual, que não agrega valor.
- A remoção quebra algum client já em produção que dependa desse campo? Não há indicação de nenhum consumidor externo do sistema (é single-tenant, uso interno da professora via o próprio frontend do projeto) — o único consumidor identificado (o frontend deste mesmo repositório) já não lê o campo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST deixar de incluir o campo do índice de cobertura de custos fixos na resposta de `GET /api/dashboard`.
- **FR-002**: O sistema MUST deixar de incluir esse mesmo campo na resposta de `GET /api/relatorios/indicadores-financeiros`.
- **FR-003**: O sistema MUST deixar de executar, em ambos os pontos que montam essas respostas, qualquer cálculo cujo único propósito seja preencher esse campo.
- **FR-004**: O sistema MUST manter inalterados todos os demais campos e valores de indicadores financeiros retornados pelos dois endpoints (inadimplência, prazo médio de atraso, margem de segurança, gargalo de caixa, fluxo de caixa mensal, e quaisquer outros já existentes) — esta mudança MUST se limitar exclusivamente ao campo removido.
- **FR-005**: Qualquer representação do contrato da API mantida fora do backend (como um espelhamento de tipos usado pelo frontend deste sistema) MUST ser atualizada na mesma mudança, para que nenhuma parte do código continue declarando um campo que a API não envia mais.

### Key Entities

- Não introduz nem remove entidades de domínio — afeta apenas a estrutura de resposta (DTO) compartilhada pelos dois endpoints de indicadores financeiros, removendo um campo calculado que não corresponde a nenhuma entidade persistida.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das respostas de `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros`, em qualquer combinação de filtros testada, deixam de conter o campo removido.
- **SC-002**: 100% dos demais campos de indicadores financeiros retornados pelos dois endpoints permanecem com o mesmo valor, para o mesmo conjunto de dados, antes e depois da mudança.
- **SC-003**: Nenhuma referência ao campo removido (cálculo, propriedade de resposta, ou tipo espelhado) permanece em nenhuma parte do código do sistema (backend ou frontend) após a mudança.

## Assumptions

- "Não é mais exibido em nenhuma tela desde a spec 024" é aceito como fato já verificado (confirmado nesta investigação prévia) — esta spec não repete essa verificação, apenas parte dela como premissa.
- A remoção é tratada como uma mudança de contrato aceitável sem período de transição/depreciação, já que o sistema é single-tenant e de uso interno, sem consumidores externos conhecidos da API.
- "Remover completamente" inclui o espelhamento de tipo usado pelo frontend deste mesmo repositório (ainda que o pedido original mencione só "backend" e "contrato da API"), porque deixar um campo tipado no frontend que a API nunca mais envia recriaria o mesmo tipo de dado morto que esta mudança busca eliminar.
