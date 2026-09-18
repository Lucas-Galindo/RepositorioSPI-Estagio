# Quickstart: Validar a remoção do indicador "Margem de Segurança %"

## Pré-requisitos

- Backend rodando localmente (`dotnet run` em `src/SPI.Api`) e frontend rodando localmente
  (`npm run dev` em `frontend/`), com o banco MySQL de dev acessível (nenhuma migração nova —
  ver [data-model.md](./data-model.md)).
- Uma credencial válida para autenticar. **Não usar/criar credenciais de teste persistentes** —
  pedir uma credencial temporária no momento do teste, conforme a regra de trabalho combinada.

## Cenário 1 — Card ausente na tela (User Story 1, Acceptance Scenarios 1-2)

1. Acessar Relatórios → Relatório Financeiro → aba "Indicadores".
2. **Esperado**: apenas 2 cards aparecem na primeira fileira — "Inadimplência" e "Prazo médio de
   atraso" — sem nenhum card "Margem de segurança".
3. Alterar os filtros (período, turma, matéria, aluno) e repetir a checagem.
4. **Esperado**: o card continua ausente em qualquer combinação de filtros/estado de dados
   (com dados, sem dados, carregando, erro).

## Cenário 2 — Layout sem vão vazio (Acceptance Scenario 3 / SC-002)

1. Com a tela do Cenário 1 aberta, observar a largura dos 2 cards restantes em desktop.
2. **Esperado**: os 2 cards preenchem toda a largura da fileira, com a mesma largura entre si,
   sem espaço vazio perceptível no lugar do terceiro card removido.
3. Repetir em uma largura de tela menor (mobile/tablet).
4. **Esperado**: os 2 cards continuam legíveis e organizados, seguindo o mesmo padrão responsivo
   já usado pelas demais fileiras de indicadores do sistema.

## Cenário 3 — Campo ausente em `GET /api/dashboard` (User Story 2, Acceptance Scenario 1)

1. Autenticar e obter `accessToken`.
2. Chamar `GET /api/dashboard`.
3. **Esperado**: no objeto `indicadores` da resposta, a chave `margemSegurancaPercentual` não
   existe mais.

## Cenário 4 — Campo ausente em `GET /api/relatorios/indicadores-financeiros` (Acceptance Scenario 2)

1. Chamar `GET /api/relatorios/indicadores-financeiros`, com e sem filtros (`turmaId`,
   `materiaId`, `alunoId`).
2. **Esperado**: mesma ausência do campo em `indicadores`, em todas as combinações testadas.

## Cenário 5 — Nenhum outro indicador muda (Acceptance Scenario 3 / SC-004)

1. Comparar, antes e depois da mudança, os valores de `taxaInadimplenciaPercentual`,
   `prazoMedioAtrasoDias`, `fluxoCaixaOperacional` e `gargaloCaixa` nas duas respostas acima, e os
   cards "Inadimplência"/"Prazo médio de atraso" na tela.
2. **Esperado**: valores idênticos — só o indicador removido some.

## Cenário 6 — Nenhuma referência residual no código (SC-005)

1. Buscar por `MargemSegurancaPercentual` (backend) e `margemSegurancaPercentual` (frontend) em
   todo o repositório.
2. **Esperado**: zero ocorrências em código-fonte (`src/`, `frontend/`, `tests/`). Referências em
   `specs/*.md` que documentam o histórico da mudança são esperadas e não contam como residuais.

## Validação automatizada

Não há teste de unidade a atualizar (confirmado em [research.md — R4](./research.md#r4--nenhum-teste-automatizado-cobre-o-indicador-hoje)):
a validação desta feature é por **compilação** (backend) mais os 6 cenários manuais acima
(frontend visual + payload HTTP).
