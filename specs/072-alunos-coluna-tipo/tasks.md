# Tasks: Alunos — coluna "Tipo" de cobrança na tabela

**Input**: `specs/072-alunos-coluna-tipo/` ([spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [quickstart.md](quickstart.md))

**Formato**: `- [ ] T### [P?] [US?] descrição com caminho`.

**Ambiente**: `NODE_OPTIONS=--max-old-space-size=1536` no `tsc`/`eslint`/build; API em `http://localhost:5099` e Next com `NEXT_PUBLIC_API_URL` definida dentro do comando; senha só por `SPI_TESTE_SENHA`; sem commit/push até o usuário pedir. Frontend apenas: nenhum arquivo novo.

## Phase 1: Linha de base

- [X] T001 Anotar a linha de base: `git status --porcelain` (só os itens do usuário e a spec 071 ainda sem commit); `cd frontend`: `npx tsc --noEmit`, `npx eslint` (0 problemas); ler `components/alunos/SeloCobranca.tsx` e confirmar que `modo="lista"` só é usado em `app/(app)/alunos/page.tsx` (`grep -rn 'modo="lista"' frontend`). Confirmado: `modo="lista"` só era usado na tela de Alunos.

## Phase 2: User Story 1 — Coluna "Tipo" (P1) 🎯 MVP

**Goal**: o tipo numa coluna própria depois de "Cobrança". **Independent Test**: Alunos no Chrome.

- [X] T002 [US1] `frontend/components/alunos/SeloCobranca.tsx`: substituir o modo `"lista"` por `"tipo"` (pílula com `classeDaPilula` e "· restam N" no pacote, "+N" de `outrosVinculos` e a dica `title` com todos os vínculos, como hoje) e `"detalhe"` (a linha do valor: `currency(selo.valor)`, " · turma" e " · pacote esgotado", como hoje); `"grupo"` **sem alteração**; sem selo, os dois mostram "—". Atualizar o tipo da propriedade `modo` e o comentário do componente (specs/072).
- [X] T003 [US1] `frontend/app/(app)/alunos/page.tsx`: `<th>Tipo</th>` logo depois de `<th>Cobrança</th>` e, na linha, uma `<td>` com `<SeloCobranca selo={a.cobranca} modo="tipo" />` depois da `<td>` de Cobrança, que passa a usar `modo="detalhe"`. Nada mais muda na página.
- [X] T004 [US1] `cd frontend`: `npx tsc --noEmit` e `npx eslint` (projeto inteiro, 0 problemas).

**Checkpoint**: coluna funcionando; `tsc` e `eslint` verdes.

## Phase 3: Acabamento e validação

- [X] T005 [P] Nota na `specs/068-badge-cobranca-aluno/spec.md` (ou a pasta da 068 existente): "specs/072: na lista de Alunos, a pílula do tipo passou para a coluna própria 'Tipo' e a coluna 'Cobrança' mostra só valor e turma; ver `specs/072-alunos-coluna-tipo/`." (Princípio V).
- [X] T006 **Verificação dos "MUST NOT" FR-004, FR-005, FR-006**: (a) FR-004: ler o `git diff` de `SeloCobranca.tsx` e `alunos/page.tsx` e confirmar que só reorganiza o que já era exibido (nada de `.sort`, `.filter`, `.reduce`, comparação de categoria nova, soma ou regra de tipo/valor/vínculo no frontend); (b) FR-005: no diff de `SeloCobranca.tsx` o ramo `"grupo"` (textos, classes e condições) permanece idêntico e `turmas/[id]/page.tsx` e `aulas/[id]/page.tsx` não têm alteração (`git diff --name-only`); (c) FR-006: `git status --porcelain -- frontend src tests database` mostrando **só** `SeloCobranca.tsx` e `alunos/page.tsx` modificados pela 072 (mais o que já estava modificado antes e é de outras tarefas: `dashboard/page.tsx` da 071), nenhum arquivo não rastreado novo no frontend, e `git diff -- frontend/package.json` vazio.
- [X] T007 Chrome com login real (quickstart §2), claro, escuro e 360 px, **com capturas**: ordem do cabeçalho; João da Silva (Pacote / valor e turma); resposta de `GET /api/alunos` **simulada** (interceptação, sem gravar no banco) com Mensalidade, Semestral, Anual, Avulsa, Sem vínculo, pacote com "restam N", pacote esgotado, aluno com "+N" e dica, e aluno sem selo; clique na linha abre o aluno; página da turma e detalhes da aula iguais aos de antes; 360 px sem rolagem horizontal da página; 0 erros de console. **22 PASS, 0 FAIL** (dados reais e resposta simulada de `GET /api/alunos`; um 401 isolado de console numa execução anterior não se repetiu). Na página da turma o selo "grupo" segue "Pacote / R$ 100,00".
- [X] T008 Final: `npx tsc --noEmit`, `npx eslint`, `npm run build` (API parada; `.next/dev` removido antes).
- [X] T009 Fechamento: `docs/relatorios/<data>-alunos-coluna-tipo.md`; hook `revisao-codigo` do Trello (API parada); memória do projeto (arquivo novo + linha no `MEMORY.md`); **sem commit/push**.

## Dependências e ordem

T001 → T002 → T003 → T004 → T006/T007 → T008 → T009; T005 em paralelo.

## Estratégia

MVP = T002 + T003 (a coluna). Em seguida validar com os tipos simulados e fechar.

## Cobertura dos requisitos

| Requisito | Tarefas |
|---|---|
| FR-001 (coluna depois de Cobrança) | T003, T007 |
| FR-002 (pílula, "+N" e dica iguais a hoje) | T002, T007 |
| FR-003 (Cobrança só valor e turma) | T002, T003, T007 |
| FR-004 (**MUST NOT** decidir/calcular no frontend) | **T006(a)** |
| FR-005 (**MUST NOT** mudar o modo "grupo") | **T006(b)**, T007 |
| FR-006 (**MUST NOT** arquivo novo/backend/banco/dependência) | **T006(c)** |
| SC-001 a SC-004 | T007, T006 |
