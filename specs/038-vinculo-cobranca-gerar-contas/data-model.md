# Data Model: Cobrança Automática por Modalidade do Vínculo

Nenhuma entidade nova, nenhuma coluna nova, nenhuma migração. Esta feature muda apenas **quem lê e escreve** campos já existentes (specs/037), dentro de `AulaService`.

## Entidades existentes envolvidas (sem mudança de schema)

| Entidade | Campo(s) relevantes | O que muda nesta feature |
|---|---|---|
| `VinculoCobranca` (specs/037) | `AlunoId`, `TurmaId`, `Modalidade`, `Valor`, `SaldoAulas`, `Ativo` | Passa a ser **lido** por `AulaService` a cada presença confirmada (novo consumidor). `SaldoAulas` passa a ser **escrito automaticamente** (decremento), além de continuar editável manualmente via `PUT` (specs/037) — as duas formas de escrita coexistem, sem conflito, pois o decremento automático só acontece dentro de `RegistrarSessaoAsync`. |
| `Aluno` | `ValorAula` | Continua **só lido**, exatamente como hoje, e só quando não há `VinculoCobranca` correspondente ao contexto (FR-002). Nenhuma escrita nova. |
| `Aula` / `AulaAluno` | `TurmaId`, `Presente` | Continuam lidos exatamente como hoje; `TurmaId` da aula passa a também ser usado como chave de busca do vínculo (além do já existente uso para escolher a categoria da conta). |
| `Pagamento` | todos os campos já existentes | Passa a ser gerado **condicionalmente** (FR-002/FR-003), em vez de sempre — quando gerado, os campos são preenchidos exatamente como hoje, exceto `ValorFinal`, que pode vir de `VinculoCobranca.Valor` em vez de `Aluno.ValorAula` (FR-003). |

## Efeito por modalidade (regra central desta feature)

| Vínculo correspondente ao contexto | Gera `Pagamento`? | Valor usado | `SaldoAulas` |
|---|---|---|---|
| Nenhum (ausente, excluído, ou de outro contexto) | Sim | `Aluno.ValorAula` | N/A |
| `Modalidade = Avulsa` | Sim | `VinculoCobranca.Valor` | N/A |
| `Modalidade = Mensalidade` | Não | — | N/A (permanece com efeito zero até job futuro) |
| `Modalidade = Pacote`, `SaldoAulas > 0` | Não | — | Decrementa em 1 |
| `Modalidade = Pacote`, `SaldoAulas` é `0` ou `null` | Não | — | Inalterado (nunca negativo) |

## Novo método de repositório (contrato interno, não HTTP)

`IVinculoCobrancaRepository.ObterAtivoPorAlunoEContextoAsync(int alunoId, int? turmaId, CancellationToken)` → `VinculoCobranca?`

- Entrada: `alunoId` (obrigatório), `turmaId` (nulo = contexto de atendimento individual).
- Saída: o único `VinculoCobranca` com `Ativo = true` para essa combinação exata, ou `null` se não houver (nunca mais de um, por causa da unicidade garantida em specs/037).
- Sem efeito colateral (somente leitura).

## Transição de estado de `SaldoAulas` nesta feature

```text
SaldoAulas > 0  --presença confirmada (Pacote)-->  SaldoAulas - 1   (nunca abaixo de 0)
SaldoAulas = 0  --presença confirmada (Pacote)-->  SaldoAulas = 0   (inalterado)
SaldoAulas = null --presença confirmada (Pacote)--> SaldoAulas = null (inalterado, tratado como esgotado)
```

Nenhuma outra transição é introduzida; edição manual do `SaldoAulas` (specs/037, `PUT /api/alunos/{alunoId}/vinculos-cobranca/{id}`) continua funcionando exatamente como antes, sem relação com esta tabela.

## Validações (dono único: `AulaService.GerarContasAReceberAsync`)

| Regra | Onde |
|---|---|
| Buscar vínculo correspondente ao contexto (FR-001) | `AulaService`, via `ObterAtivoPorAlunoEContextoAsync` |
| Ramificar por modalidade (FR-002 a FR-006) | `AulaService.GerarContasAReceberAsync` |
| `SaldoAulas` nunca fica negativo (FR-006) | `AulaService.GerarContasAReceberAsync` (guarda `SaldoAulas > 0` antes de decrementar) |
| Independência por aluno na mesma aula (FR-007) | Laço existente sobre `aula.AulaAlunos.Where(v => v.Presente == true)`, já presente no código atual |
