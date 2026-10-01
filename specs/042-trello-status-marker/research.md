# Research: Marcador de STATUS nos Cards do Trello

Decisões técnicas da Phase 0. Não havia "NEEDS CLARIFICATION" pendente no Technical Context — as 3 perguntas de produto já foram resolvidas no spec (sessão 2026-09-23).

## R1. Formato da linha de STATUS e como ela convive com o resto da descrição

- **Decision**: a descrição de um card sempre tem a forma `STATUS: <detalhe>` + (linha em branco) + `<corpo>`, onde `<corpo>` é tudo que a descrição já tinha antes (resumo do spec, ou texto manual, terminando no marcador `Feature: <id>` quando existir). Duas funções puras novas em `sync-card.ps1`:
  - `Extrair-CorpoSemStatus($DescAtual)`: se a 1ª linha bate com `^STATUS:.*$`, remove essa linha e uma linha em branco imediatamente seguinte (se houver); caso contrário devolve `$DescAtual` inalterada (não havia `STATUS:`).
  - `Montar-DescricaoComStatus($Detalhe, $Corpo)`: devolve `"STATUS: $Detalhe"` sozinho quando `$Corpo` está vazio, ou `"STATUS: $Detalhe`n`n$Corpo"` caso contrário.
- **Rationale**: extrair-e-reconstruir em vez de "achar e substituir com regex direto na string toda" evita duplicar ou corromper a linha em reescritas sucessivas (FR-006) e mantém uma única passagem de dados pelo corpo (FR-005). Como toda fase de escrita (existente ou nova) passa por essas duas funções, um card antigo sem `STATUS:` ganha a linha automaticamente na próxima vez que qualquer fase o tocar — resolve a US3 sem nenhum código extra dedicado a "detectar ausência de STATUS".
- **Alternatives considered**: regex de substituição direto (`-replace '^STATUS:.*'`) na descrição inteira — descartado, mais frágil a variações de quebra de linha (`\r\n` vs `\n`) e não distingue "não tinha `STATUS:`" de "tinha um `STATUS:` diferente no meio do texto manual" (edge case da spec: `STATUS:` fora da primeira linha não conta).

## R2. Eventos `after_analyze`/`after_clarify` já são suportados pelo mecanismo de hook

- **Decision**: confirmado por leitura das próprias skills `speckit-analyze` (seção "9. Check for extension hooks", lendo `hooks.after_analyze`) e `speckit-clarify` (seção "Mandatory Post-Execution Hooks", lendo `hooks.after_clarify`) — ambas já têm o mesmo bloco genérico de disparo de hook usado por `speckit-specify`/`speckit-plan`/`speckit-tasks`/`speckit-implement` (spec 040). **Nenhum arquivo de skill do Spec Kit precisa ser criado ou editado** — basta registrar as duas entradas em `.specify/extensions.yml`.
- **Rationale**: resolve o ponto de avaliação pedido no requisito original ("avaliar se isso exige registrar hooks novos... o Spec Kit pode ou não suportar esses eventos") — suporta, e já suportava antes desta feature.
- **Alternatives considered**: nenhuma — não havia alternativa a avaliar, só a confirmação.

## R3. Como um detalhe "só existente na conversa" chega ao script (FR-008, opção B)

- **Decision**: o disparo do hook continua manual (o agente que executa `/speckit-analyze`/`/speckit-clarify` invoca a skill `trello-sync-card` "do mesmo jeito que rodaria o comando ele mesmo" — mecanismo já documentado na spec 040). O **argumento** que a skill recebe passa a aceitar o formato `<fase>[: <detalhe>]` (ex.: `analyze: 2 achados aguardando decisão`). Quando o agente tiver algo relevante da conversa (contagem de achados do analyze, perguntas respondidas do clarify), ele inclui o `: <detalhe>`; caso contrário, passa só `<fase>` e o script deriva o detalhe dos arquivos (R4). Essa convenção fica documentada no próprio `SKILL.md` de `trello-sync-card` — não em nenhuma skill núcleo do Spec Kit.
- **Rationale**: o campo `prompt` de uma entrada em `extensions.yml` é estático (definido uma vez, no arquivo); não pode carregar "quantos achados o analyze desta execução específica encontrou". A única informação dinâmica disponível na hora do disparo está na própria conversa, e quem tem acesso a ela é o agente que invoca a skill — não o script PowerShell (que só enxerga arquivos em disco). Documentar a convenção no `SKILL.md` do projeto (que **pode** ser editado, ao contrário das skills núcleo) é o único lugar onde essa ponte pode viver sem violar a separação de responsabilidades já estabelecida.
- **Alternatives considered**: um arquivo temporário que o comando `/speckit-analyze`/`/speckit-clarify` escrevesse com o detalhe, para o script ler — descartado, complexidade desnecessária para um texto curto que o próprio agente já vai digitar ao invocar a skill de qualquer forma.

## R4. Detalhe derivado dos arquivos, por fase (fallback quando não há `-Detalhe`)

- **Decision**: nova função `Obter-StatusDetalhePadrao($Fase, $FeatureId)`, usada só quando `-Detalhe` não é passado:
  - `backlog` → `"/speckit-specify concluído"`
  - `design` → `"/speckit-plan concluído"`
  - `a-fazer` → `"/speckit-tasks concluído, {total} tarefas geradas"` (conta linhas `- [ ]`/`- [X]` `T###` em `tasks.md`)
  - `em-andamento` → `"/speckit-implement rodando, {concluidas}/{total} tarefas concluídas"` (mesmo `tasks.md`, no instante em que o hook dispara)
  - `revisao-codigo` → depende do resultado já calculado pela lógica existente (research.md R7 da spec 040): se o card foi movido, `"/speckit-implement concluído"`, ou `"/speckit-implement concluído, aguardando validação manual ({ids})"` quando sobrarem tarefas `(parte manual)` não marcadas; se **não** foi movido, `"/speckit-implement rodando, {pendentes} tarefa(s) pendente(s), testes {ok|falharam}"` (reaproveita o `$motivo` já calculado).
  - `analyze` → `"/speckit-analyze concluído"`
  - `clarify` → `"/speckit-clarify concluído"`
- **Rationale**: cobre o exemplo do pedido original ("8/12 tarefas concluídas", "aguardando validação manual (T018/T019)") sem exigir nenhum evento novo a cada tarefa concluída (não existe esse evento — ver Nota de investigação prévia do spec.md) — o progresso é sempre lido do `tasks.md` no instante do hook, nunca de um contador em memória entre execuções.
- **Alternatives considered**: persistir um contador de progresso em algum arquivo — descartado, `tasks.md` já é a fonte de verdade do progresso; duplicar esse dado violaria o Princípio II em espírito (uma fonte só).

## R5. `-Fase retroativo`: atualização em lote dos cards manuais (US4/FR-009)

- **Decision**: nova fase `retroativo`, sem hook associado (não é `after_*` de nenhum comando — roda manualmente, uma vez, via `sync-card.ps1 -Fase retroativo`). Busca todos os cards do quadro (`Get-TrelloCards`, já existente); para cada card cuja descrição **não** termina em `Feature: <id>` (ou seja, é manual) e cuja primeira linha **não** começa com `STATUS:`, aplica `Montar-DescricaoComStatus("card criado manualmente, sem spec formal ainda", $card.desc)` e grava. Idempotente: uma segunda execução não altera nada (todos já têm `STATUS:`).
- **Rationale**: é uma correção de dados existentes, não um comportamento contínuo — não faz sentido registrar hook para algo que roda uma vez. Reaproveita a mesma checagem "termina em `Feature: <id>`?" que `Find-FeatureCard` já usa (research.md R4 da spec 040), só invertida (procura quem **não** bate).
- **Alternatives considered**: rodar essa varredura a cada execução de qualquer fase — descartado, viola FR-011 (a leitura do quadro não pode virar um polling regular) e seria custoso sem necessidade (o conjunto de cards manuais já existentes é fixo; novos cards manuais que a professora criar no futuro não são o alvo de FR-009, que é explicitamente retroativo).

## R6. `PUT` de `desc` precisa de query string URL-encoded (extensão do bug já documentado na spec 040)

- **Decision**: nova função `Update-TrelloCardDescription($Cred, $CardId, $Desc)`: `$uri = ".../cards/${CardId}?key=...&token=...&desc=$([System.Uri]::EscapeDataString($Desc))"`, `Invoke-RestMethod -Method Put` (sem `-Body`) — mesmo padrão de `Move-TrelloCard` (idList também foi movido para a query string), mas com `EscapeDataString` porque `$Desc` pode conter espaços, quebras de linha, acentos e os caracteres `&`/`=` que quebrariam a query string se não escapados.
- **Rationale**: a spec 040 já provou, em produção (validação manual da 040), que `Invoke-RestMethod -Method Put -Body <hashtable>` não é aplicado pela API do Trello nesta versão do PowerShell — retorna `200` sem aplicar a mudança. A correção usada para `idList` (um valor sempre alfanumérico simples) não precisou de encoding; `desc` precisa.
- **Alternatives considered**: `Invoke-WebRequest` com corpo bruto pré-codificado (`key=...&desc=...`) e `-ContentType application/x-www-form-urlencoded` — funcionalmente equivalente a colocar tudo na query string de um PUT; optou-se pela query string por consistência com `Move-TrelloCard` (mesma forma, mesmo arquivo, mais fácil de revisar).
- **Risco documentado**: URLs muito longas podem esbarrar em limites de alguns proxies/CDNs; descrições desta integração são resumos curtos (poucas frases), então o risco é baixo, mas fica registrado para o caso de uma feature futura gerar resumos muito extensos.

## R7. Nenhum teste automatizado (mantém a decisão da spec 040, R8)

- **Decision**: sem suíte Pester/unitária para o script — validação via `-DryRun` e `quickstart.md`, mesma justificativa já aceita e usada com sucesso na spec 040 e na validação real contra o quadro "Estágio SPI".
- **Rationale**: consistência com a decisão já tomada; o escopo desta feature é pequeno o suficiente (extração/reconstrução de string + 3 fases novas) para ser coberto por dry-run e por uma passagem manual real, sem introduzir a primeira suíte de testes do projeto para um script PowerShell.
