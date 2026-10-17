# Data Model: Auditoria de Aulas Dadas por Vínculo de Cobrança

Duas entidades já existentes ganham campos aditivos. Nenhuma entidade nova.

## `VinculoCobranca` (existente, + 1 campo persistido)

| Campo | Tipo | Já existia? | Regra |
|---|---|---|---|
| `Id`, `AlunoId`, `TurmaId`, `Modalidade`, `Valor`, `Ativo` | — | Sim | Inalterados |
| `AulasIncluidas` | `int?` | Sim | **Semântica estendida (sem mudança de schema)**: continua só Mensalidade; passa a ser tratado como teto mensal opcional, comparado contra a contagem de presenças da competência corrente (FR-008/FR-010) |
| `SaldoAulas` | `int?` | Sim | Inalterado — só Pacote, decrementado por presença (specs/038), nunca abaixo de zero |
| `TotalAulasPacote` | `int?` | **Novo** | Só Pacote (CHECK no banco, mesmo padrão de `AulasIncluidas`/`SaldoAulas`); gravado na criação; imutável por consumo automático (FR-002); editável pela professora, sujeito ao bloqueio de FR-007 |

**Coluna no banco**: `vinculo_cobranca.total_aulas_pacote INT NULL`, `CHECK (total_aulas_pacote IS NULL OR (modalidade = 'Pacote' AND total_aulas_pacote >= 1))`.

**Regra derivada (não persistida)**: "aulas dadas" de um Pacote = `TotalAulasPacote - SaldoAulas` (ambos não nulos); indefinido quando `TotalAulasPacote` é nulo (vínculo nunca migrado/corrigido).

## `AulaAluno` (existente, + 1 campo persistido + 1 navegação)

| Campo | Tipo | Já existia? | Regra |
|---|---|---|---|
| `AulaId`, `AlunoId`, `Presente`, `MotivoNaoRegistro` | — | Sim | Inalterados |
| `VinculoCobrancaId` | `int?` | **Novo** | FK para `vinculo_cobranca.id`; preenchido automaticamente por `AulaService.GerarContasAReceberAsync` no momento em que a presença é confirmada (FR-004); `null` quando não havia vínculo correspondente ao contexto (FR-005) ou quando a presença é falta (FR-006) |
| `VinculoCobranca` | `VinculoCobranca?` | **Novo** | Navegação EF — sem coleção recíproca em `VinculoCobranca` (mesmo padrão já usado por `Pagamento.VinculoCobranca`, specs/039) |

**Coluna no banco**: `aula_aluno.vinculo_cobranca_id INT NULL`, `FOREIGN KEY (vinculo_cobranca_id) REFERENCES vinculo_cobranca(id)` — sem índice único (uma mesma presença pertence a no máximo 1 vínculo, mas um vínculo tem muitas presenças ao longo do tempo).

**Nunca editável manualmente**: diferente de `Presente`/`MotivoNaoRegistro` (que vêm de ação da professora), `VinculoCobrancaId` é somente-sistema — nenhum endpoint aceita esse campo em request.

## Fluxo de preenchimento (`AulaService.GerarContasAReceberAsync`)

| Modalidade do vínculo correspondente | `AulaAluno.VinculoCobrancaId` | Efeito financeiro (inalterado, specs/038) |
|---|---|---|
| Nenhum vínculo correspondente | `null` | Cobrança avulsa por `Aluno.ValorAula` |
| Avulsa | Id do vínculo | Cobrança avulsa pelo `Valor` do vínculo |
| Mensalidade | Id do vínculo | Nenhuma cobrança nesta presença (job mensal separado, specs/039) |
| Pacote | Id do vínculo | Decrementa `SaldoAulas` em 1 (nunca abaixo de zero) |

## Regra de bloqueio de edição (`VinculoCobrancaService.AtualizarAsync`)

| Modalidade | Condição de bloqueio | Mensagem |
|---|---|---|
| Pacote | `request.TotalAulasPacote.HasValue && request.TotalAulasPacote < (vinculo.TotalAulasPacote ?? 0) - (vinculo.SaldoAulas ?? 0)` | "O novo total de aulas do pacote não pode ser menor que as aulas já dadas (N)." |
| Mensalidade | `request.AulasIncluidas.HasValue && request.AulasIncluidas < aulasDadasNoMesCorrente` | "O novo teto de aulas da mensalidade não pode ser menor que as aulas já dadas neste mês (N)." |

## Migração de dados (`database/18_auditoria_vinculo_cobranca.sql`)

| Ação | Alvo | Valor |
|---|---|---|
| Backfill | `vinculo_cobranca.total_aulas_pacote` de todo vínculo `Modalidade = 'Pacote'` com `saldo_aulas` informado | `saldo_aulas` atual (assume consumo zero prévio — ver spec.md, Assumptions) |
| Sem backfill | `aula_aluno.vinculo_cobranca_id` de presenças já existentes | Permanece `NULL` (sem reconstrução retroativa — ver spec.md, Edge Cases) |
