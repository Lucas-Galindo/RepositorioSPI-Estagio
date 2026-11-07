# Relatório: Turmas — filtro de matéria (spec 076)

**Data**: 2026-11-05 · **Card**: "Turmas: filtro de matéria (depende do vínculo Turma ↔ Matéria)" (retomado do stand by) · **Spec**: `specs/076-turmas-filtro-materia/`

## Pedido

Na aba Turmas, filtro pela matéria da turma (campo entregue na spec 075).

## O que foi feito

| Camada | Arquivo | Mudança |
|---|---|---|
| API | `TurmasController.cs` | `materiaId` e `semMateria` opcionais em `GET /api/turmas`; os dois juntos → 400 (lista de mensagens, como no resto do projeto) |
| Serviço/Repositório | `ITurmaService`/`TurmaService`, `ITurmaRepository`/`TurmaRepository` | os dois parâmetros opcionais no fim; `Where` por matéria ou por turmas sem matéria; ordenação e `Include` inalterados |
| Testes | 5 fakes de `ITurmaRepository` + `TurmaServiceListarFiltrosTests.cs` (novo) | assinatura nova; 3 testes do repasse |
| Frontend | `lib/api/turmas.ts`, `turmas/page.tsx` | seletor "Matéria — todas / Sem matéria / matérias (inclusive inativas)" entre o status e a busca; "Limpar filtros" também o limpa |

Sem migração, sem dependência, sem arquivo novo no frontend. Os demais consumidores não passam os parâmetros novos e não mudaram.

## Validação

- **Testes**: 715 aprovados (712 + 3). `tsc` limpo, `eslint` 0 problemas, `npm run build` ok.
- **MUST NOT**: nenhum `.filter/.sort/.reduce` novo no frontend; só `turmas/page.tsx` e `turmas.ts` no frontend; nenhum `csproj`/`package.json`/migração.
- **API real (banco local + dados de teste "TESTE-076" em duas matérias)**: **13 PASS**: sem parâmetros o JSON é **idêntico** à base; `materiaId` devolve exatamente as turmas da matéria; `semMateria=true` exatamente as sem matéria; `semMateria=false` não filtra; combinações com `ativo` e `nome`; matéria inexistente = 200 e lista vazia; `materiaId` + `semMateria` = 400; ordem por nome preservada; dados de teste removidos.
- **Chrome (login real, capturas)**: **19 PASS, 0 FAIL**: ordem dos seletores; opções; Matemática (só a "Turma teste", requisição `materiaId=1`); Sem matéria (as 3, `semMateria=true`); combinações com status e busca; sem resultado; "Limpar filtros"; matéria inativada continua no seletor e filtrável; clique no card; modo escuro; 360 px sem rolagem horizontal; 0 erros de console.

## Observações

- Além do pedido, o filtro oferece **"Sem matéria"** (decisão de escopo da spec, justificada porque 3 das 4 turmas atuais estão sem matéria após a migração 27); fácil de remover se a professora não quiser.
- Os dados de teste e a inativação temporária da matéria "teste" foram revertidos; só `refresh_token` cresceu (logins de teste).
- Sem commit e sem push (aguarda pedido). `analyze` não foi rodado (spec pequena, cada "MUST NOT" coberto por tarefa).
