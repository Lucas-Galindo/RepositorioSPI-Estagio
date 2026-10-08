# Research: Lançamentos Clicáveis na Tabela de Indicadores

Decisões técnicas da Phase 0. O único ponto não trivial do Technical Context é a preservação de filtros ao voltar (FR-009) — todo o resto já estava resolvido pela investigação prévia registrada no próprio `spec.md`.

## R1. Preservação de filtros ao voltar: não sincronizar com a URL; reaproveitar o comportamento de navegação já existente

- **Decision**: não introduzir nenhum mecanismo novo de persistência de filtro (nem query params na URL, nem `sessionStorage`, nem contexto React) para a aba Indicadores. A navegação da linha para o detalhe usa exatamente o mesmo `router.push` já usado em `contas-a-pagar/page.tsx` — e o retorno ("voltar") depende do mesmo comportamento de navegação que o Next.js App Router já tem hoje para qualquer fluxo lista/relatório → detalhe → voltar no sistema, sem nenhuma mudança de arquitetura introduzida por esta feature.
- **Rationale**: os filtros da aba Indicadores (`inicio`, `fim`, `turmaId`, `materiaId`, `alunoId`, `semestre`, `tipoLancamento`) são hoje 7 `useState` locais, nenhum refletido na URL. Sincronizar todos eles com query params exigiria reescrever o gerenciamento de estado de uma página inteira já funcional, só para um requisito `SHOULD` de prioridade P3 — desproporcional (viola o princípio de não adicionar complexidade além do necessário). A spec já previu esse exato cenário e aceitou o fallback (FR-009: "quando não for viável... o comportamento de fallback é aceitável").
- **Verificação empírica necessária na implementação**: como o comportamento real de preservação de estado ao voltar depende do cache de rota do Next.js App Router (que pode variar por versão/configuração e não é algo que valha a pena deduzir só por leitura de documentação), a tarefa de validação manual desta feature (`tasks.md`) MUST incluir o teste real: aplicar um filtro, clicar num lançamento, voltar pelo botão/gesto do navegador, e observar se os filtros continuam aplicados. O resultado observado (preservado ou resetado) é documentado como fato, não como falha — ambos os resultados são aceitáveis pela spec.
- **Alternatives considered**:
  - Refletir todos os filtros na URL (`?inicio=...&fim=...&turmaId=...`): descartado por desproporcionalidade (reescrita de 7 `useState` + lógica de parse/sync bidirecional, para um requisito P3 com fallback já aceito).
  - Guardar o estado dos filtros em `sessionStorage` antes de navegar e restaurar ao montar a página: descartado pelo mesmo motivo de desproporcionalidade, além de introduzir uma segunda fonte de verdade para o mesmo estado (risco de divergência entre `sessionStorage` e o `useState` atual).
  - Adicionar um botão "voltar" específico na tela de detalhe que levasse de volta ao relatório (em vez de depender do botão do navegador): descartado — alteraria as telas de detalhe de Contas a Pagar/Receber, que o FR-004 proíbe tocar além da navegação de entrada vinda desta feature.

## R2. Mecanismo de navegação: reaproveitar `router.push` + classe `row-link`, sem nenhuma abstração nova

- **Decision**: usar exatamente o padrão já existente em `frontend/app/(app)/financeiro/contas-a-pagar/page.tsx` (linha ~196): `<tr className="row-link" onClick={() => router.push(rota)}>`. Para uma linha de entrada (`lancamento.tipo === "Entrada"`), `rota = /financeiro/contas-a-receber/${lancamento.id}`; para uma saída (`lancamento.tipo === "Saida"`), `rota = /financeiro/contas-a-pagar/${lancamento.id}`.
- **Rationale**: é literalmente o mesmo padrão pedido no requisito original (FR-005), já implementado e estilizado (CSS `row-link` já cobre cursor + hover, confirmado em `styles_dashboard_css`). Não há motivo para criar uma função/componente auxiliar nova para uma lógica de 2 linhas (`if/else` de rota por tipo).
- **Alternatives considered**: extrair um helper `obterRotaDetalhe(lancamento)` reutilizável — descartado por ser a única tabela do sistema que precisa escolher a rota por tipo de registro; introduzir uma abstração para um único ponto de uso é complexidade sem benefício concreto hoje.

## R3. Nenhuma mudança de contrato de API (FR-006) — já confirmado pela investigação prévia do spec

- **Decision**: nenhuma alteração em `LancamentoIndicadorItem`, `RelatorioService`, `RelatorioRepository`, ou no endpoint de indicadores filtrados.
- **Rationale**: `Id` (int, do `Pagamento`/`ContaPagar` de origem) e `Tipo` (`"Entrada"`/`"Saida"`) já existem na resposta e já são corretos (confirmado em `RelatorioService.cs`, linhas ~270-292, e usados hoje como `key` da linha: `` `${lancamento.tipo}-${lancamento.id}` ``).
- **Alternatives considered**: nenhuma — não havia decisão real a tomar aqui, só confirmação.

## R4. Estratégia de testes

- **Decision**: nenhum teste automatizado novo (nem backend — não há mudança de backend — nem frontend, mesma decisão já aceita nas specs 020-044 para mudanças de UI). Validação via `quickstart.md`, incluindo a verificação empírica de R1.
- **Rationale**: a mudança é puramente de apresentação/navegação num componente React sem lógica de negócio nova; o projeto não tem suíte de testes de frontend configurada (confirmado nas specs anteriores).
