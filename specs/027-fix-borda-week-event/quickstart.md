# Quickstart: Validar Correção da Borda em `.week-event`

Guia de validação manual (não há framework de teste automatizado no frontend — ver plan.md
Technical Context).

## Pré-requisitos

- `frontend` rodando localmente (`npm run dev` dentro de `frontend/`).
- Dados de exemplo com pelo menos um dia da semana atual tendo 2+ aulas cadastradas (para cobrir
  cards de alturas diferentes), incluindo, se possível, uma aula com status "Realizada" ou
  "Cancelada" para conferir a cor de status.

## Cenário 1 — Texto legível em todos os cards da semana

1. Abrir a Home (`/dashboard` ou rota equivalente) logado como Professora/Admin.
2. Observar a agenda semanal (7 colunas, uma por dia).
3. **Esperado**: em todo card de evento, o texto "HH:mm Matéria" (sem separador entre os dois) e
   o nome da turma/aluno abaixo estão completamente legíveis, sem nenhuma parte coberta por um
   elemento gráfico (FR-001, FR-002, FR-008, SC-001).

## Cenário 2 — Curvatura sutil (igual a `.cal-event`) em cards de alturas diferentes

1. Localizar um dia com apenas uma aula e outro dia com várias aulas empilhadas.
2. **Esperado**: em ambos, a borda colorida da esquerda de cada card tem a mesma curvatura
   sutil já usada em `.cal-event` (nem totalmente reta, nem a curva exagerada do bug original),
   sem cobrir texto em nenhum ponto (FR-001, SC-002).

## Cenário 3 — Cores de status preservadas

1. Localizar (ou criar via dados de teste) uma aula com status "Realizada" e outra "Cancelada"
   na semana exibida.
2. **Esperado**: a cor da borda esquerda continua cinza para "Realizada" e vermelha para
   "Cancelada" (mesmo comportamento de hoje), com a mesma curvatura sutil (FR-004).

## Cenário 4 — Nenhuma mudança na tela de Agenda

1. Abrir a tela cheia de Agenda (fora da Home).
2. Comparar visualmente os cards de evento (`.cal-event`) com o estado anterior à correção
   (screenshot antes/depois, ou `git stash`/`git stash pop` para alternar).
3. **Esperado**: nenhuma diferença visual perceptível — a correção não tocou `.cal-event`
   (FR-003, SC-003).

## Cenário 5 — Arredondamento consistente em todos os cantos

1. Observar os 4 cantos de qualquer card de evento na Home.
2. **Esperado**: todos os cantos usam a mesma curvatura sutil (igual à de `.cal-event`) — nenhum
   canto zerado, nenhum canto com raio diferente dos outros (FR-005).

## Cenário 6 — Texto sem estilo de link do navegador

1. Observar o texto de qualquer card de evento na Home (é um link clicável para o detalhe da
   aula).
2. **Esperado**: o texto aparece em cor neutra (a mesma cor usada no resto do card), sem
   sublinhado — nenhum estilo padrão de link azul/sublinhado do navegador visível (FR-007,
   SC-004).
3. Clicar no card.
4. **Esperado**: a navegação para o detalhe da aula continua funcionando normalmente — só a
   aparência do texto mudou, não o comportamento de clique.

## Cenário 7 — Borda e texto na mesma caixa visual (mesmo em cards estreitos)

1. Observar qualquer card de evento na Home, prestando atenção se a borda colorida aparece ao
   lado do texto (horário/matéria), como uma única caixa.
2. **Esperado**: a borda nunca aparece separada numa faixa isolada acima do texto — sempre ao
   lado dele, como parte da mesma caixa (FR-009, SC-005).
3. Repetir a observação numa janela de navegador mais estreita (ex.: redimensionar para
   ~360-400px de largura, simulando mobile).
4. **Esperado**: mesmo comportamento — borda e texto continuam na mesma caixa visual,
   independente da largura do card.
