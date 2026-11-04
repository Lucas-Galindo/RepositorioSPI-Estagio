# Data Model: Link da aula temporário (spec 067)

## Banco de dados: nenhuma mudança (requisito FR-002)

- **Sem migração**, sem coluna nova em `aula`, sem tabela. `Aula.cs`, `AulaConfiguration.cs` e `database/` **não são alterados**.
- A última migração continua a atual (a `26` da spec 064, quando existir). Nada desta spec consome número de migração.

## Entidade em memória (não persistida)

**Link da aula (temporário)** — vive só no processo da API, em um `ConcurrentDictionary<int, EntradaLink>` dentro do singleton `LinkAulaTemporario`.

| Campo | Tipo | Regra |
|---|---|---|
| `AulaId` (chave) | `int` | Identificador da aula; no máximo **um** link por aula (o último salvo vale) |
| `Link` | `string` | Endereço `http(s)` já validado, ≤ 500 caracteres, sem espaços; **nunca** logado |
| `ExpiraEm` | `DateTimeOffset` | Fim da aula (`DataInicio` + `HoraFim`, hora de São Paulo) convertido para instante absoluto |

### Ciclo de vida

```text
(sem link) --Definir--> (com link, vigente) --Definir(novo)--> (com link novo)
(com link) --Remover | Cancelar aula | Excluir aula--> (sem link)
(com link) --passa ExpiraEm--> (sem link)            [expiração na leitura]
(com link) --editar data/hora--> (com link, nova validade) | (sem link, se o novo fim já passou)
(qualquer) --reinício da API--> (sem link)
```

## Entidade `Aula` (sem alteração de estrutura)

`AulaResponse` (DTO) ganha dois campos **calculados** a cada resposta, não persistidos:

| Campo | Tipo | Valor |
|---|---|---|
| `LinkAula` | `string?` | o link vigente da aula, ou `null` |
| `AceitaLinkAula` | `bool` | `RegraLinkAula.AceitaLink`: aula ativa, não cancelada e que ainda não terminou |

## E-mail do lembrete (conteúdo)

Quando há link vigente da próxima aula da turma, o corpo ganha um parágrafo adicional (texto e `href` protegidos); caso contrário o corpo é exatamente o atual. Nenhum campo de `Lembrete` muda.
