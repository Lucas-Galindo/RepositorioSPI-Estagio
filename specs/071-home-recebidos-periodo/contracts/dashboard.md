# Contrato: `GET /api/dashboard` (spec 071)

**Sem campo novo, removido ou renomeado** na resposta (FR-008). O que muda é o significado de dois parâmetros/campos existentes.

## Parâmetros (query)

| Parâmetro | Antes | Depois |
|---|---|---|
| `periodoInicio` | início do período; padrão: 1º dia do mês atual | igual; **se ausente e `periodoFim` presente**, o início é o 1º dia do mês de `periodoFim` |
| `periodoFim` | fim do período; padrão: um mês depois do início, menos um dia | igual |
| `alunoId` | filtrava só o bloco `pacotes` | filtra **também** `alunosAtendidosNoPeriodo` e `valorFaturadoNoPeriodo` (e continua filtrando `pacotes`) |

## Resposta (campos afetados, nomes mantidos)

- `alunosAtendidosNoPeriodo` (int): alunos distintos com aula "Realizada" no período; com `alunoId`, é 0 ou 1.
- `valorFaturadoNoPeriodo` (decimal): soma de `ValorFinal` dos pagamentos com status "Pago" e **data de pagamento** no período; com `alunoId`, só os pagamentos desse aluno. Na Home é exibido como **"Recebidos no período"**.
- Os demais campos (`totalAulasAgendadasNoPeriodo`, `totalAlunosAtivos`, `pacotes` etc.) não mudam de significado; só são calculados com o período já resolvido pela regra acima.

## Garantias

- Sem `alunoId`: resultado idêntico ao de antes para os mesmos parâmetros (exceto o caso "só `periodoFim`").
- `RelatorioRepository.ObterValorFaturadoNoPeriodoAsync` continua com a mesma assinatura (Financeiro e Relatórios não mudam).
