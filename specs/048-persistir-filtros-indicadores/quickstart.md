# Quickstart: validar a persistência dos filtros de Indicadores

Guia de validação ponta a ponta. Modelo de dados reutilizado em [data-model.md](./data-model.md); decisões técnicas em [research.md](./research.md).

## Pré-requisitos

- Backend e frontend rodando; login feito; dados suficientes para aplicar um período com pelo menos uma entrada e uma saída, e para filtrar por turma/aluno.

## 1. Filtros restaurados ao voltar do detalhe (US1, FR-001 a FR-003, SC-001)

| # | Passo | Esperado |
|---|---|---|
| 1 | Ir em Relatórios → Relatório Financeiro, trocar para "Visão de Indicadores", aplicar um período e um filtro de turma/aluno | Tabela de lançamentos e indicadores refletem os filtros |
| 2 | Clicar em um lançamento (entrada ou saída) | Abre o detalhe correspondente (spec 045, inalterado) |
| 3 | Voltar pelo botão do navegador | Volta para a página de Relatório Financeiro, na aba **"Visão de Indicadores"** (não "Visão Financeiro"), com o período e o filtro de turma/aluno **ainda aplicados**, e os números batendo com esses filtros |
| 4 | Repetir aplicando também o filtro "Entradas" ou "Saídas" na tabela de lançamentos antes de clicar | Esse filtro de tipo também continua aplicado ao voltar |

## 2. Limpar todos os filtros de uma vez (US2, FR-005/FR-006, SC-002)

| # | Passo | Esperado |
|---|---|---|
| 1 | Com algum filtro aplicado (período, turma, matéria, aluno, forma de pagamento, ou só o filtro Entradas/Saídas) | Botão "Limpar filtros" visível/ativo |
| 2 | Clicar em "Limpar filtros" | Todos os filtros (incluindo Entradas/Saídas) voltam ao padrão; dados recarregados sem filtro |
| 3 | Observar a área de filtros depois de limpar | Botão "Limpar filtros" desaparece (ou fica inativo) |
| 4 | Sem aplicar nenhum filtro novo, clicar num lançamento e voltar | Página continua sem filtro — o estado "limpo" também foi persistido |

## 3. Sem filtro aplicado, nada muda (US1, cenário 4)

| # | Passo | Esperado |
|---|---|---|
| 1 | Sem nenhum filtro aplicado, clicar num lançamento e voltar | Página continua sem filtro, comportamento idêntico ao de antes desta feature |

## 4. Não-regressão e verificação (FR-007 a FR-011, SC-003/SC-004/SC-005)

- A tela de detalhe de Entradas/Saídas (`/financeiro/contas-a-pagar/{id}`, `/financeiro/contas-a-receber/{id}`) continua mostrando exatamente o que mostrava antes (spec 045 inalterada).
- Fechar o navegador (ou abrir a página numa aba completamente nova) e acessar a Visão de Indicadores de novo: filtros no padrão, não os últimos usados (confirma FR-004 — `sessionStorage`, não `localStorage`).
- `git status --porcelain` nas pastas do frontend não mostra nenhum arquivo novo (regra do `CLAUDE.md` para FR-009 "MUST NOT exigir mudança de API/backend" e para não introduzir componente/arquivo novo).
- Ler o diff de `relatorios/financeiro/page.tsx`: confirmar que os dois `useEffect` novos só chamam `setState` com valores lidos/serializados de `sessionStorage` — nenhuma chamada de API nova, nenhum cálculo duplicado (os `useEffect` de busca de dados já existentes continuam sendo os únicos a chamar `obterRelatorioFinanceiro`/`obterIndicadoresFinanceiros`) (regra do `CLAUDE.md` para FR-008 "MUST NOT recalcular no frontend").
- `git diff --stat` mostra só `frontend/app/(app)/relatorios/financeiro/page.tsx` — nenhuma mudança em `src`/`database`/`contas-a-pagar`/`contas-a-receber`.
