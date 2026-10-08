# Quickstart: validar os lançamentos clicáveis na tabela de Indicadores

Guia de validação ponta a ponta. Modelo de dados reutilizado em [data-model.md](./data-model.md); decisões técnicas em [research.md](./research.md).

## Pré-requisitos

- Backend e frontend rodando; login feito (qualquer perfil que acesse Relatórios → Relatório Financeiro).
- Pelo menos uma entrada (Pagamento) e uma saída (Conta a Pagar) com vencimento dentro de um período fácil de filtrar, ambas com status diferente de "Cancelado".

## 1. Clicar numa linha de entrada abre o detalhe certo (US1, FR-002, FR-005, SC-001/SC-003)

| # | Passo | Esperado |
|---|---|---|
| 1 | Ir em Relatórios → Relatório Financeiro → aba Indicadores, com uma entrada visível na tabela de lançamentos | Linha visível, com descrição e data |
| 2 | Passar o mouse sobre a linha, sem clicar | Cursor muda para ponteiro; linha destaca visualmente (hover) — mesmo padrão de Contas a Pagar |
| 3 | Clicar em qualquer ponto da linha | Navega para `/financeiro/contas-a-receber/{id}`, mostrando o mesmo pagamento (confirmar nome/valor/data coincidem com a linha clicada) |

## 2. Clicar numa linha de saída abre o detalhe certo (US2, FR-003, FR-005, SC-002/SC-003)

| # | Passo | Esperado |
|---|---|---|
| 1 | Na mesma tabela, localizar uma linha de saída | Linha visível |
| 2 | Clicar na linha | Navega para `/financeiro/contas-a-pagar/{id}`, mostrando a mesma conta |

## 3. Preservação de filtros ao voltar — verificação empírica (US3, FR-009)

| # | Passo | Esperado |
|---|---|---|
| 1 | Aplicar um filtro de período (datas específicas) na aba Indicadores, e um filtro de turma/aluno se houver dados | Tabela de lançamentos reflete o filtro |
| 2 | Clicar num lançamento (entrada ou saída) | Abre o detalhe correspondente (cenários 1/2) |
| 3 | Voltar usando o botão/gesto "voltar" do navegador | **Registrar o resultado observado**: os filtros de período/turma/aluno continuam aplicados, OU voltaram ao estado padrão. Qualquer um dos dois é um resultado válido (FR-009) — mas o resultado real observado MUST ser anotado em `research.md` (atualizando R1 com "confirmado: preservado" ou "confirmado: resetado, limitação aceita") |

## 4. Não-regressão (FR-007, FR-008, SC-004, SC-005)

- Os botões "Entradas"/"Saídas" continuam filtrando a tabela exatamente como antes (spec 022) — clicar neles não deve ser interceptado pelo clique da linha.
- O cálculo e a ordenação da tabela (mais recente primeiro) permanecem idênticos.
- Os demais indicadores da aba (KPIs, fluxo de caixa, prazo médio de atraso) continuam inalterados.
- `git diff --stat -- src frontend/lib database` vazio (nenhuma mudança fora de `frontend/app/(app)/relatorios/financeiro/page.tsx`) — confirma FR-006 (nenhuma mudança de backend).
