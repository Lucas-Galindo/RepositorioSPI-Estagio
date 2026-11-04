# Data Model: Selo de cobrança do aluno (spec 068)

## Banco de dados: nenhuma mudança (FR-008)

Somente leitura. **Sem migração**, sem coluna, sem tabela; `database/` não é alterado.

## Resumo de cobrança (calculado, não persistido)

`SeloCobrancaResponse` (novo DTO, `SPI.Application/VinculosCobranca/Dtos/`):

| Campo | Tipo | Regra |
|---|---|---|
| `Categoria` | `string` | `"Mensalidade"`, `"Pacote"`, `"Avulsa"` ou `"SemVinculo"`; vem do nome da modalidade (Semestral/Anual entram sozinhas quando existirem) |
| `Valor` | `decimal` | valor do vínculo exibido; em `SemVinculo`, o `ValorAula` do aluno |
| `TurmaNome` | `string?` | nome da turma do vínculo (nulo = aulas individuais / sem turma); preenchido **só no resumo da lista** |
| `Progresso` | `ProgressoPacoteResponse?` | só quando a categoria é `Pacote` (total, aulas dadas, restantes, estado EmDia/Atencao/Esgotado/SemTotal) — o mesmo DTO da spec 058 |
| `AvisoPacoteEsgotado` | `bool` | `true` na `Avulsa` que só vale porque o pacote do grupo esgotou |
| `OutrosVinculos` | `int` | só no resumo da lista: quantos vínculos ativos além do principal |
| `Detalhes` | `List<DetalheCobrancaResponse>` | só no resumo da lista: todos os vínculos ativos (para a dica) |

`DetalheCobrancaResponse`: `Categoria`, `TurmaNome?`, `Valor`, `Progresso?` (um item por vínculo ativo, do mais recente ao mais antigo dentro da ordem Mensalidade → Pacote → Avulsa).

## Onde o selo viaja (campos novos opcionais)

| DTO existente | Campo | Observação |
|---|---|---|
| `AlunoResponse` | `Cobranca: SeloCobrancaResponse?` | resumo da lista; só em `GET /api/alunos?incluirCobranca=true` (sob demanda); sem o parâmetro fica `null` |
| `AlunoResumoResponse` | `Cobranca: SeloCobrancaResponse?` | vínculo que vale na turma; em `GET /api/turmas/{id}` (alunos matriculados) |
| `AlunoPresencaResponse` | `Cobranca: SeloCobrancaResponse?` | vínculo que vale para a aula; em `GET /api/aulas/{id}` e nos retornos de uma aula só |

Todos `null` quando o dado não foi calculado (ex.: respostas de outras ações); o frontend mostra um traço.

## Regras (todas no servidor)

- **Principal** (lista): Mensalidade → Pacote → Avulsa; mais recente (maior Id) em empate; só vínculos **ativos**.
- **Grupo** (turma/aula): `RegraCobrancaPresenca.Decidir(ativos, turmaId)`: mensalidade; senão pacote **do grupo** com saldo (ou sem saldo informado); senão avulsa do contexto (com aviso se o pacote esgotou); senão pacote esgotado (bloqueia); senão sem vínculo.
- **Progresso do pacote**: `ProgressoPacote.Calcular` (spec 058), alimentado por `ContarPresencasPorVinculosAsync` em lote.

## Consultas (por tela, no máximo duas)

1. Vínculos ativos de todos os alunos da tela: **uma** consulta `IN (alunoIds)` (método novo `ListarAtivosDosAlunosAsync`).
2. Presenças por pacote: **uma** consulta em lote (já existe), só se houver pacote.
