# Data Model: Cobrança por Aluno e Reposição de Aulas

## vinculo_cobranca (alterada)

| Item | Antes | Depois |
|---|---|---|
| `turma_id` | Qualquer modalidade; NULL = atendimento individual | Só Avulsa pode ter turma. Mensalidade/Pacote sempre NULL (= "do aluno"). `CHECK (turma_id IS NULL OR modalidade = 'Avulsa')` |
| Unicidade | `chave_ativa` = `aluno:turma` para todo vínculo ativo, índice único | `chave_avulsa_ativa` = `aluno:turma` só para Avulsa ativa, índice único. Pacote/Mensalidade: regra no serviço (research.md R3) |
| Regras de combinação (serviço) | — | Mensalidade ativa ⇒ nenhum outro vínculo ativo do aluno; no máximo 1 Pacote ativo por aluno; no máximo 1 Avulsa ativa por contexto |

Migração: Pacote/Mensalidade existentes com turma passam a `turma_id = NULL`; nenhum vínculo é desativado, somado ou alterado em valor/saldo (FR-020/FR-021).

## aula_aluno (alterada)

| Coluna | Tipo | Regra |
|---|---|---|
| `reposicao` | `TINYINT(1) NOT NULL DEFAULT 0` | `1` = aluno adicionado à aula de turma pela professora sem ser matriculado nela. Só pode ser removido individualmente enquanto a aula está "Agendada". |
| `vinculo_cobranca_id` | (existente) | Passa a registrar o vínculo efetivamente aplicado pela regra de prioridade (mensalidade, pacote ou avulsa), em qualquer contexto. |

## DTOs

- `VinculoCobrancaResponse.Conflito: string?` — preenchido só por `GET /api/alunos/{id}/vinculos-cobranca` para vínculos ativos em combinação proibida (research.md R6).
- `AlunoPresencaResponse.Reposicao: bool`.
- Novo request `AdicionarParticipanteRequest { AlunoId: int }`.

## Decisão de cobrança (Application, sem persistência)

`DecisaoCobranca { Tipo: Mensalidade | Pacote | Avulsa | Bloqueada | SemVinculo; Vinculo: VinculoCobranca?; PacoteEsgotado: bool }`

| Tipo | Efeito na presença confirmada | `AulaAluno.VinculoCobrancaId` |
|---|---|---|
| Mensalidade | nenhuma cobrança | id da mensalidade |
| Pacote | decrementa `SaldoAulas` se > 0 (nulo: não decrementa) | id do pacote |
| Avulsa | conta a receber pelo `Valor` da avulsa; aviso se `PacoteEsgotado` | id da avulsa |
| Bloqueada | presença registrada como não presente, `MotivoNaoRegistro = PacoteEsgotado`, aviso de renovar | nulo |
| SemVinculo | conta a receber por `Aluno.ValorAula` | nulo |

Falta: nenhuma decisão é aplicada (nada cobrado, nada consumido), como hoje.
