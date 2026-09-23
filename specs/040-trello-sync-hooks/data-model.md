# Data Model: Sincronização Automática Spec Kit → Trello

Não há entidade de domínio do SPI envolvida (FR-013) — o "modelo de dados" aqui é inteiramente externo (Trello) e efêmero (nada é persistido pelo SPI). Este documento descreve a forma dos dados que o script lê/escreve no Trello, e o pequeno estado em memória que ele mantém durante uma execução.

## Conceito: Card de Feature (no Trello, não no SPI)

| Campo (no Trello) | Origem/Regra |
|---|---|
| Nome do card | Título da feature, extraído do `spec.md` (primeira linha `# Feature Specification: <título>`) — definido só na criação (FR-001); nunca reescrito nas movimentações seguintes. |
| Descrição do card | Um resumo do `spec.md` (texto livre, gerado na criação) seguido de uma última linha fixa: `Feature: <identificador>` (ex.: `Feature: 037-vinculo-cobranca`) — esse marcador é o identificador de busca (FR-002/research.md R4), nunca deve ser removido nas atualizações seguintes. |
| Lista atual (`idList`) | Reflete a fase automática mais recente atingida: Backlog → Design → A Fazer → Em andamento → Revisão de código (FR-004 a FR-007). Nunca é escrita para "Fase de teste"/"Concluído" por este mecanismo (FR-009). |
| Comentários | Um novo comentário é adicionado só na transição para "Revisão de código", resumindo o que foi implementado (FR-008) — nunca editado ou removido pelo script. |

## Identificador de feature

- Formato: o nome do diretório da feature (ex.: `037-vinculo-cobranca`), igual ao valor de `feature_directory` em `.specify/feature.json` (sem o prefixo `specs/`).
- Único por feature; é a chave usada para localizar o card (nunca o título, que pode ter variações de texto entre a hora da criação e uma consulta futura).

## Estado em memória de uma execução de `sync-card.ps1`

| Campo | Descrição |
|---|---|
| `Fase` | Parâmetro recebido: `backlog`, `design`, `a-fazer`, `em-andamento` ou `revisao-codigo`. |
| `FeatureId` | Lido de `.specify/feature.json` no momento da execução. |
| `Cards` | Resultado de uma única chamada `GET /1/boards/{id}/cards` (nome, descrição, lista) — usado só para localizar o card da feature atual (research.md R4). |
| `Listas` | Resultado de uma única chamada `GET /1/boards/{id}/lists` — mapa nome → id, usado para resolver o destino de cada fase (research.md R5). |
| `TarefasBloqueantes` (só na fase `revisao-codigo`) | Contagem de linhas `- [ ] T###` em `tasks.md` que não contêm o marcador de validação manual (research.md R7). |
| `TestesPassaram` (só na fase `revisao-codigo`) | Código de saída de `dotnet test "tests/SPI.Application.Tests"`. |

## Efeito por fase

| Fase | Card não existe | Card existe |
|---|---|---|
| `backlog` | Cria na lista "Backlog", com o marcador de identificador (FR-001) | Não faz nada (idempotente, FR-003) |
| `design` | Registra "card não encontrado" no log e não faz nada (FR-011, edge case) | Move para "Design" (FR-004) |
| `a-fazer` | idem | Move para "A Fazer" (FR-005) |
| `em-andamento` | idem | Move para "Em andamento" (FR-006) |
| `revisao-codigo` | idem | Se `TarefasBloqueantes == 0` e `TestesPassaram`: move para "Revisão de código" + comenta (FR-007/FR-008); senão: não move, registra o motivo (FR-007a) |

## Validações (dono único: `sync-card.ps1`)

| Regra | Onde |
|---|---|
| Nunca criar um segundo card para a mesma feature (FR-003) | Busca por marcador antes de qualquer `POST` de criação |
| Nunca mover para "Fase de teste"/"Concluído" (FR-009) | Essas duas listas nunca aparecem como destino possível no código — não é uma checagem condicional, é a ausência estrutural do caminho |
| Nunca ler de volta para o Spec Kit (FR-010) | O script nunca escreve em nenhum arquivo do repositório; só lê `spec.md`/`tasks.md`/`.specify/feature.json` e chama a API do Trello |
| Completude exclui tarefas de validação manual (FR-007) | Filtro por substring `(parte manual)` (case-insensitive) na contagem de `TarefasBloqueantes` |
| Nunca propagar falha (FR-011) | `try/catch` global + `exit 0` sempre (research.md R6) |
