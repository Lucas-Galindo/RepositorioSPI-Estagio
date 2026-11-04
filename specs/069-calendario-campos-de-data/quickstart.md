# Quickstart: validar o calendário nos campos de data (spec 069)

Contrato em [contracts/date-input.md](contracts/date-input.md); decisões em [research.md](research.md).

## Pré-requisitos

- API local (porta 5099, SMTP inválido) e frontend em desenvolvimento apontando para ela; login de teste só por `SPI_TESTE_SENHA`.
- **Somente frontend**: não há migração, backend nem dado de teste a criar (nada a limpar).

## 1. Verificações estáticas

`cd frontend`: `npx tsc --noEmit`, `npx eslint` (projeto inteiro), `npm run verificar-contraste` (0 pares abaixo de 4,5:1) e `npm run build` (API parada; `experimental.cpus: 2` temporário).

## 2. Chrome (claro e escuro)

| # | Passo | Esperado |
|---|---|---|
| 1 | Telas com filtros de período (Home, Financeiro, Contas a Pagar e a Receber, Lembretes, Relatórios de alunos, turmas, pagamentos e financeiro) | cada campo de data tem o botão de calendário dentro do campo |
| 2 | Clicar no botão de um filtro e escolher um dia | o campo mostra `dd/mm/aaaa`; a requisição enviada tem a mesma data que teria se fosse digitada |
| 3 | Digitar uma data completa e abrir o calendário | abre no dia digitado; texto parcial/inválido abre em hoje sem alterar o texto |
| 4 | Campo desabilitado (data de aula já realizada) | botão desabilitado, nada abre |
| 5 | Teclado: Tab até o botão, Enter/Espaço | foco visível, abre o calendário; Tab volta ao fluxo normal |
| 6 | Modo escuro e claro: cor do ícone | ícone claro no escuro e escuro no claro (contraste ≥ 4,5:1 medido); troca na hora ao alternar o tema |
| 7 | `color-scheme` calculado do auxiliar e dos campos de mês | `light` no claro e `dark` no escuro |
| 8 | Formulários: nova turma de pacote, novo plano Semestral/Anual (inclusive as parcelas), nova conta, editar aula | formato `dd/mm/aaaa`, botão e cores iguais; as datas salvas são as mesmas de antes |
| 9 | Campos de competência (mês) em conta a pagar/receber | ícone e seletor no tema escuro; valor `yyyy-MM` como antes |
| 10 | 360 px nas telas acima | sem rolagem horizontal; botão não cobre o texto |
| 11 | `grep -rn 'type="date"' frontend/app frontend/components` | só o auxiliar dentro de `DateInput.tsx` |

**Limite**: o calendário aberto é uma janela do navegador e não sai em captura de tela; o tema dele é conferido pelo `color-scheme` calculado.
