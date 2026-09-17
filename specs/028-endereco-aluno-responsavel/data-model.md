# Data Model: Endereço do Aluno e do Responsável

## Entidade: `Aluno` (estendida)

Nenhuma entidade nova — `Aluno` (`src/SPI.Domain/Entities/Aluno.cs`) ganha 15 campos novos.

| Campo (C#) | Coluna (MySQL) | Tipo | Nullable | Obrigatório na validação |
|---|---|---|---|---|
| `Cep` | `cep` | `VARCHAR(8)` | Sim | Sim (cadastro/edição do endereço do Aluno) |
| `Rua` | `rua` | `VARCHAR(150)` | Sim | Sim |
| `Numero` | `numero` | `VARCHAR(10)` | Sim | Sim |
| `Complemento` | `complemento` | `VARCHAR(100)` | Sim | Não |
| `Bairro` | `bairro` | `VARCHAR(100)` | Sim | Não |
| `Cidade` | `cidade` | `VARCHAR(100)` | Sim | Não |
| `Estado` | `estado` | `VARCHAR(2)` | Sim | Não (sigla UF) |
| `ResponsavelMesmoEndereco` | `responsavel_mesmo_endereco` | `BOOLEAN` | Não (`DEFAULT TRUE`) | N/A (flag, não endereço) |
| `ResponsavelCep` | `responsavel_cep` | `VARCHAR(8)` | Sim | Sim, só quando `EhMenorDeIdade` e `ResponsavelMesmoEndereco = false` |
| `ResponsavelRua` | `responsavel_rua` | `VARCHAR(150)` | Sim | Idem |
| `ResponsavelNumero` | `responsavel_numero` | `VARCHAR(10)` | Sim | Idem |
| `ResponsavelComplemento` | `responsavel_complemento` | `VARCHAR(100)` | Sim | Não |
| `ResponsavelBairro` | `responsavel_bairro` | `VARCHAR(100)` | Sim | Não |
| `ResponsavelCidade` | `responsavel_cidade` | `VARCHAR(100)` | Sim | Não |
| `ResponsavelEstado` | `responsavel_estado` | `VARCHAR(2)` | Sim | Não |

Todas as colunas são `NULL`-áveis no banco (ver research.md Decisão 3) — a obrigatoriedade é
imposta só pelo FluentValidation dos DTOs, nunca por `NOT NULL`, para não quebrar Alunos já
cadastrados sem endereço.

### Regra de leitura do endereço do responsável (Decisão 2 do research.md)

Quando `ResponsavelMesmoEndereco == true`, o `AlunoResponse` retorna os campos de endereço do
responsável **iguais aos do próprio Aluno** (`ResponsavelCep = Cep`, `ResponsavelRua = Rua`,
etc.), calculado no momento da leitura — as colunas `responsavel_*` no banco permanecem
inalteradas/`NULL` nesse estado. Quando `false`, os campos `responsavel_*` do banco são
retornados como estão.

### Regra de "é menor de idade" (Decisão 1 do research.md)

Não existe mais campo persistido para isso do que já não existia — `EhMenorDeIdade` continua
sendo só um campo de request (`CadastrarAlunoRequest`/`AtualizarAlunoRequest`), não uma coluna.
O frontend, ao carregar um Aluno para edição, inicializa o indicador visual como marcado se
`telefoneResponsavel`, `emailResponsavel` ou qualquer campo `responsavel*` vier preenchido na
resposta.

## DTOs afetados (`src/SPI.Application/Alunos/Dtos/`)

**`CadastrarAlunoRequest`** — adiciona:
```csharp
public string? Cep { get; set; }
public string? Rua { get; set; }
public string? Numero { get; set; }
public string? Complemento { get; set; }
public string? Bairro { get; set; }
public string? Cidade { get; set; }
public string? Estado { get; set; }
public bool ResponsavelMesmoEndereco { get; set; } = true;
public string? ResponsavelCep { get; set; }
public string? ResponsavelRua { get; set; }
public string? ResponsavelNumero { get; set; }
public string? ResponsavelComplemento { get; set; }
public string? ResponsavelBairro { get; set; }
public string? ResponsavelCidade { get; set; }
public string? ResponsavelEstado { get; set; }
```

**`AtualizarAlunoRequest`** — mesmos 15 campos (mesmo padrão de "campo não enviado = não
altera" já usado pelos demais campos opcionais desse DTO).

**`AlunoResponse`** — mesmos 15 campos (com a regra de espelhamento da seção acima aplicada aos
7 campos `Responsavel*` quando `ResponsavelMesmoEndereco == true`).

## Validação (`CadastrarAlunoRequestValidator` / `AtualizarAlunoRequestValidator`)

**Regra assimétrica entre cadastro e edição** (necessária para não quebrar Alunos já
cadastrados sem endereço — ver spec.md Assumptions e research.md Decisão 3):

- **`CadastrarAlunoRequestValidator`** (Aluno novo, sempre exige endereço completo — FR-002):
  - `Cep`, `Rua`, `Numero`: `NotEmpty` sempre.
  - `Cep`: `Matches(@"^\d{8}$")` — 8 dígitos numéricos.
  - Bloco `When(x => x.EhMenorDeIdade && !x.ResponsavelMesmoEndereco, () => { ... })`: exige
    `ResponsavelCep`, `ResponsavelRua`, `ResponsavelNumero` `NotEmpty` e `ResponsavelCep` com a
    mesma regra de 8 dígitos — mesmo padrão já usado para `TelefoneResponsavel`/
    `EmailResponsavel` no bloco `When(x => x.EhMenorDeIdade, ...)` existente.

- **`AtualizarAlunoRequestValidator`** (Aluno existente — "campo não enviado = não altera", o
  mesmo padrão já usado pelos demais campos opcionais deste DTO, ex. `Cpf`, `TelefoneAluno`):
  - `Cep`, `Rua`, `Numero` só se tornam obrigatórios **juntos** quando **pelo menos um dos três**
    vier preenchido na requisição — `When(x => x.Cep != null || x.Rua != null || x.Numero !=
    null, () => RuleFor(x => x.Cep).NotEmpty()... idem Rua, Numero)`. Se os três vierem vazios
    (Aluno antigo cujo endereço não está sendo tocado nesta edição), nenhum erro é gerado — o
    endereço simplesmente permanece como estava (ou vazio).
  - `Cep`, quando preenchido: mesma regra de 8 dígitos.
  - Mesma regra assimétrica para `ResponsavelCep`/`ResponsavelRua`/`ResponsavelNumero`: só
    obrigatórios juntos quando `EhMenorDeIdade && !ResponsavelMesmoEndereco` **e** pelo menos um
    dos três vier preenchido na requisição.

- Em ambos os validators: quando `EhMenorDeIdade && ResponsavelMesmoEndereco`, nenhuma validação
  extra nos campos `Responsavel*` é aplicada (eles serão ignorados/sobrescritos pela regra de
  espelhamento).

## Migração de banco (`database/13_endereco_aluno_responsavel.sql`)

```sql
USE spi_db;

ALTER TABLE alunos
    ADD COLUMN cep VARCHAR(8) NULL COMMENT 'CEP do endereco do aluno (8 digitos, sem mascara)',
    ADD COLUMN rua VARCHAR(150) NULL COMMENT 'Logradouro do endereco do aluno',
    ADD COLUMN numero VARCHAR(10) NULL COMMENT 'Numero do endereco do aluno',
    ADD COLUMN complemento VARCHAR(100) NULL COMMENT 'Complemento do endereco do aluno (opcional)',
    ADD COLUMN bairro VARCHAR(100) NULL COMMENT 'Bairro do endereco do aluno (opcional)',
    ADD COLUMN cidade VARCHAR(100) NULL COMMENT 'Cidade do endereco do aluno (opcional)',
    ADD COLUMN estado VARCHAR(2) NULL COMMENT 'UF do endereco do aluno (opcional)',
    ADD COLUMN responsavel_mesmo_endereco BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Quando true, o endereco do responsavel espelha o do aluno (colunas responsavel_* abaixo ficam sem uso)',
    ADD COLUMN responsavel_cep VARCHAR(8) NULL COMMENT 'CEP proprio do responsavel, usado só quando responsavel_mesmo_endereco = false',
    ADD COLUMN responsavel_rua VARCHAR(150) NULL COMMENT 'Logradouro proprio do responsavel',
    ADD COLUMN responsavel_numero VARCHAR(10) NULL COMMENT 'Numero proprio do responsavel',
    ADD COLUMN responsavel_complemento VARCHAR(100) NULL COMMENT 'Complemento proprio do responsavel (opcional)',
    ADD COLUMN responsavel_bairro VARCHAR(100) NULL COMMENT 'Bairro proprio do responsavel (opcional)',
    ADD COLUMN responsavel_cidade VARCHAR(100) NULL COMMENT 'Cidade propria do responsavel (opcional)',
    ADD COLUMN responsavel_estado VARCHAR(2) NULL COMMENT 'UF propria do responsavel (opcional)';
```

## Frontend — tipos (`frontend/lib/api/alunos.ts`)

`Aluno`, `CadastrarAlunoRequest`, `AtualizarAlunoRequest` ganham os mesmos 15 campos, em
camelCase, espelhando os DTOs C# (convenção `/** Espelha ... */` já usada no arquivo).
