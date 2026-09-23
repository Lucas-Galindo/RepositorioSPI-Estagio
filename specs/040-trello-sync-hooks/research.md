# Research: Sincronização Automática Spec Kit → Trello

Todas as decisões abaixo foram tomadas lendo o mecanismo de hooks documentado nas skills `speckit-*` já presentes neste projeto (`.claude/skills/speckit-*/SKILL.md`, todas idênticas nesse ponto) e a ausência de `.specify/extensions.yml`/exemplos anteriores; nenhum `NEEDS CLARIFICATION` restou após `/speckit-clarify`.

## R1. Linguagem do script (deferida pelo usuário para esta fase)

- **Decision**: PowerShell 5.1, usando `Invoke-RestMethod` (nativo, sem dependência externa).
- **Rationale**: `.specify/scripts/powershell/` já é 100% do jeito que este projeto automatiza o Spec Kit (`init-options.json` tem `"script": "ps"`) — usar PowerShell mantém a nova peça no mesmo "dialeto" de automação já estabelecido, sem exigir `npm install`/`package.json`/`node_modules` só para um script pequeno e de baixa frequência de execução. `Invoke-RestMethod` já resolve serialização JSON e chamadas HTTP sem nenhuma biblioteca extra.
- **Alternatives considered**: Node.js (citado como opção pelo usuário) — descartado por exigir uma árvore de dependências própria (`package.json`, `node_modules`) para uma ferramenta pequena que roda poucas vezes por sessão de trabalho, e por divergir do único dialeto de script que o Spec Kit deste projeto já usa.

## R2. Como o hook realmente aciona o script (mecanismo confirmado, não hipotético)

- **Decision**: `extensions.yml` registra `command: trello.sync-card` (mesmo comando, nome idêntico) nas 5 entradas de hook — a diferença entre elas fica só no campo `prompt` de cada entrada, que carrega a fase (`backlog`, `design`, `a-fazer`, `em-andamento`, `revisao-codigo`). A skill nova `.claude/skills/trello-sync-card/SKILL.md` (invocável como `/trello-sync-card <fase>`) lê esse argumento e chama `sync-card.ps1 -Fase <fase>`.
- **Rationale**: Confirmado, lendo as 6 skills `speckit-*` já usadas nesta sessão (specify, plan, tasks, implement, taskstoissues, e o texto repetido em todas), que o mecanismo de hook **não é executado por um serviço externo** — é o próprio Claude que lê `extensions.yml`, decide quais hooks disparar, e invoca o comando indicado "da mesma forma que executaria você mesmo". Não existe, neste projeto, nenhum binário/serviço "HookExecutor" (confirmado: nenhuma ocorrência de `extensions.yml`/`HookExecutor` em `.specify/scripts/powershell/*.ps1`). Isso também confirma que **hooks `optional: true` nunca disparam sozinhos** — o bloco fixo do formato "Optional Pre-Hook" só *oferece* o comando ("To execute: `/{command}`"), sem o campo `EXECUTE_COMMAND` que instrui a invocação automática. Como o pedido do usuário é sincronização **sem intervenção manual**, as 5 entradas desta feature MUST ser `optional: false` (mandatórias) — é esse o único caminho documentado para disparo automático.
- **Alternatives considered**: 5 skills separadas (`trello-sync-backlog`, `trello-sync-design`, ...) — descartado por duplicar quase todo o corpo da skill 5 vezes só para variar um parâmetro; um único hook `command` com lógica de "adivinhar a fase pelo hook que disparou" dentro da skill — descartado por ser mais frágil que simplesmente receber a fase como argumento explícito via `prompt`.

## R3. Por que nenhuma entrada de hook usa `condition`

- **Decision**: Nenhuma das 5 entradas em `extensions.yml` define `condition`. Toda decisão condicional (a mais notável: FR-007a, "só mover para Revisão de código se os testes passarem") vive inteiramente dentro de `sync-card.ps1`.
- **Rationale**: O texto de toda skill `speckit-*` deste projeto instrui Claude a, diante de uma entrada de hook com `condition` não-vazio, **pular o hook e não avaliar a condição** ("skip the hook and leave condition evaluation to the HookExecutor implementation"). Como confirmado em R2, não existe HookExecutor real instalado — logo, qualquer hook com `condition` preenchido neste projeto **nunca dispara**, incondicionalmente. Usar esse campo para algo funcional seria recriar, dentro da nossa própria feature, exatamente o padrão que motivou o Princípio III da constituição (uma opção que existe na configuração mas nunca é processada de fato).
- **Alternatives considered**: usar `condition: "tests_passed"` como forma "declarativa" de expressar FR-007a — descartado pelo motivo acima; a lógica precisa estar em código que de fato roda.

## R4. Identificação do card sem duplicar (FR-002/FR-003)

- **Decision**: Toda vez que `sync-card.ps1` roda, ele busca **todos os cards do quadro** (`GET /1/boards/{id}/cards?fields=name,desc,idList`, uma chamada só) e filtra client-side por uma linha reconhecível na descrição: `Feature: <identificador>` (ex.: `Feature: 037-vinculo-cobranca`), sempre a última linha da descrição. Se encontrar exatamente um card com esse marcador, usa-o (para mover/comentar); se não encontrar nenhum, cria um novo (só na fase "backlog"); se encontrar mais de um (não deveria acontecer, mas é defensivo), usa o mais recente e registra um aviso no log.
- **Rationale**: Evita Trello **Custom Fields**, que exigem um Power-Up habilitado no quadro (passo de configuração extra e frágil de depender silenciosamente) — a descrição já é um campo padrão, sempre disponível, e a spec já aceitava "descrição ou campo customizado" (spec.md, Key Entities) como formas equivalentes.
- **Alternatives considered**: Trello Custom Fields — descartado pela dependência de Power-Up; usar a API de busca (`GET /1/search`) em vez de listar todos os cards — descartado por ser uma busca textual mais imprecisa (indexação assíncrona do Trello) para um board pequeno onde listar tudo é barato e determinístico.

## R5. Resolução de quadro e listas por nome, não por ID fixo

- **Decision**: `sync-card.ps1` recebe `TRELLO_BOARD_ID` (variável de ambiente, resolvido uma única vez manualmente durante o setup — documentado no README) e, a cada execução, busca as listas do quadro (`GET /1/boards/{id}/lists`) e casa pelo nome exato ("Backlog", "Design", "A Fazer", "Em andamento", "Revisão de código"). Não há um `TRELLO_LIST_ID_*` por lista.
- **Rationale**: O board ID é estável e barato de configurar uma vez; os nomes das listas podem ser reordenados sem quebrar a integração (a busca é por nome, não por posição). Resolver o board também por nome a cada chamada (em vez de ID) adicionaria uma chamada extra sem necessidade real, já que o board em si não muda de nome com a mesma frequência que a ordem das listas dentro dele poderia mudar.
- **Alternatives considered**: `TRELLO_LIST_ID_BACKLOG`, `TRELLO_LIST_ID_DESIGN`, etc. (uma variável por lista) — descartado por exigir mais passos de setup e quebrar silenciosamente se uma lista for recriada (ID muda, nome não).

## R6. Contrato de erro (FR-011): nunca travar o Spec Kit

- **Decision**: `sync-card.ps1` envolve toda a lógica de rede/Trello em `try/catch`; qualquer exceção é capturada, gravada em `.specify/hooks/trello/sync.log` (gitignored) com timestamp e o erro completo, e o script **sempre termina com `exit 0`**, independentemente do resultado real da sincronização. A skill `trello-sync-card` nunca trata um "erro do Trello" como um erro da skill em si — ela só invoca o script e segue em frente.
- **Rationale**: É a tradução direta de FR-011/SC-003: a sincronização é sempre "melhor esforço"; o único jeito de garantir que ela nunca interrompe o Spec Kit é o script nunca sinalizar falha para quem o invoca, e a skill nunca reagir como se algo tivesse quebrado.
- **Alternatives considered**: propagar o código de saída e deixar a skill decidir se avisa o usuário — descartado por criar uma superfície onde uma falha de rede do Trello poderia, por engano, ser interpretada como um problema do comando `/speckit-*` que disparou o hook; mais seguro nunca deixar essa ambiguidade existir.

## R7. Checagem de completude para "Revisão de código" (FR-007/FR-007a)

- **Decision**: Quando `sync-card.ps1 -Fase revisao-codigo` roda, ele: (1) lê `tasks.md` da feature atual; (2) conta como "pendente e bloqueante" toda linha `- [ ] T###...` que **não** contenha um marcador reconhecível de validação manual; (3) considera **testes automatizados** como "passando" reexecutando `dotnet test "tests/SPI.Application.Tests"` (o único comando de teste automatizado real deste projeto, confirmado em specs anteriores desta sessão — o frontend não tem suíte, só `tsc`/`lint`) e checando o código de saída. Se não houver nenhuma tarefa bloqueante pendente **e** `dotnet test` sair com sucesso, move para "Revisão de código" e comenta o resumo (FR-008); caso contrário, **não move** (permanece onde está — tipicamente "Em andamento") e registra o motivo no log (FR-007a).
- **Marcador de tarefa manual**: como a clarificação já apontou, as `tasks.md` geradas nesta sessão vêm anotando esse tipo de tarefa de forma textual e inconsistente (ex.: "**PENDENTE (parte manual):**", presente nas tarefas finais de specs 034/036/037). Esta feature **padroniza** esse marcador daqui para frente: toda tarefa cujo texto contenha a substring `(parte manual)` (case-insensitive) é tratada como não-bloqueante para esta checagem. Isso não exige reescrever `tasks.md` já existentes — só formaliza, para o script, um padrão que a prática desta sessão já vinha seguindo informalmente.
- **Rationale**: Reaproveita exatamente o comando de teste que todas as specs desta sessão já rodam manualmente (`dotnet test "tests/SPI.Application.Tests"`), sem inventar um mecanismo de "status de build" novo. O marcador textual é a opção mais simples que não exige mudar o formato de `tasks.md` nem o template do Spec Kit.
- **Alternatives considered**: introduzir uma tag estrutural nova (ex.: `[MANUAL]` como um marcador de posição fixa, tipo `[P]`/`[US1]`) — mais "limpo" formalmente, mas exigiria migrar todo o histórico de `tasks.md` já escrito nesta sessão para o novo formato para ficar consistente; a substring já usada organicamente é suficiente e não quebra nada existente.

## R8. Fora do escopo de teste automatizado desta feature (decisão explícita, não uma omissão)

- **Decision**: Não há suíte de teste automatizado (Pester ou equivalente) para `sync-card.ps1` nesta fatia. A validação é: (a) um modo `-DryRun` no script, que executa toda a lógica de busca/decisão (qual card, qual lista, se moveria ou não) sem fazer nenhuma chamada que crie/mova/comente de verdade, permitindo inspecionar o resultado antes de rodar "para valer"; (b) os cenários manuais de `quickstart.md`, contra um quadro de teste real no Trello.
- **Rationale**: Diferente do backend C# (onde este projeto já tem uma suíte xUnit robusta e a regra do usuário exige cobertura explícita para requisitos negativos), este script é uma ferramenta de processo, de baixo risco para o produto em si (nunca toca `src/SPI.*`/dados de aluno), cujo comportamento real depende inteiramente de uma API externa (Trello) — testá-lo de verdade exigiria mockar HTTP em PowerShell, algo sem nenhum precedente nesta base de código, para uma ferramenta que roda poucas vezes por dia. `-DryRun` cobre a parte determinística (lógica de busca/decisão) sem esse custo.
- **Alternatives considered**: Pester com mocks de `Invoke-RestMethod` — descartado por introduzir uma ferramenta de teste nova, sem precedente no projeto, para uma peça de escopo pequeno; ficará como sugestão de fatia futura se o script crescer em complexidade.

## R9. Segredos: variável de ambiente real + `.env` local opcional

- **Decision**: `sync-card.ps1` lê `TRELLO_API_KEY`, `TRELLO_TOKEN`, `TRELLO_BOARD_ID` do processo (`$env:`); se qualquer uma estiver ausente, tenta carregar `.specify/hooks/trello/.env` (formato `CHAVE=valor`, uma por linha) antes de desistir e logar o erro (FR-011). `.gitignore` ganha `.specify/hooks/trello/.env` e `.specify/hooks/trello/*.log`. Um `.env.example` (sem valores reais) documenta as 3 chaves esperadas.
- **Rationale**: Espelha o padrão já em uso no projeto para segredos fora do .NET (`frontend/.env.local`, gitignored, com `.env.local.example` como template) — mesma forma, adaptada ao PowerShell. Variáveis de ambiente reais continuam funcionando (ex.: definidas no perfil do shell do desenvolvedor), sem exigir o arquivo.
- **Alternatives considered**: exigir só variáveis de ambiente reais, sem `.env` — mais simples, mas menos ergonômico para quem prefere não poluir o perfil do shell; o `.env` opcional não compromete a segurança (permanece fora do controle de versão) e segue precedente já validado no projeto.

## R10. Fora de escopo confirmado

- Mover cards para "Fase de teste" ou "Concluído" (FR-009).
- Qualquer leitura do Trello de volta para o Spec Kit (FR-010).
- Criação retroativa de cards para `specs/001` a `specs/039` (FR-014, decidido em `/speckit-clarify`).
- Criação do quadro "Estágio SPI" e das 7 listas em si — assume-se que já existem (Assumptions do spec.md).
