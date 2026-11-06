# Research: Turma ↔ Matéria (spec 075)

## R1 — Coluna e chave na própria turma

**Decisão**: `turma.materia_id INT NULL` com `FOREIGN KEY fk_turma_materia (materia_id) REFERENCES materias(id)` (sem `ON DELETE`: as matérias têm exclusão lógica, então o vínculo nunca fica órfão). Nova migração `database/27_turma_materia.sql`, no estilo da 25 (cabeçalho com segurança e pré-requisito, `USE spi_db`, só `ALTER TABLE turma` e um `UPDATE` em `turma`).

**Por quê**: uma matéria por turma e opcional (clarificações); só `turma` é alterada (FR-009).

## R2 — Preenchimento das turmas existentes (migração)

**Decisão**: `UPDATE turma t SET t.materia_id = (SELECT MIN(a.materia_id) FROM aula a WHERE a.turma_id = t.id AND a.ativo = TRUE) WHERE t.materia_id IS NULL AND (SELECT COUNT(DISTINCT a.materia_id) FROM aula a WHERE a.turma_id = t.id AND a.ativo = TRUE) = 1;`. Só preenche quando as aulas ativas da turma usam **uma** matéria; turma sem aulas ou com várias fica `NULL`. A condição `materia_id IS NULL` torna a segunda execução inócua (idempotente); a coluna e a chave só são criadas se ainda não existirem (consulta ao `information_schema`, como nos scripts anteriores). Nenhum `DELETE`; nenhuma outra tabela é tocada.

## R3 — Backend

**Decisão**:
- `Turma.MateriaId (int?)` e `Materia? Materia` (navegação) em `SPI.Domain/Entities/Turma.cs`; mapeamento em `TurmaConfiguration.cs`.
- `TurmaRequest.MateriaId (int?)`; `TurmaResponse.MateriaId` e `MateriaNome`.
- Validação **no `TurmaService`** (precisa consultar a matéria): ao criar, ou ao editar quando o `MateriaId` **mudou**, a matéria deve existir e estar **ativa** (inexistente → `NaoEncontradoException`; inativa → `ConflitoException`, mensagens claras). Se o `MateriaId` enviado é o mesmo que a turma já tem, não revalida (matéria inativada depois pode ser mantida — FR-003). No PUT, `MateriaId` nulo remove o vínculo (FR-002).
- O serviço passa a receber `IMateriaRepository` (injeção; já registrado no container); os testes que constroem o `TurmaService` ganham o parâmetro (mesmo caso de fakes das specs 071/074).
- `TurmaRepository`: `Include(t => t.Materia)` nas leituras que mapeiam para `TurmaResponse` (lista e detalhe), sem mudar filtros nem ordenação (FR-008).

## R4 — A regra de aulas não muda

**Decisão**: `AulaService` e seus validadores não leem `Turma.MateriaId` (FR-007). A pré-seleção é só do formulário.

## R5 — Frontend

**Decisão** (sem arquivo novo):
- `lib/api/turmas.ts`: `Turma` ganha `materiaId`/`materiaNome` e `TurmaRequest` ganha `materiaId`.
- `components/turmas/TurmaForm.tsx`: `<select>` "Matéria (opcional)" com "Sem matéria" + matérias ativas (`listarMaterias({ ativo: true })`) + a matéria atual da turma mesmo inativa (marcada "(inativa)"); envia `materiaId` (ou `null`).
- `app/(app)/turmas/page.tsx` e `turmas/[id]/page.tsx`: exibir o nome da matéria quando houver.
- `components/aulas/AulaForm.tsx`: ao trocar a turma na **criação**, se `materiaId` do formulário está vazio e a turma escolhida tem `materiaId`, preenche; nunca sobrescreve uma escolha; sem efeito na edição. `listarTurmas` já traz a turma; basta o campo novo.

## R6 — Testes

**Decisão**: `TurmaServiceMateriaTests` (xUnit, mesmo estilo de `TurmaServiceTipoTests`): criar com matéria válida (response com nome), inexistente (404), inativa (409); editar trocando por ativa, por inativa (409), mantendo a mesma matéria inativa (ok), removendo (nula); turma sem matéria inalterada. Banco: conferir a migração com contagens antes/depois e uma consulta por turma (SC-002), com backup antes.

## R7 — Riscos

| Risco | Mitigação |
|---|---|
| Migração tocar outras tabelas ou perder dados | só `ALTER`/`UPDATE` em `turma`; backup antes; contagens de todas as tabelas antes/depois; `git diff` só do `.sql` novo |
| Construtor do `TurmaService` quebrar testes | atualizar os construtores; `dotnet test` (sem `--no-build`; com `--artifacts-path` se a API do usuário travar as DLLs) |
| Pré-selecionar sobrescrever escolha da professora | só preenche se `materiaId` está vazio; testado no Chrome |
| Aula passar a ser barrada pela matéria da turma | nenhuma leitura de `Turma.MateriaId` no `AulaService`; verificação por leitura do diff |
| Produção | migração aplicada **só no banco local**; produção fica em standby (decisão do usuário) |
