# Data Model: Job de Cobrança Automática de Mensalidade

## Entidade alterada: `Pagamento`

| Campo (C#) | Coluna | Tipo | Regra |
|---|---|---|---|
| `VinculoCobrancaId` *(novo)* | `vinculo_cobranca_id` | INT NULL, FK → `vinculo_cobranca(id)` (`fk_pagamento_vinculo_cobranca`) | `NULL` para todo `Pagamento` que não veio deste job (o padrão hoje — presença de aula, lançamento manual, contas a pagar não se aplicam aqui). Preenchido **apenas** pelas cobranças geradas por `MensalidadeDispatcherService`. |
| — | `uq_pagamento_vinculo_competencia` | `UNIQUE INDEX (vinculo_cobranca_id, competencia)` | Sustenta FR-004 no banco. `NULL` em `vinculo_cobranca_id` nunca colide em índice único do MySQL, então `Pagamento`s sem relação com um vínculo (todos os já existentes, e todos os gerados por specs/038) nunca são afetados por esse índice. |

Navegação: `Pagamento.VinculoCobranca` (nova, `VinculoCobranca?`), sem navegação recíproca em `VinculoCobranca` (não é necessário listar pagamentos a partir do vínculo nesta fatia).

## Novos métodos de repositório

### `IVinculoCobrancaRepository.ListarAtivosPorModalidadeAsync(ModalidadeCobranca modalidade, CancellationToken)`

- Devolve todos os `VinculoCobranca` com `Ativo == true` e `Modalidade == modalidade` (usado com `Mensalidade`), com a navegação `Aluno` e `Turma` já carregadas (necessárias para `Valor`/`AlunoId` e para o contexto da `Descricao`, R5 de research.md).
- Só leitura; nenhum efeito colateral.

### `IPagamentoRepository.ExisteMensalidadeGeradaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken)`

- `true` se já existir um `Pagamento` com esse `VinculoCobrancaId` e essa `Competencia`. Só leitura.

## Fluxo de geração (uma execução do job, dentro da janela de disparo)

```text
Para cada VinculoCobranca em ListarAtivosPorModalidadeAsync(Mensalidade):
    competencia := primeiro dia do mês corrente (o mês que está terminando)
    se ExisteMensalidadeGeradaAsync(vinculo.Id, competencia) → pular (FR-004)
    senão:
        criar Pagamento {
            AlunoId = vinculo.AlunoId,
            VinculoCobrancaId = vinculo.Id,
            Descricao = "Mensalidade - {contexto} - {competencia:MM/yyyy}",   (R5)
            CategoriaReceitaId = categoria "Mensalidade".Id,                  (R6)
            DataVencimento = primeiro dia do mês seguinte à competencia,      (R4)
            Competencia = competencia,
            ValorFinal = vinculo.Valor,
            Status = "Pendente"
        }
        adicionar e salvar (nenhum PagamentoAula é criado — Assumptions do spec.md)
```

Cada vínculo é processado dentro de seu próprio `try/catch` (FR-011): uma falha em um não impede os demais.

## Nova categoria de receita (dado, não schema)

| Campo | Valor |
|---|---|
| `nome` | `"Mensalidade"` |
| `ativo` | `TRUE` |

Inserida via `database/15_job_cobranca_mensalidade.sql`, resolvida via `ICategoriaReceitaRepository.ObterPorNomeAsync("Mensalidade", ct)` (interface já existente, sem mudança de assinatura).

## Configuração nova

| Chave | Default | Uso |
|---|---|---|
| `Mensalidade:IntervaloVerificacaoSegundos` | `3600` | Intervalo do `PeriodicTimer` de `MensalidadeDispatcherService` (research.md R3). |

## Validações (dono único: `MensalidadeDispatcherService`)

| Regra | Onde |
|---|---|
| Disparar só no último dia do mês, 23h ou depois (FR-001) | `MensalidadeDispatcherService.DeveDispararNesteMomento` (método estático puro, testável) |
| Uma cobrança por vínculo Mensalidade ativo, não por aluno (FR-003) | `ListarAtivosPorModalidadeAsync` já lista por vínculo, não por aluno |
| Nunca duplicar por competência (FR-004) | `ExisteMensalidadeGeradaAsync` (aplicação) + `uq_pagamento_vinculo_competencia` (banco) |
| Vínculo excluído não gera cobrança (FR-005) | `ListarAtivosPorModalidadeAsync` já filtra `Ativo == true` no momento da chamada (dentro da janela de disparo) |
| Falha isolada por vínculo (FR-011) | `try/catch` por iteração dentro do laço de geração |

## Entidades relacionadas (inalteradas)

- `VinculoCobranca` (specs/037): nenhuma mudança de schema ou de regra de cadastro — só passa a ser **lido** por mais um consumidor.
- `Aluno.ValorAula`: continua irrelevante para Mensalidade (o valor sempre vem do vínculo, já em specs/038).
- `Aula`/`AulaAluno`/`PagamentoAula`: nenhuma relação com este job — a cobrança de mensalidade não depende de nenhuma aula específica (Assumptions).
