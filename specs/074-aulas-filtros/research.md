# Research: Aulas — filtros (spec 074)

## R1 — Parâmetros novos opcionais na mesma cadeia

**Decisão**: acrescentar `int? materiaId`, `TimeOnly? horaInicioDe`, `TimeOnly? horaInicioAte` **depois** do `CancellationToken` (opcionais) em `IAulaRepository.ListarAsync`, `AulaRepository.ListarAsync`, `IAulaService.ListarAsync`/`AulaService.ListarAsync` e como `[FromQuery]` no `AulasController.Listar`. O `RelatorioService` (duas chamadas ao repositório) e todos os demais chamadores seguem sem passar os novos parâmetros (valem `null`), então o resultado deles não muda (FR-004).

**Efeito**: a assinatura do repositório muda, então os **5 fakes** de `IAulaRepository` em `tests/` ganham os três parâmetros (uma linha cada; mesmo caso da spec 071).

## R2 — Consulta

**Decisão**: em `AulaRepository.ListarAsync`, três `Where` condicionais: `a.MateriaId == materiaId`, `a.HoraInicio >= horaInicioDe`, `a.HoraInicio <= horaInicioAte` (limites inclusos). A ordenação (data e horário de início) não muda.

## R3 — Binding de `TimeOnly` na query

**Decisão**: o ASP.NET Core converte `HH:mm` para `TimeOnly?` (já existe `DateOnly?` no mesmo método). Valor inválido devolve o 400 padrão do `[ApiController]`. O frontend só envia o horário quando o campo está completo (o `TimeInput` só emite `HH:mm` válido ou vazio).

## R4 — Frontend (`aulas/page.tsx`, `lib/api/aulas.ts`)

**Decisão**: estados `dataDe`, `dataAte` (`DateInput`), `horaDe`, `horaAte` (`TimeInput`), `materiaId` (select com `listarMaterias`), além de `status` e `turmaId`. `listarAulas` ganha `materiaId`, `horaInicioDe`, `horaInicioAte` no tipo dos filtros; `dataInicio`/`dataFim` já existem. Botão `.filter-clear` ("Limpar filtros") quando houver algum filtro. Rótulos curtos em texto ("Data" … "a" … e "Horário" … "a") com `count-text`, sem CSS novo. Só `aulas/page.tsx` e `lib/api/aulas.ts` mudam no frontend.

## R5 — Testes

**Decisão**: o fake `FakeAulaRepositoryParaCadastro` (usado pelo cenário de cadastro) passa a guardar os últimos filtros extras recebidos; teste do `AulaService.ListarAsync` confere que `materiaId` e os horários chegam ao repositório e que sem eles chegam nulos. O filtro SQL em si é provado na **API real** com as aulas do banco local (SC-001) e com a comparação sem filtro antes/depois (SC-002).

## R6 — Riscos

| Risco | Mitigação |
|---|---|
| Mudar o resultado dos outros consumidores | parâmetros opcionais nulos; comparar `GET /api/aulas` sem parâmetros e os usos de Agenda/Home antes/depois |
| Quebrar fakes de teste | 5 fakes atualizados; `dotnet test` sem `--no-build` |
| Barra de filtros larga em 360 px | `filter-bar` já quebra linha; conferido no Chrome |
