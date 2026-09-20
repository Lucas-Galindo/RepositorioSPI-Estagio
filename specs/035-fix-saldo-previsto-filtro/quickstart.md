# Quickstart: Validar a correção de Receita Pendente/Saldo Previsto filtrados

## Pré-requisitos

- Backend rodando localmente (`dotnet run` em `src/SPI.Api`), com o banco MySQL de dev acessível
  (nenhuma migração nova — ver [data-model.md](./data-model.md)).
- Uma credencial válida para autenticar. **Não usar/criar credenciais de teste persistentes** —
  pedir uma credencial temporária no momento do teste, conforme a regra de trabalho combinada.

## Validação automatizada (principal)

Rodar `dotnet test --filter RelatorioServiceFinanceiroPendenteTests` cobre, no mínimo:

1. Filtro por `alunoId` com pagamentos pendentes de múltiplos alunos → `ReceitaPendente` soma
   apenas os pendentes do aluno filtrado, não o total.
2. Filtro por `turmaId` (sem `alunoId`) → `ReceitaPendente` soma apenas os pendentes dos alunos
   vinculados àquela turma.
3. Qualquer um dos filtros acima aplicado → `DespesaPendente` permanece idêntico ao valor sem
   filtro (sempre global).
4. `SaldoPrevisto` corresponde a `SaldoRealizado + (ReceitaPendente filtrada - DespesaPendente
   global)`.
5. Sem nenhum filtro → `ReceitaPendente`/`DespesaPendente`/`SaldoPrevisto` idênticos ao
   comportamento anterior à correção (regressão).

Rodar também os testes já existentes (`RelatorioServiceFinanceiroPorTurmaTests`,
`RelatorioServiceLancamentosTests`) para confirmar que a extensão de
`IRelatorioRepository.ObterReceitasPendentesSegregadasAsync` não quebrou nenhum teste que já
implementa `FakeRelatorioRepository` (ver research.md — R4).

## Validação manual (complementar, contra dados reais)

1. Autenticar e obter `accessToken`.
2. Identificar um aluno com pelo menos um pagamento `Pendente` (a vencer ou atrasado).
3. Chamar `GET /api/relatorios/financeiro` sem nenhum filtro de aluno/turma/matéria.
   - Anotar `receitaPendente` (deve ser o total do negócio).
4. Chamar `GET /api/relatorios/financeiro?alunoId={id do aluno do passo 2}`.
   - **Esperado**: `receitaPendente` é menor ou igual ao valor do passo 3 (a menos que esse
     aluno concentre 100% da receita pendente), e corresponde à soma manual dos pagamentos
     pendentes apenas desse aluno.
   - **Esperado**: `despesaPendente` é idêntico ao valor obtido no passo 3 (não filtrado).
5. Repetir o passo 4 usando `turmaId` ou `materiaId` no lugar de `alunoId`.

## Regressão

Rodar a suíte de testes completa (`dotnet test`) para confirmar que nenhum outro consumidor de
`ObterReceitasPendentesSegregadasAsync` (a tela "Financeiro — Visão Geral", via
`FinanceiroService.ObterVisaoGeralAsync`, que usa os filtros por nome já existentes) foi afetado
— os novos parâmetros são opcionais e não alteram chamadas que não os informam.
