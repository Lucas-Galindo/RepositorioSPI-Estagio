# Quickstart: validar a spec 075

## Pré-requisitos

- **Backup do banco local antes da migração** (`database/backup`); credencial do banco só pelos User Secrets/variáveis de ambiente, nunca impressa.
- API nova na 5099 e Next; **se o usuário estiver com API/Next rodando (7002/3000), não encerrar nem buildar por cima**: usar `--artifacts-path` no `dotnet test/run` e, no Playwright, redirecionar `https://localhost:7002/**` para `http://localhost:5099` (`page.route`). Login só por `SPI_TESTE_SENHA`.

## 1. Banco (local)

| # | Passo | Esperado |
|---|---|---|
| 1 | Contagem de linhas de todas as tabelas e dump das turmas (id, nome, tipo, ativo) e das matérias por turma (aulas ativas) | registrada como linha de base |
| 2 | Aplicar `27_turma_materia.sql` | coluna `materia_id` e chave `fk_turma_materia` criadas |
| 3 | Conferir cada turma | matéria preenchida só onde as aulas ativas têm **uma** matéria; as demais `NULL` |
| 4 | Contagens depois | **iguais** às de antes em todas as tabelas; só `turma.materia_id` mudou |
| 5 | Aplicar o script de novo | nenhuma mudança (idempotente) |

## 2. Automatizado

`dotnet test tests/SPI.Application.Tests` (sem `--no-build`); `cd frontend`: `npx tsc --noEmit`, `npx eslint`.

## 3. API real

| # | Chamada | Esperado |
|---|---|---|
| 1 | `GET /api/turmas` | `materiaId`/`materiaNome` conforme a migração; resto igual ao de antes |
| 2 | `POST /api/turmas` com matéria ativa (dado de teste "TESTE-075") | 201 com o nome da matéria |
| 3 | com matéria inexistente / inativa | 404 / 409 |
| 4 | `PUT` trocando, mantendo a mesma inativa, removendo | conforme FR-002/FR-003 |
| 5 | Limpeza dos dados de teste | banco volta às contagens do passo 1 |

## 4. Chrome (login real, claro, escuro e 360 px, capturas)

| # | Passo | Esperado |
|---|---|---|
| 1 | Nova turma | campo "Matéria (opcional)" com "Sem matéria" e só matérias ativas |
| 2 | Salvar com matéria | aparece na lista e nos detalhes |
| 3 | Editar: trocar, esvaziar; matéria inativada | conforme as histórias 1.3 e 1.4 |
| 4 | Nova aula: escolher turma com matéria | matéria pré-selecionada; escolhida antes: não sobrescreve; trocar e salvar funciona |
| 5 | Turma sem matéria / aula individual | nada pré-selecionado |
| 6 | Editar aula existente | nada muda |
| 7 | 360 px | sem rolagem horizontal da página |
| 8 | `git status` | só os arquivos previstos; sem dependência nova |
