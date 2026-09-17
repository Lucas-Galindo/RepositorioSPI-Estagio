# Research: Reativar Registros Desativados (Turma, Aluno, Matéria)

## Contexto levantado no código atual

- O fluxo de desativação (`DELETE /api/{turmas,alunos,materias}/{id}`) é idêntico nas três
  entidades: controller com `[Authorize(Roles = nameof(PerfilUsuario.Professor))]` a nível de
  classe (sem override por ação), chamando um `ExcluirAsync(id)` no service que só faz
  `ObterPorIdAsync` → `entidade.Ativo = false` → `SalvarAlteracoesAsync`. Nenhum cascade, nenhuma
  checagem de registros relacionados, nenhuma mutação de histórico.
- `ITurmaRepository`, `IAlunoRepository`, `IMateriaRepository` são interfaces independentes, sem
  classe base genérica/compartilhada — é o padrão já estabelecido no projeto (confirmado
  varrendo `src/SPI.Domain/Repositories/*.cs`, nenhum repositório usa abstração genérica).
- No frontend, o `StatusPill` (somente leitura) e o botão "Excluir" vivem só na tela de detalhe
  (`[id]/page.tsx`) de cada entidade — as listagens não têm ações por linha hoje. O botão
  "Excluir" é renderizado incondicionalmente (mesmo em registro já Inativo), sem checar `ativo`.
- Após excluir, as três telas de detalhe navegam de volta para a listagem
  (`router.push("/turmas")` etc.) em vez de atualizar o estado local — a listagem então recarrega
  os dados do zero. Não existe hoje um padrão de "atualizar a tela sem navegar" para copiar.
- `frontend/lib/api/client.ts` tem `apiGet`, `apiPost`, `apiPut`, `apiPostFile`, `apiGetBlob`,
  `apiDelete` — **não tem `apiPatch`**.
- As checagens de unicidade existentes (`MateriaRepository.ExisteNomeAsync`,
  `AlunoRepository.ExisteCpfAsync`) já rodam contra todos os registros (ativos ou não) no
  cadastro/edição — confirmado durante a especificação (spec.md FR-008), então reativar nunca
  pode colidir com um registro já Ativo.

## Decisão 1: Método de reativação independente por serviço, espelhando `ExcluirAsync`

**Decision**: Cada serviço (`TurmaService`, `AlunoService`, `MateriaService`) ganha um
`ReativarAsync(int id)` próprio, com a mesma forma do `ExcluirAsync` já existente
(`ObterPorIdAsync` → `entidade.Ativo = true` → `SalvarAlteracoesAsync`). Nenhuma classe base
genérica nova é introduzida.

**Rationale**: O projeto já tem três implementações independentes do mesmo padrão trivial de 3
linhas para desativação — é a convenção estabelecida (ver Stack Tecnológico da constituição:
sem abstração de repositório genérica). Criar uma abstração nova só para reaproveitar 3 linhas
de código entre 3 entidades adicionaria uma camada de indireção desproporcional ao ganho, e
divergiria do estilo já usado no restante do backend. "Reaproveitar lógica comum" (pedido do
usuário) é atendido pela **consistência estrutural** — mesmo nome de método, mesma forma, mesmo
posicionamento no controller — não por herança/generics.

**Alternatives considered**:
- **Serviço/repositório genérico `IReativavelService<T>`**: rejeitado — deslocaria o projeto do
  padrão não-genérico já estabelecido, para um ganho pequeno (3 métodos triviais).

## Decisão 2: Rota e verbo — `PATCH /api/{entidade}/{id}/reativar`

**Decision**: Cada controller ganha uma ação `[HttpPatch("{id}/reativar")]`, sem `[Authorize]`
próprio — herda o `[Authorize(Roles = nameof(PerfilUsuario.Professor))]` já aplicado a nível de
classe, idêntico ao usado por `Excluir`.

**Rationale**: Mantém a mesma permissão de quem já pode desativar (FR-006), sem criar nível de
acesso novo (Princípio III da constituição — nenhuma capacidade exposta sem implementação real
e sem controle de acesso correspondente). `PATCH` reflete corretamente uma mudança parcial de
estado (só o campo `Ativo`), consistente com o verbo sugerido no próprio pedido do usuário.

## Decisão 3: Resposta do endpoint — devolver o recurso atualizado (200), não 204

**Decision**: `Reativar` devolve o mesmo `Response` DTO já usado por `Obter{Entidade}`
(`TurmaResponse`/`AlunoResponse`/`MateriaResponse`), com `200 OK`, em vez de `204 No Content`.

**Rationale**: FR-004 exige que a tela reflita o novo status imediatamente, sem recarregar a
página manualmente. Como as telas de detalhe hoje não ficam na mesma página após `Excluir`
(navegam de volta à listagem), não existe um padrão pronto de "atualizar estado local" para
copiar — mas ao reativar, a usuária permanece na tela de detalhe (não faz sentido navegar para
lugar nenhum). Devolver o recurso atualizado permite ao frontend fazer
`setTurma(resposta)`/`setAluno(resposta)`/`setMateria(resposta)` diretamente, sem uma segunda
chamada `obterX` só para buscar o estado atualizado.

**Alternatives considered**:
- **204 No Content + refetch manual**: rejeitado — exigiria uma chamada HTTP extra
  (`obterTurma`/etc.) só para atualizar a tela, sem benefício real sobre devolver o recurso já
  atualizado na própria resposta do PATCH.

## Decisão 4: Adicionar `apiPatch` ao cliente HTTP do frontend

**Decision**: Adicionar uma função `apiPatch` em `frontend/lib/api/client.ts`, espelhando
`apiPut` (mesma assinatura, `method: "PATCH"`, sem corpo de requisição já que a reativação não
recebe payload).

**Rationale**: É a peça de infraestrutura mínima faltante — os outros 5 verbos já têm helper,
só falta esse. Reaproveitada pelas 3 novas funções `reativarTurma`/`reativarAluno`/
`reativarMateria` nos respectivos arquivos `lib/api/*.ts`, do mesmo jeito que `apiDelete` já é
reaproveitado pelas 3 funções `excluirX` existentes.

## Decisão 5: Botão "Reativar" só na tela de detalhe (não na listagem)

**Decision**: O botão "Reativar" é adicionado apenas às telas de detalhe
(`turmas/[id]/page.tsx`, `alunos/[id]/page.tsx`, `materias/[id]/page.tsx`), no mesmo lugar onde
hoje fica o botão "Excluir" e o `StatusPill`. As listagens (`turmas/page.tsx`, etc.) não ganham
nenhuma ação nova.

**Rationale**: A spec permite "listagem e/ou detalhe" (FR-003). Hoje as listagens não têm
nenhuma ação por linha — só links para a tela de detalhe; introduzir ações inline nas linhas da
listagem seria um padrão de UI novo, desproporcional ao pedido (que é dar um caminho de volta,
não redesenhar a listagem). A tela de detalhe já é o lugar natural, pois é onde o status e a
ação de desativar já vivem.

## Decisão 6: "Excluir" e "Reativar" tornam-se mutuamente exclusivos

**Decision**: Na tela de detalhe, o botão "Excluir" passa a aparecer somente quando o registro
está Ativo; o botão "Reativar" aparece somente quando está Inativo — nunca os dois ao mesmo
tempo.

**Rationale**: Hoje "Excluir" é renderizado incondicionalmente, mesmo num registro já Inativo
(clicar nele hoje é um no-op inofensivo, já que já está `Ativo=false`). FR-003 exige que
"Reativar" apareça só quando Inativo; deixar "Excluir" visível ao mesmo tempo criaria uma tela
com dois botões de ação de status conflitantes, confusa para a usuária. Esconder "Excluir" num
registro já Inativo é o contraponto natural e de baixíssimo risco (nenhum comportamento de
"Excluir" muda quando o registro está Ativo — só some quando já não há o que excluir).

## Decisão 7: Nenhuma validação nova na reativação

**Decision**: `ReativarAsync` não adiciona nenhuma checagem de unicidade (nome de Matéria, CPF
de Aluno) nem qualquer outra validação de negócio nova.

**Rationale**: Confirmado na especificação (spec.md FR-008): as checagens de unicidade já
existentes (`ExisteNomeAsync`, `ExisteCpfAsync`) rodam contra todos os registros, ativos ou não,
no momento do cadastro/edição — logo nunca existem dois registros com o mesmo nome/CPF, e
reativar nunca pode colidir com um registro já Ativo. Adicionar uma checagem redundante seria
trabalho sem efeito prático.
