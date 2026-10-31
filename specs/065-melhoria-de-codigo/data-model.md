# Data Model: Melhoria do Código (spec 065)

**Sem mudança de dados.** Nenhuma tabela, coluna, índice ou migração é criada ou alterada (FR-001). Nenhuma entidade nova.

## Elementos de código novos

| Elemento | Camada | Papel |
|---|---|---|
| `ErrosDeNegocioFilter` | `SPI.Api/Filters` | Filtro de exceção que traduz `NaoEncontradoException`, `ConflitoException` e erros inesperados para 404, 409 e 500, conforme os `ProducesResponseType` da ação (ver contracts/erros-da-api.md) |
| `ErrosDeNegocioFilterTests` | `tests/.../Api` | Garante o mapeamento e a igualdade com o formato de hoje |

## Estruturas existentes que ganham clareza (sem mudar contrato)

- `VinculoCobrancaService.ValidarTurmaDoVinculoAsync` (novo método privado): reúne a decisão "pacote vai para a turma de pacote, avulsa vai para a turma do aluno".
- Comentários de regra em métodos de `Services`, `Repositories` e `BackgroundServices` (sem alterar o código deles).
