# Quickstart: validar o Job de Cobrança Automática de Mensalidade

Guia de validação ponta a ponta. Modelo em [data-model.md](./data-model.md); ausência de contrato de API em [contracts/README.md](./contracts/README.md).

Como o disparo real só ocorre no último dia do mês às 23h, a validação **não espera o relógio real** — usa a extração testável da condição de disparo (research.md R2/R7) e chama o método de geração diretamente, sem depender do `PeriodicTimer`.

## Pré-requisitos

- Backend rodando (mesmo fluxo de `run-dev.ps1`), migração `database/15_job_cobranca_mensalidade.sql` aplicada.
- Uma professora autenticada.
- Um aluno com dois Vínculos de Cobrança Mensalidade ativos em contextos diferentes (uma turma + atendimento individual) — reaproveitar os endpoints de specs/037.

## 1. Testes automatizados

```powershell
dotnet test "C:\PROJETO - SPI\tests\SPI.Application.Tests"
```

Esperado: suíte inteira verde, incluindo:

- `MensalidadeDispatcherServiceDeveDispararTests` — `DeveDispararNesteMomento` testado para: dia comum (falso), último dia às 22h59 (falso), último dia às 23h00 (verdadeiro), último dia às 23h59 (verdadeiro), meses de 28/29/30/31 dias.
- Testes de geração/idempotência definidos em `/speckit-tasks` (FR-002 a FR-006, FR-011).

## 2. Validação do banco (unicidade)

Depois de aplicar a migração, confirmar diretamente no MySQL:

- Duas linhas em `pagamento` com o mesmo `vinculo_cobranca_id` e a mesma `competencia` → a segunda `INSERT`/`UPDATE` MUST falhar com "Duplicate entry" (`uq_pagamento_vinculo_competencia`).
- Uma linha com `vinculo_cobranca_id = NULL` nunca colide com outra `NULL`, mesmo competência (comportamento padrão do MySQL para `UNIQUE` com `NULL`).
- `SELECT nome FROM categoria_receita WHERE nome = 'Mensalidade'` retorna exatamente 1 linha, `ativo = 1`.

## 3. Cenários pela API (chamando a geração diretamente, sem esperar o relógio)

| # | Passo | Resultado esperado |
|---|---|---|
| 1 | Com dois Vínculos Mensalidade ativos do mesmo aluno (turma X, e atendimento individual), acionar a geração do mês corrente | `GET /api/pagamentos?alunoId=` mostra **duas** novas contas pendentes, uma por vínculo, cada uma com `valorFinal` igual ao Valor do respectivo vínculo, `categoriaReceitaNome == "Mensalidade"`, `aulaIds: []` (US1) |
| 2 | Acionar a geração do mesmo mês uma segunda vez | Nenhuma conta nova é criada para nenhum dos dois vínculos (US2, FR-004) |
| 3 | Excluir (desativar) um dos dois Vínculos Mensalidade antes de acionar a geração do mês seguinte | Só o vínculo que continua ativo recebe uma nova cobrança nesse mês; o excluído, nenhuma (US3, FR-005) |
| 4 | Conferir `dataVencimento` da conta gerada no passo 1 | É o dia 1º do mês seguinte ao mês de competência da conta (FR-007) |
| 5 | Conferir `descricao` da conta gerada para o vínculo de turma vs. o de atendimento individual | Cada uma identifica claramente "Mensalidade" e o contexto certo (nome da turma, ou "Atendimento individual") (FR-006) |

## 4. Não-regressão explícita

- `GET /api/alunos/{alunoId}/vinculos-cobranca` continua retornando exatamente os mesmos campos de specs/037, sem nenhum indicador novo.
- Registrar a sessão de uma aula de um aluno com vínculo Pacote ou Avulsa continua funcionando exatamente como especificado em specs/038, sem nenhuma interferência deste job.
- Na tela de detalhe do Aluno, a seção "Vínculos de Cobrança" não deve mais afirmar que um vínculo Mensalidade "ainda não afeta a cobrança automática" (FR-013).

```powershell
git -C "C:\PROJETO - SPI" diff --stat -- src/SPI.Application/Aulas src/SPI.Application/VinculosCobranca src/SPI.Api/Controllers
```

Esperado: vazio — nenhum controller, nenhum endpoint, e nada da lógica de presença (specs/038) ou do cadastro do vínculo (specs/037) é tocado por esta feature.
