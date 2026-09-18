# Quickstart: Validar a remoção do índice de cobertura de custos

## Pré-requisitos

- Backend rodando localmente (`dotnet run` em `src/SPI.Api`), com o banco MySQL de dev acessível
  (nenhuma migração nova é necessária para esta feature — ver [data-model.md](./data-model.md)).
- Uma credencial válida para autenticar (`POST /api/auth/login`) e obter um `accessToken`. **Não
  usar/criar credenciais de teste persistentes** — pedir uma credencial temporária no momento do
  teste, conforme a regra de trabalho combinada para este projeto.

## Cenário 1 — Campo ausente em `GET /api/dashboard` (User Story 1, Acceptance Scenario 1)

1. Autenticar e obter `accessToken`.
2. Chamar `GET /api/dashboard` (com ou sem `periodoInicio`/`periodoFim`).
3. **Esperado**: no objeto `indicadores` da resposta, a chave `indiceCoberturaCustosFixos` não
   existe mais (nem como `null`, nem com qualquer valor) — comparar com a estrutura documentada em
   [data-model.md](./data-model.md).

## Cenário 2 — Campo ausente em `GET /api/relatorios/indicadores-financeiros` (Acceptance Scenario 2)

1. Com o mesmo `accessToken`, chamar `GET /api/relatorios/indicadores-financeiros`, testando pelo
   menos uma combinação com filtro (`turmaId`, `materiaId` ou `alunoId`) além da chamada sem
   filtro.
2. **Esperado**: mesma ausência do campo em `indicadores`, em todas as combinações testadas.

## Cenário 3 — Nenhum outro campo muda (Acceptance Scenario 3 / SC-002)

1. Comparar, antes e depois da mudança, os valores de `taxaInadimplenciaPercentual`,
   `prazoMedioAtrasoDias`, `margemSegurancaPercentual`, `fluxoCaixaOperacional` e `gargaloCaixa`
   nas duas respostas acima, para o mesmo período/filtros.
2. **Esperado**: valores idênticos — só o campo removido muda (deixa de existir).

## Cenário 4 — Nenhuma referência residual no código (SC-003)

1. Buscar por `IndiceCoberturaCustosFixos` (backend) e `indiceCoberturaCustosFixos` (frontend) em
   todo o repositório.
2. **Esperado**: zero ocorrências em código-fonte (`src/`, `frontend/`, `tests/`). Referências em
   `specs/*.md` que documentam o histórico da mudança (como esta spec e a 015 atualizada) são
   esperadas e não contam como "residuais".

## Validação automatizada

Não há teste de unidade a atualizar (confirmado em [research.md — R2](./research.md#r2--nenhum-teste-automatizado-cobre-o-campo-hoje)):
a validação desta feature é por **compilação** (o campo deixa de existir no tipo C#, então
qualquer uso residual quebra o build) mais os 4 cenários manuais acima.
