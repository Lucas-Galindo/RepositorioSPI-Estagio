# Pesquisa técnica: Saída do aluno de turma de pacote com acerto (spec 078)

Decisões tomadas lendo o código atual (conferido em 2026-11-08). Nada precisou de "NEEDS CLARIFICATION" adicional.

## R1 — Onde mora a regra: serviço novo, cálculo puro separado

- **Decisão**: `SaidaPacoteService` (Application/VinculosCobranca/Services) com `PreVisualizarAsync` e `RegistrarAsync`, e um cálculo **estático e puro** `AcertoSaidaPacote.Calcular(valor, total, consumidas, pago)` (sem banco, sem relógio), no mesmo estilo de `ProgressoPacote` (spec 058) e `GeradorSerieAulas` (spec 077).
- **Por quê**: a prévia e a gravação **devem** usar o mesmo cálculo (FR-002, FR-007, SC-006) e o cálculo precisa de uma grade de testes sem infraestrutura (SC-001). Estender `VinculoCobrancaService` (já ~600 linhas) ou `TurmaService` misturaria responsabilidades; o desvincular existente (`TurmaService.DesvincularAlunoAsync`) **não é alterado** (FR-011).
- **Alternativas**: pôr na `TurmaService` (rejeitada: toca no desvincular e em cobrança); stored procedure (rejeitada: o EF já salva tudo numa transação só, como nas specs 062/077).

## R2 — Aulas consumidas e total: reaproveitar a fonte única da spec 058

- **Decisão**: consumidas = `IAulaRepository.ContarPresencasPorVinculosAsync([pacoteId])` (presenças gravadas com o vínculo; falta não consome, reposição conta) e total = `ProgressoPacote.Calcular(pacote, dadas).Total` (inclui o total **estimado** de pacote antigo). Sem total → 409 (spec, edge case).
- **Por quê**: a ficha do aluno e a Home já mostram esses números; o acerto **não pode** divergir deles.

## R3 — Cálculo e arredondamento

- **Decisão**: `devido = Math.Round(valor × min(consumidas, total) ÷ total, 2, MidpointRounding.AwayFromZero)` — **um** arredondamento, sobre o total (com tudo consumido dá exatamente o valor; R$ 100 × 2 ÷ 3 = 66,67). `valorPorAula = Math.Round(valor ÷ total, 2)` só informativo. `diferenca = max(0, devido − pago)`; `geraConta = diferenca > 0`; `excedente = max(0, pago − devido)` (só para avisar, nunca vira lançamento).
- **Alternativas**: arredondar o valor por aula antes de multiplicar (rejeitada: 100 ÷ 3 = 33,33 × 3 = 99,99 ≠ 100).

## R4 — Marcar "encerrado pela saída" (precisa de uma coluna)

- **Decisão**: nova coluna `vinculo_cobranca.saida_registrada_em DATETIME NULL` + `CHECK` (só pacote inativo pode tê-la). Migração **28**, sem apagar nem alterar dado. Fica fora a tabela de "histórico de acertos" (a spec diz que não há).
- **Por quê**: sem um marcador não há como impedir que `ReativarAsync` (spec 063, FR-021) reabra o pacote (FR-010), nem distinguir "pacote inativo porque a turma acabou" de "porque o aluno saiu"; o marcador também dá a idempotência da saída (FR-012, 2º pedido → 409) e deixa a ficha mostrar "Saída registrada em …".
- **Alternativa**: usar só a existência da conta de acerto como marcador (rejeitada: quando pago ≥ devido não há conta).

## R5 — Conta a receber do acerto (reaproveita a spec 062)

- **Decisão**: um `Pagamento` "Pendente" com `ValorFinal = diferença`, `DataVencimento` informado, `Competencia` = 1º dia do mês do vencimento, `VinculoCobranca` = o pacote, `CategoriaReceita` = nova **"Acerto de pacote"** (inserida na migração 28; o serviço falha com mensagem "aplique a migração 28" se faltar, igual à 062) e `Descricao` com aluno, turma, consumo, devido e pago.
- **Por quê**: é exatamente o formato das parcelas da 062; a unicidade `(vinculo, competência)` do banco ainda protege contra conta duplicada; nada novo em Contas a Receber/relatórios.
- **Efeito colateral conhecido e desejado**: o lembrete de vencimento por e-mail (5 dias antes) cobre todo pagamento pendente com vínculo (generalizado na 062) — a conta do acerto também recebe esse lembrete.

## R6 — Atomicidade

- **Decisão**: todos os repositórios compartilham o mesmo `DbContext` (como em 062/077). `RegistrarAsync` faz `Desvincular` (marca a remoção da matrícula), `pacote.Ativo = false` + `SaidaRegistradaEm`, `Adicionar(pagamento)` e **um** `SalvarAlteracoesAsync` (transação única do EF). Falha → nada muda (SC-004).
- **Corrida**: dois cliques quase simultâneos podem passar pela leitura ao mesmo tempo; a unicidade `(vínculo, competência)` impede a 2ª conta (erro do banco → falha, nada gravado) e, depois do 1º, o 2º pedido vê o pacote inativo → 409. Sistema de uma professora só; aceito.

## R7 — Proteção contra resumo desatualizado

- **Decisão**: a gravação recebe `consumidasEsperadas` e `valorDevidoEsperado` (ecos do que a prévia devolveu, a tela **não calcula**), recalcula tudo e responde **409** "o consumo mudou desde o resumo; confira o novo resumo" se divergirem (FR-007). A tela então refaz a prévia.

## R8 — Quem decide se o botão aparece

- **Decisão**: `AlunoResumoResponse.PodeRegistrarSaida` (bool, calculado no servidor em `TurmaService.MapearComPacotesAsync`, que já carrega os pacotes ativos da turma): turma de pacote **em andamento** (`Turma.EstaEmAndamento`) e o aluno matriculado tem pacote ativo dela. A tela só lê o campo (Princípio II); o servidor revalida no pedido (FR-012).

## R9 — Reativação e matrícula depois da saída

- **Decisão**: `VinculoCobrancaService.ReativarAsync` passa a recusar (409, mensagem clara) quando `SaidaRegistradaEm` tem valor; `VinculoCobrancaResponse` ganha `SaidaRegistradaEm` para a ficha esconder "Reativar" e mostrar "Saída registrada em …". A matrícula direta (`TurmaService.VincularAlunoAsync`) já recusa quem não tem pacote **ativo** (spec 063, FR-004) — a tarefa de teste confirma, sem mudar o código.

## R10 — Relógio

- **Decisão**: `TimeProvider` + `AgoraLocal()` (relógio do sistema, como em `AulaService`); "hoje" para o vencimento mínimo e para `SaidaRegistradaEm`. Não fixar São Paulo (decisão do usuário).

## R11 — Endpoints

- **Decisão**: dois `POST` sob a turma, no `TurmasController`, com `[ProducesResponseType]` 400/404/409 declarados (o `ErrosDeNegocioFilter` só mapeia o que está declarado): `…/alunos/{alunoId}/saida/previa` (200, não grava) e `…/alunos/{alunoId}/saida` (200, grava). O `DELETE` de desvincular **não muda**.

## R13 — Aulas futuras da turma (decisão do usuário em 2026-11-08)

- **Problema**: o desvincular de hoje só remove a matrícula; o aluno continua em `aula_aluno` das aulas futuras. Com o pacote encerrado, uma presença marcada por engano cairia em `SemVinculo` (cobra o "valor da aula") ou Avulsa (`RegraCobrancaPresenca`). Não há tela para tirar um **matriculado** de uma aula individualmente (`RemoverParticipanteAsync` só remove reposição).
- **Decisão**: a saída retira o aluno das aulas da turma com `Status = "Agendada"`, `Ativo`, `Reposicao = false`, `AlunoId` dele e **início ainda não passado** (data + hora de início > agora local). Não toca em aulas já iniciadas (a chamada delas segue pendente para a professora), realizadas, canceladas, reposição, outras turmas e individuais.
- **Como**: `IAulaRepository.ListarParticipacoesAgendadasQueNaoComecaramAsync(turmaId, alunoId, agoraLocal)` (lê, rastreado) e `RemoverParticipacoes(lista)` (`RemoveRange`), **dentro do mesmo `SalvarAlteracoesAsync`** (nada de `ExecuteDelete`, que ficaria fora da transação). A prévia só **conta** (`AulasAgendadasQueSairao`).
- **Lembretes**: não dependem do rol de alunos da aula; sem ação.

## R12 — Frontend

- **Decisão**: botão "Registrar saída" ao lado de "Remover" (só se `podeRegistrarSaida`); modal novo `SaidaPacoteModal` com campo "Já pagou (R$)", vencimento (só se a prévia disser que gera conta), resumo vindo da prévia (com proteção contra resposta atrasada, como na spec 077) e confirmação. Textos da consequência mapeados a partir de um código do servidor; nenhum cálculo de dinheiro na tela (CLAUDE.md: verificação por leitura do diff).
