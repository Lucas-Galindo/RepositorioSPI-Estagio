# Research: Recorrência na criação de aulas (spec 077)

## R1 — Dois endpoints novos, sem tocar o de aula única

**Decisão**: `POST /api/aulas/serie/previa` (calcula e devolve as datas com conflito, sem gravar) e `POST /api/aulas/serie` (cria). O `POST /api/aulas` e o `PUT` ficam **como estão** (FR-009). Mesmo `AulasController`, mesmo `AulaService` (novo método `CadastrarSerieAsync` e `PreVisualizarSerieAsync`).

## R2 — Geração das datas: função pura no servidor

**Decisão**: `GeradorSerieAulas.Gerar(dataInicial, horarios, quantidade?, dataFinal?, limite)` em `SPI.Application/Aulas/Services/`, **estática e pura** (sem banco, sem relógio), devolvendo `List<OcorrenciaSerie>` (data, dia da semana, início, fim). Percorre dia a dia a partir de `dataInicial`; em cada data cujo dia da semana tem horário(s) emite as ocorrências (ordenadas pelo início); para ao atingir `quantidade` ou passar de `dataFinal`; **interrompe e sinaliza** quando passa de `limite` (60) para não varrer períodos enormes. Datas ficam como `DateOnly`, sem `DateTime` (sem fuso).

**Por quê**: Princípio II (a regra mora no servidor) e testabilidade (casos de borda sem banco).

## R3 — Regras de validação

**Decisão**:
- `RecorrenciaAulaRequestValidator` (FluentValidation, como `AulaRequestValidator`): `materiaId > 0`; turma XOR aluno (mesma regra da aula única); `horarios` não vazio, `diaSemana` 0–6, `horaFim > horaInicio`, **sem sobreposição no mesmo dia da semana**; **exatamente um** entre `quantidadeAulas` (1–60) e `dataFinal` (≥ `dataInicial`).
- No serviço (precisa de relógio e banco): `dataInicial >= hoje` (relógio local `_relogio.AgoraLocal()`, só a data) → `ConflitoException`; total > 60 (por `dataFinal`) → `ConflitoException` com o total; referências (matéria/turma/aluno) pelo `ValidarReferenciasAsync` existente (montando um `AulaRequest` de uma ocorrência); conflitos pelo `ExisteConflitoAsync` existente, uma consulta por ocorrência (≤ 60).

## R4 — Atomicidade

**Decisão**: montar **todas** as `Aula` já com `AulaAlunos` preenchidos (alunos ativos da turma, ou o aluno) e chamar `AdicionarAsync` para cada uma e **um único** `SalvarAlteracoesAsync`: o EF grava tudo numa só transação (ou tudo ou nada). O lembrete da turma é recalculado **uma vez** depois de salvar. (Diferente do cadastro único, que salva a aula e depois os alunos em dois `SalvarAlteracoes`.)

## R5 — Conflito: bloquear tudo

**Decisão**: a prévia devolve cada ocorrência com `conflito` (true/false) e `podeCriar`; a criação, ao achar conflito, lança `ConflitoException` com a lista das datas (até 10, "e mais N") e não grava nada (FR-005). Como o cálculo é o mesmo nos dois endpoints, a prévia e a criação concordam.

## R6 — Frontend

**Decisão**:
- `lib/api/aulas.ts`: tipos `HorarioSerie`, `SerieAulasRequest`, `PreviaSerie`, `SerieCriada` e as funções `previaSerieAulas` e `cadastrarSerieAulas`.
- **Componente novo** `components/aulas/RecorrenciaAulas.tsx`: caixa "Repetir", data inicial, linhas de dia+início+fim (adicionar/remover), término (quantidade ou data final), botão "Ver prévia" e a lista da prévia (data, dia, horário, selo de conflito, total). Só estado dos campos e exibição.
- `AulaForm.tsx`: ao marcar "Repetir" (só se `!editando`), mostra o componente no lugar da seção "Data e horário" e o envio chama `cadastrarSerieAulas`; sucesso: toast "N aulas agendadas", `onSalvo(primeiraAula)` (pop-up) ou `router.push("/aulas")`.
- Dias da semana com rótulos "Domingo"… "Sábado" (valores 0–6). A data inicial usa `DateInput` com `min` = hoje (apoio visual; o servidor decide).

## R7 — Testes

**Decisão**: `GeradorSerieAulasTests` (função pura: um dia por semana; dois dias; data inicial fora dos dias; término por quantidade e por data final; limite; mesmo dia com dois horários; virada de mês/ano; domingo a sábado), `AulaServiceSerieTests` (usando os fakes de cadastro: cria N aulas com alunos, conflito bloqueia tudo e não grava, passado recusado, > 60 recusado, referências inexistentes, lembrete recalculado uma vez, cadastro único inalterado) e `RecorrenciaAulaRequestValidatorTests`. API real e Chrome com login real, com dados de teste removidos depois.

## R8 — Riscos

| Risco | Mitigação |
|---|---|
| Criação parcial | um único `SalvarAlteracoesAsync` com os alunos já em cada aula; teste de "nada criado" no conflito |
| Mudar o cadastro único | métodos novos separados; teste de regressão do cadastro e `git diff` do trecho existente |
| Varredura enorme por data final distante | o gerador interrompe ao passar de 60 |
| Pré-visualização e criação divergirem | os dois endpoints usam o mesmo gerador e a mesma checagem de conflito |
| `ExisteConflitoAsync` × 60 | no máximo 60 consultas simples; aceitável (uso eventual) |
