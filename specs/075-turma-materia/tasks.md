# Tasks: Turma ↔ Matéria — vincular a matéria no cadastro da turma

**Input**: `specs/075-turma-materia/` (spec, plan, research, data-model, contracts/turmas.md, quickstart). **Ambiente**: `dotnet test` **sem** `--no-build` (com `--artifacts-path` se a API do usuário travar as DLLs); `NODE_OPTIONS=--max-old-space-size=1536`; senha só por `SPI_TESTE_SENHA`; credencial do banco só por User Secrets/variáveis (nunca impressa); **não encerrar nem buildar por cima de processos do usuário**; migração **só no banco local**; sem commit/push até o usuário pedir.

## Phase 1: Linha de base e backup

- [X] T001 Linha de base: `git status --porcelain` limpo; `dotnet test` (701 aprovados); `tsc` e `eslint` limpos. **Backup** do banco local em `database/backup/` (mysqldump); registrar no scratchpad a contagem de linhas de **todas** as tabelas, o dump das turmas (id, nome, tipo, ativo) e, por turma, as matérias distintas das aulas ativas; `GET /api/turmas` completo. Backup `database/backup/spi_db_20261105_215622.sql`; base: 4 turmas (3, 4, 5, 15), 16 aulas no banco, contagens em `base75-contagens.txt` (scratchpad).

## Phase 2: Banco (US2)

- [X] T002 [US2] `database/27_turma_materia.sql` (novo, estilo da 25: cabeçalho de descrição/segurança/pré-requisito, `USE spi_db`): criar `turma.materia_id INT NULL` (`AFTER nome`) e `FOREIGN KEY fk_turma_materia ... REFERENCES materias(id)` **somente se ainda não existirem** (consulta ao `information_schema`); `UPDATE turma t SET t.materia_id = (SELECT MIN(a.materia_id) FROM aula a WHERE a.turma_id = t.id AND a.ativo = TRUE) WHERE t.materia_id IS NULL AND (SELECT COUNT(DISTINCT a.materia_id) FROM aula a WHERE a.turma_id = t.id AND a.ativo = TRUE) = 1`. Sem `DELETE` e sem tocar outra tabela.
- [X] T003 [US2] Aplicar o script no **banco local** pela CLI `mysql` (regra do projeto; sem imprimir a credencial) e conferir (quickstart §1): colunas e chave criadas; matéria preenchida só onde as aulas ativas têm uma matéria; contagens de todas as tabelas **iguais** às do T001; segunda execução sem mudança. **Aplicada no banco local**: turma 3 → Matemática; turmas 4, 5 (duas matérias) e 15 (sem aulas) → sem matéria; contagens de todas as tabelas iguais; segunda execução sem efeito.

## Phase 3: Backend (US1)

- [X] T004 [US1] `src/SPI.Domain/Entities/Turma.cs` (`int? MateriaId`, `Materia? Materia`) e `src/SPI.Infrastructure/Persistence/Configurations/TurmaConfiguration.cs` (coluna `materia_id`, relação opcional com `Materia`, sem cascata).
- [X] T005 [P] [US1] `src/SPI.Application/Turmas/Dtos/TurmaRequest.cs` (`int? MateriaId`) e `TurmaResponse.cs` (`int? MateriaId`, `string? MateriaNome`).
- [X] T006 [US1] `src/SPI.Infrastructure/Repositories/TurmaRepository.cs`: `Include(t => t.Materia)` nas leituras que viram `TurmaResponse` (lista e detalhe), sem mudar filtros nem ordenação.
- [X] T007 [US1] `src/SPI.Application/Turmas/Services/TurmaService.cs`: receber `IMateriaRepository`; em `CadastrarAsync` e `AtualizarAsync` validar a matéria **ao criar o vínculo ou ao trocá-lo** (inexistente → `NaoEncontradoException`; inativa → `ConflitoException`, mensagens claras), **não** revalidar quando o `MateriaId` é o mesmo da turma; `null` no PUT remove o vínculo; mapear `MateriaId`/`MateriaNome` na resposta. Atualizar os construtores do `TurmaService` nos testes existentes.
- [X] T008 [P] [US1] `tests/SPI.Application.Tests/Turmas/TurmaServiceMateriaTests.cs` (novo): criar com matéria ativa (resposta traz o nome), inexistente (404), inativa (409); editar trocando por ativa, por inativa (409), mantendo a mesma matéria inativada (ok), removendo (nula); turma sem matéria inalterada.
- [X] T009 [US1] `dotnet test` (sem `--no-build`) até passar tudo (701 + novos). 712 aprovados (701 + 11 novos).

## Phase 4: Frontend (US1, US3)

- [X] T010 [P] [US1] `frontend/lib/api/turmas.ts`: `Turma` com `materiaId`/`materiaNome`; `TurmaRequest` com `materiaId`.
- [X] T011 [US1] `frontend/components/turmas/TurmaForm.tsx`: `<select>` "Matéria (opcional)" ("Sem matéria" + matérias ativas via `listarMaterias({ ativo: true })` + a matéria atual da turma, mesmo inativa, marcada "(inativa)"); enviar `materiaId` (ou `null`).
- [X] T012 [P] [US1] `frontend/app/(app)/turmas/page.tsx` e `app/(app)/turmas/[id]/page.tsx`: mostrar o nome da matéria quando houver (linha discreta no card e nos detalhes); sem CSS novo se possível.
- [X] T013 [US3] `frontend/components/aulas/AulaForm.tsx`: na **criação**, ao escolher a turma, se o `materiaId` do formulário estiver vazio e a turma tiver `materiaId`, preencher; nunca sobrescrever uma escolha; sem efeito na edição ou na aula individual; troca livre.
- [X] T014 [US1] `cd frontend`: `npx tsc --noEmit` e `npx eslint` (0 problemas).

## Phase 5: Acabamento e validação

- [X] T015 [P] Nota na `specs/063-pacote-fora-da-turma/spec.md` (que definiu o tipo da turma): "specs/075: a turma passou a ter uma matéria opcional (`materia_id`); ver `specs/075-turma-materia/`." (Princípio V).
- [X] T016 **Verificação dos "MUST NOT" FR-005, FR-007, FR-008, FR-009**: (a) FR-005: `git diff`/leitura de `database/27_turma_materia.sql` sem `DELETE`/`DROP`/`TRUNCATE` e com `ALTER`/`UPDATE` **só** em `turma`; contagens de todas as tabelas iguais antes/depois (T003); (b) FR-007: `grep -rn "MateriaId" src/SPI.Application/Aulas` e leitura do diff mostrando que nenhum arquivo de `Aulas/` lê `Turma.MateriaId` ou compara com a matéria da turma, e `git diff --name-only -- src/SPI.Application/Aulas` vazio; (c) FR-008: `GET /api/turmas` com os mesmos campos/valores/ordem de antes (exceto os campos novos) e nenhum filtro/ordenação alterado no `TurmaRepository` (leitura do diff); (d) FR-009: `git diff -- frontend/package.json "src/**/*.csproj"` vazio, só `database/27_turma_materia.sql` novo em `database/`, e a migração **não** aplicada em produção.
- [X] T017 API real (quickstart §3), com dados de teste "TESTE-075" e **limpeza** no fim (contagens iguais às do T001): POST com matéria ativa (201 e nome), inexistente (404), inativa (409); PUT trocando, mantendo a mesma matéria inativada e removendo. **17 PASS, 1 FAIL do roteiro**: o POST com matéria inexistente devolveu 500 (a ação não declarava 404; corrigido com `[ProducesResponseType(404)]` no POST) e a comparação de contagens estranhou o BOM do arquivo e as linhas de `refresh_token` criadas pelos meus logins. Reexecução: POST 404 ok; só `refresh_token` (+2, login) difere; turmas e matérias de teste removidas/restauradas.
- [X] T018 Chrome com login real (quickstart §4), claro, escuro e 360 px, com capturas; 0 erros de console. **17 PASS, 0 FAIL.**
- [X] T019 Final: `dotnet test`, `tsc`, `eslint` e, **se o ambiente do usuário estiver parado**, `npm run build` (senão registrar que não rodou). 712 testes, `tsc`, `eslint` e `npm run build` ok (ambiente do usuário parado).
- [X] T020 Fechamento: `docs/relatorios/<data>-turma-materia.md`; hook `revisao-codigo` do Trello (se recusar por causa de DLLs travadas, mover à mão com comentário); memória do projeto; **sem commit/push**.

## Dependências e ordem

T001 → T002 → T003 → T004 → T005/T006 → T007 → T008 → T009; T010–T014 (após T005); T016–T018 só com tudo pronto → T019 → T020.

## Cobertura dos requisitos

| Requisito | Tarefas |
|---|---|
| FR-001 (coluna/chave, no máximo uma) | T002, T004 |
| FR-002 (POST/PUT, PUT nulo remove) | T005, T007, T008, T017 |
| FR-003 (inexistente/inativa; manter a mesma) | T007, T008, T017 |
| FR-004 (resposta e exibição) | T005, T006, T010, T012, T018 |
| FR-005 (**MUST NOT** perder/alterar dados; idempotente) | T002, T003, **T016(a)** |
| FR-006 (pré-seleção em nova aula) | T013, T018 |
| FR-007 (**MUST NOT** mudar a regra de aulas) | **T016(b)**, T009 |
| FR-008 (**MUST NOT** mudar turmas sem matéria) | **T016(c)**, T017 |
| FR-009 (**MUST NOT** dependência/outras tabelas/produção) | **T016(d)** |
| SC-001 a SC-004 | T008, T003, T017, T018, T016 |
