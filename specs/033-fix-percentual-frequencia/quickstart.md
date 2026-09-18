# Quickstart: Validar a correção de "% Frequência do Aluno"

## Pré-requisitos

- Backend rodando localmente (`dotnet run` em `src/SPI.Api`), com o banco MySQL de dev acessível
  (nenhuma migração nova — ver [data-model.md](./data-model.md)).
- Uma credencial válida para autenticar. **Não usar/criar credenciais de teste persistentes** —
  pedir uma credencial temporária no momento do teste, conforme a regra de trabalho combinada.

## Validação automatizada (principal)

Diferente das features anteriores desta sessão, esta tem cobertura de teste de unidade nova
(ver [research.md — R4](./research.md#r4--nenhum-teste-automatizado-cobre-este-método-hoje)).
Rodar `dotnet test --filter RelatorioServiceHistoricoAlunoTests` cobre, no mínimo:

1. Aluno com Frequência vitalícia alta (ex.: 50), mas poucas presenças reais dentro de um filtro
   de período restrito (ex.: 8 de 10 aulas Realizadas no período) → `PercentualFrequencia` = 80%,
   não 500%.
2. Mesmo cenário com filtro de `turmaId` (sem período) → mesmo comportamento corrigido.
3. Mesmo cenário com filtro de `status` (sem período nem turma) → mesmo comportamento corrigido.
4. Nenhum filtro aplicado → `PercentualFrequencia` continua usando `Aluno.Frequencia` (comportamento
   inalterado).
5. Filtro aplicado sem nenhuma aula `Realizada` no resultado → `PercentualFrequencia` = 0% (sem
   divisão por zero).

## Validação manual (complementar, contra dados reais)

1. Autenticar e obter `accessToken`.
2. Identificar um aluno com histórico de presenças anterior ao mês corrente (ou registrar uma
   nova sessão de aula como `Realizada` com presença, se necessário, para ter pelo menos uma
   presença fora do período que será filtrado).
3. Chamar `GET /api/relatorios/historico-aluno/{alunoId}` sem filtro de período.
   - **Esperado**: `percentualFrequencia` calculado como hoje (`Aluno.Frequencia` ÷ aulas
     Realizadas totais).
4. Chamar `GET /api/relatorios/historico-aluno/{alunoId}?inicio=...&fim=...` com um período mais
   restrito que o histórico total do aluno.
   - **Esperado**: `percentualFrequencia` nunca excede 100%, e corresponde às presenças reais
     dentro desse período dividido pelas aulas Realizadas no mesmo período.
5. Repetir o Passo 4 usando `turmaId` ou `status` como filtro, sem período.
   - **Esperado**: mesmo comportamento corrigido (presenças reais no filtro).

## Regressão

Rodar a suíte de testes completa (`dotnet test`) para confirmar que nenhum outro fluxo (Relatório
de Agenda, Estória 12, que compartilha o mapeamento `Aula → RelatorioAgendaItem`) foi afetado.
