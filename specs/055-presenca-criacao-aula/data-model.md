# Data Model: Registrar Presença na Criação da Aula

Nenhuma entidade de domínio nem coluna de banco é criada ou alterada por esta feature — ela reaproveita por completo o modelo já existente de `Aula`/`AulaAluno`. A única mudança de "dados" é um campo novo e opcional no DTO de request já existente.

## Entidades existentes (sem alteração de estrutura)

### Aula (`src/SPI.Domain/Entities/Aula.cs`)

| Campo | Uso nesta feature |
|---|---|
| `Status` | Passa a poder nascer como `"Realizada"` direto na criação (hoje só nasce `"Agendada"`), quando `DataInicio` é hoje ou passada. |
| `Descricao` | Passa a poder ser editada por `AtualizarAsync` mesmo quando `Status == "Realizada"` (hoje, qualquer edição de uma aula Realizada é bloqueada). |
| `DataInicio`, `HoraInicio`, `HoraFim`, `MateriaId`, `TurmaId` | Sem alteração de significado; usados para decidir se a aula é retroativa e, uma vez `Status == "Realizada"`, continuam bloqueados para edição (nenhuma mudança nesse bloqueio). |

### AulaAluno (`src/SPI.Domain/Entities/AulaAluno.cs`)

| Campo | Uso nesta feature |
|---|---|
| `Presente`, `MotivoNaoRegistro` | Preenchidos no mesmo momento da criação (via `AplicarPresencasAsync`, ver research.md R1) quando a aula é retroativa, em vez de só depois via "Registrar Sessão". Nenhuma mudança de significado ou de regra sobre esses campos. |

## DTOs (camada de aplicação)

### `AulaRequest` (`src/SPI.Application/Aulas/Dtos/AulaRequest.cs`) — campo novo

```csharp
public record AulaRequest
{
    // ... campos existentes (Descricao, MateriaId, TurmaId, AlunoId, DataInicio, HoraInicio, HoraFim) sem alteracao

    /// <summary>
    /// Presenca de cada aluno (chave = AlunoId, valor = presente), informada
    /// apenas quando DataInicio e hoje ou passada (aula retroativa). Nulo ou
    /// ausente para uma aula agendada no futuro -- a presenca continua sendo
    /// registrada depois, via RegistrarSessaoAsync, como ja funciona hoje.
    /// </summary>
    public Dictionary<int, bool>? Presencas { get; set; }
}
```

- **Validação estrutural** (FluentValidation, `AulaRequestValidator`): nenhuma regra nova — ver research.md R3 para o motivo de a exigência "obrigatório quando retroativa" viver no serviço, não no validador compartilhado.
- **Validação de negócio** (serviço, `AulaService.CadastrarAsync`): se `DataInicio <= hoje` e `Presencas` é nulo, `ConflitoException` ("Informe a presença de cada aluno para criar uma aula retroativa."). Se `Presencas` não cobrir todos os alunos da turma/aluno individual, mesma exceção já lançada hoje por `AplicarPresencasAsync` (reaproveitada de `RegistrarSessaoAsync`): `"Presenca do aluno {id} nao foi informada."`

### `AulaResponse` / `RegistrarSessaoRequest` — sem alteração

Nenhum campo novo necessário; `AulaResponse.Avisos` (já existente) passa também a ser preenchido quando a criação retroativa barra algum aluno por pacote esgotado, exatamente como já acontece hoje via "Registrar Sessão".

## Frontend (tipos espelhados)

### `AulaRequest` (`frontend/lib/api/aulas.ts`) — campo novo

```ts
export interface AulaRequest {
  // ...campos existentes sem alteracao
  presencas?: Record<number, boolean>;
}
```

## Fluxos de estado (sem nova máquina de estados)

A máquina de estados de `Aula.Status` (`Agendada` → `Realizada` | `Cancelada`) não muda. O que muda é **quem** pode colocar uma aula direto em `Realizada`: antes, só `RegistrarSessaoAsync` (a partir de uma aula já `Agendada`); agora, também `CadastrarAsync` (a aula nasce `Realizada` direto, sem nunca passar por `Agendada`), quando a data já permite.
