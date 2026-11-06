# Relatório: Turma ↔ Matéria (spec 075)

**Data**: 2026-11-05 · **Card**: "Turma ↔ Matéria: vincular matéria no cadastro da turma (banco + backend + formulário)" · **Spec**: `specs/075-turma-materia/`

## Pedido e decisões

Vincular uma matéria à turma no cadastro (base do filtro de matéria em Turmas). Decisões do usuário: matéria **opcional**, **uma** por turma; turmas existentes recebem a matéria **a partir das aulas** quando só há uma; na nova aula a matéria da turma vem **pré-selecionada mas trocável**.

## O que foi feito

| Camada | Arquivo | Mudança |
|---|---|---|
| Banco | `database/27_turma_materia.sql` (novo) | `turma.materia_id` (opcional) + chave `fk_turma_materia`; preenchimento das turmas existentes; idempotente; só toca `turma`; sem `DELETE` |
| Domínio/EF | `Turma.cs`, `TurmaConfiguration.cs` | `MateriaId` e navegação, relação opcional sem cascata |
| Backend | `TurmaService.cs`, `TurmaRepository.cs`, DTOs, `TurmasController.cs` | `materiaId` no request; `materiaId`/`materiaNome` na resposta; criar ou trocar exige matéria existente (404) e ativa (409), manter a mesma matéria inativada é permitido, nulo no PUT remove; `Include(Materia)`; `404` declarado no POST |
| Testes | `TurmaServiceMateriaTests.cs` (novo) | 11 testes |
| Frontend | `lib/api/turmas.ts`, `TurmaForm.tsx`, `turmas/page.tsx`, `turmas/[id]/page.tsx`, `AulaForm.tsx` | campo "Matéria (opcional)" (ativas + a atual mesmo inativa, marcada "(inativa)"); matéria na lista e nos detalhes; pré-seleção em Nova aula só se a matéria ainda não foi escolhida e só na criação |
| Docs | `specs/063-pacote-fora-da-turma/spec.md` | nota apontando para a 075 |

A regra de aulas não mudou: nenhum arquivo de `Aulas/` foi alterado nem lê `Turma.MateriaId`.

## Validação

- **Banco local** (backup `database/backup/spi_db_20261105_215622.sql` antes): turma 3 recebeu "Matemática" (suas aulas ativas só usam essa); turmas 4 e 5 (duas matérias) e 15 (sem aulas) ficaram sem matéria; contagens de todas as tabelas iguais; a segunda execução não alterou nada.
- **Testes**: 712 aprovados (701 + 11). `tsc` limpo, `eslint` 0 problemas, `npm run build` ok.
- **API real**: 17 de 18 no primeiro passe. O POST com matéria inexistente devolvia 500 porque a ação não declarava o 404 (o filtro de erros só mapeia o que é declarado); corrigido, e o reteste passou. Cobriu lista/detalhe após a migração, POST com matéria ativa, sem matéria, inexistente e inativa, PUT trocando, mantendo a mesma matéria inativada, removendo, e **aula de outra matéria numa turma com matéria (201, sem restrição)**. Dados de teste ("TESTE-075", uma aula) removidos e matéria "teste" restaurada; só `refresh_token` difere (+2, dos meus logins).
- **Chrome (login real, capturas)**: **17 PASS, 0 FAIL**: matéria na lista e nos detalhes; formulário com "Sem matéria" + ativas; cadastro, troca, remoção; matéria inativada continua na lista como "(inativa)" e pode ser mantida; pré-seleção em Nova aula (não sobrescreve escolha, troca livre, turma sem matéria e edição de aula não pré-selecionam); 360 px sem rolagem horizontal; 0 erros de console.
- **MUST NOT**: o `.sql` sem `DELETE/DROP/TRUNCATE` e só altera `turma`; `Aulas/` intocado; sem `csproj`/`package.json` alterados; migração **só no banco local**.

## Pendências e observações

- **Produção**: a migração 27 **não** foi aplicada em produção (fila junto das 21 a 26, em standby por decisão do usuário).
- O filtro de matéria na tela de Turmas é outro card (agora desbloqueado).
- Sem commit e sem push (aguarda pedido). O `analyze` rodou na fase de planejamento.
