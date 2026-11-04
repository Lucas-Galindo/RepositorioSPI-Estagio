# Relatório — Calendário nos campos de data (spec 069)

**Data:** 2026-11-04 · **Spec:** [specs/069-calendario-campos-de-data](../../specs/069-calendario-campos-de-data/spec.md) · **Card Trello:** "Ajustar filtros de datas"

## Pedido

Revisar os filtros de data do sistema e colocar, dentro do campo, um botão que abre o calendário; e ajustar a cor do botão ao tema (fundo escuro → botão claro e vice-versa). Decisões do usuário: usar o **calendário do próprio navegador** por enquanto, e (sugestão aceita ao mandar seguir o fluxo) tratar **todos os campos de data**, não só os filtros.

## Estado encontrado

- 23 usos do `DateInput` (campo de texto `dd/mm/aaaa`) em 14 arquivos, **sem calendário**.
- 5 campos `<input type="date">` nativos (turma de pacote e plano Semestral/Anual) e 4 campos `type="month"` (competência), com o ícone do calendário **escuro sobre fundo escuro** no modo escuro.

## O que foi feito (somente frontend)

- **`DateInput`** ganhou um botão de calendário dentro do campo (ícone `cal`, só tokens de cor) e um `<input type="date">` auxiliar invisível, usado só para abrir o calendário do navegador (`showPicker()`, com `focus()` como reserva sem erro) e devolver o dia escolhido já em `yyyy-MM-dd`: **nenhuma conversão de data**. O campo mascarado continua sendo o que se vê e digita. O auxiliar usa a data do texto exibido (vazio/parcial/inválido abre em hoje). Propriedades novas e opcionais: `ariaLabel`, `style` (invólucro) e `min`.
- **Tema:** o ícone usa `--c-text-muted` (e `--c-accent-text` no hover/foco), tokens que já passam de 4,5:1 nas 48 combinações; o auxiliar e os campos de competência recebem `color-scheme: light/dark` (só neles, não na página), o que faz o ícone nativo e o calendário seguirem o tema.
- **Migração:** os 5 campos nativos viraram `DateInput` (`TurmaForm`: início e fim da turma, este com `min` = início; `VinculoCobrancaFormModal`: início, 1º vencimento e a data de cada parcela). Os 4 campos de competência só ganharam a regra de tema (nenhuma edição nas telas).
- **CSS:** `.date-wrap`, `.date-cal-btn`, `.date-nativo` e as regras de formulário (`.field .date-wrap` em bloco, largura total e seletor mais específico para o auxiliar).
- Notas de Princípio V nas specs 062 e 063.

Arquivos de código alterados: 4 (`DateInput.tsx`, `dashboard.css`, `TurmaForm.tsx`, `VinculoCobrancaFormModal.tsx`); **nenhum arquivo novo**, nada em `src/`, `tests/` ou `database/`.

## Achados da análise (aplicados antes de implementar)

1. O campo "Fim da turma" tinha `min` no campo nativo; o `DateInput` não tinha. Criei a propriedade `min` (vai ao auxiliar). Declarado: a trava no envio do formulário, que só existia no campo nativo, deixa de existir; o servidor continua validando e devolve a mensagem de sempre.
2. O `.field` estica os filhos diretos; com o campo dentro do invólucro ele deixaria de esticar (corrigido com `width: 100%`).
3. `.field input` atingiria o auxiliar (corrigido com seletor mais específico).

Durante a validação apareceu mais um ajuste: o auxiliar usava o último valor confirmado em vez do texto exibido; passou a derivar do texto.

## Validação

| Verificação | Resultado |
|---|---|
| `tsc`, `eslint` (projeto inteiro), `npm run verificar-contraste`, `npm run build` | limpos; **ESLint do projeto inteiro: 0 problemas**; 48 combinações, 0 pares abaixo de 4,5:1; `next.config.ts` restaurado |
| Chrome (claro e escuro), **55 verificações** | todas aprovadas: botão nos 2 filtros "de/até"; clicar chama `showPicker` sem erro; escolher pelo calendário mostra `dd/mm/aaaa` e gera **a mesma requisição** de quem digitou; texto parcial preserva o que foi digitado; Tab vai ao botão (auxiliar fora da navegação), anel de foco de 2 px e Enter abre; campo desabilitado (aula realizada) desabilita botão, campo e auxiliar; contraste do ícone ≥ 4,5:1 medido nos dois temas; `color-scheme` calculado `light`/`dark` no auxiliar e nos campos de mês; alternar o tema muda a cor do botão na hora; "Fim da turma" recebe `min` = início; o modal do plano tem 5 botões (2 campos + 3 parcelas) e a data da parcela 2 escolhida pelo calendário aparece certa |
| Varredura de 16 telas com campos de data (Home, Financeiro, Contas a Pagar e a Receber — lista, novo e editar —, Lembretes, 4 Relatórios, Aulas nova e editar, Turma nova) | todos os campos com botão; **0 rolagens horizontais em 360 px**; 0 erros de JavaScript/console |
| "MUST NOT" (CLAUDE.md) | sem conversão de data no código (o `grep` só acha comentários que explicam por que não usar `new Date`); `git status`/`git diff` de `src`, `tests` e `database` vazios; **nenhum arquivo novo** no frontend; nenhuma cor literal nas linhas adicionadas; o único `type="date"` restante é o auxiliar dentro do componente |

## Limites e observações

- **O calendário aberto é uma janela do navegador e não aparece em captura de tela.** O tema dele foi conferido pelo `color-scheme` calculado do auxiliar (e pelo experimento no Chrome, em que o ícone nativo fica claro no escuro), não por imagem do calendário aberto. O desenho do calendário e o suporte ao tema escuro dependem do navegador.
- Os campos de competência (`type="month"`) seguem nativos (só ganharam o tema): não existe máscara de mês no sistema e criar uma seria escopo novo.
- Campos de contas a pagar (editar) não foram exercitados no navegador por não haver conta a pagar no banco local; usam o mesmo componente e foram cobertos pela contagem de campos no código.
- Evolução possível, fora do escopo: calendário próprio, com visual igual em todos os navegadores.

Nada foi commitado nem enviado (push).
