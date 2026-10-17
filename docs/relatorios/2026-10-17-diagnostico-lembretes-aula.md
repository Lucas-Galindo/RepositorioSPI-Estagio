# Relatório: Diagnóstico do módulo de lembretes de aula (não Mensalidade)

**Card Trello**: nenhum pré-existente — diagnóstico criado por iniciativa do processo autônomo (item 5), card novo registrado no Trello para rastreabilidade
**Spec**: não há spec formal dedicada ao módulo de Lembretes de aula; a referência mais próxima é `specs/001-lembretes-canal-email` (histórica, só sobre a restrição de canal) e comentários de código citando "Estoria 5" e "Estoria 19" (sem documento correspondente encontrado no repositório — provavelmente parte do brief original do projeto, nunca persistido como spec)
**Status final**: ✅ Concluído (3 correções aplicadas, nenhuma bloqueada por dúvida de negócio)

## Situação atual (antes deste diagnóstico)

O módulo de lembretes de aula é composto por:
- **`Lembrete`** (entidade): ligado a uma `Turma`, com `Canal` (só "Email", desde specs/001), `Destinatarios`, `AntecedenciaHora`, `HoraProgramada` (calculada) e `Status` (`Pendente`/`Enviado`/`Falha`/`Ativo`).
- **`LembreteService.RecalcularAsync`**: recalcula `HoraProgramada` de todos os lembretes ativos de uma turma com base na "próxima aula" dela, e reabre o ciclo (`Status = "Pendente"`) — chamado por `AulaService` quando uma aula é cadastrada, atualizada ou cancelada ("Estoria 5").
- **`LembreteDispatcherService`**: `BackgroundService` que varre lembretes `Pendente` vencidos e envia e-mail, respeitando um orçamento por ciclo (specs/049) ("Estoria 19").

## Desalinhamentos encontrados e corrigidos

### 1. `AulaService.ExcluirAsync` não recalculava os lembretes da turma

`CancelarAsync` já tinha o cuidado explícito (com comentário) de chamar `RecalcularAsync` porque uma aula cancelada sai da busca por "próxima aula" (`ObterProximaAulaDaTurmaAsync` filtra por `Ativo` e `Status == "Agendada"`). `ExcluirAsync` (exclusão lógica, `Ativo = false`) tem exatamente o mesmo efeito sobre essa busca, mas **não** chamava `RecalcularAsync` — uma omissão, não uma decisão documentada. Efeito prático: excluir a aula que era a "próxima" de uma turma deixava o lembrete apontando para uma aula que não existe mais, sem nunca se corrigir sozinho (só se corrigiria se uma aula nova fosse cadastrada depois).

**Corrigido**: `ExcluirAsync` agora chama `_lembreteService.RecalcularAsync(aula.TurmaId.Value, ...)`, mesmo padrão de `CancelarAsync`.

### 2. `AulaService.RegistrarSessaoAsync` (marcar aula como Realizada) não recalculava os lembretes da turma

Mesma categoria do item 1: uma aula `Realizada` também sai da busca por "próxima aula". Se uma turma já tinha uma segunda aula futura agendada (nada no sistema impede isso, mesmo sem recorrência automática), registrar a sessão da aula mais próxima deixava o lembrete "presa" na aula que acabou de acontecer (`Status = "Enviado"`, apontando para uma data no passado), sem nunca avançar para a próxima aula já agendada. Isso só se corrigia quando uma aula totalmente nova fosse cadastrada depois. Havia inclusive um comentário em `tests/.../AulaServiceFakes.cs` documentando essa lacuna como fato conhecido ("Não é chamado por RegistrarSessaoAsync"), sem que isso fosse uma decisão de negócio registrada em nenhuma spec — e **zero testes** cobriam esse comportamento (nem o buraco, nem o `CancelarAsync` que já funcionava).

**Corrigido**: `RegistrarSessaoAsync` agora também chama `_lembreteService.RecalcularAsync` após marcar a aula como `Realizada`.

### 3. Status "Cancelado" documentado e com suporte visual completo no frontend, mas nunca produzido pelo backend

Evidências de que "Cancelado" era um status **intencional**, não uma invenção da tela:
- `LembretesController.Listar`, comentário XML: `Filtro por status (Pendente, Enviado, Falha, Cancelado)`.
- `frontend/.../lembretes/page.tsx`: KPI "Cancelados", opção "Cancelado" no filtro de Status.
- `frontend/components/shared/StatusBadge.tsx`: `Cancelado: "badge-done"` já mapeado.

Mas nenhum caminho do backend jamais escrevia `Status = "Cancelado"` — os únicos valores realmente produzidos eram `Pendente` (criação/recálculo), `Enviado`/`Falha` (disparo). `LembreteService.ExcluirAsync` só mudava `Ativo` para `false`, sem tocar `Status`. Resultado: o KPI "Cancelados" e o filtro correspondente sempre mostravam zero/vazio — uma funcionalidade visível na tela que nunca funcionava.

**Corrigido**: `ExcluirAsync` agora também define `Status = "Cancelado"` (além de `Ativo = false`). Não decidi isso adivinhando — a conclusão vem de juntar as 3 evidências acima (contrato da API documentado, badge já estilizado, filtro já construído) com o fato de `ExcluirAsync` ser o único outro ponto do ciclo de vida de um `Lembrete` que poderia corresponder a esse status. Risco zero sobre o disparo de e-mails: `LembreteDispatcherService` já filtra por `Ativo` (que continua virando `false`), então o texto do `Status` não influencia se um lembrete excluído é ou não disparado.

## O que foi corrigido vs. o que ficou como observação (sem ação)

| Achado | Ação |
|---|---|
| `ExcluirAsync` (Aula) não recalculava lembretes | Corrigido |
| `RegistrarSessaoAsync` (Aula) não recalculava lembretes | Corrigido |
| Status "Cancelado" nunca produzido pelo backend | Corrigido |
| Zero cobertura de teste para os gatilhos de recálculo (Estoria 5) | Corrigido (6 testes novos) |
| Editar (`AtualizarAsync`) um lembrete já excluído/Cancelado reabre o ciclo (`HoraProgramada` recalculada) sem reverter `Ativo`/`Status` | **Observação, não corrigido** — decidir se isso deveria ser bloqueado é uma escolha de produto (a tela hoje nem oferece essa ação de forma distinta), não uma correção óbvia; registrado aqui para decisão futura, não bloqueou o diagnóstico |
| Logs de `2026-10-13` com `Access denied for user 'root'@'localhost'` no `LembreteDispatcherService` | **Observação, não é bug de código** — erro de credencial/ambiente daquele momento específico; as credenciais atuais funcionam normalmente (confirmado nesta sessão: migração 19 aplicada e teste real do item 4 rodaram sem erro contra o mesmo banco) |
| `.env.test` não existe no repositório | **Observação, ambiente** — impediu testar via login real de Professor; usei testes automatizados + leitura de código em vez de chamadas HTTP reais para este diagnóstico |

## Respostas as perguntas diagnósticas (com base nas estórias)

- **O módulo está alinhado com a "Estoria 5" (recálculo automático da hora programada)?** Não estava, em 2 de 4 pontos de mudança de "próxima aula" da turma (exclusão e registro de sessão) — agora está, nos 4 pontos (criação, edição, cancelamento, exclusão, registro de sessão).
- **O módulo está alinhado com a "Estoria 19" (disparo por e-mail)?** Sim — `LembreteDispatcherService` já implementa orçamento por ciclo, antirrepetição de destinatários entre ciclos e canal restrito a Email (specs/001, specs/049), sem desalinhamentos encontrados.
- **A tela de Lembretes reflete fielmente o que o backend faz?** Agora sim — antes, o KPI/filtro "Cancelado" era uma funcionalidade morta (nunca populada); os demais elementos da tela (Canal, Destinatários, Status Pendente/Enviado/Falha) já estavam corretos.

## Como foi testado e o resultado

- `dotnet build`: sem erros.
- `dotnet test`: **256/256** (250 baseline do início deste item + 6 novos: 4 em `AulaServiceRecalculoLembreteTests.cs` cobrindo os gatilhos de recálculo, 2 em `LembreteServiceExcluirTests.cs` cobrindo o novo status `Cancelado`).
- Não testei via HTTP real (login como Professor) por não ter `.env.test` nem a senha do professor já cadastrado (`srlucasmtg@gmail.com`, id 6) nesta sessão — nunca escrevo senha em código/relatório/Trello, então não havia como obter credencial de teste sem violar essa regra. A cobertura ficou pelos testes automatizados, que exercitam exatamente os mesmos métodos de serviço que a API chama.

## Pendências, riscos e o que não foi feito

- Teste via navegador/HTTP real não foi feito (ver acima).
- A questão de "editar um lembrete excluído reabre o ciclo" (tabela acima) ficou como observação para decisão de produto futura, não bloqueou nada aqui.
- Não foi aberta nenhuma spec formal para o módulo de Lembretes de aula — se o negócio quiser evoluir esse módulo further, recomendo um `/speckit-specify` dedicado nessa hora, já que hoje ele só existe espalhado em comentários de código ("Estoria 5"/"Estoria 19") sem documento correspondente.

## Commit

`fix(lembretes): recalculo de lembretes em exclusao/registro de sessao + status Cancelado`
