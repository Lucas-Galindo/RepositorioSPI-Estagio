# Quickstart: Remover Card "Cobertura de Custos" da Aba Indicadores

Guia de validação manual — o frontend deste projeto não tem framework de teste automatizado
configurado (ver plan.md Technical Context).

## Pré-requisitos

- Backend (`SPI.Api`) rodando e acessível pelo frontend.
- Frontend (`frontend/`) rodando em modo dev (`npm run dev` ou equivalente).
- Login como usuário com perfil Professor.
- Pelo menos um período com dados financeiros (Contas a Pagar e a Receber) para que a aba
  Indicadores não fique no estado vazio — mas repita os cenários também sem dados (Cenário 3).

## Cenário 1 — Card ausente com dados (User Story 1)

1. Acessar Relatórios → Relatório Financeiro.
2. Abrir a aba "Indicadores".
3. **Esperado**: a fileira de indicadores mostra exatamente 3 cards — "Inadimplência", "Prazo
   médio de atraso" e "Margem de segurança". O card "Cobertura de custos" não aparece em lugar
   nenhum da tela.
4. Alterar os filtros de período, turma, matéria e aluno (individualmente e combinados).
5. **Esperado**: em toda combinação de filtro, o card "Cobertura de custos" continua ausente, e
   os outros 3 cards continuam atualizando seus valores normalmente.

## Cenário 2 — Layout equilibrado (User Story 2)

1. Com a aba Indicadores aberta (Cenário 1), observar a fileira dos 3 cards restantes em uma
   janela desktop larga (≥1200px).
2. **Esperado**: os 3 cards têm a mesma largura entre si e preenchem toda a largura da fileira,
   sem espaço vazio à direita nem cards desproporcionais.
3. Redimensionar a janela (ou usar as ferramentas de dispositivo móvel do navegador) para uma
   largura menor (≤768px).
4. **Esperado**: os 3 cards continuam legíveis e organizados, seguindo o mesmo comportamento
   responsivo já visto em outras fileiras de indicadores do sistema (ex.: aba Visão Geral do
   próprio Relatório Financeiro, ou o Dashboard).

## Cenário 3 — Estados sem dados e de erro

1. Selecionar um período/filtro sem nenhum lançamento financeiro.
2. **Esperado**: a aba Indicadores exibe seu estado vazio normal (se aplicável), sem qualquer
   referência ao card "Cobertura de custos".
3. (Se reproduzível) Forçar um erro de carregamento (ex.: backend indisponível momentaneamente).
4. **Esperado**: a tela exibe o estado de erro já existente (`EmptyState` "Não foi possível
   carregar"), sem qualquer referência ao card removido.

## Cenário 4 — Não regressão em outras telas (FR-005)

1. Acessar o Dashboard (tela inicial).
2. **Esperado**: se o Dashboard já exibia um indicador equivalente a "Cobertura de custos"
   (`indiceCoberturaCustosFixos`), ele continua exatamente como estava antes desta mudança —
   esta feature não toca o Dashboard nem o backend compartilhado.
3. Acessar as demais abas do Relatório Financeiro (Visão Geral, se existir).
4. **Esperado**: nenhuma outra aba ou relatório foi alterado por esta mudança.

## Critério de aceite final

Todos os 4 cenários acima passam sem nenhuma referência visual, textual ou funcional ao card
"Cobertura de custos" dentro da aba Indicadores do Relatório Financeiro, e nenhuma outra tela do
sistema apresenta regressão.
