# Relatório: Calendário próprio no padrão do sistema (spec 070)

**Data**: 2026-11-05 · **Card**: "Calendário próprio no padrão do sistema (substitui o do navegador)" · **Spec**: `specs/070-calendario-proprio/`

## Pedido

O usuário não gostou do calendário do navegador (spec 069). Pediu um calendário próprio, com as cores e o design do sistema, aberto pelo botão dentro do campo de data. Decisões: competência (mês/ano) também ganha seletor próprio; no celular abre como janela centralizada com fundo escurecido; semana começa no domingo; extras: "Hoje", "Limpar", troca rápida de mês/ano, dia de hoje destacado e dias bloqueados pelo mínimo.

## O que foi feito (só frontend; sem backend, banco ou dependência nova)

| Arquivo | Mudança |
|---|---|
| `frontend/lib/calendario.ts` (novo) | Lógica pura sem `Date`: bissexto, dia da semana (Sakamoto), grade 6×7, navegação, limites 1900–2100, nomes em português |
| `frontend/scripts/verificar-calendario.mjs` (novo) + script `verificar-calendario` no `package.json` | 106 verificações de borda da lógica |
| `frontend/components/shared/Calendario.tsx` (novo) | Painel com visões de dias, meses e anos; portal na raiz (camada 120); abre para cima se faltar espaço; acompanha a rolagem ou fecha; janela centralizada até 640 px; teclado completo, foco preso, ARIA |
| `frontend/components/shared/MonthInput.tsx` (novo) | Campo de competência `mm/aaaa` com botão, emite `yyyy-MM` |
| `frontend/components/shared/DateInput.tsx` | Passa a abrir o `Calendario`; saem o `<input type="date">` auxiliar e o `showPicker()`; props iguais |
| `frontend/styles/dashboard.css` | Bloco `.cal-*` só com tokens do tema; saem `.date-nativo` e `color-scheme` |
| 4 telas de contas a pagar/receber | `type="month"` trocado por `MonthInput` |
| `specs/069.../spec.md` | Nota apontando para a 070 |

## Validação

- `tsc` limpo, `eslint` 0 problemas, contraste 48 combinações / 0 abaixo de 4,5:1, `verificar-calendario` 106 ok, build de produção ok.
- "MUST NOT": nenhum `type="date"`/`type="month"`/`date-nativo`/`color-scheme`; sem `new Date`/`toISOString`/`showPicker` no código novo; `package.json` só ganhou o script (nenhuma dependência); `src`, `tests` e `database` intocados; sem cor literal no CSS novo; props do `DateInput` iguais; `git status` com só os 4 arquivos novos previstos.
- **Chrome, página de teste temporária (Playwright, capturas conferidas)**: 76 verificações, 76 PASS (a única falha inicial era do roteiro, que clicava num botão coberto pelo próprio painel; reteste confirmou que abrir outro fecha o primeiro). Cobriu abrir, escolher, mês preenchido, navegação por título/ano (março de 2024), Hoje/Limpar, mínimo e dias bloqueados, Esc/fora/alternar, teclado e foco preso, dentro de modal (Esc fecha só o calendário), em tabela com rolagem, perto do rodapé, rolagem, competência, contraste medido nas cores calculadas (claro e escuro, ≥ 4,5:1), troca de tema com o calendário aberto, 360 px sem rolagem horizontal e 0 erros de console.

## O que ficou de fora / limitações

- **Telas reais (T021)**: com login real (API local na 5099 e Next em webpack), o roteiro abriu as 13 telas com campo de data (dashboard, financeiro, contas a receber e a pagar, novas contas, lembretes, 4 relatórios, aula nova, turma nova) em 1100 px e em 360 px, com capturas: **141 PASS e 2 FAIL**. As 2 falhas eram do roteiro: em "Nova turma" os campos de data só aparecem com o tipo "Pacote"; reteste com Pacote: 6/6 (dois botões, Fim com dias anteriores ao início bloqueados, escolha de 20/11/2026). Também passaram o filtro real de Contas a Receber ("Hoje" gera a requisição com a data de hoje; "Limpar" esvazia) e o seletor de competência nas duas telas "Nova conta".
- **Não exercitado**: as telas de **editar** conta (a pagar e a receber), porque o banco local não tem contas; usam o mesmo `MonthInput`, e o caso de valor preenchido ficou coberto na página de teste (mês escolhido marcado).
- Ao validar, o Turbopack em desenvolvimento falhou por memória; foi usado `next dev --webpack`.
- Sem commit e sem push (aguarda pedido).
