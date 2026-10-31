# Data Model: Semestralidade e Anuidade (spec 062)

Migração: `database/24_semestralidade_anuidade.sql`.

## `vinculo_cobranca` (alterada)

| Mudança | Detalhe |
|---|---|
| `data_inicio DATE NULL` | só para Semestral/Anual |
| `data_fim DATE NULL` | só para Semestral/Anual; = início + 6 ou 12 meses − 1 dia |
| `chk_vinculocobranca_modalidade` | passa a aceitar `'Avulsa','Mensalidade','Pacote','Semestral','Anual'` |
| `chk_vinculocobranca_periodo` | Semestral/Anual: `data_inicio` e `data_fim` preenchidas e `data_fim > data_inicio`. Demais modalidades: as duas nulas |

`valor` guarda o **valor total do período** (referência do contrato). Não há coluna de desconto nem de taxa.

## `categoria_receita` (dados)

Duas linhas novas: `Semestralidade` e `Anuidade` (ativas).

## `pagamento` (sem mudança de estrutura)

Cada parcela é um `pagamento` com `vinculo_cobranca_id` preenchido:

| Campo | Valor da parcela |
|---|---|
| `status` | `Pendente` ao criar; `Cancelado` quando o vínculo é excluído (só as pendentes futuras) |
| `data_vencimento` | a data da parcela |
| `competencia` | primeiro dia do mês do vencimento |
| `valor_final` | o valor da parcela |
| `descricao` | `"{Semestralidade ou Anuidade} - parcela X/N"` |
| `categoria_receita_id` | a categoria do plano |

A trava `uq_pagamento_vinculo_competencia (vinculo_cobranca_id, competencia)` garante uma parcela por mês no vínculo.

## `notificacao` (sem mudança de estrutura)

Novo valor de `tipo`: `RenovacaoPlano` (texto). Referência por `aluno_id`; trava `uq_notificacao_tipo_professor_aluno`.

## Estruturas calculadas (DTOs)

- **`ParcelaRequest`**: `vencimento`, `valor`.
- **`ParcelaResponse`**: `pagamentoId`, `descricao`, `vencimento`, `valor`, `status`.
- **`VinculoCobrancaRequest`** ganha `dataInicio` e `parcelas[]`.
- **`VinculoCobrancaResponse`** ganha `dataInicio`, `dataFim` e `parcelas[]`.
- **`NotificacaoResponse`** ganha `renovacao` (`alunoId`, `alunoNome`, `modalidade`, `dataFim`).

## Configuração

`Notificacoes:RenovacaoDiasAntes` (padrão 15).
