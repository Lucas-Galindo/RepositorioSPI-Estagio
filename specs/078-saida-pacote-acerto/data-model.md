# Modelo de dados: Saída do aluno de turma de pacote (spec 078)

**Uma migração pequena (28), só acréscimos.** Sem tabela nova e sem alterar dado existente.

## Migração `database/28_saida_pacote_acerto.sql`

```sql
-- 1) Marca o pacote encerrado pela saída do aluno (specs/078).
ALTER TABLE vinculo_cobranca
    ADD COLUMN saida_registrada_em DATETIME NULL
    COMMENT 'Quando a saida do aluno foi registrada com acerto (specs/078); so pacote inativo' AFTER ativo;

-- 2) Só um pacote inativo pode ter sido encerrado pela saída.
ALTER TABLE vinculo_cobranca
    ADD CONSTRAINT chk_vinculocobranca_saida CHECK (
        saida_registrada_em IS NULL OR (modalidade = 'Pacote' AND ativo = FALSE)
    );

-- 3) Categoria de receita da conta de diferença (só insere se ainda não existir).
INSERT INTO categoria_receita (nome, ativo)
SELECT 'Acerto de pacote', TRUE FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM categoria_receita WHERE nome = 'Acerto de pacote');
```

- Pré-requisito: 01 a 27. Backup antes (`database/backup`). Aplicar só no banco local durante a implementação (produção em standby, como as migrações 21–27).
- **Não muda**: `uq_pagamento_vinculo_competencia` (uma conta por mês no vínculo — a conta do acerto é uma só), `chk_vinculocobranca_*` existentes, matrículas, pagamentos e aulas.
- Cuidado conhecido (spec 062): `chk_vinculocobranca_periodo` e a coluna gerada `chave_ativa` (unicidade dos vínculos ativos) não são tocados; inativar o pacote **libera** a chave, como em qualquer exclusão.

## Entidades (C#)

| Entidade | Mudança |
|---|---|
| `VinculoCobranca` | `DateTime? SaidaRegistradaEm` (mapeada em `VinculoCobrancaConfiguration`: coluna `saida_registrada_em`). Só um pacote inativo a tem. |
| `Pagamento` | nenhuma (a conta do acerto é um `Pagamento` comum: `Status = "Pendente"`, `VinculoCobranca` = o pacote, `CategoriaReceita` = "Acerto de pacote") |
| `AlunoTurma` | nenhuma (a saída remove a matrícula como o desvincular de hoje) |
| `AulaAluno` | nenhuma de esquema; a saída **remove** as linhas do aluno nas aulas agendadas da turma que ainda não começaram (`Reposicao = false`). Presença/falta registradas nunca são removidas (só aulas que ainda não começaram) |

## DTOs (Application)

| DTO | Campos |
|---|---|
| `SaidaPacoteRequest` (prévia e gravação) | `ValorPago` (decimal ≥ 0, ≤ 2 casas), `Vencimento` (DateOnly?) |
| `ConfirmarSaidaPacoteRequest` : `SaidaPacoteRequest` | `ConsumidasEsperadas` (int), `ValorDevidoEsperado` (decimal), `AulasQueSairaoEsperadas` (int) — ecos da prévia |
| `AcertoSaidaPacoteResponse` | `AlunoId`, `AlunoNome`, `TurmaId`, `TurmaNome`, `ValorPacote`, `TotalAulas`, `TotalEstimado`, `AulasConsumidas`, `ValorPorAula`, `ValorDevido`, `ValorPago`, `Diferenca`, `Excedente`, `Consequencia` (`GerarConta` \| `Fechado` \| `ExcedenteNaoDevolvido`), `PrecisaVencimento`, `AulasAgendadasQueSairao` (int: aulas agendadas que ainda não começaram das quais o aluno será retirado) |
| `SaidaPacoteRegistradaResponse` | `Acerto` (acima), `PagamentoId` (int?, só se gerou conta) |
| `AlunoResumoResponse` (existente) | + `PodeRegistrarSaida` (bool) |
| `VinculoCobrancaResponse` (existente) | + `SaidaRegistradaEm` (DateTime?) |

## Regras de validação

- Formato (FluentValidation, `SaidaPacoteRequestValidator`): `ValorPago ≥ 0` e no máximo 2 casas decimais; teto de sanidade `≤ 9.999.999,99`. Falha → **400** com a lista de mensagens.
- Negócio (no serviço, **409**): turma e aluno existem (404), aluno matriculado, turma de pacote em andamento, pacote ativo da turma do aluno, total definido, vencimento obrigatório e ≥ hoje quando `Diferenca > 0`, consumo igual ao esperado.

## Transição de estado do pacote

`Ativo` ──(Registrar saída)──▶ `Inativo` + `SaidaRegistradaEm` preenchida. **Sem volta**: `ReativarAsync` recusa; o encerramento automático ao fim da turma (spec 063) continua deixando `SaidaRegistradaEm` nula.
