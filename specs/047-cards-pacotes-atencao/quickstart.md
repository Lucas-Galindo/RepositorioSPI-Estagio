# Quickstart: validar os cards do painel de Pacotes em Atenção

Guia de validação ponta a ponta. Modelo de dados reutilizado em [data-model.md](./data-model.md); decisões técnicas em [research.md](./research.md).

## Pré-requisitos

- Backend e frontend rodando; login feito; pelo menos um aluno com pacote em estado "Atenção" (saldo 1 ou 2) e um em "Esgotado" (saldo 0), um deles vinculado a uma turma e outro sem turma ("Atendimento individual").

## 1. Saldo em destaque, formato de card (US1, FR-001 a FR-003, SC-001/SC-002)

| # | Passo | Esperado |
|---|---|---|
| 1 | Abrir a Home | Painel "Alunos com pacote de aulas acabando ou esgotado" mostra blocos retangulares (cards), não mais linhas de lista |
| 2 | Observar um card qualquer | Saldo de aulas em tamanho visivelmente maior que o nome do aluno, o contexto, e o rótulo do estado |
| 3 | Observar o conteúdo completo de um card | Nome do aluno, turma (ou "Atendimento individual"), saldo, e rótulo do estado — todos visíveis, nada que existia antes desapareceu |
| 4 | Comparar o card de um pacote com o card "Alunos atendidos" do topo da Home | Mesmo raio de borda, padding, sombra e hierarquia tipográfica (rótulo pequeno → número grande → legenda) |

## 2. Cards de largura fixa, em fila horizontal rolável (US2, FR-004/FR-005, SC-003) — revisado em 2026-10-07

| # | Passo | Esperado |
|---|---|---|
| 1 | Com **um único** pacote em atenção, abrir a Home | O card aparece pequeno, do mesmo tamanho do card "Alunos atendidos", alinhado à esquerda — **não** esticado para ocupar a largura do painel |
| 2 | Com vários pacotes que cabem na largura da tela | Todos os cards aparecem lado a lado, do mesmo tamanho fixo, em uma única fileira |
| 3 | Com pacotes suficientes para não caber todos na largura do painel | O painel (não a página) ganha uma barra de rolagem horizontal própria; nenhum card quebra linha ou encolhe |
| 4 | Rolar a barra horizontal do painel até o fim | Todos os cards ficam acessíveis, sem nenhum cortado |
| 5 | Olhar o restante da página (acima/abaixo do painel) | A página em si nunca ganha rolagem horizontal por causa deste painel |

## 3. Esgotados primeiro, diferença clara de estado (US3, FR-006/FR-007, SC-004)

| # | Passo | Esperado |
|---|---|---|
| 1 | Com pacotes "Esgotado" e "Atenção" misturados | Todos os cards "Esgotado" aparecem antes de todos os "Atenção" (mesma ordem do backend) |
| 2 | Comparar um card "Esgotado" com um "Atenção", ignorando a cor | Ainda é possível diferenciar os dois pelo rótulo de texto do badge |

## 4. Espaçamento coerente com o resto da página (Requisito 5 da correção de 2026-10-07)

- A distância entre o painel e o título "Pacotes em atenção" acima dele é a mesma de antes.
- A distância entre o painel e a "Agenda da semana" abaixo dele é visivelmente igual à distância usada em outras seções da página (não colado, não com um espaço desproporcional).

## 5. Não-regressão (FR-010/FR-011, SC-005)

- Clicar em qualquer card continua navegando para `/alunos/{id}` do aluno correspondente — mesmo destino de antes.
- O estado vazio ("Nenhum pacote precisando de atenção") continua aparecendo quando não há nenhum pacote.
- `git diff --stat` mostra só `frontend/components/dashboard/PacotesEmAtencaoPanel.tsx` e `frontend/app/(app)/dashboard/page.tsx`; `git diff -- frontend/styles/dashboard.css` fica vazio (nenhuma mudança líquida em CSS); nenhuma mudança em `src`/`database`.
- `git status --porcelain` não mostra nenhum arquivo novo (não rastreado) em `frontend/`.
- Em outras telas que usam `.card-small`/`.big-value`/`.big-label`/`.badge-cancel`/`.badge-pending` (KPIs da Home, Relatórios, Turmas), a aparência permanece idêntica à de antes desta feature.
