# Research: Chamada só nos detalhes da aula e aviso de aula sem frequência (spec 064)

Todas as decisões abaixo vêm da leitura do código atual (2026-10-31) e de medições no banco local (somente leitura). Nenhuma `NEEDS CLARIFICATION` ficou aberta.

## R1 — Relógio e fuso: `TimeProvider` + `America/Sao_Paulo`

**Hoje**: nenhuma abstração de relógio; 22 pontos usam `DateTime.Now` (hora do servidor). O servidor de desenvolvimento está em `E. South America Standard Time` (UTC−3), mas a spec exige o fuso **fixo** de São Paulo, porque o servidor de produção pode estar em outro fuso.

**Decisão**: usar o `TimeProvider` que já vem no .NET (registrado como `TimeProvider.System` no DI) e uma extensão `AgoraEmSaoPaulo()` em `SPI.Application/Common/`, que converte `GetUtcNow()` para o fuso `America/Sao_Paulo` e devolve um `DateTime` "de parede" (sem fuso), o mesmo formato em que `aula.data_inicio` + `hora_inicio` são guardados. Os testes usam um `TimeProvider` falso escrito à mão (sobrescreve `GetUtcNow`), no padrão de fakes manuais do projeto.

- **Id do fuso**: testado nesta máquina (.NET 10, Windows): `America/Sao_Paulo` e `E. South America Standard Time` resolvem e convertem igual (`15:00Z → 12:00`). Linux resolve o id IANA nativamente; um contêiner mínimo **sem `tzdata`** não resolveria. A extensão tenta o id IANA e, se falhar, o id do Windows; se os dois falharem, **lança erro na primeira chamada** (falhar alto é melhor que decidir "já começou" com o fuso errado).
- **Horário de verão**: o Brasil não usa hoje, mas o `TimeZoneInfo` cobre o histórico; nada a configurar.

**Fronteira deliberada**: esta spec troca o relógio **só** nos pontos que decidem "a aula já começou/terminou": `AulaService` (chamada e marca de registro), `NotificacoesController` (2 chamadas) e `NotificacaoDispatcherService` (que passam o `agora` ao `NotificacaoService`). Os demais usos de `DateTime.Now` (financeiro, relatórios, lembretes, competência da mensalidade, turma de pacote) **ficam como estão**: não decidem a chamada e trocá-los mudaria comportamento fora do pedido. Isso fecha o item que a spec 065 deixou para a 064 (R7); o restante vira card sugerido.

**Alternativas**: (a) interface própria `IRelogio` — descartada, `TimeProvider` é o padrão da plataforma e já é testável; (b) `DateTime.Now` com a configuração do servidor — descartada, viola FR-007; (c) NodaTime — descartada, dependência nova sem necessidade.

## R2 — Uma regra só, no backend: `RegraChamada`

Princípio II: a regra de "chamada liberada" existe em **um lugar** e é reaproveitada. Novo tipo estático e puro em `SPI.Application/Aulas/Services/RegraChamada.cs` (mesmo padrão de `RegraCobrancaPresenca`):

- `InicioDaAula(aula)` = `DataInicio` + `HoraInicio` (como `DateTime`);
- `Liberada(aula, agora)` = aula **ativa**, `Status == "Agendada"` e `agora >= InicioDaAula(aula)` (comparação conjunta data+hora, `>=`: no minuto exato do início já está liberada);
- `SegundosParaLiberar(aula, agora)` = segundos até o início, ou `null` se já liberada/não se aplica;
- `MensagemBloqueio(aula)` = `"A chamada ficara disponivel a partir das HH:mm de dd/MM/yyyy."`.

É usada por `RegistrarSessaoAsync` (recusa com 409) e por `Mapear` (informa à tela). A tela só exibe o resultado (FR-007).

## R3 — Onde o servidor recusa (FR-006)

`RegistrarSessaoAsync`, depois das verificações atuais (existe; `Status == "Agendada"`) e **antes** de `AplicarPresencasAsync`: se `!RegraChamada.Liberada(...)`, lança `ConflitoException(MensagemBloqueio)`. O 409 sai pelo `ErrosDeNegocioFilter` da spec 065, porque a ação `RegistrarSessao` já declara `[ProducesResponseType(409)]` (conferido em `AulasController`); **não** se acrescenta `try/catch` no controller. Nada é gravado antes do `throw`.

## R4 — Aula excluída não faz chamada (edge case da spec)

A spec diz "aula cancelada ou excluída: sem chamada". Hoje `RegistrarSessaoAsync` **não** olha `Ativo` (uma aula excluída ainda "Agendada" aceitaria registro pela API). Decisão: tratar `!aula.Ativo` como `NaoEncontradoException("Aula nao encontrada.")` no começo de `RegistrarSessaoAsync`, igual a `ObterAgendadaAsync`. Mudança pequena, coberta por teste, e necessária para cumprir o edge case.

## R5 — Criação de aula sem presença (FR-001, FR-002)

- `AulaRequest.Presencas` **sai** do contrato. O `System.Text.Json` ignora campo desconhecido, então um cliente antigo que ainda mande `presencas` tem o campo ignorado e a aula nasce "Agendada" (cenário 3 da US1: "ignoradas ou recusadas").
- `AulaService.CadastrarAsync`: remove `retroativa`, a exceção "Informe a presenca…" e a chamada a `AplicarPresencasAsync`; o recálculo dos lembretes da turma passa a rodar **sempre** (hoje só roda no caso futuro, porque no retroativo era feito dentro de `AplicarPresencasAsync`).
- `AplicarPresencasAsync` passa a ter um único chamador (`RegistrarSessaoAsync`); o comentário "usado por RegistrarSessao e por CadastrarAsync" deixa de valer e é atualizado.

## R6 — Coluna `aula.frequencia_registrada_em` (FR-015)

`DATETIME NULL`, sem valor padrão, sem preenchimento retroativo. Gravada em `AplicarPresencasAsync` (o **único** ponto que torna a aula "Realizada"), com `AgoraEmSaoPaulo()`. Entidade: `Aula.FrequenciaRegistradaEm` (`DateTime?`); mapeamento `frequencia_registrada_em`. Migração `database/26_aula_frequencia_registrada_em.sql` (24 é da 062 e 25 da 063), idempotente no estilo do projeto (`ALTER TABLE … ADD COLUMN`), aplicada no banco **local** com backup antes; produção só o usuário aplica.

## R7 — Aviso de aula sem frequência, sem corte (FR-010 a FR-014)

Não há estrutura nova: o tipo `AulaPendente` da spec 059 já cria, reativa e resolve uma notificação por aula pelo conjunto `ListarPendentesDeStatusAsync`.

- Remover o parâmetro `desde` de `IAulaRepository.ListarPendentesDeStatusAsync` e da implementação (some o filtro `HoraFim >= horaDesde`); sobra "ativa, Agendada e já terminou".
- Remover `NotificacoesOptions.AulasPendentesDesde` e a chave `Notificacoes:AulasPendentesDesde` do `appsettings.json` (Princípio III: nada de opção configurável sem efeito).
- Texto da mensagem em `NotificacaoService.Mapear` e título no `NotificacoesSino.tsx`: "Esta aula já terminou e está sem frequência registrada. Registre a chamada para corrigir." / "Aula sem frequência". O botão "Ir para a aula" não muda.
- O aviso **não** depende do relógio de SP dentro do repositório: ele recebe o `agora` já em SP do chamador (R1).
- A regra "em andamento não gera aviso" (FR-011) já é a atual (`HoraFim <= agora`); ganha teste explícito.

**Medição local (somente leitura, 2026-10-31)**: 4 aulas ativas "Agendada" já terminadas; **2** são anteriores a 19/10/2026 e hoje ficam sem aviso — passam a ter. Aulas "Realizada" ativas com início no futuro: **0**.

## R8 — Auditoria de dados inconsistentes (FR-016, SC-006)

Consulta SQL **somente de leitura** em `database/consultas/aulas_realizadas_antes_do_inicio.sql` (pasta nova, fora da sequência numerada de migrações para não ser executada por engano na ordem). Lista aulas ativas "Realizada" em que (a) o início ainda está no futuro, ou (b) `frequencia_registrada_em` existe e é anterior ao início. Aulas "Realizadas" antigas, com a coluna vazia, só entram pelo critério (a). O arquivo começa com a frase "SOMENTE LEITURA — não altera nem apaga nada".

- **Por que SQL e não endpoint/tela**: é uma conferência pontual para o dono do sistema; um endpoint seria capacidade nova a manter (Princípio III) sem uso recorrente.
- **No banco local**: 0 linhas hoje. **Produção**: o usuário roda a consulta e decide; esta spec nunca acessa nem altera produção.

## R9 — Frontend

- **Resposta da API**: `AulaResponse` ganha `chamadaLiberada` (bool) e `segundosParaChamada` (int?, só quando bloqueada por horário). `Mapear` os calcula com o `agora` do servidor.
- **`aulas/[id]/page.tsx`**: com `status === "Agendada"`, mostra as caixas de presença e o botão "Registrar como realizada" **somente se** `aula.chamadaLiberada`; senão mostra "A chamada ficará disponível a partir das HH:MM de DD/MM/AAAA." montado com `dataInicio`/`horaInicio` da própria resposta (só formatação, com `fmtData`/`fmtHora` existentes). Editar, cancelar, excluir e "Adicionar (reposição)" continuam como estão (FR-009).
- **Atualização sem recarregar**: se `segundosParaChamada` vier preenchido, a página agenda **um** `setTimeout` com essa duração (+1 s) e busca a aula de novo. A duração é do servidor; o navegador não compara horas nem decide nada (FR-007).
- **`AulaForm.tsx`**: remove `retroativa`, `presencas`, `alunosDaAula`, a seção "Presença", o texto "A data já chegou…", os textos "Registrar aula realizada"/"Aula registrada como realizada" e o import de `dataIsoLocal`. Botão sempre "Agendar aula" (ou "Salvar alterações" ao editar).
- **`PresencaChecklist.tsx`** só era usado pelo `AulaForm`; fica sem uso e é **removido** (código morto, no espírito da 065).
- **`lib/api/aulas.ts`**: tira `presencas` de `AulaRequest`; acrescenta os dois campos em `Aula`.

## R10 — Impacto nos testes existentes

- `AulaServiceCadastrarRetroativoTests` testava o comportamento que esta spec **revoga** (055): é reescrito como "toda aula nasce Agendada, sem frequência nem cobrança, em qualquer data" (as expectativas mudam porque a regra mudou, e isso está documentado nas notas das specs 055/059).
- Testes que registram sessão (`RegistrarSessao*`, `GerarContasAReceber*`, `CobrancaPorAluno`, `PacoteGrupo`, `ConflitoAluno`, `Reposicao`…) usam datas qualquer: os que usam data **futura** passariam a receber 409. Eles passam a construir o `AulaService` com um `TimeProvider` falso fixo **depois** do início da aula (só o arrange muda; nenhum `Assert` é alterado).
- `NotificacaoFakes` deixa de passar `AulasPendentesDesde`; `NotificacaoServiceTests` ganham os casos "sem corte" e "em andamento não gera aviso".
- Novos: `RegraChamadaTests` (os 5 casos de borda, o minuto exato e a mudança de horário), `AulaServiceChamadaTests` (409 antes do início, liberado depois, aula excluída, `FrequenciaRegistradaEm` gravada), `NotificacaoServiceAulaPendenteSemCorteTests`.

## R11 — Documentação (FR-017, Princípio V)

Notas apontando para a 064 como regra vigente em `specs/055-*/spec.md` (FR-001 a FR-007; a US2 continua valendo) e `specs/059-*/spec.md` (FR-004). Relatório `docs/relatorios/<data>-chamada-so-nos-detalhes.md` ao fim da implementação.

## R12 — Riscos

| Risco | Mitigação |
|---|---|
| Servidor sem `tzdata` não resolve o fuso | Fallback para o id do Windows; se ambos falharem, erro explícito na primeira chamada (e teste do helper) |
| Professora com a página aberta antes do início | `segundosParaChamada` refaz a busca na hora; o servidor revalida de qualquer forma (409) |
| Aviso em massa ao remover o corte | Esperado e pedido; hoje são só 2 a mais no banco local |
| Testes antigos com data futura passam a falhar | Arrange com relógio fixo (R10); nenhum `Assert` mudado |
| Conflito de arquivo com a spec 065 | A 065 já foi implementada (nenhum conflito); `AulaService` foi tocado só em comentários |
