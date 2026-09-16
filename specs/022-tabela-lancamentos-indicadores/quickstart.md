# Quickstart: Validando a Tabela de Lançamentos em Indicadores Financeiros

## Pré-requisitos

- Backend (`SPI.Api`) e frontend (`frontend/`) rodando localmente, apontando para o mesmo banco
  MySQL de desenvolvimento.
- Login como `Professor` (ou `Admin`) — a tela de Relatórios exige sessão autenticada.
- Massa de dados no mês corrente com pelo menos: 1 `Pagamento` pago, 1 `Pagamento` pendente com
  `Descricao` nula (para validar o fallback do nome do aluno), 1 `ContaPagar` paga, 1
  `ContaPagar` pendente com `Descricao` nula (para validar o fallback de `Favorecido`/categoria).

## Cenário 1 — Tabela substitui o card "Gargalo de caixa" (User Story 1)

1. Acessar `/relatorios/financeiro` → aba "Indicadores".
2. Confirmar que o card "Gargalo de caixa" não aparece mais no lugar onde estava (ao lado de
   "Fluxo de caixa (últimos 6 meses)").
3. Confirmar que, no lugar dele, há uma tabela com uma linha por lançamento do mês corrente
   (entradas e saídas juntas), cada linha mostrando descrição e data de vencimento.
4. Para o `Pagamento` e a `ContaPagar` sem `Descricao` cadastrada, confirmar que a linha mostra o
   nome do aluno (entrada) ou o `Favorecido`/categoria (saída) no lugar da descrição vazia —
   nunca uma célula em branco.
5. Trocar o filtro de período para um mês sem nenhum lançamento e confirmar que a tabela mostra
   um estado vazio claro, sem erro no console.

Ver contrato da resposta em [contracts/indicadores-financeiros.md](./contracts/indicadores-financeiros.md)
e o esquema dos campos em [data-model.md](./data-model.md).

## Cenário 2 — Filtro "Entradas"/"Saídas" (User Story 2)

1. Na mesma tabela (com entradas e saídas no período), localizar os dois botões no topo da
   tabela: "Entradas" e "Saídas", no mesmo estilo visual das abas do menu Financeiro
   (`financeiro-tabs` — comparar visualmente com o menu "Visão Geral"/"Contas a Receber"/"Contas
   a Pagar" em `/financeiro`).
2. Clicar em "Entradas": confirmar que só sobram linhas com lançamentos de entrada, e que o
   botão "Entradas" fica com a aparência de item ativo (mesmo tratamento visual usado no menu
   Financeiro).
3. Clicar em "Saídas": confirmar a troca para só saídas, sem nenhuma chamada de rede adicional
   visível no painel Network do navegador (o filtro deve ser aplicado sobre os dados já
   carregados — ver `research.md`, decisão 1).
4. Se não houver nenhuma entrada (ou saída) no período, confirmar o estado vazio específico ao
   filtrar por aquele tipo.

## Cenário 3 — Integração com os filtros de período/turma/matéria/aluno já existentes (User Story 3)

1. Com a tabela filtrada em "Entradas", mudar o filtro de período existente na página para um
   mês diferente: confirmar que a tabela recarrega mostrando só o novo período, **mantendo** o
   filtro "Entradas" já selecionado (sem voltar para "mostrar tudo").
2. Aplicar um filtro de turma (ou aluno) existente na página: confirmar que a lista de entradas
   passa a mostrar só lançamentos daquele aluno/turma, enquanto a lista de saídas continua
   completa (sem aplicar o mesmo filtro) — comparar com o aviso já exibido na tela ("despesas...
   continuam representando o total do negócio").
3. Verificar que os totais somados manualmente pelos valores da tabela (entradas do período)
   batem com o que "Fluxo de caixa (últimos 6 meses)" mostra para o mês equivalente, confirmando
   que a nova tabela usa a mesma base de dados dos indicadores já existentes.

## Validação de backend (sem UI)

Rodar os testes novos do serviço (ver `tasks.md` para o teste específico) com:

```powershell
dotnet test tests/SPI.Application.Tests --filter "FullyQualifiedName~Relatorios"
```

Ou testar o endpoint diretamente:

```
GET /api/relatorios/indicadores-financeiros?inicio=2026-09-01&fim=2026-09-30
```

e confirmar no corpo da resposta o novo campo `lancamentos[]`, ordenado por `dataVencimento`
decrescente, sem nenhum item com `status = "Cancelado"`.
