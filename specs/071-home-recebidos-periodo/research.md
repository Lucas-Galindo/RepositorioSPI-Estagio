# Research: Home — "Recebidos no período" (spec 071)

Decisões baseadas na leitura do código (2026-11-05), na conferência no Chrome e nas respostas do usuário.

## R1 — Onde mora cada número (nada novo no frontend)

**Decisão**: os dois cards continuam vindo de `GET /api/dashboard` (`alunosAtendidosNoPeriodo`, `valorFaturadoNoPeriodo`). A tela da Home já envia `periodoInicio`, `periodoFim` e `alunoId` (o `useEffect` depende dos três). Não há campo novo na resposta e o frontend só troca textos.

**Por quê**: Princípio II (regra no servidor) e FR-004/FR-008. O parâmetro `alunoId` já existe no controller (hoje só filtra pacotes).

## R2 — Filtro de aluno no "Recebido": reaproveitar o que já existe

**Decisão**: `IRelatorioRepository.ObterValorFaturadoNoPeriodoAsync` já aceita `alunoId` (usado por Financeiro/Relatórios). O `DashboardService` passa a chamá-lo com o `alunoId` recebido. Sem aluno, `alunoId = null` e a consulta é idêntica à de hoje (FR-006).

## R3 — Filtro de aluno em "Alunos atendidos": novo parâmetro opcional

**Decisão**: `ContarAlunosAtendidosNoPeriodoAsync(inicio, fim, cancellationToken, int? alunoId = null)`. Com aluno, a consulta acrescenta `aa.AlunoId == alunoId` (o `Distinct().Count()` resulta em 0 ou 1). A assinatura da interface muda, então os **3 fakes** de `IRelatorioRepository` nos testes (`Relatorios*Tests`) recebem o parâmetro novo (uma linha cada). Único chamador real: `DashboardService`.

**Alternativa descartada**: criar um método novo só para aluno (duplicaria a consulta) ou contar no serviço (puxaria dados à toa).

## R4 — Período quando só o "até" é informado

**Decisão**: o cálculo sai de `ObterAsync` para um método estático puro `DashboardService.ResolverPeriodo(periodoInicio, periodoFim, hoje)`:
- início = `periodoInicio` ou, se só há "até", o **1º dia do mês do "até"**, ou, sem nenhum, o 1º dia do mês atual;
- fim = `periodoFim` ou, como hoje, um mês depois do início menos um dia (ex.: 10/09 → 09/10; sem nenhum limite, o mês atual inteiro). **Não muda**: a spec chegou a descrever esse caso como "último dia do mês", mas o teste de `ResolverPeriodo` mostrou o comportamento real, mantido por FR-006.

Sem mudança para os dois casos que já funcionam ("a partir de" sozinho e nenhum filtro). Como o período é único no dashboard, o ajuste vale para todos os números (aulas agendadas, pacotes no período etc.), como diz FR-005.

## R5 — Testes sem fake gigante

**Decisão**: `ResolverPeriodo` é testado como função pura (casos de borda: só "até", só "a partir de", nenhum, virada de ano, fevereiro bissexto). O repasse do `alunoId` aos dois métodos é testado com um **proxy dinâmico** (`System.Reflection.DispatchProxy`) que implementa as interfaces do repositório, devolve valores vazios e registra os argumentos, em vez de um fake de 24 métodos. O filtro SQL em si é validado na API real (SC-002), contra a soma dos pagamentos do aluno.

## R6 — Textos (frontend)

**Decisão**:
- "Alunos atendidos": legenda "Com aula realizada no período".
- "Recebidos no período" (antes "Recebido este mês"): dica "Total de contas a receber que já foram pagas dentro do período escolhido (sem período, o mês atual)."; legenda "Contas a receber pagas no período".
- Com aluno selecionado, as duas legendas ganham " · do aluno selecionado".
- O período vazio continua valendo o mês atual: a legenda explica isso na dica, sem lógica no frontend (só o texto muda conforme exista aluno escolhido, o que já é um estado da tela).

## R7 — Documentação (Princípio V)

**Decisão**: nota na spec 058 (que definiu o período e o aluno da Home) apontando para esta spec como a regra vigente do período e do filtro de aluno nos dois cards. Atualizar as descrições dos parâmetros no controller.

## R8 — Riscos

| Risco | Mitigação |
|---|---|
| Mudar a assinatura da interface quebra os fakes de teste | 3 fakes atualizados; `dotnet test` (sem `--no-build`) cobre a compilação |
| Regressão sem filtro de aluno | T de verificação: mesmos valores antes/depois no banco local (mês atual e 2026 inteiro) |
| Mexer em Financeiro/Relatórios por engano | `git diff --stat` de `FinanceiroService`/`RelatorioService`; o método de valor já existia |
| Período do "até" sozinho muda outros números do dashboard | é o desejado (FR-005); coberto por teste de `ResolverPeriodo` e pelo Chrome |
