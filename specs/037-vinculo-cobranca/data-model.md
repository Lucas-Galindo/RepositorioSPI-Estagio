# Data Model: Vínculo de Cobrança do Aluno

## Entidade: VinculoCobranca (`SPI.Domain.Entities`)

| Campo (C#) | Coluna | Tipo | Regra |
|---|---|---|---|
| `Id` | `id` | INT AUTO_INCREMENT, PK | — |
| `AlunoId` | `aluno_id` | INT NOT NULL, FK → `alunos(id)` (`fk_vinculocobranca_aluno`) | Obrigatório (FR-001) |
| `TurmaId` | `turma_id` | INT NULL, FK → `turma(id)` (`fk_vinculocobranca_turma`) | `NULL` = atendimento individual (mesmo padrão de `aula.turma_id`) |
| `Modalidade` | `modalidade` | VARCHAR(20) NOT NULL, `CHECK IN ('Avulsa','Mensalidade','Pacote')` (`chk_vinculocobranca_modalidade`) | Enum `ModalidadeCobranca` (FR-002) |
| `Valor` | `valor` | DECIMAL(10,2) NOT NULL, `CHECK (valor > 0)` (`chk_vinculocobranca_valor`) | > 0 (FR-002) |
| `AulasIncluidas` | `aulas_incluidas` | INT NULL, `CHECK (aulas_incluidas IS NULL OR (modalidade = 'Mensalidade' AND aulas_incluidas >= 1))` | Só Mensalidade (FR-003) |
| `SaldoAulas` | `saldo_aulas` | INT NULL, `CHECK (saldo_aulas IS NULL OR (modalidade = 'Pacote' AND saldo_aulas >= 0))` | Só Pacote (FR-004) |
| `Ativo` | `ativo` | BOOLEAN NOT NULL DEFAULT TRUE | Exclusão lógica (FR-010) |
| — | `chave_ativa` | VARCHAR(30) GENERATED (VIRTUAL) `= IF(ativo, CONCAT(aluno_id, ':', IFNULL(turma_id, 0)), NULL)` + `UNIQUE INDEX uq_vinculocobranca_chave_ativa` | Garante FR-005/FR-006/FR-007 no banco; não é mapeada no EF |

Navegações: `Aluno` (obrigatória), `Turma` (opcional). `Aluno.VinculosCobranca` e `Turma.VinculosCobranca` são coleções de navegação aditivas; nenhuma propriedade existente de `Aluno`/`Turma` muda (FR-012).

Índices auxiliares: `idx_vinculocobranca_aluno (aluno_id)` para a listagem por aluno (a FK já cria índice no MySQL/InnoDB; declarar explicitamente apenas se o script padrão do projeto o fizer).

## Enum: ModalidadeCobranca (`SPI.Domain.Enums`)

`Avulsa = 1`, `Mensalidade = 2`, `Pacote = 3`. Persistido como texto (`HasConversion<string>()`); trafega em JSON como string (`JsonStringEnumConverter` global).

## Regras de validação (dono único: backend)

| Regra | Onde | Resposta |
|---|---|---|
| `Modalidade` é um dos três valores | Model binding + `VinculoCobrancaRequestValidator` | 400 |
| `Valor > 0` e `<= 99999999.99` (FR-002) | `VinculoCobrancaRequestValidator` | 400 |
| `AulasIncluidas` informado ⇒ Modalidade = Mensalidade e valor ≥ 1 (FR-003) | `VinculoCobrancaRequestValidator` | 400 |
| `SaldoAulas` informado ⇒ Modalidade = Pacote e valor ≥ 0 (FR-004) | `VinculoCobrancaRequestValidator` | 400 |
| Aluno existe | `VinculoCobrancaService` | 404 |
| Vínculo existe e pertence ao aluno da rota | `VinculoCobrancaService` | 404 |
| `TurmaId` informado ⇒ turma existe (404), está ativa e o aluno participa (409) — no cadastro e quando `TurmaId` muda na edição (FR-013) | `VinculoCobrancaService` (via `ITurmaRepository`) | 404 / 409 |
| Nenhum outro vínculo **ativo** do mesmo aluno na mesma turma (ou ambos sem turma), no cadastro, na edição (se a chave muda) e na reativação (FR-005/006/007/014) | `VinculoCobrancaService` + índice único | 409 |

## Transições de estado

```text
(novo) --Cadastrar--> Ativo=true
Ativo=true --Excluir--> Ativo=false            (idempotente; nunca DELETE físico)
Ativo=false --Reativar--> Ativo=true           (409 se já existir outro ativo com a mesma chave)
Ativo=true --Atualizar--> Ativo=true           (revalida regras conforme tabela acima)
```

Editar um vínculo inativo: permitido? **Não** — `AtualizarAsync` opera apenas em vínculos ativos (um vínculo inativo é editado depois de reativado), evitando revalidar unicidade sobre linhas fora da chave ativa. Tentativa → 409 ("Reative o vínculo antes de editá-lo."). A interface só oferece "Reativar" para vínculos excluídos.

## Entidades relacionadas (inalteradas)

- `Aluno`: `ValorAula` continua sendo a única fonte de valor da cobrança automática (FR-012).
- `Turma`, `AlunoTurma`: apenas lidas para validar FR-013; nunca escritas por esta feature.
- `Aula`, `Pagamento`, `PagamentoAula`: intocadas.

## Alteração aditiva em DTO existente

`TurmaResumoResponse` (`SPI.Application.Common.Dtos`) ganha `bool Ativo`, preenchido em `AlunoService` a partir de `at.Turma.Ativo`; espelhado como `ativo: boolean` em `TurmaResumo` no frontend.
