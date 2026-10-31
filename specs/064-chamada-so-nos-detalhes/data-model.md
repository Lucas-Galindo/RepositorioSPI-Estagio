# Data Model: Chamada só nos detalhes da aula (spec 064)

## Mudança de banco (única)

**Migração `database/26_aula_frequencia_registrada_em.sql`** (24 = spec 062, 25 = spec 063):

```sql
USE spi_db;

ALTER TABLE aula
    ADD COLUMN frequencia_registrada_em DATETIME NULL
        COMMENT 'Quando a chamada foi registrada (hora de Sao Paulo); vazia nas aulas anteriores a specs/064'
        AFTER ativo;
```

- Opcional (`NULL`), **sem valor padrão** e **sem `UPDATE` retroativo** (FR-015, decisão 8): as aulas já realizadas ficam com a coluna vazia, porque não há como saber quando a chamada foi feita.
- Nenhum `DELETE`, nenhuma alteração de dado existente, nenhuma constraint nova (a aula "Realizada" sem a data continua válida).
- Cabeçalho do arquivo no padrão das migrações 22–25: objetivo, segurança ("não apaga nada"), pré-requisito (01 a 23 e 25; a 24 não é pré-requisito), "fazer backup antes (`database/backup`)".

## Entidade `Aula` (alteração)

| Campo | Tipo | Regra |
|---|---|---|
| `FrequenciaRegistradaEm` (novo) | `DateTime?` → `frequencia_registrada_em` | Gravado **apenas** em `AplicarPresencasAsync`, no instante em que a aula vira "Realizada", com a hora de São Paulo (`AgoraEmSaoPaulo()`). Nunca regravado: a aula "Realizada" não aceita nova chamada. |

Os demais campos de `Aula` não mudam. `AulaConfiguration` ganha o mapeamento da coluna.

## Campos calculados da resposta (não persistidos)

`AulaResponse` ganha dois campos, calculados por `RegraChamada` com o `agora` do servidor a cada resposta:

| Campo | Tipo | Valor |
|---|---|---|
| `ChamadaLiberada` | `bool` | `true` somente se a aula está ativa, `Status == "Agendada"` e `agora >= DataInicio + HoraInicio` |
| `SegundosParaChamada` | `int?` | segundos até o início quando a aula está ativa, "Agendada" e **ainda não começou**; `null` em todos os outros casos |

## Estados da chamada (derivados, sem máquina de estados nova)

| Status da aula | Antes do início | Do início em diante | Observação |
|---|---|---|---|
| Agendada, ativa | bloqueada (`ChamadaLiberada=false`, `SegundosParaChamada` preenchido) | **liberada** | Terminou sem frequência → aviso (`AulaPendente`) |
| Realizada | n/a (não aceita nova chamada) | n/a | Só a descrição pode ser editada (spec 055, US2) |
| Cancelada | n/a | n/a | Sem chamada e sem aviso |
| Excluída (`Ativo=false`) | n/a | n/a | Sem chamada e sem aviso; `RegistrarSessao` responde 404 |

Transição: **Agendada → Realizada** somente por `RegistrarSessaoAsync`, após o início; grava `FrequenciaRegistradaEm`. A criação **nunca** transiciona (FR-002).

## Notificação (sem mudança de estrutura)

`notificacao` e `TipoNotificacao.AulaPendente` ficam como na spec 059 (uma por aula, criar/reativar/resolver pelo conjunto). Mudam apenas:

- o **conjunto**: sai o corte de vigência, fica "aula ativa, Agendada, com término já passado";
- o **texto** da mensagem e o **título** exibido (cálculo em `Mapear` e rótulo no `NotificacoesSino`), não há coluna nova nem migração para isso.

## Configuração removida

`Notificacoes:AulasPendentesDesde` (em `appsettings.json` e `NotificacoesOptions`) — Princípio III. Permanecem `IntervaloVerificacaoSegundos`, `AulaFuturaDiasAntes` e `AulaFuturaHora`.

## Consulta de auditoria (somente leitura, fora da sequência de migrações)

`database/consultas/aulas_realizadas_antes_do_inicio.sql` — critérios em research.md R8. Nenhum `UPDATE`/`DELETE`/`INSERT` (verificado por leitura do arquivo e por `grep` na tarefa de Polish).
