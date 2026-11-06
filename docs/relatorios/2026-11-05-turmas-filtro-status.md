# Relatório: Turmas — filtro de status (spec 073)

**Data**: 2026-11-05 · **Card**: "Turmas: filtro de status (ativas/inativas)" · **Spec**: `specs/073-turmas-filtro-status/`

## Pedido

Na aba Turmas, filtro para ver turmas ativas e inativas. O card original ("Turmas: filtros por status e matéria") foi dividido: este ficou só com o status; o filtro de matéria virou o card "Turmas: filtro de matéria (depende do vínculo Turma ↔ Matéria)" no Backlog.

## O que foi feito (só frontend, 1 arquivo, nenhum arquivo novo)

`frontend/app/(app)/turmas/page.tsx`: seletor "Status — todos / Ativo / Inativo" antes da busca por nome e botão "Limpar filtros" (só com filtro aplicado), no mesmo padrão da lista de Alunos. O servidor filtra: a tela envia o parâmetro `ativo`, que a API e o cliente já aceitavam. Sem backend, banco, CSS novo ou dependência.

## Validação

- `tsc` limpo, `eslint` 0 problemas, build de produção ok.
- **MUST NOT**: o diff só adiciona estado, seletor e botão (sem `.filter/.sort/.reduce`); só `turmas/page.tsx` modificado; `src` e `package.json` intactos; sem filtro, as 4 turmas e a ordem iguais às da API.
- **Chrome (login real, capturas)**: **15 PASS, 0 FAIL**: sem filtro 4 turmas; Ativo 3 (requisição `ativo=true`); Inativo só "Turma teste" (`ativo=false`); status + busca; sem resultado mostra "Nenhuma turma encontrada"; Limpar filtros volta ao padrão; clique no card abre a turma; 360 px sem rolagem horizontal; 0 erros de console.

## Observações

- `analyze` não foi rodado (spec pequena, cada "MUST NOT" coberto por tarefa). Sem commit e sem push (aguarda pedido).
