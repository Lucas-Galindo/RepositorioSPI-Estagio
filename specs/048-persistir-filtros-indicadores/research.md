# Research: Persistir Filtros da Visão de Indicadores

Decisões técnicas da Phase 0. O mecanismo de persistência (`sessionStorage`, não URL/`localStorage`) já foi decidido e justificado no próprio `spec.md`; esta fase detalha os pontos de implementação que faltavam.

## R1. Formato do valor salvo: um único objeto JSON, uma única chave

- **Decision**: `sessionStorage.setItem("spi:filtros-relatorio-financeiro", JSON.stringify({ aba, inicio, fim, formaPagamentoId, alunoId, turmaId, materiaId, semestre, tipoLancamento }))` — um objeto só, com os 9 valores, numa única chave. Leitura: `JSON.parse(sessionStorage.getItem(chave))`, espalhando os campos de volta nos `setState` correspondentes.
- **Rationale**: é exatamente o padrão já usado por `ThemeContext.tsx` (`localStorage.setItem(STORAGE_KEY, JSON.stringify(novo))` com um objeto `Preferencias` único) — uma chave por página/contexto, um objeto serializado, em vez de 9 chaves separadas. Mais simples de ler/escrever/limpar como unidade, e evita 9 chamadas de `sessionStorage` a cada mudança.
- **Alternatives considered**: uma chave por filtro (`spi:filtro-inicio`, `spi:filtro-fim`, ...) — descartado, mais verboso para ganho nenhum; não há necessidade de ler/escrever um filtro isoladamente dos demais.

## R2. Dois `useEffect` novos: leitura após o mount, gravação a cada mudança

- **Decision**:
  1. Um `useEffect(() => {...}, [])` (roda uma vez, após o mount) que lê a chave do `sessionStorage`, faz `JSON.parse`, e chama os 9 `setState` correspondentes — dentro de `try/catch`, com o mesmo comentário de `ThemeContext.tsx` sobre não usar isso como inicializador preguiçoso do `useState` (quebraria a hidratação SSR) e o mesmo `// eslint-disable-next-line react-hooks/set-state-in-effect` onde necessário.
  2. Um `useEffect(() => {...}, [aba, inicio, fim, formaPagamentoId, alunoId, turmaId, materiaId, semestre, tipoLancamento])` que serializa e grava o objeto atual no `sessionStorage` a cada mudança de qualquer um desses 9 valores — também dentro de `try/catch`.
- **Rationale**: replica exatamente a dupla leitura-após-mount / gravação-a-cada-mudança já estabelecida por `ThemeContext.tsx`, só trocando `localStorage` por `sessionStorage` e adaptando para 9 campos em vez de 1 objeto `Preferencias`. Como os `useEffect` de busca de dados já reagem a 6 desses mesmos valores (`inicio`, `fim`, `formaPagamentoId`, `alunoId`, `turmaId`, `materiaId`), assim que o efeito de leitura (1) chama os `setState`, os efeitos de busca já existentes disparam sozinhos com os valores restaurados — nenhuma chamada de API nova é escrita por esta feature.
- **Tradeoff aceito (mesmo já aceito por `ThemeContext.tsx`)**: no primeiro render após o mount, os `useEffect` de busca já rodam uma vez com os valores padrão (vazios), antes do efeito de leitura do `sessionStorage` ter a chance de rodar e restaurar os valores reais — resultando em uma busca extra, descartável, quando há algo para restaurar (os contadores `requisicaoAtual`/`requisicaoIndicadoresAtual` já existentes, que descartam respostas antigas fora de ordem, tornam esse descarte seguro). É o mesmo "flash do valor padrão antes do salvo" que `ThemeContext.tsx` já aceita para o tema — não vale a pena uma complexidade maior (ex.: gate de carregamento) só para evitar uma busca extra imperceptível.
- **Alternatives considered**: um `useReducer`/contexto dedicado para os filtros — descartado, desproporcional a 9 campos já geridos por `useState` simples; a mesma dupla de `useEffect` já resolve sem nenhuma reestruturação do estado existente.

## R3. Restaurar `aba` não exige nenhuma mudança além do `setAba`

- **Decision**: `aba` entra no mesmo objeto salvo/restaurado (R1/R2) — nenhum tratamento especial.
- **Rationale**: o comentário já existente no código confirma que os dados de ambas as abas são buscados em paralelo, independente de `aba` (`// Indicadores buscados em paralelo, independente da aba ativa, pra troca de aba ser instantanea`) — ou seja, restaurar `aba` é só trocar qual bloco já renderizado fica visível, sem nenhum efeito colateral de busca adicional.
- **Alternatives considered**: nenhuma — não há decisão real a tomar aqui.

## R4. `temFiltro`/`limparFiltros`: estender para incluir `tipoLancamento`

- **Decision**: `const temFiltro = Boolean(inicio || fim || formaPagamentoId || alunoId || turmaId || materiaId || tipoLancamento !== "todos")`; `limparFiltros` ganha mais uma linha, `setTipoLancamento("todos")`.
- **Rationale**: FR-005/FR-006 pedem que o botão já existente cubra também o filtro Entradas/Saídas, que hoje fica de fora dessas duas funções — mudança de uma linha em cada, no mesmo lugar onde os outros 6 filtros já são tratados.
- **Alternatives considered**: nenhuma — é a extensão mínima e direta do que já existe.

## R5. Nenhuma mudança nas telas de detalhe

- **Decision**: `contas-a-pagar/[id]`/`contas-a-receber/[id]` continuam recebendo só o `id` pela rota, como a spec 045 já estabeleceu — nenhum deles lê ou escreve `sessionStorage`.
- **Rationale**: a persistência vive inteiramente do lado da página de origem (Indicadores); o detalhe nunca precisou saber dos filtros da origem para mostrar o registro certo, e continua não precisando (FR-007).
- **Alternatives considered**: nenhuma — já confirmado pela spec 045 e reafirmado aqui.
