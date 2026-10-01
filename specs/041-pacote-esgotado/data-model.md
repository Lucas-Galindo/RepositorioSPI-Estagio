# Data Model: Esgotamento de Pacote de Aulas

**Uma única alteração de schema**: coluna nova `aula_aluno.motivo_nao_registro` (research.md R2/R6), script `database/16_aula_aluno_motivo_nao_registro.sql`. O **estado de esgotamento** em si (Atenção/Esgotado) continua derivado do `VinculoCobranca.SaldoAulas` atual — nunca armazenado; só o **motivo histórico** de uma presença não registrada é persistido.

## Campo novo: `AulaAluno.MotivoNaoRegistro` (tabela `aula_aluno`)

| Atributo | Valor |
|---|---|
| Coluna | `motivo_nao_registro` `VARCHAR(30) NULL` (sem default) |
| Propriedade | `string? MotivoNaoRegistro` em `AulaAluno` |
| Valores permitidos | `NULL`, ou `'PacoteEsgotado'` (constante `AulaAluno.MotivoPacoteEsgotado`); imposto por `CHECK ck_aulaaluno_motivo_nao_registro` |
| Significado de `NULL` | comportamento normal: presente, ou falta comum — **nunca** indica bloqueio |
| Quando é preenchido | **somente** em `RegistrarSessaoAsync`, para o aluno marcado presente cujo vínculo Pacote ativo do contexto tem saldo 0; nesse caso `Presente = false` |
| Quando é limpo | nunca — só aulas `Agendada` aceitam registro, então o valor é gravado uma vez |
| Linhas existentes | permanecem `NULL` (nenhum backfill) |
| Exposto em | `AlunoPresencaResponse.MotivoNaoRegistro` (`string?`), em toda resposta que traz os alunos da aula (registro e consulta posterior) |

## Entidade existente reutilizada: `VinculoCobranca` (specs/037)

| Campo | Uso nesta feature |
|---|---|
| `Modalidade` | Só `Pacote` participa do painel e do bloqueio (FR-007) |
| `SaldoAulas` (`int?`) | `null` = nunca informado → fora do painel e nunca bloqueia; `0` = Esgotado; `1` ou `2` = Atenção; `>= 3` = fora do painel |
| `Ativo` | `false` → fora do painel e nunca bloqueia |
| `AlunoId` / `TurmaId` | Contexto do vínculo: `TurmaId == null` → "Atendimento individual" |

**Constante nova (Domain)**: `VinculoCobranca.LimiteAlertaPacote = 2` — dono único do limite de alerta (Princípio II/III); nunca configurável.

## Estados derivados (nunca persistidos)

| Saldo | Estado exposto (`Estado`) | Painel | Bloqueia presença? |
|---|---|---|---|
| `null` | — | Não | Não |
| `0` | `Esgotado` | Sim (primeiro) | **Sim** (só se o aluno vier marcado como presente) |
| `1`, `2` | `Atencao` | Sim | Não |
| `>= 3` | — | Não | Não |

## DTO novo: `PacoteEmAtencaoResponse` (item do painel — visão derivada, não persistida)

| Campo | Tipo | Regra |
|---|---|---|
| `VinculoId` | int | Id do vínculo |
| `AlunoId` | int | Para o link de renovação (`/alunos/{alunoId}`) |
| `AlunoNome` | string | Nome do aluno |
| `Contexto` | string | `Turma.Nome`, ou `"Atendimento individual"` quando `TurmaId == null` |
| `SaldoAulas` | int | Saldo atual (0, 1 ou 2) |
| `Estado` | string | `"Esgotado"` (saldo 0) ou `"Atencao"` (saldo 1 ou 2) — calculado no backend |

**Ordenação da lista** (FR-003): `Esgotado` antes de `Atencao`; depois `SaldoAulas` crescente; depois `AlunoNome`.

## Campos novos em DTOs existentes

| DTO | Campo | Regra |
|---|---|---|
| `DashboardResponse` | `PacotesEmAtencao: List<PacoteEmAtencaoResponse>` | Sempre presente; lista vazia quando ninguém precisa de atenção |
| `AulaResponse` | `Avisos: List<string>` | Padrão vazio; só `RegistrarSessaoAsync` preenche (1 mensagem por aluno bloqueado) — aviso imediato, não persistido |
| `AlunoPresencaResponse` | `MotivoNaoRegistro: string?` | `null` na maioria dos casos; `"PacoteEsgotado"` para o aluno barrado — persistido, aparece também ao consultar a aula depois |

## Efeito em `AulaAluno` (aluno bloqueado, opção A)

| Campo | Valor |
|---|---|
| `Presente` | `false` |
| `MotivoNaoRegistro` | `"PacoteEsgotado"` (distingue de falta comum, que fica `null`) |
| `Aluno.Frequencia` | inalterada |
| Débito de `SaldoAulas` | nenhum (segue em 0) |
| `Pagamento` | nenhum (o laço de `GerarContasAReceberAsync` filtra `Presente == true`) |

## Validações (dono único)

| Regra | Onde |
|---|---|
| Presença de aluno com Pacote ativo e saldo 0 não é registrada; aula segue `Realizada` (FR-005/006) | `AulaService.RegistrarSessaoAsync` |
| Motivo só é gravado no bloqueio; `NULL` = normal/falta comum; valores restritos a `'PacoteEsgotado'` | `AulaService.RegistrarSessaoAsync` + `CHECK` no banco (`16_aula_aluno_motivo_nao_registro.sql`) |
| Limite de alerta = 2 | `VinculoCobranca.LimiteAlertaPacote` (Domain) |
| Filtro do painel (ativo, Pacote, saldo informado ≤ limite, aluno ativo) | `DashboardService.MontarPacotesEmAtencao` (estático, puro — testável sem banco) |
| Estado (`Atencao`/`Esgotado`) e ordenação | `DashboardService.MontarPacotesEmAtencao` (mesmo método) |
| Frontend nunca compara saldos nem define estado | Só renderiza `Estado` recebido |
