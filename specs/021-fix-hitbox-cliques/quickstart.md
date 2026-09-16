# Quickstart: Validando a Área de Clique Expandida no Menu do Financeiro e a Correção de Hitbox na Tabela de Contas a Pagar

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-09-14

Guia manual de diagnóstico e validação. Não há framework de teste de UI automatizado no projeto
(sem Jest/Vitest/Playwright em `frontend/package.json`), então tanto a confirmação da causa raiz
de US2 quanto a validação das duas user stories são feitas com o navegador e suas DevTools.

## Pré-requisitos

- Backend rodando localmente (`dotnet run` em `src/SPI.Api`).
- Frontend rodando em modo desenvolvimento: `npm run dev` dentro de `frontend/`.
- Uma sessão autenticada como Professora.
- Navegador com DevTools acessível (aba Elements para inspecionar `getBoundingClientRect()`).

## Etapa 0 — Diagnóstico da causa raiz de US2 (obrigatório antes da correção da tabela)

Ver [research.md](./research.md) Decisão 2. US1 não precisa desta etapa — a causa e a solução já
estão definidas (ver research.md Decisão 1).

1. Acesse `/financeiro/contas-a-pagar` com ao menos um registro cadastrado.
2. No DevTools, inspecione o `getBoundingClientRect()` real do `<thead>` e da primeira `<tr
   class="row-link">` do `<tbody>`.
3. Clique repetidamente perto da borda inferior do `thead` (variando o zoom do navegador entre
   90% e 125%) e observe se a navegação da primeira linha é disparada indevidamente.
4. **Resultado esperado desta etapa**: confirmação por escrito (comentário no PR ou nas notas de
   implementação) de que a hipótese do `research.md` (vazamento geométrico de hitbox) se
   confirmou, antes de prosseguir para a correção de CSS de US2.

## Cenário 1 — Qualquer clique dentro da faixa do menu do Financeiro navega para uma aba (US1 / FR-001 a FR-004 / SC-001, SC-002)

1. Acesse `/financeiro`.
2. Clique dentro do texto/limites visuais de "Contas a Receber".
3. **Esperado**: navega para `/financeiro/contas-a-receber` (comportamento correto, não deve
   mudar — FR-003).
4. Clique em um ponto do espaço antes vazio entre "Contas a Receber" e "Contas a Pagar" (agora
   coberto pela área expandida de uma das duas abas, conforme a divisão estática do layout).
5. **Esperado após a implementação**: navega para a aba correspondente ao lado do container em
   que o clique caiu (FR-001, FR-004) — verificar visualmente qual metade do espaço pertence a
   qual aba, e confirmar que o resultado é sempre o mesmo para o mesmo ponto (determinístico).
6. Clique em um ponto do espaço vazio nas extremidades (à esquerda de "Visão Geral", à direita de
   "Contas a Pagar", ainda dentro da faixa do menu).
7. **Esperado**: navega para a aba da extremidade correspondente ("Visão Geral" ou "Contas a
   Pagar").
8. Clique em um ponto claramente fora da faixa do menu (acima ou abaixo dela).
9. **Esperado**: nenhuma navegação ocorre (FR-002, SC-002).
10. Repita os passos 2-9 com o navegador em largura mobile (~400px, onde os botões podem quebrar
    linha) e com zoom variado — a divisão estática deve se adaptar ao layout renderizado em cada
    caso (ver Edge Cases do spec.md).

## Cenário 2 — Tabela de Contas a Pagar só responde dentro dos limites do elemento correto (US2 / FR-005, FR-006 / SC-003)

1. Acesse `/financeiro/contas-a-pagar` com ao menos um registro cadastrado.
2. Clique em uma célula de dados de uma linha real (por exemplo, na coluna "Descrição" de um
   registro).
3. **Esperado**: navega para o detalhe daquele registro (`/financeiro/contas-a-pagar/{id}`) —
   comportamento correto, não deve mudar (FR-007, sem regressão).
4. Clique em um ponto dentro da faixa do `<thead>` (cabeçalho da tabela), inclusive perto da
   borda inferior dele.
5. **Esperado após a correção**: nenhuma navegação ocorre e nenhuma outra ação é disparada.
6. Repita os passos 2-5 em largura mobile (~400px) e zoom variado (90%-125%).

## Critério de conclusão

Ambos os cenários passam visualmente em pelo menos duas larguras de tela (desktop e ~400px) e
dois níveis de zoom do navegador, sem nenhuma regressão nos cliques que hoje funcionam
corretamente (Cenário 1 passos 2-3, Cenário 2 passos 2-3), sem nenhuma mudança de dado, rota de
destino ou chamada de API observável na aba Network do navegador, e sem nenhuma regressão visual
nas outras 13 telas que reusam a classe `.row-gap` (que permanece inalterada — ver
[research.md](./research.md) Decisão 1).
