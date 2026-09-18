# Phase 0 Research: Corrigir Cálculo de "% Frequência do Aluno"

## R1 — Confirmar que os dados do numerador correto já estão disponíveis sem consulta nova

**Decision**: `IAulaRepository.ListarAsync` ([IAulaRepository.cs:9-15](../../src/SPI.Domain/Repositories/IAulaRepository.cs#L9-L15)), já chamado por `RelatorioService.ObterAgendaAsync` (por sua vez chamado por `ObterHistoricoAlunoAsync`), é implementado em `AulaRepository.ListarAsync` ([AulaRepository.cs:25-38](../../src/SPI.Infrastructure/Repositories/AulaRepository.cs#L25-L38)) com `.Include(a => a.AulaAlunos).ThenInclude(aa => aa.Aluno)` — ou seja, cada `Aula` retornada já vem com a lista completa de `AulaAluno`, incluindo o campo `Presente` (`bool?`). Esse dado é descartado hoje porque `ObterAgendaAsync` mapeia `Aula` para `RelatorioAgendaItem` (que não tem campo de presença) antes de `ObterHistoricoAlunoAsync` calcular o percentual.

**Rationale**: Confirma que a correção não precisa de nenhuma consulta nova ao banco — só precisa parar de descartar um dado já carregado, evitando qualquer impacto de performance.

**Alternatives considered**: Criar uma consulta nova e dedicada (`ContarPresencasNoFiltroAsync`) em `IAulaRepository` ou `IRelatorioRepository` — rejeitado por desnecessário: o mesmo `ListarAsync` já usado por `ObterAgendaAsync` traz tudo que é preciso; adicionar uma segunda consulta faria uma ida a mais ao banco para recalcular algo que já está em memória.

## R2 — Onde inserir a correção sem duplicar a lógica de mapeamento

**Decision**: `ObterHistoricoAlunoAsync` passa a chamar `_aulaRepository.ListarAsync(status, turmaId, alunoId, inicio, fim, cancellationToken)` diretamente (em vez de `ObterAgendaAsync`), obtendo a lista crua de `Aula`. A partir dela: (a) monta a lista `Aulas` da resposta usando um pequeno método privado estático de mapeamento (`MapearAgendaItem`) extraído do corpo hoje inline em `ObterAgendaAsync`, reaproveitado pelos dois métodos; (b) calcula `totalRealizadasNoFiltro` e o numerador (presenças reais ou `Aluno.Frequencia`, conforme R3) a partir da mesma lista crua.

**Rationale**: Evita duplicar a expressão de mapeamento `Aula → RelatorioAgendaItem` (5 campos) em dois lugares, mantendo `ObterAgendaAsync` (usado pelo Relatório de Agenda, Estória 12) totalmente inalterado em comportamento — só a expressão de mapeamento é compartilhada, não a assinatura pública do método.

**Alternatives considered**:
- Manter a chamada a `ObterAgendaAsync` e fazer uma segunda chamada a `_aulaRepository.ListarAsync` só para obter a presença: rejeitado — duas idas ao banco com os mesmos filtros para obter dados que uma única chamada já traria.
- Adicionar um campo de presença a `RelatorioAgendaItem` (a resposta pública da Estória 12) para não precisar de uma segunda fonte: rejeitado — misturaria uma preocupação de outro relatório (Estória 12, estrutura já estável e sem relação com frequência) só para servir a correção deste (Estória 13); a extração de um helper privado resolve sem esse acoplamento.

## R3 — Regra exata de "quando usar presença real vs. contador vitalício"

**Decision**: Conforme a clarificação da spec (Session 2026-09-18), usar presenças reais como numerador sempre que **qualquer** um dos parâmetros `inicio`, `fim`, `status` ou `turmaId` for informado (não nulo/não vazio); usar `Aluno.Frequencia` apenas quando nenhum desses quatro parâmetros restringe a consulta. Em código: `bool temFiltroRestritivo = inicio.HasValue || fim.HasValue || turmaId.HasValue || !string.IsNullOrWhiteSpace(status);`.

**Rationale**: `alunoId` não entra nessa condição — é o parâmetro inerente ao próprio endpoint (`GET /api/relatorios/historico-aluno/{alunoId}`), não um filtro opcional que restringe o escopo em relação ao histórico do aluno; ele já delimita "todas as aulas desse aluno" em ambos os casos.

**Alternatives considered**: Restringir o gatilho só a `inicio`/`fim` (interpretação literal do pedido original) — descartada explicitamente pela resposta do usuário na clarificação, por deixar filtros de `turmaId`/`status` isolados com o mesmo bug (percentual potencialmente acima de 100%).

## R4 — Nenhum teste automatizado cobre este método hoje

**Decision**: Confirmado (busca por `ObterHistoricoAlunoAsync` em `tests/`) que não existe nenhum teste de unidade para este método. Esta feature adiciona a primeira cobertura, num arquivo novo (`RelatorioServiceHistoricoAlunoTests.cs`), seguindo o padrão de fakes manuais já estabelecido no projeto (`RelatorioServiceLancamentosTests.cs`, `RelatorioServiceFinanceiroPorTurmaTests.cs`) — sem biblioteca de mock (não instalada no projeto de testes).

**Rationale**: Diferente das features de remoção anteriores desta sessão (030/031/032), aqui há uma oportunidade natural e de baixo custo de adicionar teste automatizado real, já que o bug é puramente lógico (sem dependência de banco real) e facilmente reproduzível com um fake simples de `IAulaRepository`/`IAlunoRepository`.
