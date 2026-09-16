# Research: Tabela de Lançamentos em Indicadores Financeiros

Nenhum `NEEDS CLARIFICATION` permaneceu no Technical Context do plan.md — o spec já resolveu as
duas ambiguidades de maior impacto (escopo dos lançamentos e fallback de descrição) na sessão de
`/speckit-clarify` de 2026-09-16. As decisões abaixo cobrem os pontos de design que a fase de
planejamento precisou fechar para poder desenhar `data-model.md`/`contracts/`.

## 1. Onde filtrar por tipo (Entradas/Saídas): backend ou frontend?

- **Decision**: A API devolve, em cada chamada, a lista completa e já unificada de lançamentos
  do período (entradas + saídas juntos, cada item com um campo `tipo`). O filtro visual
  "Entradas"/"Saídas" (User Story 2) é aplicado no frontend, sobre os dados já carregados —
  nenhum parâmetro de tipo é adicionado à query string do endpoint `GET
  /api/relatorios/indicadores-financeiros`.
- **Rationale**: SC-003 exige que a troca entre "Entradas" e "Saídas" reflita em até 1 segundo
  "sem recarregar a página inteira" — um filtro client-side sobre uma lista já em memória atende
  isso trivialmente, sem round-trip de rede. O volume de lançamentos por período (spec Edge
  Cases: "centenas de registros", sem exigência de paginação) é pequeno o suficiente para não
  justificar buscar cada tipo separadamente. Além disso, os outros indicadores da mesma resposta
  (KPIs, fluxo de caixa) já dependem do mesmo período/filtros — manter a lista de lançamentos na
  mesma resposta evita uma segunda chamada de API e um segundo estado de loading/erro na tela.
- **Alternatives considered**:
  - Adicionar `tipo` como parâmetro de query e endpoint devolver só entradas ou só saídas por
    chamada → rejeitado: exigiria uma nova chamada de API a cada clique no filtro, violando o
    espírito de SC-003 ("sem recarregar") e duplicando lógica de filtro de período/turma/matéria
    /aluno que já existe para a chamada combinada.
  - Dois endpoints separados (`/entradas`, `/saidas`) → rejeitado: mesma desvantagem acima, e
    fragmenta o que hoje é uma única resposta coesa (`IndicadoresFinanceirosFiltradosResponse`)
    usada por uma única tela.

## 2. Como obter a lista individual de entradas e saídas no backend

- **Decision**: Dois métodos novos em `IRelatorioRepository`/`RelatorioRepository` — análogos a
  `ObterEntradasPorDiaDoMesAsync`/`ObterSaidasPorDiaDoMesAsync` (mesmo filtro de período por
  `DataVencimento`, mesmo filtro de status `!= "Cancelado"`, mesmo `AplicarFiltroReceita` para
  turma/matéria/aluno no lado das entradas), mas retornando o registro individual (id,
  descrição, aluno/favorecido, data de vencimento, valor) em vez de agrupar por dia/soma.
  `RelatorioService.ObterIndicadoresFinanceirosAsync` chama os dois métodos, aplica o fallback de
  descrição (nome do aluno / favorecido-categoria quando a descrição estiver vazia), unifica em
  uma lista só marcada por `tipo` ("Entrada"/"Saída"), ordena por data de vencimento decrescente
  (FR-011) e inclui no novo campo da resposta.
- **Rationale**: Reaproveita exatamente o padrão de filtro já centralizado e testado
  implicitamente pelos métodos vizinhos (`AplicarFiltroReceita`), em vez de reimplementar a regra
  de turma/matéria/aluno em outro lugar — atende ao Princípio II da constituição (validação de
  negócio única e centralizada). Não requer nenhuma consulta nova a tabelas fora de `Pagamentos`
  e `ContasPagar`, ambas já mapeadas e usadas pelo repositório.
- **Alternatives considered**:
  - Reaproveitar `ObterEntradasPorDiaDoMesAsync`/`ObterSaidasPorDiaDoMesAsync` existentes e
    "desagregar" no serviço → rejeitado: esses métodos já retornam apenas `(Dia, Valor)`
    agregados por `GroupBy`, perdendo a descrição/id individual exigida pela FR-002; não há como
    recuperar o lançamento original a partir do resultado agregado.
  - Trazer os `Pagamento`/`ContaPagar` completos (entidades EF) até o controller → rejeitado:
    quebraria a convenção do projeto de expor somente DTOs de resposta (`*Response.cs`) a
    partir do serviço/controller, e exporia campos internos (ids de forma de pagamento, etc.)
    sem necessidade.

## 3. Reaproveitar a classe `financeiro-tabs` como wrapper dos botões de filtro?

- **Decision**: Os botões "Entradas"/"Saídas" reaproveitam apenas as classes de botão
  `btn btn-sm btn-primary`/`btn-ghost` (mesmo padrão visual de cor/tamanho corrigido em
  [021-fix-hitbox-cliques](../021-fix-hitbox-cliques/spec.md)). Eles **não** são envolvidos pelo
  wrapper `.financeiro-tabs` — usam apenas um contêiner flex simples (`display: flex; gap: 8px`)
  para o espaçamento entre os dois botões.
- **Rationale**: `.financeiro-tabs` (`frontend/styles/dashboard.css:251-258`) define
  `display:flex; align-items:stretch` no contêiner e, mais importante, `> a { flex: 1 1 0;
  justify-content: center; }` — uma regra que só se aplica a filhos `<a>`, pensada para os três
  `<Link>` de navegação de página inteira do menu do Financeiro (ocupando 100% da largura do
  menu). Os botões de filtro de tipo desta feature são `<button>` locais (sem navegação, sem
  troca de rota) dentro do cabeçalho de uma tabela — não precisam nem devem se esticar para
  ocupar toda a largura do painel. Aplicar a classe teria efeito nulo sobre os botões (a regra
  `> a` não os alcançaria) e mudaria a semântica de "wrapper de navegação" para um uso que não é
  navegação, então o requisito (FR-004) foi reescrito para exigir apenas o padrão visual de
  botão, não o wrapper.
- **Alternatives considered**:
  - Estender `.financeiro-tabs` para também estilizar `> button` → rejeitado: mudaria o
    comportamento de um seletor já usado especificamente para o menu de 3 abas de navegação,
    arriscando efeito colateral em outro lugar caso a classe seja reaproveitada futuramente para
    outro grupo de botões que não deva se esticar.
  - Criar uma classe nova dedicada (`.tipo-lancamento-tabs`) só para replicar o mesmo
    `display:flex` → rejeitado como desnecessário: um `style={{ display: "flex", gap: 8 }}`
    inline é suficiente para dois botões, sem introduzir uma classe CSS nova de uso único.

## 4. Nome/descrição do lançamento quando o campo `Descricao` está vazio

- **Decision**: Já registrado no spec (Clarifications, 2026-09-16): quando `Pagamento.Descricao`
  ou `ContaPagar.Descricao` for nulo/vazio, o backend substitui pelo nome do aluno vinculado
  (entrada) ou pelo `Favorecido` (ou, na ausência dele, o nome da `CategoriaDespesa`) da conta a
  pagar (saída). Essa composição é feita no backend (no DTO de resposta), não no frontend, para
  manter a regra de "o que aparece como descrição" centralizada em um único lugar (Princípio II).
- **Rationale**: Ver Clarifications do spec — decisão já tomada com o usuário; aqui apenas se fixa
  a camada (backend) responsável pela composição do texto final.
