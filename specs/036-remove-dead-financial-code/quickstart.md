# Quickstart: Validar a remoção de código morto do módulo Financeiro

## Pré-requisitos

- Backend rodando localmente (`dotnet run` em `src/SPI.Api`) para a validação manual opcional.
- Uma credencial válida para autenticar, se for validar manualmente — pedir uma credencial
  temporária no momento do teste, conforme a regra de trabalho combinada; não criar/usar
  credenciais de teste persistentes.

## Validação automatizada (principal)

1. Rodar `dotnet build` e confirmar que o projeto compila sem erros após ambas as remoções
   (confirma que nenhuma referência a `ObterIndicadoresFinanceirosAsync`/
   `ObterFluxoCaixaMensalAsync`/`ObterValorAPagarAsync` ficou órfã).
2. Rodar `dotnet test` (suíte completa) e confirmar 100% de aprovação (FR-006, SC-004) — em
   especial os testes que hoje cobrem `RelatorioService.ObterIndicadoresFinanceirosAsync` (aba
   Indicadores), que devem continuar passando sem nenhuma mudança de resultado, já que os DTOs e
   métodos de repositório que ela usa não foram tocados (SC-003).
3. Buscar no código-fonte por `ObterIndicadoresFinanceirosAsync` (deve aparecer só em
   `RelatorioService.cs`/`IRelatorioService.cs`, nunca mais em `DashboardService.cs`),
   `ObterFluxoCaixaMensalAsync` e `ObterValorAPagarAsync` (nenhuma ocorrência deve restar) —
   confirma SC-005.

## Validação manual (complementar, contra dados reais)

4. Autenticar e obter `accessToken`.
5. Chamar `GET /api/dashboard` e confirmar que a resposta não tem mais os campos `indicadores`
   nem `fluxoCaixaMensal`, e que os demais campos (`alunosAtendidosNoPeriodo`,
   `valorFaturadoNoPeriodo`, etc.) continuam presentes com valores plausíveis.
6. Abrir a tela Home (`/dashboard`) no navegador e confirmar que os cards "Alunos atendidos" e
   "Recebido este mês" continuam exibindo valores corretos, sem erro no console.
7. Abrir o Relatório Financeiro → aba "Indicadores" e confirmar que "Inadimplência", "Prazo
   médio de atraso" e "Fluxo de caixa (últimos 6 meses)" continuam funcionando normalmente —
   endpoint/implementação não tocados por esta feature (SC-003).

## Regressão

O passo 2 (suíte completa) já cobre a regressão — como a remoção não altera nenhum método de
repositório compartilhado nem nenhum DTO reaproveitado (research.md — R2), não há superfície
adicional a testar além da compilação e dos testes existentes.
