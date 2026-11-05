# Quickstart: validar o calendário próprio (spec 070)

Contratos em [contracts/calendario.md](contracts/calendario.md); decisões em [research.md](research.md).

## Pré-requisitos

- API local (porta 5099, SMTP inválido) e frontend em desenvolvimento apontando para ela; login de teste só por `SPI_TESTE_SENHA`.
- **Somente frontend**: sem migração e sem dados de teste (nada é gravado; os formulários são abertos e cancelados).
- Memória baixa nesta máquina: usar `NODE_OPTIONS=--max-old-space-size=1536` no `tsc`/`eslint`/`build`; gravar a saída dos roteiros longos em arquivo.

## 1. Verificações estáticas e da lógica

`cd frontend`: `npm run verificar-calendario` (todos os casos de borda da lógica), `npx tsc --noEmit`, `npx eslint` (projeto inteiro, 0 problemas), `npm run verificar-contraste` (0 pares abaixo de 4,5:1) e `npm run build` (API parada; `experimental.cpus: 2` temporário).

## 2. Chrome (claro e escuro; o calendário aparece nas capturas)

| # | Passo | Esperado |
|---|---|---|
| 1 | Filtro de Contas a Receber: clicar no botão do campo | abre o calendário do SPI (não o do navegador), no mês de hoje, hoje destacado, semana começando no domingo |
| 2 | Escolher um dia | campo `dd/mm/aaaa`, calendário fecha, a requisição é a mesma de digitar a mesma data |
| 3 | Campo com data completa; parcial/inválido | abre no mês da data (dia marcado); parcial abre em hoje sem mexer no texto |
| 4 | Setas de mês; clicar no título → meses; clicar no ano → anos; escolher ano → meses → mês → dias | navega e volta aos dias (depois de abrir, março de 2024 em 4 cliques: título, ano, 2024, março) |
| 5 | "Hoje" e "Limpar" | "Hoje" escolhe a data atual e fecha; "Limpar" esvazia o filtro (a lista volta ao total) |
| 6 | "Fim da turma" com início já escolhido | dias anteriores apagados e sem clique; "Hoje" desabilitado se hoje < mínimo |
| 7 | Esc, clique fora, clicar de novo no botão | fecha sem alterar o campo; o foco volta ao botão |
| 8 | Só teclado: Enter no botão; setas, PageUp/PageDown, Home/End; Enter escolhe; Tab fica preso no calendário; Esc fecha | tudo funciona; dentro de um modal o Esc fecha só o calendário |
| 9 | Dentro do modal do plano Semestral/Anual e numa tabela; campo perto do rodapé | calendário inteiro visível, por cima de tudo, abre para cima se faltar espaço; acompanha/fecha ao rolar |
| 10 | 360 px | janela centralizada com fundo escurecido, sem rolagem horizontal; tocar fora fecha |
| 11 | Alternar claro/escuro com o calendário aberto | as cores mudam na hora; contraste medido ≥ 4,5:1 em dia, escolhido, hoje, setas, atalhos, meses e anos |
| 12 | Competência (nova/editar conta a pagar e a receber) | campo `mm/aaaa` com botão; seletor de meses no padrão; valor `yyyy-MM`; "Este mês" e "Limpar" |
| 13 | Varredura das 16 telas com campo de data | botão em todos, calendário abre, 0 erros de console |
| 14 | `grep -rn 'type="date"\|type="month"' frontend/app frontend/components` | sem resultado |
