# Quickstart: validar o novo visual do alternador Visão Financeiro/Indicadores

Guia de validação ponta a ponta. Decisões técnicas em [research.md](./research.md).

## Pré-requisitos

- Frontend rodando (`npm run dev` em `frontend/`), login feito.

## 1. Visual igual ao de Contas a Receber/Pagar (US1, FR-001, SC-001)

| # | Passo | Esperado |
|---|---|---|
| 1 | Abrir Relatórios → Relatório Financeiro | O alternador "Visão Financeiro"/"Visão de Indicadores" ocupa a largura total da área, os 2 botões do mesmo tamanho, colados um ao outro |
| 2 | Abrir Financeiro → Contas a Receber (ou Contas a Pagar) em outra aba/janela | Comparar lado a lado: o padrão de layout (largura total, divisão igual, sem espaço entre os botões) é o mesmo nos dois |

## 2. Comportamento de clique inalterado (US1, FR-002, SC-002)

| # | Passo | Esperado |
|---|---|---|
| 1 | Na tela de Relatório Financeiro, clicar em "Visão de Indicadores" | Troca para o conteúdo de indicadores, sem navegar para outra URL (continua em `/relatorios/financeiro`) |
| 2 | Aplicar um filtro (ex.: período), navegar para outra tela e voltar | Filtro e aba selecionada continuam como estavam (persistência de specs/048 intacta) |
| 3 | Clicar em "Visão Financeiro" | Volta para o conteúdo financeiro normalmente |

## 3. Não-regressão nas abas de referência (FR-003, SC-003)

| # | Passo | Esperado |
|---|---|---|
| 1 | Abrir Financeiro → Visão Geral / Contas a Receber / Contas a Pagar | As 3 abas continuam com o visual e comportamento de sempre — nenhuma mudança perceptível |

## 4. Responsividade (Edge Case)

| # | Passo | Esperado |
|---|---|---|
| 1 | Reduzir a largura da janela (ou emular um celular) na tela de Relatório Financeiro | O alternador continua esticando os 2 botões proporcionalmente, legível e clicável, sem quebrar o layout |
