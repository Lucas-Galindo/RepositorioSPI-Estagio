# Relatório: Alunos — coluna "Tipo" de cobrança (spec 072)

**Data**: 2026-11-05 · **Card**: "Alunos: nova coluna 'Tipo' de cobrança na tabela" · **Spec**: `specs/072-alunos-coluna-tipo/`

## Pedido

Na lista de Alunos, uma coluna depois de "Cobrança" com o tipo de cobrança (Pacote, Anual, Mensalidade...). Decisão do usuário: nova coluna "Tipo" com a pílula; a coluna "Cobrança" fica só com valor e turma.

## O que foi feito (só frontend, 2 arquivos, nenhum arquivo novo)

| Arquivo | Mudança |
|---|---|
| `frontend/components/alunos/SeloCobranca.tsx` | o modo `"lista"` virou `"tipo"` (pílula, "· restam N", "+N" e dica) e `"detalhe"` (valor, turma e "pacote esgotado"); o modo `"grupo"` ficou igual |
| `frontend/app/(app)/alunos/page.tsx` | `<th>Tipo</th>` depois de "Cobrança" e a célula nova com o modo `"tipo"` |
| `specs/068-badge-cobranca-aluno/spec.md` | nota apontando para a 072 |

## Validação

- `tsc` limpo, `eslint` 0 problemas, build de produção ok.
- **MUST NOT**: o diff só reorganiza o que já era exibido (nenhum `.sort/.filter/.reduce` nem regra nova); `turmas/[id]` e `aulas/[id]` sem alteração; `git status` do frontend com só os 2 arquivos da 072; `package.json` intacto.
- **Chrome (login real, capturas)**: **22 PASS, 0 FAIL**. Dados reais: cabeçalho "Aluno, RA, Turma(s), Telefone, Valor da aula, Cobrança, Tipo, Status"; João da Silva com "Pacote" em Tipo e "R$ 100,00 · Matematica basica 1" em Cobrança; clique na linha abre o aluno. Resposta de `GET /api/alunos` **simulada** (sem gravar no banco) com Mensalidade, Semestral, Anual, Avulsa, Sem vínculo, Pacote com "restam 3", pacote esgotado, aluno com "+2" e dica de 3 vínculos, e aluno sem selo (— nas duas colunas). Claro, escuro e 360 px sem rolagem horizontal da página. Página da turma com o selo "grupo" igual ao de antes ("Pacote / R$ 100,00").

## Observações

- Um 401 isolado no console apareceu numa execução; não se repetiu em duas reexecuções (provavelmente a sessão da API que eu subia ao mesmo tempo que o ambiente local do usuário na porta 7002).
- Durante a validação existia um ambiente do usuário (API na 7002); não mexi nesses processos, só nos que subi.
- Sem commit e sem push (aguarda pedido). `analyze` não foi rodado (spec pequena, cada "MUST NOT" coberto por tarefa).
